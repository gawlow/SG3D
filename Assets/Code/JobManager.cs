using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using System;

namespace SG3D {

public abstract class JobManager<T, K> 
    where T : struct, IJob
{
    T[] jobs;
    Nullable<JobHandle>[] jobHandles;
    Queue<int> freeJobs;
    Queue<K> scheduledJobs;
    K[] runningJobParams;

    public void Initialize(int maxJobs)
    {
        jobs = new T[maxJobs];
        runningJobParams = new K[maxJobs];
        jobHandles = new Nullable<JobHandle>[maxJobs];
        scheduledJobs = new Queue<K>(maxJobs);  // Give it some initial capacity

        freeJobs = new Queue<int>(maxJobs);
        for (int i = 0; i < maxJobs; i++) 
            freeJobs.Enqueue(i);
    }

    public void ScheduleJob(K startParams)
    {
        scheduledJobs.Enqueue(startParams);
        StartJob();
    }

    public void CompleteAndRun()
    {
        CompleteJobs();

        while (true) {
            if (!StartJob())
                break;
        }
    }

    private bool StartJob()
    {
        if (freeJobs.Count == 0)
            return false;
        
        if (scheduledJobs.Count == 0)
            return false;

        int index = freeJobs.Dequeue();

        runningJobParams[index] = scheduledJobs.Dequeue();
        OnReady(index, ref jobs[index], runningJobParams[index]);

        jobHandles[index] = jobs[index].Schedule();
        return true;
    }

    private void CompleteJobs()
    {
        for (int i = 0; i < jobHandles.Length; i++) {
            if (!jobHandles[i].HasValue)
                continue;

            JobHandle handle = jobHandles[i].Value;
            if (!handle.IsCompleted)
                continue;
            
            handle.Complete();
            OnComplete(i, ref jobs[i], ref runningJobParams[i]);

            jobHandles[i] = null;
            freeJobs.Enqueue(i);
        }
    }

    public abstract void OnReady(int i, ref T job, K startParams);

    public abstract void OnComplete(int i, ref T job, ref K startParams);
}

}