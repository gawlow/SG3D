using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace SG3D {

public enum TerrainType {
    Grass = 0,
    Dirt = 1,
    SoftRocks = 2,
    HardRocks = 3,
    Sand = 4,
};

// This class holds data about entire terrain in SOA layout.
// Terrain is a collection of cubes identified by discrete X/Y/Z (although passing convention is more often X/Z/Y).
// A single terrain cube is often refered to as 'tile' here and 'voxel' in visual layer.
// 
// Data is stored in NativeArrays and multidimentional coordinates are converted to linear index.
// The idea is to store all data here in format suitable for multithreaded jobs and do minimal required
// logic in GameObjects.
public class Terrain
{
    public int terrainWidth;   // x-axis
    public int terrainDepth;  // z-axis
    public int terrainHeight;  // y-axis

    public delegate void TilePresentChange(Vector3Int tile, bool value);
    public delegate void TileTypeChange(Vector3Int tile, TerrainType value);
    public event TilePresentChange tilePresentChanged;
    public event TileTypeChange tileTypeChanged;

    // Is tile present at all?
    NativeArray<bool> present;
    NativeArray<TerrainType> type;

    public void Generate(int width, int depth, int height)
    {
        terrainWidth = width;
        terrainDepth = depth;
        terrainHeight = height;

        present = new NativeArray<bool>(width * depth * height, Allocator.Persistent);
        type = new NativeArray<TerrainType>(width * depth * height, Allocator.Persistent);
    }

    public void Cleanup()
    {
        present.Dispose();
        type.Dispose();
    }

    // Use for init, does not invoke callback
    public void SetPresent(bool value)
    {
        for (int i = 0, max = present.Length; i < max; i++) {
            present[i] = value;
        }
    }

    public void SetPresent(Vector3Int tile, bool value)
    {
        int index = GetArrayIndex(tile.x, tile.z, tile.y);
        Debug.Assert(index < present.Length);

        present[index] = value;
        tilePresentChanged?.Invoke(tile, value);
    }

    public bool IsPresent(int width, int depth, int height)
    {
        if (width < 0 || width >= terrainWidth)
            return false;
        if (depth < 0 || depth >= terrainDepth)
            return false;
        if (height < 0 || height >= terrainHeight)
            return false;

        int index = GetArrayIndex(width, depth, height);
        if (index >= present.Length)
            return false;

        return present[index];
    }

    public void SetType(TerrainType value)
    {
        for (int i = 0, max = present.Length; i < max; i++) {
            type[i] = value;
        }
    }

    public void SetType(Vector3Int tile, TerrainType value)
    {
        int index = GetArrayIndex(tile.x, tile.z, tile.y);
        Debug.Assert(index < present.Length);

        type[index] = value;
        tileTypeChanged?.Invoke(tile, value);
    }

    public TerrainType GetType(int width, int depth, int height)
    {
        return type[GetArrayIndex(width, depth, height)];
    }

    private int GetArrayIndex(int x, int z, int y)
    {
        return (y * terrainWidth * terrainDepth) + (z * terrainWidth) + x;
    }
}

}

