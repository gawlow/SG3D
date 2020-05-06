using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace SG3D {

// This class is responsible for creating meshes and all other components
// required to display given world chunk
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    public int chunkX;          // chunk X
    public int chunkZ;          // chunk Z
    public int size;            // horizontal dimensions of chunk
    public int textureSize;   // spread texture over how many tiles

    MeshFilter meshFilter;
    Terrain terrain;
    new TerrainRenderer renderer;

    MeshCollider meshCollider;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    public void Initialise(Terrain terrain, TerrainRenderer renderer, int x, int z, int size, int textureSize, Material material)
    {
        this.renderer = renderer;
        this.terrain = terrain;
        this.chunkX = x;
        this.chunkZ = z;
        this.size = size;
        this.textureSize = textureSize;

        GetComponent<MeshRenderer>().material = material;
    }

    public void ApplyMesh(Mesh mesh)
    {
        meshFilter.mesh = null;
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        position += new Vector3(size / 2 * renderer.tileSize.x, renderer.tileSize.y / 2 * terrain.height, size / 2 * renderer.tileSize.z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(position, new Vector3(size * renderer.tileSize.x, terrain.height * renderer.tileSize.y, size * renderer.tileSize.z));
    }
}

}
