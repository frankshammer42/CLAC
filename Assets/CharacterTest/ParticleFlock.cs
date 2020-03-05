using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using kmty.Util;
using NearestNeighbor;
using UnityEngine;
using BestHTTP;
using Random = System.Random;
//TODO: Change the logic of using affectors

namespace CharacterTest
{
    public class ParticleFlock : MonoBehaviour
    {
        public struct ParticleBoid
        {
            public Vector3 position;
            public Vector3 direction;
            public float noise_offset;
            public float speed;
        }
        
        public struct BoidAffector
        {
            public Vector3 position;
            public float force;
        }
        
        
        public struct Int3
        {
            public int x;
            public int y;
            public int z;
        }

        public struct Uint2
        {
            public uint x;
            public uint y;
        }
        
        const int NUM_THREAD_X = 8; // スレッドグループのX成分のスレッド数
        const int NUM_THREAD_Y = 1; // スレッドグループのY成分のスレッド数
        const int NUM_THREAD_Z = 1; // スレッドグループのZ成分のスレッド数

        public ComputeShader _ComputeFlock;
        public Shader BoidParticleShader;
        public Texture2D ParticleTex;          // パーティクルのテクスチャ
        public float ParticleSize;
        // Boid Variable Declaration
        public int BoidsCount;
        public int StepBoidCheckNeighbours = 1;
        public float SpawnRadius;
        public float RotationSpeed = 1f;
        public float BoidSpeed = 6f;
        public float NeighbourDistance = 2f;
        public float BoidSpeedVariation = 0.9f;
        public GameObject Target;
        //Affector Related Declaration
        private bool _startChangeAffectorDistance;
        public float AffectorDistanceChangeStep;
        private int AffectorCounts;
        public float AffectorForce;
        public float AffectorDistance;
        public int useAffector = 1;
        public string[] paths;
        public int currentPath;
        //Web Related Variables 
        private int currentID = 0;
        //Compute Shader Varaibels
        public Camera RenderCam;
        private ComputeBuffer ParticleBoidBufferRead;
        private ComputeBuffer ParticleBoidBufferWrite;
        private ComputeBuffer ParticleGridPositionCheck;
        private ComputeBuffer BoidAffectorBuffer;  
        private Material BoidParticleRenderMateiral;
        private int _numberOfGrids;
        //Sorting related variables
        static readonly int SIM_BLOCK_SIZE = 256;
        private static readonly Vector3 range = Vector3.one * 200; 
        static readonly float MAX_TIMESTEP = 0.001f;
        public Vector3 gridDim = new Vector3(16, 16, 16);
        GridOptimizer3D<ParticleBoid> sorter;
        public bool changeRotationSpeed = true;
        //Debug Related Varaibels
        public bool DrawingAffectors = true;
        //Plane Projection Variable
        private Vector3 _planeNormal;
        private Vector3 _planePoint;
        //Camera Movement Variable
        public GameObject Camera;
        public float CameraDistanceScaler;
        
        ParticleBoid CreateBoidData()
        {
            ParticleBoid boidData = new ParticleBoid();
            Vector3 pos = Target.transform.position + UnityEngine.Random.insideUnitSphere * SpawnRadius;
            //Vector3 pos = new Vector3(UnityEngine.Random.Range(0f, 100f), UnityEngine.Random.Range(0f, 100f),
             //   UnityEngine.Random.Range(0f, 100f));
            Quaternion rot = Quaternion.Slerp(transform.rotation, UnityEngine.Random.rotation, 0.3f);
            boidData.position = pos;
            boidData.direction = rot.eulerAngles;
            boidData.noise_offset = UnityEngine.Random.value * 10.0f;
            return boidData;
        }

        IEnumerator ChangeRotationSpeed()
        {
            yield return new WaitForSeconds(3);
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (changeRotationSpeed)
                {
                    RotationSpeed = UnityEngine.Random.Range(0f, 1f);
                }
            }
        }

        //IEnumerator GetData()
        //{
        //    yield return new WaitForSeconds(20f);
        //    while (true)
        //    {
        //        yield return new WaitForSeconds(20f);
        //        RetrieveData(UnityEngine.Random.Range(0, 4000));
        //        currentID++;
        //    }
        //}
        public void RetrieveData(string name)
        {
            string requestPath = "https://poetrycloud.herokuapp.com/api/characters/name/";
            requestPath += name;
            HTTPRequest request = new HTTPRequest(new Uri(requestPath), OnRequestFinished);
            request.Send();
        }

