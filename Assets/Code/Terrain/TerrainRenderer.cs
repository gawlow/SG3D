using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

public class TerrainRenderer : MonoBehaviour
{
    public delegate void OnTileClick(TerrainTile tile);

    public event OnTileClick tileClicked;

    public float tileWidth = 1f;
    public float tileDepth = 1f;
    public float tileHeight = 1f;

    public TerrainTile terrainTilePrefab;

    Mesh mesh;
    TerrainTile[] tiles;

    void Awake()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    public void CreateWorldMesh(Terrain terrain)
    {
        float t = Time.realtimeSinceStartup;

        Vector3[] vertices = new Vector3[terrain.width * terrain.depth * terrain.height * 4];
        int[] triangles = new int[terrain.width * terrain.depth * terrain.height * 6];
        int i = 0, j = 0;

        // x/y/z
        for (int y = 0; y < terrain.height; y++) {
            for (int x = 0; x < terrain.width; x++) {
                for (int z = 0; z < terrain.depth; z++) {
                    if (!terrain.IsFilled(x, z, y))
                        continue;

                    vertices[i] = new Vector3(x * tileWidth, y * tileHeight, z * tileDepth);                                // Bottom left
                    vertices[i + 1] = new Vector3(x * tileWidth, y * tileHeight, z * tileDepth + tileDepth);                // Top left
                    vertices[i + 2] = new Vector3(x * tileWidth + tileWidth, y * tileHeight, z * tileDepth + tileDepth);    // Top right
                    vertices[i + 3] = new Vector3(x * tileWidth + tileWidth, y * tileHeight, z * tileDepth);                // Bottom right

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

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        Debug.Log($"Create world mesh took {Time.realtimeSinceStartup - t}s. Created {vertices.Length} vertices and {triangles.Length} indexes");
    }

    public void CreateWorldTiles(Terrain terrain)
    {
        float t = Time.realtimeSinceStartup;

        tiles = new TerrainTile[terrain.GetArraySize()];

        for (int y = 0; y < terrain.height; y++) {
            for (int x = 0; x < terrain.width; x++) {
                for (int z = 0; z < terrain.depth; z++) {
                    TerrainTile tile = Instantiate<TerrainTile>(terrainTilePrefab,  this.transform);
                    tile.transform.localPosition = new Vector3(x * tileWidth, y * tileHeight, z * tileDepth);
                    tile.transform.localRotation = Quaternion.identity;
                    tile.gameObject.name = $"Tile {x}/{z}/{y}";

                    tile.x = x;
                    tile.z = z;
                    tile.y = y;

                    tile.collider = tile.gameObject.GetComponent<BoxCollider>();
                    tile.collider.center = new Vector3(0.5f, 0.5f, 0.5f);
                    tile.collider.size = new Vector3(tileWidth, tileHeight, tileDepth);
                    if (!terrain.IsFilled(x, z, y))
                        tile.collider.enabled = false;

                    tiles[terrain.GetArrayIndex(x, z, y)] = tile;
                }
            }
        }

        Debug.Log($"Create world tiles took {Time.realtimeSinceStartup - t}s. Created {tiles.Length} tiles");
    }

    public TerrainTile GetCollider(Terrain terrain, int x, int z, int y)
    {
        return tiles[terrain.GetArrayIndex(x, z, y)];
    }

    public void UpdateWorld(Terrain terrain, MeshFilter meshFilter)
    {
        meshFilter.mesh = mesh;

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100f)) {
                TerrainTile tile = hit.collider.GetComponent<TerrainTile>();
                if (tile)
                    tileClicked?.Invoke(tile);
            }
        }
    }
}

}
