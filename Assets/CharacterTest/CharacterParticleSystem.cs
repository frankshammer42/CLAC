using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

namespace CharacterTest 
{
    // パーティクルデータの構造体
    public struct ParticleData
    {
        public Vector3 Velocity; // 速度
        public Vector3 Position; // 位置
    };

    public struct ConnectionData
    {
        public Vector3 Start;
        public Vector3 End;
        public int Connect;
    }


    [System.Serializable]
    public class InkData
    {
        public string name;
        public int id;
        public int[] inks;
    }
    
    
    public class CharacterParticleSystem : MonoBehaviour
    {
        const int NUM_PARTICLES = 64; // 生成するパーティクルの数
        //TODO: How do we actually decide which thread to use and how many threads to work on?
        const int NUM_THREAD_X = 8; // スレッドグループのX成分のスレッド数
        const int NUM_THREAD_Y = 1; // スレッドグループのY成分のスレッド数
        const int NUM_THREAD_Z = 1; // スレッドグループのZ成分のスレッド数
        private int numberOfParticles = 0; 

        public Color particleColor;

        public ComputeShader SimpleParticleComputeShader; // パーティクルの動きを計算するコンピュートシェーダ
        public Shader SimpleParticleRenderShader;  // パーティクルをレンダリングするシェーダ
        public Shader SimpleLineRenderShader;

        public Vector3 Gravity = new Vector3(0.0f, -1.0f, 0.0f); // 重力
        public Vector3 AreaSize = Vector3.one * 10.0f;            // パーティクルが存在するエリアのサイズ

        public Texture2D ParticleTex;          // パーティクルのテクスチャ
        public float ParticleSize = 0.05f; // パーティクルのサイズ
        public float LineThreshold = 2.0f;

        public Camera RenderCam; // パーティクルをレンダリングするカメラ（ビルボードのための逆ビュー行列計算に使用）

        ComputeBuffer particleBuffer;     // パーティクルのデータを格納するコンピュートバッファ 
        ComputeBuffer connectionBuffer;
        Material particleRenderMat;  // パーティクルをレンダリングするマテリアル
        Material lineRenderMat;
        

        void Start()
        {
            //Read Position data from json file 
            //TODO: Maybe Adding Network utility  
            string path = "Assets/CharacterTest/ren.json";
            string jsonString = File.ReadAllText(path);
            InkData character = JsonUtility.FromJson<InkData>(jsonString);
            numberOfParticles = character.inks.Length/3;
            
            // パーティクルのコンピュートバッファを作成
            //particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(ParticleData)));
            particleBuffer = new ComputeBuffer(numberOfParticles, Marshal.SizeOf(typeof(ParticleData)));
            connectionBuffer = new ComputeBuffer(numberOfParticles, Marshal.SizeOf(typeof(ConnectionData)));
            
            // パーティクルの初期値を設定
            //var pData = new ParticleData[NUM_PARTICLES];
            var pData = new ParticleData[numberOfParticles];
            var cData = new ConnectionData[numberOfParticles];
            
            for (int i = 0; i < pData.Length; i++)
            {
                //pData[i].Velocity = Random.insideUnitSphere;
                //pData[i].Position = Random.insideUnitSphere;
                float vIndicator = Random.Range(0.0f, 1.0f);
                float zSpeed = Random.Range(0.0f, 0.2f);
                if (vIndicator > 0.5)
                {
                    zSpeed = -1 * zSpeed;
                }
                pData[i].Velocity = new Vector3(0,-0.00001f, zSpeed);
                int xIndex = i * 3;
                int yIndex = i * 3 + 1;
                int zIndex = i * 3 + 2;
//                float xPos = character.inks[xIndex]/(float)100.0; 
//                float yPos = character.inks[yIndex]/(float)100.0; 
//                float zPos = character.inks[zIndex]/(float)100.0; 
                float xPos = UnityEngine.Random.Range(0f, 100f);
                float yPos = UnityEngine.Random.Range(0f, 100f);
                float zPos = UnityEngine.Random.Range(0f, 100f);
                Vector3 position = new Vector3(yPos*2, -xPos*2, 0);
                pData[i].Position = position;
                // Set Up Connection Data
                cData[i].Start = new Vector3(0, 0, 0);
                cData[i].End = new Vector3(0, 0, 0);
                cData[i].Connect = 0;
            }
            // コンピュートバッファに初期値データをセット
            particleBuffer.SetData(pData);
            connectionBuffer.SetData(cData);
            
            pData = null;
            cData = null;
            // パーティクルをレンダリングするマテリアルを作成
            particleRenderMat = new Material(SimpleParticleRenderShader);
            particleRenderMat.hideFlags = HideFlags.HideAndDontSave;
            
            lineRenderMat = new Material(SimpleLineRenderShader);
            lineRenderMat.hideFlags = HideFlags.HideAndDontSave;

        }

        void OnRenderObject()
        {
            ComputeShader cs = SimpleParticleComputeShader;
            // スレッドグループ数を計算
            int numThreadGroup = numberOfParticles / NUM_THREAD_X;
            // カーネルIDを取得
            int kernelId = cs.FindKernel("CSMain");
            // 各パラメータをセット
            cs.SetFloat("_TimeStep", Time.deltaTime);
            cs.SetVector("_Gravity", Gravity);
            cs.SetFloats("_AreaSize", new float[3] { AreaSize.x, AreaSize.y, AreaSize.z });
            cs.SetInt("_ParticleCount", particleBuffer.count);
            cs.SetFloat("_Threshold", LineThreshold);
            // コンピュートバッファをセット
            cs.SetBuffer(kernelId, "_ParticleBuffer", particleBuffer);
            // Set Connection Buffer
            cs.SetBuffer(kernelId, "_ConnectionBuffer", connectionBuffer);
            
            // コンピュートシェーダを実行
            cs.Dispatch(kernelId, numThreadGroup, 1, 1);

            // 逆ビュー行列を計算
            var inverseViewMatrix = RenderCam.worldToCameraMatrix.inverse;

            Material m = particleRenderMat;
            m.SetPass(0); // レンダリングのためのシェーダパスをセット
            // 各パラメータをセット
            m.SetMatrix("_InvViewMatrix", inverseViewMatrix);
            m.SetTexture("_MainTex", ParticleTex);
            m.SetFloat("_ParticleSize", ParticleSize);
            // コンピュートバッファをセット
            m.SetBuffer("_ParticleBuffer", particleBuffer);
            m.SetColor("_ParticleColor", particleColor);
            // パーティクルをレンダリング
            Graphics.DrawProceduralNow(MeshTopology.Points, numberOfParticles);

            Material l = lineRenderMat;
            l.SetPass(0);
            l.SetMatrix("_InvViewMatrix", inverseViewMatrix);
            l.SetBuffer("_ConnectionBuffer", connectionBuffer);
            Graphics.DrawProceduralNow(MeshTopology.Lines, numberOfParticles);
        }
        
        static Material lineMaterial;

        void OnDestroy()
        {
            if (particleBuffer != null)
            {
                // バッファをリリース（忘れずに！）
                particleBuffer.Release();
            }

            if (connectionBuffer != null)
            {
                connectionBuffer.Release();
            }

            if (particleRenderMat != null)
            {
                // レンダリングのためのマテリアルを削除
                DestroyImmediate(particleRenderMat);
            }
            
        }
    }
}