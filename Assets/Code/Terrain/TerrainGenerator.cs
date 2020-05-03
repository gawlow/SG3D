using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

[RequireComponent(typeof(SG3D.TerrainRenderer))]
public class TerrainGenerator : MonoBehaviour
{
    SG3D.Terrain terrain;
    SG3D.TerrainRenderer terrainRenderer;

    public int width;
    public int depth;
    public int height;

    void Awake()
    {
        terrainRenderer = GetComponent<SG3D.TerrainRenderer>();
        terrain = new SG3D.Terrain();
        terrainRenderer.Initialise(terrain);
    }

    void Start()
    {
        // Some sample terrain
        terrain.Generate(width, depth, height);
        terrain.SetPresent(true);
        // terrain.SetPresent(new Vector3Int(1, 0, 0), false);
        // terrain.SetPresent(new Vector3Int(2, 0, 0), false);

        int chunks = terrainRenderer.CreateWorld();
        Debug.Log($"Created {chunks} terrain chunks");

        terrainRenderer.UpdateWorldMesh();
        terrainRenderer.tileClicked += OnTileClicked;
        terrain.tilePresentChanged += OnTilePresentChanged;
    }

    void OnDestroy() {
        // Required for NativeArray cleanup
        terrainRenderer.tileClicked -= OnTileClicked;
        terrain.tilePresentChanged -= OnTilePresentChanged;
        terrain.Cleanup();
    }

    public void OnTileClicked(Vector3Int tile)
    {
        terrain.SetPresent(tile, false);
    }

    public void OnTilePresentChanged(Vector3Int tile, bool value)
    {
        terrainRenderer.GetVoxel(tile).collider.enabled = value;
        terrainRenderer.UpdateWorldMeshForTile(tile);
    }
}

}
