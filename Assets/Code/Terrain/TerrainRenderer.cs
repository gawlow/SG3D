using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

// This class is responsible for displaying terrain.
// It takes data from Terrain class and uses it to generate meshes, colliders and
// other things required for user interaction
public class TerrainRenderer : MonoBehaviour
{
    public delegate void OnTileClick(Vector3Int tile);

    public event OnTileClick tileClicked;   // Called when user clicked on a tile

    public float tileWidth = 1f;
    public float tileDepth = 1f;
    public float tileHeight = 1f;

    public TerrainChunk terrainChunkPrefab;
    public int chunkSize = 10;
    public int chunkTextureSize = 10;
    public Material grassMaterial;
    public Material dirtMaterial;
    public Material softRocksMaterial;
    public Material hardRocksMaterial;
    public Material sandMaterial;

    TerrainChunk[,] chunks;
    Terrain terrainData;
    Texture2DArray terrainBaseTextures;
    Texture2DArray terrainNormalMaps;
    Texture2DArray terrainMetallicMaps;
    Texture2DArray terrainOcclusionMaps;

    public Material terrainMaterial;

    public void Initialise(Terrain terrainData)
    {
        this.terrainData = terrainData;
        PrepareTextureArrays();
    }

    private void PrepareTextureArrays()
    {
        // We read format properties from grass texture and assume rest is the same. It better be :)
        Texture2D grassTexture = grassMaterial.GetTexture("_BaseMap") as Texture2D;
        Texture2D grassNormals = grassMaterial.GetTexture("_BumpMap") as Texture2D;
        Texture2D grassMetallic = grassMaterial.GetTexture("_MetallicGlossMap") as Texture2D;
        Texture2D grassOcclusion = grassMaterial.GetTexture("_OcclusionMap") as Texture2D;

        terrainBaseTextures = new Texture2DArray(grassTexture.width, grassTexture.height, 5, grassTexture.format, false);
        Graphics.CopyTexture(grassTexture, 0, 0, terrainBaseTextures, (int) TerrainType.Grass - 1, 0);
        Graphics.CopyTexture(dirtMaterial.GetTexture("_BaseMap") as Texture2D, 0, 0, terrainBaseTextures, (int) TerrainType.Dirt - 1, 0);
        Graphics.CopyTexture(softRocksMaterial.GetTexture("_BaseMap") as Texture2D, 0, 0, terrainBaseTextures, (int) TerrainType.SoftRocks - 1, 0);
        Graphics.CopyTexture(hardRocksMaterial.GetTexture("_BaseMap") as Texture2D, 0, 0, terrainBaseTextures, (int) TerrainType.HardRocks - 1, 0);
        Graphics.CopyTexture(sandMaterial.GetTexture("_BaseMap") as Texture2D, 0, 0, terrainBaseTextures, (int) TerrainType.Sand - 1, 0);

        terrainNormalMaps = new Texture2DArray(grassNormals.width, grassNormals.height, 5, grassNormals.format, false);
        Graphics.CopyTexture(grassNormals, 0, 0, terrainNormalMaps, (int) TerrainType.Grass - 1, 0);
        Graphics.CopyTexture(dirtMaterial.GetTexture("_BumpMap") as Texture2D, 0, 0, terrainNormalMaps, (int) TerrainType.Dirt - 1, 0);
        Graphics.CopyTexture(softRocksMaterial.GetTexture("_BumpMap") as Texture2D, 0, 0, terrainNormalMaps, (int) TerrainType.SoftRocks - 1, 0);
        Graphics.CopyTexture(hardRocksMaterial.GetTexture("_BumpMap") as Texture2D, 0, 0, terrainNormalMaps, (int) TerrainType.HardRocks - 1, 0);
        Graphics.CopyTexture(sandMaterial.GetTexture("_BumpMap") as Texture2D, 0, 0, terrainNormalMaps, (int) TerrainType.Sand - 1, 0);

        terrainMetallicMaps = new Texture2DArray(grassMetallic.width, grassMetallic.height, 5, grassMetallic.format, false);
        Graphics.CopyTexture(grassMetallic, 0, 0, terrainMetallicMaps, (int) TerrainType.Grass - 1, 0);
        Graphics.CopyTexture(dirtMaterial.GetTexture("_MetallicGlossMap") as Texture2D, 0, 0, terrainMetallicMaps, (int) TerrainType.Dirt - 1, 0);
        Graphics.CopyTexture(softRocksMaterial.GetTexture("_MetallicGlossMap") as Texture2D, 0, 0, terrainMetallicMaps, (int) TerrainType.SoftRocks - 1, 0);
        Graphics.CopyTexture(hardRocksMaterial.GetTexture("_MetallicGlossMap") as Texture2D, 0, 0, terrainMetallicMaps, (int) TerrainType.HardRocks - 1, 0);
        Graphics.CopyTexture(sandMaterial.GetTexture("_MetallicGlossMap") as Texture2D, 0, 0, terrainMetallicMaps, (int) TerrainType.Sand - 1, 0);

        terrainOcclusionMaps = new Texture2DArray(grassOcclusion.width, grassOcclusion.height, 5, grassOcclusion.format, false);
        Graphics.CopyTexture(grassOcclusion, 0, 0, terrainOcclusionMaps, (int) TerrainType.Grass - 1, 0);
        Graphics.CopyTexture(dirtMaterial.GetTexture("_OcclusionMap") as Texture2D, 0, 0, terrainOcclusionMaps, (int) TerrainType.Dirt - 1, 0);
        Graphics.CopyTexture(softRocksMaterial.GetTexture("_OcclusionMap") as Texture2D, 0, 0, terrainOcclusionMaps, (int) TerrainType.SoftRocks - 1, 0);
        Graphics.CopyTexture(hardRocksMaterial.GetTexture("_OcclusionMap") as Texture2D, 0, 0, terrainOcclusionMaps, (int) TerrainType.HardRocks - 1, 0);
        Graphics.CopyTexture(sandMaterial.GetTexture("_OcclusionMap") as Texture2D, 0, 0, terrainOcclusionMaps, (int) TerrainType.Sand - 1, 0);


        // terrainMaterial.SetTexture("BaseTextures", terrainBaseTextures);

        // Debug.Log($"Format is {texture.format}");
        // terrainBaseTextures = new Texture2DArray(2048, 2048, 5, false);
        var names = grassMaterial.GetTexturePropertyNames();
        for (int i = 0; i < names.Length; i++) {
            Debug.Log($"texture names for grass: {names[i]}");
        }
    }

