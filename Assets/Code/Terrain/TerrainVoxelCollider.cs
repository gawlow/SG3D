using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG3D {

// Just a dummy container for BoxCollider, that needs to be associated with
// living GameObject, so here we are...
public class TerrainVoxelCollider : MonoBehaviour
{
    public int tileX;
    public int tileZ;
    public int tileY;
    new public BoxCollider collider;
}

}
