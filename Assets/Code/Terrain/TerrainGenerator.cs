using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

    // Start is called before the first frame update
    void Start()
    {
        terrain.Generate(20, 20, 2);
        terrain.SetFilled(true);
        terrain.SetFilled(false, 0, 1, 0);
        terrain.SetFilled(false, 0, 2, 0);

        terrainRenderer.CreateWorldMesh(terrain);
        terrainRenderer.CreateWorldTiles(terrain);
        terrainRenderer.UpdateWorld(terrain, meshFilter);
    }

    void Update()
    {
    }
}
