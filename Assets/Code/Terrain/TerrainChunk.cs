using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

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

    NativeArray<Vector3> vertices;
    NativeArray<int> triangles;
    NativeArray<Vector2> uv0;
    NativeArray<Color> colors;   // Index for texture array. Technically Color32 would be better, but I'm scared of rounding errors

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

        // Preallocate to avoid GC
        vertices = new NativeArray<Vector3>(terrainData.terrainWidth * terrainData.terrainDepth * terrainData.terrainHeight * 4, Allocator.Persistent);
        triangles = new NativeArray<int>(terrainData.terrainWidth * terrainData.terrainDepth * terrainData.terrainHeight * 6, Allocator.Persistent);
        uv0 = new NativeArray<Vector2>(terrainData.terrainWidth * terrainData.terrainDepth * terrainData.terrainHeight * 4, Allocator.Persistent);
        colors = new NativeArray<Color>(terrainData.terrainWidth * terrainData.terrainDepth * terrainData.terrainHeight * 4, Allocator.Persistent);
    }
    void OnDestroy()
    {
        vertices.Dispose();
        triangles.Dispose();
        uv0.Dispose();
        colors.Dispose();
    }

    // Mesh generation. It's never fun
    public void UpdateMesh()
    {
        // x/y/z
        int index = 0;
        for (int y = 0; y < terrainData.terrainHeight; y++) {
            for (int x = 0; x < size; x++) {
                for (int z = 0; z < size; z++) {
                    int worldX = WorldTileXPosition() + x;
                    int worldZ = WorldTileZPosition() + z;
                    int worldY = y;

                    if (!terrainData.IsPresent(worldX, worldZ, worldY))
                        continue;

                    TerrainType type = terrainData.GetType(worldX, worldZ, worldY);
                    TerrainTypeMaterialInfo materialInfo = renderer.GetMaterialInfo(type);

                    if (!terrainData.IsPresent(worldX, worldZ, worldY + 1))
                        index += GenerateTopWall(index, x, z, y, materialInfo);
                    
                    if (!terrainData.IsPresent(worldX, worldZ, worldY - 1))
                        index += GenerateBottomWall(index, x, z, y, materialInfo);

                    if (!terrainData.IsPresent(worldX, worldZ - 1, worldY))
                        index += GenerateSouthWall(index, x, z, y, materialInfo);

                    if (!terrainData.IsPresent(worldX, worldZ + 1, worldY))
                        index += GenerateNorthWall(index, x, z, y, materialInfo);

                    if (!terrainData.IsPresent(worldX - 1, worldZ, worldY))
                        index += GenerateWestWall(index, x, z, y, materialInfo);

                    if (!terrainData.IsPresent(worldX + 1, worldZ, worldY))
                        index += GenerateEastWall(index, x, z, y, materialInfo);
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices, 0, index * 4);
        mesh.SetIndices(triangles, 0, index * 6, MeshTopology.Triangles, 0, true);
        mesh.SetUVs(0, uv0, 0, index * 4);
        mesh.SetColors(colors, 0, index * 4);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private int GenerateTopWall(int index, int x, int z, int y, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along Y axis
        // Bottom left
        vertices[i] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth);

        // Top left
        vertices[i + 1] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Top right
        vertices[i + 2] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Bottom right
        vertices[i + 3] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth);

        float uv = 1f / textureSize;        
        uv0[i] = new Vector2((WorldTileXPosition() + x) * uv, (WorldTileZPosition() + z) * uv);
        uv0[i + 1] = new Vector2((WorldTileXPosition() + x) * uv, (WorldTileZPosition() + z) * uv + uv);
        uv0[i + 2] = new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileZPosition() + z) * uv + uv);
        uv0[i + 3] = new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileZPosition() + z) * uv);

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

    private int GenerateBottomWall(int index, int x, int z, int y, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along Y axis
        // Bottom left
        vertices[i] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

        // Top left
        vertices[i + 1] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Top right
        vertices[i + 2] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Bottom right
        vertices[i + 3] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

        float uv = 1f / textureSize;        
        uv0[i] = new Vector2((WorldTileXPosition() + x) * uv, (WorldTileZPosition() + z) * uv);
        uv0[i + 1] = new Vector2((WorldTileXPosition() + x) * uv, (WorldTileZPosition() + z) * uv + uv);
        uv0[i + 2] = new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileZPosition() + z) * uv + uv);
        uv0[i + 3] = new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileZPosition() + z) * uv);

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

    private int GenerateWestWall(int index, int x, int z, int y, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along X axis
        // Bottom left
        vertices[i] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Top left
        vertices[i + 1] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Top right
        vertices[i + 2] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth);

        // Bottom right
        vertices[i + 3] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

        float uv = 1f / textureSize;
        uv0[i] = new Vector2((WorldTileZPosition() + z) * uv, (WorldTileYPosition() + y) * uv);
        uv0[i + 1] = new Vector2((WorldTileZPosition() + z) * uv, (WorldTileYPosition() + y) * uv + uv);
        uv0[i + 2] = new Vector2((WorldTileZPosition() + z) * uv + uv, (WorldTileYPosition() + y) * uv + uv);
        uv0[i + 3] = new Vector2((WorldTileZPosition() + z) * uv + uv, (WorldTileYPosition() + y) * uv);

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

    private int GenerateEastWall(int index, int x, int z, int y, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along X axis
        // Bottom left
        vertices[i] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Top left
        vertices[i + 1] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Top right
        vertices[i + 2] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth);

        // Bottom right
        vertices[i + 3] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

        float uv = 1f / textureSize;
        uv0[i] = new Vector2((WorldTileZPosition() + z) * uv, (WorldTileYPosition() + y) * uv);
        uv0[i + 1] = new Vector2((WorldTileZPosition() + z) * uv, (WorldTileYPosition() + y) * uv + uv);
        uv0[i + 2] = new Vector2((WorldTileZPosition() + z) * uv + uv, (WorldTileYPosition() + y) * uv + uv);
        uv0[i + 3] = new Vector2((WorldTileZPosition() + z) * uv + uv, (WorldTileYPosition() + y) * uv);

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

    private int GenerateSouthWall(int index, int x, int z, int y, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along Z axis
        // Bottom left
        vertices[i] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

        // Top left
        vertices[i + 1] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth);

        // Top right
        vertices[i + 2] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth);

        // Bottom right
        vertices[i + 3] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

        float uv = 1f / textureSize;
        uv0[i] = new Vector2((WorldTileXPosition() + x) * uv, (WorldTileYPosition() + y) * uv);
        uv0[i + 1] = new Vector2((WorldTileXPosition() + x) * uv, (WorldTileYPosition() + y) * uv + uv);
        uv0[i + 2] = new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileYPosition() + y) * uv + uv);
        uv0[i + 3] = new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileYPosition() + y) * uv);

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

    private int GenerateNorthWall(int index, int x, int z, int y, TerrainTypeMaterialInfo materialInfo)
    {
        int i = index * 4;
        int j = index * 6;

        // Looking along Z axis
        // Bottom left
        vertices[i] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Top left
        vertices[i + 1] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Top right
        vertices[i + 2] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        // Bottom right
        vertices[i + 3] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

        float uv = 1f / textureSize;
        uv0[i] = new Vector2((WorldTileXPosition() + x) * uv, (WorldTileYPosition() + y) * uv);
        uv0[i + 1] = new Vector2((WorldTileXPosition() + x) * uv, (WorldTileYPosition() + y) * uv + uv);
        uv0[i + 2] = new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileYPosition() + y) * uv + uv);
        uv0[i + 3] = new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileYPosition() + y) * uv);

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

    // Returns X coordinate of our (0,0) tile in the world
    private int WorldTileXPosition()
    {
        return size * chunkX;
    }

    // Returns Z coordinate of our (0,0) tile in the world
    private int WorldTileZPosition()
    {
        return size * chunkZ;
    }

    // Placerholder just in case
    private int WorldTileYPosition()
    {
        return 0;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        position += new Vector3(size / 2 * renderer.tileWidth, renderer.tileHeight / 2 * terrainData.terrainHeight, size / 2 * renderer.tileDepth);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(position, new Vector3(size * renderer.tileWidth, terrainData.terrainHeight * renderer.tileHeight, size * renderer.tileDepth));
    }
}

}