        private void OnRequestFinished(HTTPRequest request, HTTPResponse response)
        {
            useAffector = 1;
            RAWInkData character = JsonUtility.FromJson<RAWInkData>(response.DataAsText);
            GenerateCharacterAffectorFromData(character.data);
        }

        private void StartMoveCamera(Vector3 planeNormal)
        {
            //if (planeNormal.z < 0)
            //{
            //    planeNormal *= -1;
            //}
            Vector3 center = new Vector3(50, 50,50);
            Vector3 targetPosition = center + CameraDistanceScaler*planeNormal;
            Camera.GetComponent<CameraMovements>().startMove = true;
            Camera.GetComponent<CameraMovements>().startRotate = true;
            Camera.GetComponent<CameraMovements>().targetPosition = targetPosition;
            Camera.GetComponent<CameraMovements>().focusPosition = center;
        }
        

        private void GenerateCharacterAffectorFromData(InkData character)
        {
            Debug.Log(character.name);
            useAffector = 1;
            if (BoidAffectorBuffer != null)
            {
                BoidAffectorBuffer.Release();
            }
            AffectorCounts = character.inks.Length / 3;
            var affectorData = new BoidAffector[AffectorCounts];
            //TODO: Put it in a function
            float randomAngleX = UnityEngine.Random.Range(-50, 50);
            float randomAngleY = UnityEngine.Random.Range(-50, 50);
            Debug.Log(randomAngleX);
            Debug.Log(randomAngleY);
            Debug.Log("-------------------------------");
            for (int i = 0; i < AffectorCounts; i++)
            {
                int xIndex = i * 3;
                int yIndex = i * 3 + 1;
                int zIndex = i * 3 + 2;
                float xPos = character.inks[xIndex]; 
                float yPos = character.inks[yIndex]; 
                //float zPos = character.inks[zIndex]; 
                Vector3 position = new Vector3(xPos, yPos, 50);
                //position = RotatePointAroundPivot(position, Vector3.zero, Quaternion.Euler(new Vector3(0, 135, 0)));
                Vector3 rotatedPosition = RotatePointAroundPivot(position, new Vector3(50,50,50), new Vector3(randomAngleX, randomAngleY, -90));
                var affector = new BoidAffector();
                affector.position = rotatedPosition;
                affector.force = 0;
                affectorData[i] = affector;
            }
            if (DrawingAffectors) {
                foreach(var affector in affectorData) {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = new Vector3(1,1,1);
                    go.transform.position = affector.position;
                }
            }
            //AffectorForce = 0.2f; 
            
            _planeNormal = TestPlaneProjection.CalculatePlaneNormals(affectorData);
            Debug.Log(_planeNormal);
            Debug.Log("IIIIIIIIIIIIIIIIIIII");
            _planePoint = affectorData[89].position;
            StartMoveCamera(_planeNormal);
            BoidAffectorBuffer = new ComputeBuffer(AffectorCounts, Marshal.SizeOf(typeof(BoidAffector)));
            BoidAffectorBuffer.SetData(affectorData);
            affectorData = null;
            _startChangeAffectorDistance = true;
            AffectorDistance = -2f;
        }

        private void GenerateCharacterAffectorFromPath(string path)
        {
            //path = "Assets/CharacterTest/ren.json";
            if (BoidAffectorBuffer != null)
            {
                BoidAffectorBuffer.Release();
            }
            string jsonString = File.ReadAllText(path);
            InkData character = JsonUtility.FromJson<InkData>(jsonString);
            AffectorCounts = character.inks.Length / 3;
            var affectorData = new BoidAffector[AffectorCounts];
            //TODO: Put it in a function
            for (int i = 0; i < AffectorCounts; i++)
            {
                int xIndex = i * 3;
                int yIndex = i * 3 + 1;
                int zIndex = i * 3 + 2;
                float xPos = character.inks[xIndex]; 
                float yPos = character.inks[yIndex]; 
                //float zPos = character.inks[zIndex]; 
                Vector3 position = new Vector3(xPos, yPos, 50);
                Vector3 rotatedPosition = RotatePointAroundPivot(position, new Vector3(50,50,50), new Vector3(25, 34, -90));
                var affector = new BoidAffector();
                affector.position = rotatedPosition;
                affector.force = 0;
                affectorData[i] = affector;
            }
            
            if (DrawingAffectors) {
                foreach(var affector in affectorData) {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = new Vector3(1,1,1);
                    go.transform.position = affector.position;
                }
            }

            _planeNormal = TestPlaneProjection.CalculatePlaneNormals(affectorData);
            _planePoint = affectorData[89].position;
            Debug.Log(_planeNormal);
            Debug.Log("-------------------------------");
            //StartMoveCamera(_planeNormal);
            BoidAffectorBuffer = new ComputeBuffer(AffectorCounts, Marshal.SizeOf(typeof(BoidAffector)));
            BoidAffectorBuffer.SetData(affectorData);
            affectorData = null;
        }



