using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace SG3D {

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
    public event TilePresentChange tilePresentChanged;

    // Is tile present at all?
    NativeArray<bool> present;

    public void Generate(int width, int depth, int height)
    {
        terrainWidth = width;
        terrainDepth = depth;
        terrainHeight = height;

        present = new NativeArray<bool>(width * depth * height, Allocator.Persistent);
    }

    public void Cleanup()
    {
        present.Dispose();
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
        if (index >= present.Length)
            Debug.Log($"Setting {tile} to {value}. Index is {index}");

        present[index] = value;
        tilePresentChanged?.Invoke(tile, value);
    }

    public bool IsPresent(int width, int depth, int height)
    {
        if (width >= terrainWidth)
            return false;
        if (depth >= terrainDepth)
            return false;
        if (height >= terrainHeight)
            return false;

        int index = GetArrayIndex(width, depth, height);
        if (index >= present.Length)
            return false;

        return present[index];
    }

    private int GetArrayIndex(int x, int z, int y)
    {
        return (y * terrainWidth * terrainDepth) + (z * terrainWidth) + x;
    }
}

}

