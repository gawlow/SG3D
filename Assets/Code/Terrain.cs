using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public int GetArraySize()
    {
        return (width * depth * height);
    }

    public int GetArrayIndex(int x, int z, int y)
    {
        return (y * width * height) + (z * width) + x;
    }
}

}