        void Start()
        {
            //Create Compute Shader Data
            
            ParticleGridPositionCheck = new ComputeBuffer(BoidsCount, Marshal.SizeOf(typeof(Int3)));
            ParticleBoidBufferRead = new ComputeBuffer(BoidsCount, Marshal.SizeOf(typeof(ParticleBoid)));
            ParticleBoidBufferWrite = new ComputeBuffer(BoidsCount, Marshal.SizeOf(typeof(ParticleBoid)));
            
            GenerateCharacterAffectorFromPath("Assets/CharacterTest/ren.json");
            
            
            //foreach (var affector in affectorData)
            //{
            //    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //    go.transform.localScale = new Vector3(1,1, 1);
            //    go.transform.position = affector.position;
            //}
            
            
            
            var pData = new ParticleBoid[BoidsCount];
            for (int i = 0; i < pData.Length; i++)
            {
                pData[i] = CreateBoidData();
            }
            ParticleBoidBufferRead.SetData(pData);
            pData = null;
            BoidParticleRenderMateiral = new Material(BoidParticleShader);
            BoidParticleRenderMateiral.hideFlags = HideFlags.HideAndDontSave;
            _numberOfGrids = (int) (gridDim.x * gridDim.y * gridDim.z);
            sorter = new GridOptimizer3D<ParticleBoid>(BoidsCount, range, gridDim);
            //StartCoroutine(GetData());
            StartCoroutine(ChangeRotationSpeed());
        }

        void SetComputeData()
        {
            int kernelId = _ComputeFlock.FindKernel("CSMain");
            int numThreadGroup = BoidsCount / NUM_THREAD_X;
            _ComputeFlock.SetFloat("_GridH", sorter.GetGridH());
            _ComputeFlock.SetVector("_GridDim", gridDim);
            _ComputeFlock.SetFloat("DeltaTime", Time.deltaTime);
            _ComputeFlock.SetFloat("RotationSpeed", RotationSpeed);
            _ComputeFlock.SetFloat("BoidSpeed", BoidSpeed);
            _ComputeFlock.SetFloat("BoidSpeedVariation", BoidSpeedVariation);
            _ComputeFlock.SetVector("FlockPosition", Target.transform.position);
            _ComputeFlock.SetVector("PlaneNormal", _planeNormal);
            _ComputeFlock.SetVector("PlanePoint", _planePoint);
            _ComputeFlock.SetVector("SpherePos", Target.transform.position);
            _ComputeFlock.SetFloat("NeighbourDistance", NeighbourDistance);
            _ComputeFlock.SetInt("BoidsCount", BoidsCount);
            _ComputeFlock.SetInt("AffectorCount", AffectorCounts);
            _ComputeFlock.SetFloat("AffectorForce", AffectorForce);
            _ComputeFlock.SetFloat("AffectorDistance", AffectorDistance);
            _ComputeFlock.SetInt("useAffector", useAffector);
            _ComputeFlock.SetBuffer(kernelId, "_GridIndicesBufferRead", sorter.GetGridIndicesBuffer());
            _ComputeFlock.SetBuffer(kernelId, "boidBuffer_read", ParticleBoidBufferRead);
            _ComputeFlock.SetBuffer(kernelId, "boidBuffer_write", ParticleBoidBufferWrite);
            _ComputeFlock.SetBuffer(kernelId, "position_debug", ParticleGridPositionCheck);
            _ComputeFlock.SetBuffer(kernelId, "affector_write", BoidAffectorBuffer);
            _ComputeFlock.Dispatch(kernelId, numThreadGroup, 1, 1);
            //ComputeShaderUtil.Swap(ref ParticleBoidBufferWrite, ref ParticleBoidBufferRead);
            ComputeShaderUtil.Swap(ref ParticleBoidBufferRead, ref ParticleBoidBufferWrite);
        }

