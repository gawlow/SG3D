using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

[RequireComponent(typeof(SG3D.TerrainRenderer))]
public class TerrainGenerator : MonoBehaviour
{

    SG3D.Terrain terrain;
    SG3D.TerrainRenderer terrainRenderer;

    void Awake()
    {
        terrainRenderer = GetComponent<SG3D.TerrainRenderer>();
        terrain = new SG3D.Terrain();
        terrainRenderer.Initialise(terrain);
    }

    // Start is called before the first frame update
    void Start()
    {
        terrain.Generate(100, 100, 2);
        terrain.SetFilled(true);
        terrain.SetFilled(false, 0, 1, 0);
        terrain.SetFilled(false, 0, 2, 0);

        int chunks = terrainRenderer.CreateWorldMesh();
        Debug.Log($"Created {chunks} terrain chunks");

        terrainRenderer.CreateLevelColliders();
        terrainRenderer.UpdateWorldMesh();
        terrainRenderer.tileClicked += OnTileClicked;
    }

    void OnDestroy() {
        // Required for NativeArray cleanup
        terrainRenderer.tileClicked -= OnTileClicked;
        terrain.Cleanup();
    }

    public void OnTileClicked(int x, int z, int y) {
        // tileInfo.collider.enabled = false;

        terrain.SetFilled(false, x, z, y);
        terrainRenderer.UpdateWorldMeshForTile(x, z, y);
    }
}

}
