using System;
using System.Numerics;
using UnityEngine;
using System.Runtime.InteropServices;
using CharacterTest;
using kmty.Util;
using Vector3 = UnityEngine.Vector3;

namespace NearestNeighbor {

    public class GridOptimizer3D<T> : GridOptimizerBase where T : struct {
        public struct Uint2
        {
            public uint x;
            public uint y;
        }

        Vector3 gridDim;

        public GridOptimizer3D(int numObjects, Vector3 range, Vector3 dimension) : base(numObjects) {
            gridDim = dimension;
            numGrid = (int)(dimension.x * dimension.y * dimension.z);
            gridH = range.x / gridDim.x;
            GridSortCS = (ComputeShader)Resources.Load("GridSort3D");
            InitializeBuffer();
            Debug.Log("=== Instantiated Grid Sort === \nRange:" + range + ", NumGrid:" + numGrid + ", GridDim:" + gridDim + ", GridH:" + gridH);
        }

        protected override void InitializeBuffer() {
            gridBuffer = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(Uint2)));
            gridPingPongBuffer = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(Uint2)));
            gridIndicesBuffer = new ComputeBuffer(numGrid, Marshal.SizeOf(typeof(Uint2)));
            sortedObjectsBufferOutput = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(T)));
        }

        protected override void SetCSVariables() {
            GridSortCS.SetVector("_GridDim", gridDim);
            GridSortCS.SetFloat("_GridH", gridH);
        }
    }
    

    public abstract class GridOptimizerBase {

        BitonicSort bitonicSort;
        protected ComputeBuffer gridBuffer;
        protected ComputeBuffer gridPingPongBuffer;
        protected ComputeBuffer gridIndicesBuffer;
        protected ComputeBuffer sortedObjectsBufferOutput;
        protected int numObjects;
        protected ComputeShader GridSortCS;
        protected static readonly int SIMULATION_BLOCK_SIZE_FOR_GRID = 32;
        protected int threadGroupSize;
        protected int numGrid;
        protected float gridH;

        public GridOptimizerBase(int numObjects) {
            this.numObjects = numObjects;
            this.threadGroupSize = numObjects / SIMULATION_BLOCK_SIZE_FOR_GRID;
            bitonicSort = new BitonicSort(numObjects);
        }

        public Vector3 CalculateCell(Vector3 pos)
        {
            return pos / gridH;
        }

        public int GetGridKey(Vector3Int xyz)
        {
            return xyz.x + xyz.y * 16 + xyz.z * 16 * 16;
        }

        public float GetGridH() => gridH;
        public ComputeBuffer GetGridIndicesBuffer() => gridIndicesBuffer;

        public void Release() {
            ComputeShaderUtil.Destroy(gridBuffer);
            ComputeShaderUtil.Destroy(gridIndicesBuffer);
            ComputeShaderUtil.Destroy(gridPingPongBuffer);
            ComputeShaderUtil.Destroy(sortedObjectsBufferOutput);
        }


        public void GridSort(ref ComputeBuffer objectsBufferInput) {
            GridSortCS.SetInt("_NumParticles", numObjects);
            SetCSVariables();
            int kernel = 0;

            #region GridOptimization
            // Build Grid
            kernel = GridSortCS.FindKernel("BuildGridCS");
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferRead", objectsBufferInput);
            GridSortCS.SetBuffer(kernel, "_GridBufferWrite", gridBuffer);
            GridSortCS.Dispatch(kernel, threadGroupSize, 1, 1);
            
            // Sort Grid
            bitonicSort.Sort(ref gridBuffer, ref gridPingPongBuffer);
            
            // Debug Test to see if bitonic sort works
            //var gridBufferTemp = new ParticleFlock.Uint2[numObjects];
            //gridBuffer.GetData(gridBufferTemp);
            //int indice2Count = 0;
            //for (int i = 0; i < 2000; i++)
            //{
            //    if (gridBufferTemp[i].x == 2)
            //    {
            //        indice2Count++;
            //    } 
            //}
            //Debug.Log(indice2Count);
            
            
            // Build Grid Indices
            kernel = GridSortCS.FindKernel("ClearGridIndicesCS");
            GridSortCS.SetBuffer(kernel, "_GridIndicesBufferWrite", gridIndicesBuffer);
            GridSortCS.Dispatch(kernel, (int)(numGrid / SIMULATION_BLOCK_SIZE_FOR_GRID), 1, 1);

            kernel = GridSortCS.FindKernel("BuildGridIndicesCS");
            GridSortCS.SetBuffer(kernel, "_GridBufferRead", gridBuffer);
            GridSortCS.SetBuffer(kernel, "_GridIndicesBufferWrite", gridIndicesBuffer);
            GridSortCS.Dispatch(kernel, threadGroupSize, 1, 1);
            
            //Debug Test to see if GridIndeces works
            //var gridIndicesBufferTest = new ParticleFlock.Uint2[numGrid];
            //gridIndicesBuffer.GetData(gridIndicesBufferTest);
            //Debug.Log(gridIndicesBufferTest[2].x);
            //Debug.Log(gridIndicesBufferTest[2].y);
            //Debug.Log("-----------------------------");
            
            
            // Rearrange
            kernel = GridSortCS.FindKernel("RearrangeParticlesCS");
            GridSortCS.SetBuffer(kernel, "_GridBufferRead", gridBuffer);
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferRead", objectsBufferInput);
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferWrite", sortedObjectsBufferOutput);
            GridSortCS.Dispatch(kernel, threadGroupSize, 1, 1);
            #endregion GridOptimization
            
            //Debug Test to see if rearrange particles work
            //var gridIndicesBufferTest = new ParticleFlock.Uint2[numGrid];
            //gridIndicesBuffer.GetData(gridIndicesBufferTest);
            //uint totalFor1 = gridIndicesBufferTest[1].y - gridIndicesBufferTest[1].x;
            //var gridBufferTemp = new ParticleFlock.Uint2[numObjects];
            //gridBuffer.GetData(gridBufferTemp);
            
            
            
            
            //int total1 = 0;
            //for (uint i = 0; i < 500; i++)
            //{
            //    Vector3 cellIndex = CalculateCell(sortedParticleBuffer[i].position);
            //    Vector3Int cellIndexCasted = new Vector3Int((int)Mathf.Floor(cellIndex.x), (int)Mathf.Floor(cellIndex.y), (int)Mathf.Floor(cellIndex.z));
            //    int gridKey = GetGridKey(cellIndexCasted);
            //    if (gridKey == 1)
            //    {
            //        total1++;
            //    }
            //}
            //Debug.Log(totalFor1);
            //Debug.Log(total1);
            //Debug.Log("---------------");
            
            
            kernel = GridSortCS.FindKernel("CopyBuffer");
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferRead", sortedObjectsBufferOutput);
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferWrite", objectsBufferInput);
            GridSortCS.Dispatch(kernel, threadGroupSize, 1, 1);
            
            //Debug Test to see if copy particles works
            
            //var gridIndicesBufferTest = new ParticleFlock.Uint2[numGrid];
            //gridIndicesBuffer.GetData(gridIndicesBufferTest);
            //for (int i = 1000; i < 1100; i++)
            //{
            //    Debug.Log(gridIndicesBufferTest[i].x);
            //    Debug.Log(gridIndicesBufferTest[i].y);
            //}
            //Debug.Log("---------");
            //uint totalFor1 = gridIndicesBufferTest[2].y - gridIndicesBufferTest[2].x;
            //uint start100 = gridIndicesBufferTest[7].y;
            //var gridBufferTemp = new ParticleFlock.Uint2[numObjects];
            //gridBuffer.GetData(gridBufferTemp);
            //var sortedParticleBuffer = new ParticleFlock.ParticleBoid[numObjects];
            //objectsBufferInput.GetData(sortedParticleBuffer);
            //int total1 = 0;
            //for (uint i = 0; i < 300; i++)
            //{
            //    Vector3 cellIndex = CalculateCell(sortedParticleBuffer[i].position);
            //    Vector3Int cellIndexCasted = new Vector3Int((int)Mathf.Floor(cellIndex.x), (int)Mathf.Floor(cellIndex.y), (int)Mathf.Floor(cellIndex.z));
            //    int gridKey = GetGridKey(cellIndexCasted);
            //    if (gridKey == 2)
            //    {
            //        total1++;
            //    }
            //}
            //Debug.Log(totalFor1);
            //Debug.Log(total1);
            //var sortedParticleBuffer = new ParticleFlock.ParticleBoid[numObjects];
            //sortedObjectsBufferOutput.GetData(sortedParticleBuffer);
            //    Vector3 cellIndex = CalculateCell(sortedParticleBuffer[i].position);
            //    Vector3Int cellIndexCasted = new Vector3Int((int)Mathf.Floor(cellIndex.x), (int)Mathf.Floor(cellIndex.y), (int)Mathf.Floor(cellIndex.z));
            //    int gridKey = GetGridKey(cellIndexCasted);
            //Debug.Log(GetGridKey())
        }
        protected abstract void InitializeBuffer();
        protected abstract void SetCSVariables();
    }
}