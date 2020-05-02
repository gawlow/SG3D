using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

public class TerrainRenderer : MonoBehaviour
{
    public delegate void OnTileClick(int x, int z, int y);

    public event OnTileClick tileClicked;

    public float tileWidth = 1f;
    public float tileDepth = 1f;
    public float tileHeight = 1f;

    public TerrainLayer terrainLayerPrefab;
    public TerrainChunk terrainChunkPrefab;
    public int chunkSize = 10;

    Mesh mesh;
    TerrainLayer[] tiles;
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

    public void CreateLevelColliders()
    {
        tiles = new TerrainLayer[terrainData.height];

        for (int y = 0; y < terrainData.height; y++) {
            TerrainLayer layer = Instantiate<TerrainLayer>(terrainLayerPrefab, this.transform);
            layer.transform.localPosition = new Vector3(0f, y * tileHeight, 0f);
            layer.transform.localRotation = Quaternion.identity;
            layer.name = $"Terrain collider - layer {y}";
            layer.collider = layer.GetComponent<BoxCollider>();
            layer.collider.center = new Vector3(terrainData.width * tileWidth / 2, tileHeight / 2, terrainData.depth * tileDepth / 2);
            layer.collider.size = new Vector3(terrainData.width * tileWidth, tileHeight, terrainData.depth * tileDepth);
            layer.y = y;

            tiles[y] = layer;
        }
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

    public Vector3Int WorldToTileCoordinates(Vector3 position)
    {
        return new Vector3Int(Mathf.RoundToInt(position.x / tileWidth), Mathf.RoundToInt(position.y / tileHeight), Mathf.RoundToInt(position.z / tileDepth));
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

            Vector3Int? highest = null;
            for (int i = 0, max = hits.Length; i < max; i++) {
                // hits order is undefined, so little mess here
                TerrainLayer layer = hits[i].collider.GetComponent<TerrainLayer>();
                if (!layer)
                    continue;

                Vector3Int tile = WorldToTileCoordinates(hits[i].point);

                // We could try to read 'y' from hit coordinates, but boundaries between layers are shaky. 
                // Better be safe and read it from collider itself
                tile.y = layer.y;

                if (terrainData.IsFilled(tile.x, tile.z, tile.y)) {
                    if (!highest.HasValue || layer.y > highest.Value.y) {
                        highest = tile;
                    }
                }
            }

            if (highest.HasValue)
                tileClicked?.Invoke(highest.Value.x, highest.Value.z, highest.Value.y);
        }
    }
}

}
