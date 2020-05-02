using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

[RequireComponent(typeof(SG3D.TerrainRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainGenerator : MonoBehaviour
{

    SG3D.Terrain terrain;
    SG3D.TerrainRenderer terrainRenderer;

    MeshFilter meshFilter;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        terrainRenderer = GetComponent<SG3D.TerrainRenderer>();
        terrain = new SG3D.Terrain();
        terrainRenderer.Initialise(terrain);
    }

    // Start is called before the first frame update
    void Start()
    {
        terrain.Generate(100, 100, 1);
        terrain.SetFilled(true);
        terrain.SetFilled(false, 0, 1, 0);
        terrain.SetFilled(false, 0, 2, 0);

        int chunks = terrainRenderer.CreateWorldMesh();
        Debug.Log($"Created {chunks} terrain chunks");

        terrainRenderer.CreateWorldTiles();
        terrainRenderer.UpdateWorldMesh();
        terrainRenderer.tileClicked += OnTileClicked;
    }

    void OnDestroy() {
        // Required for NativeArray cleanup
        terrainRenderer.tileClicked -= OnTileClicked;
        terrain.Cleanup();
    }

    public void OnTileClicked(TerrainTile tileInfo) {
        tileInfo.collider.enabled = false;

        terrain.SetFilled(false, tileInfo.x, tileInfo.z, tileInfo.y);
        terrainRenderer.UpdateWorldMeshForTile(tileInfo.x, tileInfo.z, tileInfo.y);
    }
}

}
