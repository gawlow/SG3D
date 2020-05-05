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

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < depth; z++) {
                terrain.SetType(new Vector3Int(x, 0, z), TerrainType.Grass);
                terrain.SetType(new Vector3Int(x, 1, z), TerrainType.Dirt);
                terrain.SetType(new Vector3Int(x, 2, z), TerrainType.Grass);
            }
        }

        terrainRenderer.tileClicked += OnTileClicked;
        terrain.tilePresentChanged += OnTilePresentChanged;

        StartCoroutine(terrainRenderer.CreateWorld());
        StartCoroutine(terrainRenderer.UpdateWorldMesh());
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
        terrainRenderer.UpdateWorldMeshForTile(tile);
    }
}

}