    public int CreateWorld()
    {
        // Because map can be really large, generating a single mesh is a no-go, as updates to it would take too
        // much time. So we divide world into equaly sized chunks, slicing the world along the X and Z coordinates 
        // (Y is expected to be small anyway). Each chunk then generates its meshes, colliders, etc
        int width = (terrainData.terrainWidth / chunkSize) + ((terrainData.terrainWidth % chunkSize > 0) ? 1 : 0);
        int depth = (terrainData.terrainDepth / chunkSize) + ((terrainData.terrainDepth % chunkSize > 0) ? 1 : 0);

        chunks = new TerrainChunk[width, depth];

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < depth; z++) {
                TerrainChunk chunk = Instantiate<TerrainChunk>(terrainChunkPrefab, this.transform);
                chunk.transform.localPosition = new Vector3(x * chunkSize * tileWidth, 0f, z * chunkSize * tileDepth);
                chunk.transform.localRotation = Quaternion.identity;
                chunk.name = $"Chunk X: {x * chunkSize} Z:{z * chunkSize}, size: {chunkSize}";
                chunk.GetComponent<MeshRenderer>().material = terrainMaterial;
                chunk.GetComponent<MeshRenderer>().material.SetTexture("BaseTextures", terrainBaseTextures);
                chunk.GetComponent<MeshRenderer>().material.SetTexture("NormalTextures", terrainNormalMaps);
                chunk.GetComponent<MeshRenderer>().material.SetTexture("MetallicTextures", terrainMetallicMaps);
                chunk.GetComponent<MeshRenderer>().material.SetTexture("OcclusionTextures", terrainOcclusionMaps);
                chunk.Initialise(terrainData, this, x, z, chunkSize, chunkTextureSize);
                chunk.CreateVoxels();
                chunks[x, z] = chunk;
            }
        }

        return width * depth;
    }

    // This updates entire world, should be called only on startup really
    public void UpdateWorldMesh()
    {
        for (int x = 0; x < chunks.GetLength(0); x++) {
            for (int z = 0; z < chunks.GetLength(1); z++) {
                chunks[x, z].UpdateMesh();
            }
        }
    }

    // Updates mesh for chunk containing given tile
    public void UpdateWorldMeshForTile(Vector3Int tile)
    {
        GetChunkForTile(tile).UpdateMesh();

        // Check if tile is at the chunk boundary and refresh neighbours if needed
        int tileX = tile.x % chunkSize;
        int tileZ = tile.z % chunkSize;

        if (tileX == chunkSize - 1 && terrainData.terrainWidth > tile.x + 1) {  // +1 because tiles are indexed from 0
            GetChunkForTile(new Vector3Int(tile.x + 1, tile.y, tile.z)).UpdateMesh();
        } else if (tileX == 1 && tile.x > 0) {
            GetChunkForTile(new Vector3Int(tile.x - 1, tile.y, tile.z)).UpdateMesh();
        }

        if (tileZ == chunkSize - 1 && terrainData.terrainDepth > tile.z + 1) {  // +1 because tiles are indexed from 0
            GetChunkForTile(new Vector3Int(tile.x, tile.y, tile.z + 1)).UpdateMesh();
        } else if (tileZ == 1 && tile.x > 0) {
            GetChunkForTile(new Vector3Int(tile.x, tile.y , tile.z - 1)).UpdateMesh();
        }
    }

    public TerrainVoxelCollider GetVoxel(Vector3Int tile)
    {
        return GetChunkForTile(tile).GetVoxel(tile);
    }

    private TerrainChunk GetChunkForTile(Vector3Int tile)
    {
        return chunks[tile.x / chunkSize, tile.z / chunkSize];
    }

    void Update()
    {
        // Detection of clicking on tiles
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f)) {
                TerrainVoxelCollider voxel = hit.collider.GetComponent<TerrainVoxelCollider>();
                if (voxel)
                    tileClicked?.Invoke(new Vector3Int(voxel.tileX, voxel.tileY, voxel.tileZ));
            }
        }
    }
}

}
