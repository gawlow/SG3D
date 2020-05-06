using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace SG3D {


[BurstCompile]
struct UpdateMeshJob : IJob
{
    public int chunkX;
    public int chunkZ;
    public Vector3Int worldSize;
    public Vector3 tileSize;
    public Vector3Int chunkWorldPosition;
    public int chunkSize;
    public int textureSize;
    [WriteOnly] public NativeArray<Vector3> vertices;
    [WriteOnly] public NativeArray<int> triangles;
    [WriteOnly] public NativeArray<Vector2> uv0;
    [WriteOnly] public NativeArray<Color> colors;
    [WriteOnly] public NativeArray<int> counts;
    public TerrainInfo terrain;
    [ReadOnly] public NativeHashMap<int, TerrainTypeMaterialInfo> materials;

    public void Execute()
    {
        GenerateMesh();
    }

    // Mesh generation. It's never fun
    private void GenerateMesh()
    {
        // x/y/z
        int index = 0;
        for (int y = 0; y < worldSize.y; y++) {
            for (int x = 0; x < chunkSize; x++) {
                for (int z = 0; z < chunkSize; z++) {
                    Vector3Int localPosition = new Vector3Int(x, y, z);
                    Vector3Int worldPosition = chunkWorldPosition + localPosition;

                    if (!terrain.IsPresent(worldPosition))
                        continue;

                    TerrainType tileType = terrain.GetType(worldPosition);
                    TerrainTypeMaterialInfo materialInfo = materials[(int) tileType];

                    if (!terrain.IsPresent(new Vector3Int(worldPosition.x, worldPosition.y + 1, worldPosition.z)))
                        index += GenerateTopWall(index, worldPosition, localPosition, materialInfo);
                    
                    if (!terrain.IsPresent(new Vector3Int(worldPosition.x, worldPosition.y - 1, worldPosition.z)))
                        index += GenerateBottomWall(index, worldPosition, localPosition, materialInfo);

                    if (!terrain.IsPresent(new Vector3Int(worldPosition.x, worldPosition.y, worldPosition.z - 1)))
                        index += GenerateSouthWall(index, worldPosition, localPosition, materialInfo);

                    if (!terrain.IsPresent(new Vector3Int(worldPosition.x, worldPosition.y, worldPosition.z + 1)))
                        index += GenerateNorthWall(index, worldPosition, localPosition, materialInfo);

                    if (!terrain.IsPresent(new Vector3Int(worldPosition.x - 1, worldPosition.y, worldPosition.z)))
                        index += GenerateWestWall(index, worldPosition, localPosition, materialInfo);

                    if (!terrain.IsPresent(new Vector3Int(worldPosition.x + 1, worldPosition.y, worldPosition.z)))
                        index += GenerateEastWall(index, worldPosition, localPosition, materialInfo);
                }
            }
        }

        counts[0] = index;
    }

    private int GenerateTopWall(int index, Vector3Int worldPosition, Vector3Int chunkPosition, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along Y axis
        // Bottom left
        vertices[i] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z);

        // Top left
        vertices[i + 1] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Top right
        vertices[i + 2] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Bottom right
        vertices[i + 3] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z);

        float uv = 1f / textureSize;        
        uv0[i] = new Vector2(worldPosition.x * uv, worldPosition.z * uv);
        uv0[i + 1] = new Vector2(worldPosition.x * uv, worldPosition.z * uv + uv);
        uv0[i + 2] = new Vector2(worldPosition.x * uv + uv, worldPosition.z * uv + uv);
        uv0[i + 3] = new Vector2(worldPosition.x * uv + uv, worldPosition.z * uv);

        colors[i] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 1] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 2] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 3] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);

        triangles[j] = i;
        triangles[j + 1] = i + 1;
        triangles[j + 2] = i + 2;
        triangles[j + 3] = i;
        triangles[j + 4] = i + 2;
        triangles[j + 5] = i + 3;

        return 1;
    }

    private int GenerateBottomWall(int index, Vector3Int worldPosition, Vector3Int chunkPosition, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along Y axis
        // Bottom left
        vertices[i] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z);

        // Top left
        vertices[i + 1] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Top right
        vertices[i + 2] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Bottom right
        vertices[i + 3] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z);

        float uv = 1f / textureSize;        
        uv0[i] = new Vector2(worldPosition.x * uv, worldPosition.z * uv);
        uv0[i + 1] = new Vector2(worldPosition.x * uv, worldPosition.z * uv + uv);
        uv0[i + 2] = new Vector2(worldPosition.x * uv + uv, worldPosition.z * uv + uv);
        uv0[i + 3] = new Vector2(worldPosition.x * uv + uv, worldPosition.z * uv);

        colors[i] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 1] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 2] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 3] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);

        triangles[j] = i;
        triangles[j + 1] = i + 2;
        triangles[j + 2] = i + 1;
        triangles[j + 3] = i;
        triangles[j + 4] = i + 3;
        triangles[j + 5] = i + 2;

        return 1;
    }

    private int GenerateWestWall(int index, Vector3Int worldPosition, Vector3Int chunkPosition, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along X axis
        // Bottom left
        vertices[i] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Top left
        vertices[i + 1] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Top right
        vertices[i + 2] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z);

        // Bottom right
        vertices[i + 3] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z);

        float uv = 1f / textureSize;
        uv0[i] = new Vector2(worldPosition.z * uv, worldPosition.y * uv);
        uv0[i + 1] = new Vector2(worldPosition.z * uv, worldPosition.y * uv + uv);
        uv0[i + 2] = new Vector2(worldPosition.z * uv + uv, worldPosition.y * uv + uv);
        uv0[i + 3] = new Vector2(worldPosition.z * uv + uv, worldPosition.y * uv);

        colors[i] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 1] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 2] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 3] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);

        triangles[j] = i;
        triangles[j + 1] = i + 1;
        triangles[j + 2] = i + 2;
        triangles[j + 3] = i;
        triangles[j + 4] = i + 2;
        triangles[j + 5] = i + 3;

        return 1;
    }

    private int GenerateEastWall(int index, Vector3Int worldPosition, Vector3Int chunkPosition, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along X axis
        // Bottom left
        vertices[i] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Top left
        vertices[i + 1] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Top right
        vertices[i + 2] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z);

        // Bottom right
        vertices[i + 3] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z);

        float uv = 1f / textureSize;
        uv0[i] = new Vector2(worldPosition.z * uv, worldPosition.y * uv);
        uv0[i + 1] = new Vector2(worldPosition.z * uv, worldPosition.y * uv + uv);
        uv0[i + 2] = new Vector2(worldPosition.z * uv + uv, worldPosition.y * uv + uv);
        uv0[i + 3] = new Vector2(worldPosition.z * uv + uv, worldPosition.y * uv);

        colors[i] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 1] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 2] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 3] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);

        triangles[j] = i;
        triangles[j + 1] = i + 2;
        triangles[j + 2] = i + 1;
        triangles[j + 3] = i;
        triangles[j + 4] = i + 3;
        triangles[j + 5] = i + 2;

        return 1;
    }

    private int GenerateSouthWall(int index, Vector3Int worldPosition, Vector3Int chunkPosition, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along Z axis
        // Bottom left
        vertices[i] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z);

        // Top left
        vertices[i + 1] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z);

        // Top right
        vertices[i + 2] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z);

        // Bottom right
        vertices[i + 3] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z);

        float uv = 1f / textureSize;
        uv0[i] = new Vector2(worldPosition.x * uv, worldPosition.y * uv);
        uv0[i + 1] = new Vector2(worldPosition.x * uv, worldPosition.y * uv + uv);
        uv0[i + 2] = new Vector2(worldPosition.x * uv + uv, worldPosition.y * uv + uv);
        uv0[i + 3] = new Vector2(worldPosition.x * uv + uv, worldPosition.y * uv);

        colors[i] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 1] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 2] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 3] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);

        triangles[j] = i;
        triangles[j + 1] = i + 1;
        triangles[j + 2] = i + 2;
        triangles[j + 3] = i;
        triangles[j + 4] = i + 2;
        triangles[j + 5] = i + 3;

        return 1;
    }

    private int GenerateNorthWall(int index, Vector3Int worldPosition, Vector3Int chunkPosition, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along Z axis
        // Bottom left
        vertices[i] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Top left
        vertices[i + 1] = new Vector3(chunkPosition.x * tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Top right
        vertices[i + 2] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y + tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        // Bottom right
        vertices[i + 3] = new Vector3(chunkPosition.x * tileSize.x + tileSize.x, chunkPosition.y * tileSize.y, chunkPosition.z * tileSize.z + tileSize.z);

        float uv = 1f / textureSize;
        uv0[i] = new Vector2(worldPosition.x * uv, worldPosition.y * uv);
        uv0[i + 1] = new Vector2(worldPosition.x * uv, worldPosition.y * uv + uv);
        uv0[i + 2] = new Vector2(worldPosition.x * uv + uv, worldPosition.y * uv + uv);
        uv0[i + 3] = new Vector2(worldPosition.x * uv + uv, worldPosition.y * uv);

        colors[i] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 1] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 2] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);
        colors[i + 3] = new Color(materialInfo.shaderIndex, materialInfo.smoothness, 0, 0);

        triangles[j] = i;
        triangles[j + 1] = i + 2;
        triangles[j + 2] = i + 1;
        triangles[j + 3] = i;
        triangles[j + 4] = i + 3;
        triangles[j + 5] = i + 2;

        return 1;
    }

}


class TerrainMeshRendererJobManager : JobManager<UpdateMeshJob, TerrainMeshRendererJobManager.JobParam>
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

    public override void OnReady(int i, ref UpdateMeshJob job, TerrainMeshRendererJobManager.JobParam startParams)
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

    public override void OnComplete(int i, ref UpdateMeshJob job, ref TerrainMeshRendererJobManager.JobParam startParams)
    {
        int index = job.counts[0];

        Mesh mesh = new Mesh();
        TerrainBakeMeshJobManager.JobParams bakeParams;
        bakeParams.mesh = mesh;
        bakeParams.chunk = renderer.GetChunk(job.chunkX, job.chunkZ);

        mesh.SetVertices(job.vertices, 0, index * 4);
        mesh.SetIndices(job.triangles, 0, index * 6, MeshTopology.Triangles, 0, true);
        mesh.SetUVs(0, job.uv0, 0, index * 4);
        mesh.SetColors(job.colors, 0, index * 4);
        mesh.RecalculateNormals();

        bakeMeshJobManager.ScheduleJob(bakeParams);
        job.counts.Dispose();
    }
}

}