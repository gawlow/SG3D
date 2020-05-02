using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

public class TerrainRenderer : MonoBehaviour
{
    public int tileWidth = 1;
    public int tileDepth = 1;
    public int tileHeight = 1;

    Mesh mesh;
    BoxCollider[] colliders;

    void Awake()
    {
        mesh = new Mesh();
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

        Debug.Log($"Create world took {Time.realtimeSinceStartup - t}. Created {vertices.Length} vertices and {triangles.Length} indexes");
    }

    public void CreateWorldColliders(Terrain terrain)
    {
        colliders = new BoxCollider[terrain.GetArraySize()];

        for (int y = 0; y < terrain.height; y++) {
            for (int x = 0; x < terrain.width; x++) {
                for (int z = 0; z < terrain.depth; z++) {
                    GameObject go = new GameObject($"Collider {x}/{z}/{y}");
                    go.transform.parent = this.transform;    // Make ourself parent
                    go.transform.position = new Vector3(x * tileWidth, y * tileHeight, z * tileDepth);

                    BoxCollider collider = go.AddComponent<BoxCollider>();
                    collider.size = new Vector3(tileWidth, tileHeight, tileDepth);
                    collider.center = new Vector3(0.5f, 0.5f, 0.5f);
                    if (!terrain.IsFilled(x, z, y))
                        collider.enabled = false;

                    colliders[terrain.GetArrayIndex(x, z, y)] = collider;
                }
            }
        }
    }

    public BoxCollider GetCollider(Terrain terrain, int x, int z, int y)
    {
        return colliders[terrain.GetArrayIndex(x, z, y)];
    }

    public void UpdateWorld(Terrain terrain, MeshFilter meshFilter)
    {
        meshFilter.mesh = mesh;
        // CombineInstance[] combine = new CombineInstance[terrain.width * terrain.depth * terrain.height];
        // Mesh terrainMesh = cube.GetComponent<MeshFilter>().sharedMesh;
        // Transform terrainTransform = cube.GetComponent<Transform>();

        // float t = Time.realtimeSinceStartup;

        // int i = 0;
        // for (int x = 0; x < terrain.width; x++) {
        //     for (int z = 0; z < terrain.depth; z++) {
        //         for (int y = 0; y < terrain.height; y++) {
        //             // combine[i].mesh = terrainMesh;

        //             // terrainTransform.position = new Vector3(x, y, z);
        //             // combine[i].transform = terrainTransform.localToWorldMatrix;
        //             GameObject.Instantiate(terrainCube, new Vector3(x, y, z), Quaternion.identity);
        //             i++;
        //         }
        //     }
        // }

        // Debug.Log($"Creating combine objects took {Time.realtimeSinceStartup - t}");

        // // meshFilter.mesh = new Mesh();
        // // meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        // // meshFilter.mesh.CombineMeshes(combine);
        // // meshFilter.mesh.Optimize();

        // Debug.Log($"Update world took {Time.realtimeSinceStartup - t}");
    }
}

}
