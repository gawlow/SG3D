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
//
// This class is suitable to use in Unity Jobs system
public struct TerrainInfo
{
    public int width;   // x-axis
    public int depth;  // z-axis
    public int height;  // y-axis

    [ReadOnly] public NativeArray<bool> present;
    // Terrain fill type
    [ReadOnly] public NativeArray<TerrainType> type;

    public void Initialize(int width, int depth, int height)
    {
        this.width = width;
        this.depth = depth;
        this.height = height;

        present = new NativeArray<bool>(width * depth * height, Allocator.Persistent);
        type = new NativeArray<TerrainType>(width * depth * height, Allocator.Persistent);
    }

    public void Cleanup()
    {
        present.Dispose();
        type.Dispose();
    }

    public bool IsPresent(Vector3Int tile)
    {
        if (tile.x < 0 || tile.x >= width)
            return false;
        if (tile.z < 0 || tile.z >= depth)
            return false;
        if (tile.y < 0 || tile.y >= height)
            return false;

        int index = GetArrayIndex(tile);
        if (index >= present.Length)
            return false;

        return present[index];
    }

    public void SetPresentAll(bool value)
    {
        for (int i = 0, max = present.Length; i < max; i++) {
            present[i] = value;
        }
    }

    public void SetPresent(Vector3Int tile, bool value)
    {
        int index = GetArrayIndex(tile);
        Debug.Assert(index < present.Length);

        present[index] = value;
    }

    public TerrainType GetType(Vector3Int tile)
    {
        return type[GetArrayIndex(tile)];
    }

    private int GetArrayIndex(Vector3Int tile)
    {
        return (tile.y * width * depth) + (tile.z * width) + tile.x;
    }

    public void SetType(Vector3Int tile, TerrainType value)
    {
        int index = GetArrayIndex(tile);
        Debug.Assert(index < present.Length, tile);

        type[index] = value;
    }
}

// This class is wrapper around TerrainInfo class that holds additional reference types
// used for handling events and notifications
public class Terrain
{
    public int width;   // x-axis
    public int depth;  // z-axis
    public int height;  // y-axis

    public delegate void TilePresentChange(Vector3Int tile, bool value);
    public delegate void TileTypeChange(Vector3Int tile, TerrainType value);
    public event TilePresentChange tilePresentChanged;
    public event TileTypeChange tileTypeChanged;

    TerrainInfo info;

    public TerrainInfo GetTerrainInfo()
    {
        return info;
    } 

    public void Generate(int width, int depth, int height)
    {
        this.width = width;
        this.depth = depth;
        this.height = height;

        info = new TerrainInfo();
        info.Initialize(width, depth, height);
    }

    public void Cleanup()
    {
        info.Cleanup();
    }

    public Vector3Int GetWorldSize()
    {
        return new Vector3Int(info.width, info.height, info.depth);
    }

    // Use for init, does not invoke callback
    public void SetPresent(bool value)
    {
        info.SetPresentAll(value);
    }

    public void SetPresent(Vector3Int tile, bool value)
    {
        info.SetPresent(tile, value);
        tilePresentChanged?.Invoke(tile, value);
    }

    public bool IsPresent(Vector3Int tile)
    {
        return info.IsPresent(tile);
    }

    public void SetType(Vector3Int tile, TerrainType value)
    {
        info.SetType(tile, value);
        tileTypeChanged?.Invoke(tile, value);
    }
}


}

