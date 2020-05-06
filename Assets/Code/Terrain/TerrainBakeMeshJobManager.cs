using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

namespace SG3D
{

struct BakeMeshJob : IJob
{
    public int meshID;

    public void Execute()
    {
        Physics.BakeMesh(meshID, false);
    }
}

class TerrainBakeMeshJobManager : JobManager<BakeMeshJob, TerrainBakeMeshJobManager.JobParams>
{
    public struct JobParams {
        public Mesh mesh;
        public TerrainChunk chunk;
    }

    public override void OnReady(int i, ref BakeMeshJob job, TerrainBakeMeshJobManager.JobParams startParams)
    {
        job.meshID = startParams.mesh.GetInstanceID();
    }

    public override void OnComplete(int i, ref BakeMeshJob job, TerrainBakeMeshJobManager.JobParams startParams)
    {
        startParams.chunk.ApplyMesh();
    }
}

}