using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    public int chunkX;       // chunk X
    public int chunkZ;       // chunk Z
    public int size;        // chunks are cubes sized size x size in X and Z dimensions, and whole height
    public TerrainLayer terrainLayerPrefab;
    public TerrainVoxelCollider terrainVoxelColliderPrefab;
    MeshFilter meshFilter;
    Mesh mesh;
    Terrain terrainData;
    new TerrainRenderer renderer;

    TerrainVoxelCollider[,,] voxels;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
    }

    public void Initialise(Terrain terrainData, TerrainRenderer renderer, int x, int z, int size)
    {
        this.renderer = renderer;
        this.terrainData = terrainData;
        this.chunkX = x;
        this.chunkZ = z;
        this.size = size;
    }

    public int WorldTileXPosition()
    {
        return size * chunkX;
    }

    public int WorldTileZPosition()
    {
        return size * chunkZ;
    }

    public TerrainVoxelCollider GetVoxel(Vector3Int tile)
    {
        return voxels[tile.y, tile.z - WorldTileZPosition(), tile.x - WorldTileXPosition()];
    }

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
                    voxels[y, z, x] = voxel;
                }
            }
        }
    }

    public void UpdateMesh()
    {
        float t = Time.realtimeSinceStartup;

        Vector3[] vertices = new Vector3[terrainData.terrainWidth * terrainData.terrainDepth * terrainData.terrainHeight * 4];
        int[] triangles = new int[terrainData.terrainWidth * terrainData.terrainDepth * terrainData.terrainHeight * 6];
        int i = 0, j = 0;

        // x/y/z
        for (int y = 0; y < terrainData.terrainHeight; y++) {
            for (int x = 0; x < size; x++) {
                for (int z = 0; z < size; z++) {
                    if (!terrainData.IsPresent(WorldTileXPosition() + x, WorldTileZPosition() + z, y))
                        continue;

                    // Bottom left
                    vertices[i] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

                    // Top left
                    vertices[i + 1] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

                    // Top right
                    vertices[i + 2] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

                    // Bottom right
                    vertices[i + 3] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

                    triangles[j] = i;
                    triangles[j + 1] = i + 1;
                    triangles[j + 2] = i + 2;
                    triangles[j + 3] = i;
                    triangles[j + 4] = i + 2;
                    triangles[j + 5] = i + 3;

                    i += 4;
                    j += 6;
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices, 0, i);
        mesh.SetTriangles(triangles, 0, j, 0);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        Debug.Log($"Create chunk ({chunkX}x{chunkZ}) mesh took {Time.realtimeSinceStartup - t}s. Created {i} vertices and {j} indexes");
    }

    void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        position += new Vector3(size / 2 * renderer.tileWidth, terrainData.terrainHeight  / 2 * renderer.tileHeight, size / 2 * renderer.tileDepth);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(position, new Vector3(size * renderer.tileWidth, terrainData.terrainHeight * renderer.tileHeight, size * renderer.tileDepth));
    }
}

}
