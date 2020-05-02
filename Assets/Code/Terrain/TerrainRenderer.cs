using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

public class TerrainRenderer : MonoBehaviour
{
    public delegate void OnTileClick(TerrainTile tileInfo);

    public event OnTileClick tileClicked;

    public float tileWidth = 1f;
    public float tileDepth = 1f;
    public float tileHeight = 1f;

    public TerrainTile terrainTilePrefab;
    public TerrainChunk terrainChunkPrefab;
    public int chunkSize = 10;

    Mesh mesh;
    TerrainTile[] tiles;
    TerrainChunk[] chunks;
    Terrain terrainData;

    void Awake()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    private int GetChunkIndex(int x, int z)
    {
        int width = (terrainData.width / chunkSize) + ((terrainData.width % chunkSize > 0) ? 1 : 0);
        return (z * width + x);
    }

    public void Initialise(Terrain terrainData)
    {
        this.terrainData = terrainData;
    }

    public int CreateWorldMesh()
    {
        int width = (terrainData.width / chunkSize) + ((terrainData.width % chunkSize > 0) ? 1 : 0);
        int depth = (terrainData.depth / chunkSize) + ((terrainData.depth % chunkSize > 0) ? 1 : 0);

        chunks = new TerrainChunk[width * depth];

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < depth; z++) {
                TerrainChunk chunk = Instantiate<TerrainChunk>(terrainChunkPrefab, this.transform);
                chunk.transform.localPosition = new Vector3(x * chunkSize, 0f, z * chunkSize);
                chunk.transform.localRotation = Quaternion.identity;
                chunk.name = $"Chunk {x * chunkSize}x{z * chunkSize} - {x * chunkSize + chunkSize}x{z * chunkSize + chunkSize}";
                chunk.Initialise(terrainData, this, x, z, chunkSize);

                chunks[GetChunkIndex(x, z)] = chunk;
            }
        }

        return width * depth;
    }

    public void CreateWorldTiles()
    {
        float t = Time.realtimeSinceStartup;

        tiles = new TerrainTile[terrainData.GetArraySize()];

        for (int y = 0; y < terrainData.height; y++) {
            for (int x = 0; x < terrainData.width; x++) {
                for (int z = 0; z < terrainData.depth; z++) {
                    TerrainTile tile = Instantiate<TerrainTile>(terrainTilePrefab,  this.transform);
                    tile.transform.localPosition = new Vector3(x * tileWidth, y * tileHeight, z * tileDepth);
                    tile.transform.localRotation = Quaternion.identity;
                    tile.name = $"Tile {x}/{z}/{y}";

                    tile.x = x;
                    tile.z = z;
                    tile.y = y;

                    tile.collider = tile.gameObject.GetComponent<BoxCollider>();
                    tile.collider.center = new Vector3(0.5f, 0.5f, 0.5f);
                    tile.collider.size = new Vector3(tileWidth, tileHeight, tileDepth);
                    if (!terrainData.IsFilled(x, z, y))
                        tile.collider.enabled = false;

                    tiles[terrainData.GetArrayIndex(x, z, y)] = tile;
                }
            }
        }

        Debug.Log($"Create world tiles took {Time.realtimeSinceStartup - t}s. Created {tiles.Length} tiles");
    }

    public void UpdateWorldMesh()
    {
        for (int i = 0, max = chunks.Length; i < max; i++) {
            chunks[i].UpdateMesh();
        }
    }

    public void UpdateWorldMeshForTile(int x, int z, int y)
    {
        chunks[GetChunkIndex(x / chunkSize, z / chunkSize)].UpdateMesh();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100f)) {
                TerrainTile tileInfo = hit.collider.GetComponent<TerrainTile>();
                if (tileInfo)
                    tileClicked?.Invoke(tileInfo);
            }
        }
    }
}

}
