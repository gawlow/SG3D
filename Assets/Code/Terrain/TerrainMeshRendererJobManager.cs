using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace SG3D {

class TerrainMeshRendererJobManager : JobManager<TerrainChunk.UpdateMeshJob, Tuple<int, int>>
{
    NativeArray<Vector3>[] vertexBuffers;
    NativeArray<int>[] indexBuffers;
    NativeArray<Vector2>[] uv0Buffers;
    NativeArray<Color>[] colorBuffers;
    Terrain terrain;
    TerrainRenderer renderer;

    public void Initialize(int maxJobs, Terrain terrain, TerrainRenderer renderer)
    {
        this.terrain = terrain;
        this.renderer = renderer;

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

    public override void OnReady(int i, ref TerrainChunk.UpdateMeshJob job, Tuple<int, int> startParams)
    {
        int x = startParams.Item1;
        int z = startParams.Item2;

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

    public override void OnComplete(int i, ref TerrainChunk.UpdateMeshJob job)
    {
        renderer.GetChunk(job.chunkX, job.chunkZ).UpdateMesh(job.counts[0], job.vertices, job.triangles, job.uv0, job.colors);
        job.counts.Dispose();
    }
}

}