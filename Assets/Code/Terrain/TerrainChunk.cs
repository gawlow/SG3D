using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public TerrainVoxelCollider terrainVoxelColliderPrefab;
    MeshFilter meshFilter;
    Mesh mesh;
    Terrain terrainData;
    new TerrainRenderer renderer;

    List<Vector3> vertices;
    List<int> triangles;
    List<Vector2> uv0;

    TerrainVoxelCollider[,,] voxels;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
    }

    public void Initialise(Terrain terrainData, TerrainRenderer renderer, int x, int z, int size, int textureSize)
    {
        this.renderer = renderer;
        this.terrainData = terrainData;
        this.chunkX = x;
        this.chunkZ = z;
        this.size = size;
        this.textureSize = textureSize;

        // Preallocate to avoid GC
        vertices = new List<Vector3>(terrainData.terrainWidth * terrainData.terrainDepth * terrainData.terrainHeight * 4);
        triangles = new List<int>(terrainData.terrainWidth * terrainData.terrainDepth * terrainData.terrainHeight * 6);
        uv0 = new List<Vector2>(terrainData.terrainWidth * terrainData.terrainDepth * terrainData.terrainHeight * 4);
    }

    // Get our Voxel object for given tile. Note that 'tile' refers to world location, not local chunk location
    public TerrainVoxelCollider GetVoxel(Vector3Int tile)
    {
        return voxels[tile.y, tile.z - WorldTileZPosition(), tile.x - WorldTileXPosition()];
    }

    // Create all the Voxel objects. We're using them not to store data or logic handling, but mainly as  
    // containers to BoxColliders. I'm not entirly happy with this solution but it seems to work and is quite performant
    // (apart from the generation phase)
    public void CreateVoxels()
    {
        voxels = new TerrainVoxelCollider[terrainData.terrainHeight, size, size];

        for (int y = 0; y < terrainData.terrainHeight; y++) {
            for (int z = 0; z < size; z++) {
                for (int x = 0; x < size; x++) {
                    TerrainVoxelCollider voxel = Instantiate<TerrainVoxelCollider>(terrainVoxelColliderPrefab, this.transform);
                    voxel.transform.localPosition = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);
                    voxel.transform.localRotation = Quaternion.identity;
                    voxel.name = $"Voxel X: {WorldTileXPosition() + x}, Z: {WorldTileZPosition() + z}, Y: {y}";
                    voxel.tileX = WorldTileXPosition() + x;
                    voxel.tileZ = WorldTileZPosition() + z;
                    voxel.tileY = y;
                    voxel.collider = voxel.GetComponent<BoxCollider>();
                    voxel.collider.size = new Vector3(renderer.tileWidth, renderer.tileHeight, renderer.tileDepth);
                    voxel.collider.center = new Vector3(renderer.tileWidth / 2, renderer.tileHeight / 2, renderer.tileDepth / 2);
                    voxel.collider.enabled = terrainData.IsPresent(x, z, y);
                    voxels[y, z, x] = voxel;
                }
            }
        }
    }

    // Mesh generation. It's never fun
    public void UpdateMesh()
    {
        float t = Time.realtimeSinceStartup;

        // int i = 0, j = 0;
        vertices.Clear();
        triangles.Clear();
        uv0.Clear();

        // x/y/z
        for (int y = 0; y < terrainData.terrainHeight; y++) {
            for (int x = 0; x < size; x++) {
                for (int z = 0; z < size; z++) {
                    int worldX = WorldTileXPosition() + x;
                    int worldZ = WorldTileZPosition() + z;
                    int worldY = y;

                    if (!terrainData.IsPresent(worldX, worldZ, worldY))
                        continue;

                    if (!terrainData.IsPresent(worldX, worldZ, worldY + 1))
                        GenerateTopWall(x, z, y);
                    
                    if (!terrainData.IsPresent(worldX, worldZ, worldY - 1))
                        GenerateBottomWall(x, z, y);

                    if (!terrainData.IsPresent(worldX, worldZ - 1, worldY))
                        GenerateSouthWall(x, z, y);

                    if (!terrainData.IsPresent(worldX, worldZ + 1, worldY))
                        GenerateNorthWall(x, z, y);

                    if (!terrainData.IsPresent(worldX - 1, worldZ, worldY))
                        GenerateWestWall(x, z, y);

                    if (!terrainData.IsPresent(worldX + 1, worldZ, worldY))
                        GenerateEastWall(x, z, y);
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices, 0, vertices.Count);
        mesh.SetTriangles(triangles, 0, triangles.Count, 0);
        mesh.SetUVs(0, uv0, 0, uv0.Count);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    private void GenerateTopWall(int x, int z, int y)
    {
        int i = vertices.Count;

        // Looking along Y axis
        // Bottom left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth));

        // Top left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Top right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Bottom right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth));

        float uv = 1f / textureSize;        
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv, (WorldTileZPosition() + z) * uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv, (WorldTileZPosition() + z) * uv + uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileZPosition() + z) * uv + uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileZPosition() + z) * uv));

        triangles.Add(i);
        triangles.Add(i + 1);
        triangles.Add(i + 2);
        triangles.Add(i);
        triangles.Add(i + 2);
        triangles.Add(i + 3);
    }


    private void GenerateBottomWall(int x, int z, int y)
    {
        int i = vertices.Count;

        // Looking along Y axis
        // Bottom left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth));

        // Top left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Top right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Bottom right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth));

        float uv = 1f / textureSize;        
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv, (WorldTileZPosition() + z) * uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv, (WorldTileZPosition() + z) * uv + uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileZPosition() + z) * uv + uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileZPosition() + z) * uv));

        triangles.Add(i);
        triangles.Add(i + 2);
        triangles.Add(i + 1);
        triangles.Add(i);
        triangles.Add(i + 3);
        triangles.Add(i + 2);
    }

    private void GenerateWestWall(int x, int z, int y)
    {
        int i = vertices.Count;

        // Looking along X axis
        // Bottom left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Top left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Top right
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth));

        // Bottom right
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth));

        float uv = 1f / textureSize;
        uv0.Add(new Vector2((WorldTileZPosition() + z) * uv, (WorldTileYPosition() + y) * uv));
        uv0.Add(new Vector2((WorldTileZPosition() + z) * uv, (WorldTileYPosition() + y) * uv + uv));
        uv0.Add(new Vector2((WorldTileZPosition() + z) * uv + uv, (WorldTileYPosition() + y) * uv + uv));
        uv0.Add(new Vector2((WorldTileZPosition() + z) * uv + uv, (WorldTileYPosition() + y) * uv));

        triangles.Add(i);
        triangles.Add(i + 1);
        triangles.Add(i + 2);
        triangles.Add(i);
        triangles.Add(i + 2);
        triangles.Add(i + 3);
    }

    private void GenerateEastWall(int x, int z, int y)
    {
        int i = vertices.Count;

        // Looking along X axis
        // Bottom left
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Top left
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Top right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth));

        // Bottom right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth));

        float uv = 1f / textureSize;
        uv0.Add(new Vector2((WorldTileZPosition() + z) * uv, (WorldTileYPosition() + y) * uv));
        uv0.Add(new Vector2((WorldTileZPosition() + z) * uv, (WorldTileYPosition() + y) * uv + uv));
        uv0.Add(new Vector2((WorldTileZPosition() + z) * uv + uv, (WorldTileYPosition() + y) * uv + uv));
        uv0.Add(new Vector2((WorldTileZPosition() + z) * uv + uv, (WorldTileYPosition() + y) * uv));

        triangles.Add(i);
        triangles.Add(i + 2);
        triangles.Add(i + 1);
        triangles.Add(i);
        triangles.Add(i + 3);
        triangles.Add(i + 2);
    }

    private void GenerateSouthWall(int x, int z, int y)
    {
        int i = vertices.Count;

        // Looking along Z axis
        // Bottom left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth));

        // Top left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth));

        // Top right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth));

        // Bottom right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth));

        float uv = 1f / textureSize;
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv, (WorldTileYPosition() + y) * uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv, (WorldTileYPosition() + y) * uv + uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileYPosition() + y) * uv + uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileYPosition() + y) * uv));

        triangles.Add(i);
        triangles.Add(i + 1);
        triangles.Add(i + 2);
        triangles.Add(i);
        triangles.Add(i + 2);
        triangles.Add(i + 3);
    }

    private void GenerateNorthWall(int x, int z, int y)
    {
        int i = vertices.Count;

        // Looking along Z axis
        // Bottom left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Top left
        vertices.Add(new Vector3(x * renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Top right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight + renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        // Bottom right
        vertices.Add(new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth));

        float uv = 1f / textureSize;
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv, (WorldTileYPosition() + y) * uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv, (WorldTileYPosition() + y) * uv + uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileYPosition() + y) * uv + uv));
        uv0.Add(new Vector2((WorldTileXPosition() + x) * uv + uv, (WorldTileYPosition() + y) * uv));

        triangles.Add(i);
        triangles.Add(i + 2);
        triangles.Add(i + 1);
        triangles.Add(i);
        triangles.Add(i + 3);
        triangles.Add(i + 2);
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
