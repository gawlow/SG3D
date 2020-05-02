using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    public int x;
    public int z;
    public int size;
    public TerrainLayer terrainLayerPrefab;
    MeshFilter meshFilter;
    Mesh mesh;
    Terrain terrainData;
    new TerrainRenderer renderer;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
    }

    public void Initialise(Terrain terrainData, TerrainRenderer renderer, int x, int z, int size)
    {
        this.renderer = renderer;
        this.terrainData = terrainData;
        this.x = x;
        this.z = z;
        this.size = size;
    }

    public void UpdateMesh()
    {
        float t = Time.realtimeSinceStartup;

        Vector3[] vertices = new Vector3[terrainData.width * terrainData.depth * terrainData.height * 4];
        int[] triangles = new int[terrainData.width * terrainData.depth * terrainData.height * 6];
        int i = 0, j = 0;

        // x/y/z
        for (int y = 0; y < terrainData.height; y++) {
            for (int x = 0; x < size; x++) {
                for (int z = 0; z < size; z++) {
                    if (!terrainData.IsFilled(this.x * size + x, this.z * size + z, y))
                        continue;

                    // Bottom left
                    vertices[i] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

                    // Top left
                    vertices[i + 1] = new Vector3(x * renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

                    // Top right
                    vertices[i + 2] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth + renderer.tileDepth);

                    // Bottom right
                    vertices[i + 3] = new Vector3(x * renderer.tileWidth + renderer.tileWidth, y * renderer.tileHeight, z * renderer.tileDepth);

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

        mesh.Clear();
        mesh.SetVertices(vertices, 0, i);
        mesh.SetTriangles(triangles, 0, j, 0);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        Debug.Log($"Create chunk ({x}x{z}) mesh took {Time.realtimeSinceStartup - t}s. Created {i} vertices and {j} indexes");
    }

    void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        position += new Vector3(size / 2, terrainData.height / 2, size / 2);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(position, new Vector3(size, terrainData.height, size));
    }
}

}
