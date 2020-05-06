using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace SG3D {

class TerrainMeshRendererJobManager : JobManager<TerrainChunk.UpdateMeshJob, TerrainMeshRendererJobManager.JobParam>
{
    public struct JobParam {
        public JobParam(int x, int z) 
        {
            chunkX = x;
            chunkZ = z;
        }
        
        public int chunkX;
        public int chunkZ;
    };

    NativeArray<Vector3>[] vertexBuffers;
    NativeArray<int>[] indexBuffers;
    NativeArray<Vector2>[] uv0Buffers;
    NativeArray<Color>[] colorBuffers;
    Terrain terrain;
    TerrainRenderer renderer;
    TerrainBakeMeshJobManager bakeMeshJobManager;

    public void Initialize(int maxJobs, Terrain terrain, TerrainRenderer renderer, TerrainBakeMeshJobManager bakeMeshJobManager)
    {
        this.terrain = terrain;
        this.renderer = renderer;
        this.bakeMeshJobManager = bakeMeshJobManager;

        vertexBuffers = new NativeArray<Vector3>[maxJobs];
        indexBuffers = new NativeArray<int>[maxJobs];
        uv0Buffers = new NativeArray<Vector2>[maxJobs];
        colorBuffers = new NativeArray<Color>[maxJobs];

        for (int i = 0; i < maxJobs; i++) {
            vertexBuffers[i] = new NativeArray<Vector3>(renderer.chunkSize * terrain.height * renderer.chunkSize * 4, Allocator.Persistent);
            indexBuffers[i] = new NativeArray<int>(renderer.chunkSize * terrain.height * renderer.chunkSize * 6, Allocator.Persistent);
            uv0Buffers[i] = new NativeArray<Vector2>(renderer.chunkSize * terrain.height * renderer.chunkSize * 4, Allocator.Persistent);
            colorBuffers[i] = new NativeArray<Color>(renderer.chunkSize * terrain.height * renderer.chunkSize * 4, Allocator.Persistent);
        }

        base.Initialize(maxJobs);
    }

    public void Cleanup()
    {
        for (int i = 0; i < vertexBuffers.Length; i++) {
            vertexBuffers[i].Dispose();
            indexBuffers[i].Dispose();
            uv0Buffers[i].Dispose();
            colorBuffers[i].Dispose();
        }
    }

    public override void OnReady(int i, ref TerrainChunk.UpdateMeshJob job, TerrainMeshRendererJobManager.JobParam startParams)
    {
        int x = startParams.chunkX;
        int z = startParams.chunkZ;

        job.worldSize = terrain.GetWorldSize();
        job.tileSize = renderer.tileSize;
        job.chunkWorldPosition = new Vector3Int(x * renderer.chunkSize, 0 * renderer.chunkSize, z * renderer.chunkSize);
        job.chunkSize = renderer.chunkSize;
        job.textureSize = renderer.chunkTextureSize;
        job.vertices = vertexBuffers[i];
        job.triangles = indexBuffers[i];
        job.uv0 = uv0Buffers[i];
        job.colors = colorBuffers[i];
        job.materials = renderer.materialInfo;
        job.chunkX = x;
        job.chunkZ = z;
        job.terrain = terrain.GetTerrainInfo();
        job.counts = new NativeArray<int>(1, Allocator.TempJob);
    }

    public override void OnComplete(int i, ref TerrainChunk.UpdateMeshJob job, TerrainMeshRendererJobManager.JobParam startParams)
    {
        TerrainChunk chunk = renderer.GetChunk(job.chunkX, job.chunkZ);

        TerrainBakeMeshJobManager.JobParams bakeParams;
        bakeParams.mesh = chunk.mesh;
        bakeParams.chunk = chunk;

        chunk.UpdateMeshData(job.counts[0], job.vertices, job.triangles, job.uv0, job.colors);
        bakeMeshJobManager.ScheduleJob(bakeParams);
        job.counts.Dispose();
    }
}

}