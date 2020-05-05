using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace SG3D {

// This class is responsible for creating meshes and all other components
// required to display given world chunk
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    public int chunkX;          // chunk X
    public int chunkZ;          // chunk Z
    public int size;            // horizontal dimensions of chunk
    public int textureSize;   // spread texture over how many tiles

    MeshFilter meshFilter;
    Mesh mesh;
    Terrain terrainData;
    new TerrainRenderer renderer;

    MeshCollider meshCollider;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        mesh = new Mesh();
    }

    public void Initialise(Terrain terrainData, TerrainRenderer renderer, int x, int z, int size, int textureSize, Material material)
    {
        this.renderer = renderer;
        this.terrainData = terrainData;
        this.chunkX = x;
        this.chunkZ = z;
        this.size = size;
        this.textureSize = textureSize;

        GetComponent<MeshRenderer>().material = material;
    }

    public void UpdateMesh(int index, NativeArray<Vector3> vertices, NativeArray<int> indices, NativeArray<Vector2> uv0, NativeArray<Color> colors)
    {
        mesh.Clear();
        mesh.SetVertices(vertices, 0, index * 4);
        mesh.SetIndices(indices, 0, index * 6, MeshTopology.Triangles, 0, true);
        mesh.SetUVs(0, uv0, 0, index * 4);
        mesh.SetColors(colors, 0, index * 4);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        position += new Vector3(size / 2 * renderer.tileWidth, renderer.tileHeight / 2 * terrainData.terrainHeight, size / 2 * renderer.tileDepth);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(position, new Vector3(size * renderer.tileWidth, terrainData.terrainHeight * renderer.tileHeight, size * renderer.tileDepth));
    }

    public struct UpdateMeshJob : IJob
    {
        public int chunkX;
        public int chunkZ;
        public Vector3Int worldSize;
        public Vector3 tileSize;
        public Vector3Int chunkWorldPosition;
        public int chunkSize;
        public int textureSize;
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public NativeArray<Vector2> uv0;
        public NativeArray<Color> colors;
        public NativeArray<int> counts;
        [ReadOnly] public NativeArray<bool> present;
        [ReadOnly] public NativeArray<TerrainType> type;
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

                        if (!Terrain.IsPresent(worldPosition, worldSize, present))
                            continue;

                        TerrainType tileType = Terrain.GetType(worldPosition, worldSize, type);
                        TerrainTypeMaterialInfo materialInfo = materials[(int) tileType];

                        if (!Terrain.IsPresent(new Vector3Int(worldPosition.x, worldPosition.y + 1, worldPosition.z), worldSize, present))
                            index += GenerateTopWall(index, worldPosition, localPosition, materialInfo);
                        
                        if (!Terrain.IsPresent(new Vector3Int(worldPosition.x, worldPosition.y - 1, worldPosition.z), worldSize, present))
                            index += GenerateBottomWall(index, worldPosition, localPosition, materialInfo);

                        if (!Terrain.IsPresent(new Vector3Int(worldPosition.x, worldPosition.y, worldPosition.z - 1), worldSize, present))
                            index += GenerateSouthWall(index, worldPosition, localPosition, materialInfo);

                        if (!Terrain.IsPresent(new Vector3Int(worldPosition.x, worldPosition.y, worldPosition.z + 1), worldSize, present))
                            index += GenerateNorthWall(index, worldPosition, localPosition, materialInfo);

                        if (!Terrain.IsPresent(new Vector3Int(worldPosition.x - 1, worldPosition.y, worldPosition.z), worldSize, present))
                            index += GenerateWestWall(index, worldPosition, localPosition, materialInfo);

                        if (!Terrain.IsPresent(new Vector3Int(worldPosition.x + 1, worldPosition.y, worldPosition.z), worldSize, present))
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
}

}
