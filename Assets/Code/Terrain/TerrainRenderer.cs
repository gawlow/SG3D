using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

// This class is responsible for displaying terrain.
// It takes data from Terrain class and uses it to generate meshes, colliders and
// other things required for user interaction
public class TerrainRenderer : MonoBehaviour
{
    public delegate void OnTileClick(Vector3Int tile);

    public event OnTileClick tileClicked;   // Called when user clicked on a tile

    public float tileWidth = 1f;
    public float tileDepth = 1f;
    public float tileHeight = 1f;

    public TerrainChunk terrainChunkPrefab;
    public int chunkSize = 10;
    public int chunkTextureSize = 10;
    TerrainChunk[,] chunks;
    Terrain terrainData;

    public void Initialise(Terrain terrainData)
    {
        this.terrainData = terrainData;
    }

    public int CreateWorld()
    {
        // Because map can be really large, generating a single mesh is a no-go, as updates to it would take too
        // much time. So we divide world into equaly sized chunks, slicing the world along the X and Z coordinates 
        // (Y is expected to be small anyway). Each chunk then generates its meshes, colliders, etc
        int width = (terrainData.terrainWidth / chunkSize) + ((terrainData.terrainWidth % chunkSize > 0) ? 1 : 0);
        int depth = (terrainData.terrainDepth / chunkSize) + ((terrainData.terrainDepth % chunkSize > 0) ? 1 : 0);

        chunks = new TerrainChunk[width, depth];

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < depth; z++) {
                TerrainChunk chunk = Instantiate<TerrainChunk>(terrainChunkPrefab, this.transform);
                chunk.transform.localPosition = new Vector3(x * chunkSize * tileWidth, 0f, z * chunkSize * tileDepth);
                chunk.transform.localRotation = Quaternion.identity;
                chunk.name = $"Chunk X: {x * chunkSize} Z:{z * chunkSize}, size: {chunkSize}";
                chunk.Initialise(terrainData, this, x, z, chunkSize, chunkTextureSize);
                chunk.CreateVoxels();
                chunks[x, z] = chunk;
            }
        }

        return width * depth;
    }

    // This updates entire world, should be called only on startup really
    public void UpdateWorldMesh()
    {
        for (int x = 0; x < chunks.GetLength(0); x++) {
            for (int z = 0; z < chunks.GetLength(1); z++) {
                chunks[x, z].UpdateMesh();
            }
        }
    }

    // Updates mesh for chunk containing given tile
    public void UpdateWorldMeshForTile(Vector3Int tile)
    {
        GetChunkForTile(tile).UpdateMesh();

        // Check if tile is at the chunk boundary and refresh neighbours if needed
        int tileX = tile.x % chunkSize;
        int tileZ = tile.z % chunkSize;

        if (tileX == chunkSize - 1 && terrainData.terrainWidth > tile.x + 1) {  // +1 because tiles are indexed from 0
            GetChunkForTile(new Vector3Int(tile.x + 1, tile.y, tile.z)).UpdateMesh();
        } else if (tileX == 1 && tile.x > 0) {
            GetChunkForTile(new Vector3Int(tile.x - 1, tile.y, tile.z)).UpdateMesh();
        }

        if (tileZ == chunkSize - 1 && terrainData.terrainDepth > tile.z + 1) {  // +1 because tiles are indexed from 0
            GetChunkForTile(new Vector3Int(tile.x, tile.y, tile.z + 1)).UpdateMesh();
        } else if (tileZ == 1 && tile.x > 0) {
            GetChunkForTile(new Vector3Int(tile.x, tile.y , tile.z - 1)).UpdateMesh();
        }
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
        // Detection of clicking on tiles
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
