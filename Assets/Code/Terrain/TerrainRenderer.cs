using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace SG3D {

public struct TerrainTypeMaterialInfo {
    public int type;
    public float smoothness;
    public float shaderIndex;
};

// This class is responsible for displaying terrain.
// It takes data from Terrain class and uses it to generate meshes, colliders and
// other things required for user interaction
public class TerrainRenderer : MonoBehaviour
{
    public delegate void OnTileClick(Vector3Int tile);

    public event OnTileClick tileClicked;   // Called when user clicked on a tile

    public Vector3 tileSize = new Vector3(1f, 1f, 1f);

    public int maxJobs = 32;

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

    public bool useNormalMaps;
    public bool useMetallicMaps;
    public bool useOcclusionMaps;
    public int maxConcurrentJobs = 32;

    public Material terrainMaterial;
    Material materialRuntimeCopy;

    public NativeHashMap<int, TerrainTypeMaterialInfo> materialInfo;

    TerrainMeshRendererJobManager meshJobManager;

    public void Initialise(Terrain terrainData)
    {
        this.terrainData = terrainData;
        meshJobManager = new TerrainMeshRendererJobManager();

        PrepareTerrainMaterial();
    }

    void OnDestroy()
    {
        materialInfo.Dispose();
        meshJobManager.Cleanup();
    }

    private void PrepareTerrainMaterial()
    {
        int materialsCount = Enum.GetNames(typeof(TerrainType)).Length;
        materialInfo = new NativeHashMap<int, TerrainTypeMaterialInfo>(materialsCount, Allocator.Persistent);

        // We read format properties from grass texture and assume rest is the same. It better be :)
        Texture2D grassTexture = grassMaterial.GetTexture("_BaseMap") as Texture2D;

        if (useNormalMaps) {
            Texture2D grassNormals = grassMaterial.GetTexture("_BumpMap") as Texture2D;
            terrainNormalMaps = new Texture2DArray(grassNormals.width, grassNormals.height, materialsCount, grassNormals.format, false);
        }

        if (useMetallicMaps) {
            Texture2D grassMetallic = grassMaterial.GetTexture("_MetallicGlossMap") as Texture2D;
            terrainMetallicMaps = new Texture2DArray(grassMetallic.width, grassMetallic.height, materialsCount, grassMetallic.format, false);
        }

        if (useOcclusionMaps) {
            Texture2D grassOcclusion = grassMaterial.GetTexture("_OcclusionMap") as Texture2D;
            terrainOcclusionMaps = new Texture2DArray(grassOcclusion.width, grassOcclusion.height, materialsCount, grassOcclusion.format, false);
        }

        terrainBaseTextures = new Texture2DArray(grassTexture.width, grassTexture.height, materialsCount, grassTexture.format, false);

        ReadMaterialInfo(TerrainType.Grass, grassMaterial);
        ReadMaterialInfo(TerrainType.Dirt, dirtMaterial);
        ReadMaterialInfo(TerrainType.SoftRocks, softRocksMaterial);
        ReadMaterialInfo(TerrainType.HardRocks, hardRocksMaterial);
        ReadMaterialInfo(TerrainType.Sand, sandMaterial);

        // Make a copy so we won't mess with original
        materialRuntimeCopy = new Material(terrainMaterial);
        materialRuntimeCopy.SetTexture("BaseTextures", terrainBaseTextures);

        if (useNormalMaps) {
            materialRuntimeCopy.SetTexture("NormalTextures", terrainNormalMaps);
            materialRuntimeCopy.EnableKeyword("HASNORMALMAP");
        }

        if (useMetallicMaps) {
            materialRuntimeCopy.SetTexture("MetallicTextures", terrainMetallicMaps);
            materialRuntimeCopy.EnableKeyword("HASMETALLICMAP");
        }

        if (useOcclusionMaps) {
            materialRuntimeCopy.SetTexture("OcclusionTextures", terrainOcclusionMaps);
            materialRuntimeCopy.EnableKeyword("HASOCCLUSIONMAP");
        }
    }
    private void ReadMaterialInfo(TerrainType type, Material material)
    {
        Graphics.CopyTexture(material.GetTexture("_BaseMap"), 0, 0, terrainBaseTextures, (int) type, 0);

        if (useNormalMaps)
            Graphics.CopyTexture(material.GetTexture("_BumpMap"), 0, 0, terrainNormalMaps, (int) type, 0);
        
        if (useMetallicMaps)
            Graphics.CopyTexture(material.GetTexture("_MetallicGlossMap"), 0, 0, terrainMetallicMaps, (int) type, 0);

        if (useOcclusionMaps)
            Graphics.CopyTexture(material.GetTexture("_OcclusionMap"), 0, 0, terrainOcclusionMaps, (int) type, 0);

        TerrainTypeMaterialInfo info;
        info.smoothness = (useMetallicMaps) ? material.GetFloat("_Smoothness") : 0f;
        info.type = (int) type;
        info.shaderIndex = (float) type + 0.1f; // +0.1f is to ensure that truncation to int will return correct number in shader
        materialInfo[(int) type] = info;
    }

    public void CreateWorld()
    {
        meshJobManager.Initialize(maxJobs, terrainData, this);

        // Because map can be really large, generating a single mesh is a no-go, as updates to it would take too
        // much time. So we divide world into equaly sized chunks, slicing the world along the X and Z coordinates 
        // (Y is expected to be small anyway). Each chunk then generates its meshes, colliders, etc
        int width = (terrainData.terrainWidth / chunkSize) + ((terrainData.terrainWidth % chunkSize > 0) ? 1 : 0);
        int depth = (terrainData.terrainDepth / chunkSize) + ((terrainData.terrainDepth % chunkSize > 0) ? 1 : 0);

        chunks = new TerrainChunk[width, depth];

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < depth; z++) {
                TerrainChunk chunk = Instantiate<TerrainChunk>(terrainChunkPrefab, this.transform);
                chunk.transform.localPosition = new Vector3(x * chunkSize * tileSize.x, 0f, z * chunkSize * tileSize.z);
                chunk.transform.localRotation = Quaternion.identity;
                chunk.name = $"Chunk X: {x * chunkSize} Z:{z * chunkSize}, size: {chunkSize}";
                chunk.Initialise(terrainData, this, x, z, chunkSize, chunkTextureSize, materialRuntimeCopy);
                chunks[x, z] = chunk;
            }
        }
    }

    // This updates entire world, should be called only on startup really
    public void UpdateWorldMesh()
    {
        // int i = 0;
        for (int x = 0; x < chunks.GetLength(0); x++) {
            for (int z = 0; z < chunks.GetLength(1); z++) {
                ScheduleChunkUpdate(x, z);
            }
        }
    }

    private void ScheduleChunkUpdate(int x, int z)
    {
        meshJobManager.ScheduleJob(new Tuple<int, int>(x, z));
    }
    private void ScheduleChunkUpdateForTile(Vector3Int tile)
    {
        ScheduleChunkUpdate(tile.x / chunkSize, tile.z / chunkSize);
    }

    // Updates mesh for chunk containing given tile
    public void UpdateWorldMeshForTile(Vector3Int tile)
    {
        ScheduleChunkUpdateForTile(tile);

        // Check if tile is at the chunk boundary and refresh neighbours if needed
        int tileX = tile.x % chunkSize;
        int tileZ = tile.z % chunkSize;

        if (tileX == chunkSize - 1 && terrainData.terrainWidth > tile.x + 1) {  // +1 because tiles are indexed from 0
            ScheduleChunkUpdateForTile(new Vector3Int(tile.x + 1, tile.y, tile.z));
        } else if (tileX == 1 && tile.x > 0) {
            ScheduleChunkUpdateForTile(new Vector3Int(tile.x - 1, tile.y, tile.z));
        }

        if (tileZ == chunkSize - 1 && terrainData.terrainDepth > tile.z + 1) {  // +1 because tiles are indexed from 0
            ScheduleChunkUpdateForTile(new Vector3Int(tile.x, tile.y, tile.z + 1));
        } else if (tileZ == 1 && tile.x > 0) {
            ScheduleChunkUpdateForTile(new Vector3Int(tile.x, tile.y, tile.z - 1));
        }
    }

    public TerrainChunk GetChunk(int x, int z)
    {
        return chunks[x, z];
    }

    public TerrainTypeMaterialInfo GetMaterialInfo(TerrainType type)
    {
        return materialInfo[(int) type];
    }

    private TerrainChunk GetChunkForTile(Vector3Int tile)
    {
        return chunks[tile.x / chunkSize, tile.z / chunkSize];
    }

    private Vector3Int WorldToTileCoordinates(Vector3 tile)
    {
        Vector3Int result = new Vector3Int(
            Mathf.FloorToInt(Mathf.Clamp(tile.x, 0f, terrainData.terrainWidth * tileSize.x) / tileSize.x),
            Mathf.FloorToInt(Mathf.Clamp(tile.y, 0f, terrainData.terrainHeight * tileSize.y) / tileSize.y),
            Mathf.FloorToInt(Mathf.Clamp(tile.z, 0f, terrainData.terrainDepth * tileSize.z) / tileSize.z)
        );

        return result;
    }

    void Update()
    {
        meshJobManager.CompleteAndRun();

        // Detection of clicking on tiles
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f)) {
                Vector3Int clickedTile = WorldToTileCoordinates(hit.point);

                // If tile is not present, it means that we probably clicked on tile boundary and need to guess
                // the correct one. Check all neighbours within 0.01f unit of click coordinates
                if (!terrainData.IsPresent(clickedTile)) {
                    float[] offsets = new float[]{-0.01f, 0f, 0.01f};

                    foreach (float x in offsets) {
                        foreach (float y in offsets) {
                            foreach (float z in offsets) {
                                Vector3Int possibleTile = WorldToTileCoordinates(new Vector3(hit.point.x + x, hit.point.y + y, hit.point.z + z));
                                if (terrainData.IsPresent(possibleTile)) {
                                    Debug.Log($"Tile click deduction guessed {possibleTile}");
                                    clickedTile = possibleTile;
                                    goto Out;
                                }
                            }
                        }
                    }
                }

            Out:
                if (!terrainData.IsPresent(clickedTile)) {
                    Debug.Log($"Can't figure out clicked tile correctly for {hit.point}");
                } else {
                    Debug.Log($"Detected hit for {hit.collider.name} at {hit.point} (UV {hit.textureCoord}) = {clickedTile}");
                    tileClicked?.Invoke(clickedTile);
                }
            }
        }
    }

    void LateUpdate()
    {
        meshJobManager.CompleteAndRun();
    }
}

}
