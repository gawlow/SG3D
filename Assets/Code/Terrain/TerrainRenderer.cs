using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

public class TerrainRenderer : MonoBehaviour
{
    public delegate void OnTileClick(Vector3Int tile);

    public event OnTileClick tileClicked;

    public float tileWidth = 1f;
    public float tileDepth = 1f;
    public float tileHeight = 1f;

    public TerrainChunk terrainChunkPrefab;
    public int chunkSize = 10;

    Mesh mesh;
    TerrainChunk[,] chunks;
    Terrain terrainData;

    void Awake()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    public void Initialise(Terrain terrainData)
    {
        this.terrainData = terrainData;
    }

    public int CreateWorldChunks()
    {
        int width = (terrainData.terrainWidth / chunkSize) + ((terrainData.terrainWidth % chunkSize > 0) ? 1 : 0);
        int depth = (terrainData.terrainDepth / chunkSize) + ((terrainData.terrainDepth % chunkSize > 0) ? 1 : 0);

        chunks = new TerrainChunk[width, depth];

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < depth; z++) {
                TerrainChunk chunk = Instantiate<TerrainChunk>(terrainChunkPrefab, this.transform);
                chunk.transform.localPosition = new Vector3(x * chunkSize * tileWidth, 0f, z * chunkSize * tileDepth);
                chunk.transform.localRotation = Quaternion.identity;
                chunk.name = $"Chunk X: {x * chunkSize} Z:{z * chunkSize}, size: {chunkSize}";
                chunk.Initialise(terrainData, this, x, z, chunkSize);
                chunk.CreateVoxels();
                chunks[x, z] = chunk;
            }
        }

        return width * depth;
    }

    public void UpdateWorldMesh()
    {
        for (int x = 0; x < chunks.GetLength(0); x++) {
            for (int z = 0; z < chunks.GetLength(1); z++) {
                chunks[x, z].UpdateMesh();
            }
        }
    }

    public void UpdateWorldMeshForTile(Vector3Int tile)
    {
        GetChunkForTile(tile).UpdateMesh();
    }

    public TerrainVoxelCollider GetVoxel(Vector3Int tile)
    {
        return GetChunkForTile(tile).GetVoxel(tile);
    }

    private TerrainChunk GetChunkForTile(Vector3Int tile)
    {
        return chunks[tile.x / chunkSize, tile.z / chunkSize];
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f)) {
                TerrainVoxelCollider voxel = hit.collider.GetComponent<TerrainVoxelCollider>();
                if (voxel)
                    tileClicked?.Invoke(new Vector3Int(voxel.tileX, voxel.tileY, voxel.tileZ));
            }
        }
    }
}

}
