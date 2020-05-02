using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace SG3D {

public struct Vexel
{
    bool filled;
}

public class Terrain
{
    public int width;   // x-axis
    public int depth;  // z-axis
    public int height;  // y-axis

    NativeArray<bool> filled;

    public void Generate(int width, int depth, int height) {
        this.width = width;
        this.depth = depth;
        this.height = height;

        filled = new NativeArray<bool>(width * depth * height, Allocator.Persistent);
    }

    public void Cleanup()
    {
        filled.Dispose();
    }

    public void SetFilled(bool value)
    {
        for (int i = 0, max = filled.Length; i < max; i++) {
            filled[i] = value;
        }
    }

    public void SetFilled(bool value, int width, int depth, int height)
    {
        filled[GetArrayIndex(width, depth, height)] = value;
    }

    public bool IsFilled(int width, int depth, int height)
    {
        return filled[GetArrayIndex(width, depth, height)];
    }

    public int GetArraySize()
    {
        return (width * depth * height);
    }

    public int GetArrayIndex(int x, int z, int y)
    {
        return (y * width * depth) + (z * width) + x;
    }
}

}