        void SetMaterialData()
        {
            Material m = BoidParticleRenderMateiral;
            var inverseViewMatrix = RenderCam.worldToCameraMatrix.inverse;
            m.SetPass(0);
            // 各パラメータをセット
            m.SetMatrix("_InvViewMatrix", inverseViewMatrix);
            m.SetTexture("_MainTex", ParticleTex);
            m.SetFloat("_ParticleSize", ParticleSize);
            // コンピュートバッファをセット
            m.SetBuffer("_ParticleBuffer", ParticleBoidBufferRead);
            // パーティクルをレンダリング
            Graphics.DrawProceduralNow(MeshTopology.Points, BoidsCount);
        }
        
        public Vector3 CalculateCell(Vector3 pos)
        {
            float gridHeight = 200 / 16;
            return pos / gridHeight;
        }

        public int GetGridKey(Vector3Int xyz)
        {
            return xyz.x + xyz.y * 16 + xyz.z * 16 * 16;
        }

        void OnRenderObject()
        {
            //Debug.Log(ParticleBufferTemp.Length);
            //Debug.Log(ParticleBufferTemp[20].position);
            //Debug.Log(ParticleBufferTemp[2100].position);
            //Debug.Log(ParticleBufferTemp[300].position);
            //SetComputeData();
            //if (currentPath != 0)
            //{
            //    string path = paths[currentPath];
            //    GenerateCharacterAffectorFromPath(path);
            //}
            if (_startChangeAffectorDistance)
            {
                AffectorDistance += AffectorDistanceChangeStep;
                if (AffectorDistance >= 10)
                {
                    _startChangeAffectorDistance = false;
                }
            }


            //AffectorForce += 0.08f;
            //if (AffectorForce > 4)
            //{
            //    AffectorForce = 4.0f;
            //}
            sorter.GridSort(ref ParticleBoidBufferRead);
            SetComputeData();
            //ComputeBuffer gridInices = sorter.GetGridIndicesBuffer();
            //var GridIndicesBufferDebug = new Uint2[16*16*16];
            //gridInices.GetData(GridIndicesBufferDebug);
            //Debug.Log(GridIndicesBufferDebug[0].x);
            //Debug.Log(GridIndicesBufferDebug[0].y);
            //Debug.Log("---------------------");
            
            //var ParticleBufferTemp = new ParticleBoid[BoidsCount];
            //ParticleBoidBufferRead.GetData(ParticleBufferTemp);
            //Debug.Log(ParticleBufferTemp[20].position);
            //Vector3 particlePosCheck = ParticleBufferTemp[20].position;
            //particlePosCheck.z = 0;

            //var AffectorBufferTemp = new BoidAffector[AffectorCounts];
            //BoidAffectorBuffer.GetData(AffectorBufferTemp);
            //Debug.Log(AffectorBufferTemp[20].position);
            //Vector3 affectorPosCheck = AffectorBufferTemp[20].position;
            //affectorPosCheck.z = 0;
            //Debug.Log((affectorPosCheck - particlePosCheck).magnitude);

            //var ParticleGridPositionBuffer = new Int3[BoidsCount];
            //ParticleGridPositionCheck.GetData(ParticleGridPositionBuffer);
            //Debug.Log(ParticleGridPositionBuffer[0].x);
            //Debug.Log(ParticleGridPositionBuffer[0].y);
            //Debug.Log(ParticleGridPositionBuffer[0].z);
            //Debug.Log("----------------------------------");
            
            SetMaterialData();
            GL.Flush();
        }

        private void OnDestroy()
        {
            Release();
            sorter.Release();
        }

        public void Release()
        {
            ComputeShaderUtil.Destroy(ParticleBoidBufferRead);
            ComputeShaderUtil.Destroy(ParticleBoidBufferWrite);
            ComputeShaderUtil.Destroy(ParticleGridPositionCheck);
        }
        
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
             return RotatePointAroundPivot(point, pivot, Quaternion.Euler(angles));
         }
     
         public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation) {
             return rotation * (point - pivot) + pivot;
         }
        
    }
}