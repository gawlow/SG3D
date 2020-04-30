using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    SG3D.Terrain terrain;
    SG3D.TerrainRenderer terrainRenderer;

    MeshFilter meshFilter;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        terrain = new SG3D.Terrain();
        terrainRenderer = new SG3D.TerrainRenderer();
    }

    // Start is called before the first frame update
    void Start()
    {
        terrain.width = 100;
        terrain.height = 5;
        terrain.depth = 100;

        terrainRenderer.CreateWorld(terrain, transform);
        terrainRenderer.UpdateWorld(terrain, meshFilter);
    }

}
