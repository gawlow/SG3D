using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

public class TerrainRenderer
{
    GameObject cube;

    GameObject[] cubes;

    public void CreateWorld(Terrain terrain, Transform parent)
    {
        float t = Time.realtimeSinceStartup;

        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.SetActive(false);

        cubes = new GameObject[terrain.GetArraySize()];

        for (int x = 0; x < terrain.width; x++) {
            for (int z = 0; z < terrain.depth; z++) {
                for (int y = 0; y < terrain.height; y++) {
                    GameObject obj = new GameObject($"Cube {x}/{y}/{z}");
                    obj.transform.parent = parent;
                    obj.transform.position = new Vector3(x, y, z);

                    BoxCollider collider = obj.AddComponent<BoxCollider>();
                    collider.size = new Vector3(1f, 1f, 1f);

                    cubes[terrain.GetArrayIndex(x, z, y)] = obj;
                }
            }
        }

        Debug.Log($"Create world took {Time.realtimeSinceStartup - t}");
    }

    public void UpdateWorld(Terrain terrain, MeshFilter meshFilter)
    {
        CombineInstance[] combine = new CombineInstance[terrain.width * terrain.depth * terrain.height];
        Mesh terrainMesh = cube.GetComponent<MeshFilter>().sharedMesh;
        Transform terrainTransform = cube.GetComponent<Transform>();

        float t = Time.realtimeSinceStartup;

        int i = 0;
        for (int x = 0; x < terrain.width; x++) {
            for (int z = 0; z < terrain.depth; z++) {
                for (int y = 0; y < terrain.height; y++) {
                    combine[i].mesh = terrainMesh;

                    terrainTransform.position = new Vector3(x, y, z);
                    combine[i].transform = terrainTransform.localToWorldMatrix;
                    i++;
                }
            }
        }

        Debug.Log($"Creating combine objects took {Time.realtimeSinceStartup - t}");

        meshFilter.mesh = new Mesh();
        meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh.CombineMeshes(combine);
        meshFilter.mesh.Optimize();

        Debug.Log($"Update world took {Time.realtimeSinceStartup - t}");
    }
}

}
