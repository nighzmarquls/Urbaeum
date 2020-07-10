using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UrbUtility;


public enum UrbScentTag
{
    Goal = 0,
    Plant,
    Meat,
    Sweet,
    Fluff,
    Male,
    Female,
    Violence,
    TestScent = 250,
    MaxScentTag
}

public struct scentTagComponent : IComponentData
{
    public float value;
}

//If we move to ECS for sure, This would be inheriting a SystemBase
public class UrbScent : IDisposable
{
    public const float ScentDecay = 0.5f;
    public const float DecayLimit = 0.001f;
    public const float ScentDiffusion = 0.95f;
    public const float ScentInterval = 0.9f;
    public const int MaxTag = (int)UrbScentTag.MaxScentTag;

    //A separate container specifically for reading from the tagList
    public NativeArray<scentTagComponent> readOnlyList;
    
    public NativeList<UrbScentTag> scentIndexes;
    public NativeArray<scentTagComponent> tagList;
    
    //Can't nest Native components together. 
    NativeArray<scentTagComponent> input;

    public float DiffusionRate = ScentDiffusion;
    public bool dirty = false;
    public bool hasInput = false;
    
    protected float lastDecayTime = 0.0f;

    protected JobHandle currentDecayJob;
    protected JobHandle currentRcvScentJob;
    
    DecayScentJob decayJob;
    ReceiveScentJob receiveScentJob;
    
    public UrbScent()
    {
        readOnlyList = new NativeArray<scentTagComponent>(MaxTag, Allocator.Persistent);
        tagList = new NativeArray<scentTagComponent>(MaxTag, Allocator.Persistent);
        input = new NativeArray<scentTagComponent>(MaxTag, Allocator.Persistent);
        scentIndexes = new NativeList<UrbScentTag>(5, Allocator.Persistent);

        decayJob = new DecayScentJob();
        decayJob.scentTag = tagList;
        
        receiveScentJob = new ReceiveScentJob();
        receiveScentJob.Diffusion = DiffusionRate;
        receiveScentJob.output = tagList;
        // receiveScentJob.input = new NativeArray<scentTagComponent>(MaxTag, Allocator.Persistent);
            
        currentDecayJob = new JobHandle();
        currentRcvScentJob = new JobHandle();
    }

    public int FindScent(UrbScentTag tag)
    {
        for (int i = 0; i < scentIndexes.Length; i++)
        {
            if (scentIndexes[i] == tag)
            {
                return i;
            }
        }

        return -1;
    }

    //protected override void when doing this the "correct" way.
    public void OnUpdate()
    {
        if (Time.fixedTime - lastDecayTime > ScentInterval)
        {
            currentDecayJob.Complete();
            currentRcvScentJob.Complete();
            
            dirty = false;
            dirty |= decayJob.dirty;
            dirty |= receiveScentJob.dirty;
            decayJob.dirty = false;
            receiveScentJob.dirty = false;
            
            readOnlyList.CopyFrom(tagList);

            decayJob.scentTag = tagList;
            currentDecayJob = decayJob.Schedule(tagList.Length, 12);
            lastDecayTime = Time.fixedTime;
            goto scheduleJob;
        }

        if (hasInput)
        {
            currentDecayJob.Complete();
            currentRcvScentJob.Complete();
            
            dirty = false;
            dirty |= decayJob.dirty;
            dirty |= receiveScentJob.dirty;
            decayJob.dirty = false;
            receiveScentJob.dirty = false;
            
            receiveScentJob.Diffusion = DiffusionRate;
            input.CopyTo(receiveScentJob.input);
            currentRcvScentJob = receiveScentJob.Schedule(input.Length, 12);
            hasInput = false;
        }
        
        scheduleJob:        
        JobHandle.ScheduleBatchedJobs();
    }
    
    public IEnumerator ReceiveScent(NativeArray<scentTagComponent> newInput, float diffusion)
    {
        yield return new WaitUntil(() => (hasInput == false));
        yield return new WaitUntil(() => (currentRcvScentJob.IsCompleted));
        yield return new WaitUntil(() => (currentDecayJob.IsCompleted));
        
        currentRcvScentJob.Complete();
        currentDecayJob.Complete();
        
        dirty = true;
        hasInput = true;
        
        receiveScentJob.Diffusion = diffusion;
        input.CopyFrom(newInput);
    }
    
    [BurstCompile]
    public struct DecayScentJob : IJobParallelFor
    {
        public bool dirty;
        public NativeArray<scentTagComponent> scentTag;
        
        public void Execute(int index)
        {
            var tag = scentTag[index];
            
            if (tag.value > DecayLimit)
            {
                tag.value *= ScentDecay;
                dirty = true;
            }
            else
            {
                tag.value = 0.0f;
            }

            scentTag[index] = tag;
        }
    }

    [BurstCompile]
    public struct ReceiveScentJob : IJobParallelFor
    {
        [ReadOnly] public float Diffusion;
        [ReadOnly] public NativeArray<scentTagComponent> input;
        public bool dirty;
        public NativeArray<scentTagComponent> output;

        public void Execute(int i)
        {
            float diffused = 0.0f;
            scentTagComponent current;

            var tag = input[i];

            if (tag.value <= 0)
            {
                return;
            }

            diffused = tag.value * Diffusion;
            current = output[i];

            if (current.value < diffused)
            {
                current.value = diffused;
                dirty = true;
            }

            output[i] = current;
        }
    }

    public void Dispose()
    {
        currentDecayJob.Complete();
        currentRcvScentJob.Complete();
        
        readOnlyList.Dispose();
        scentIndexes.Dispose();
        tagList.Dispose();
        input.Dispose();
    }
}


//This class should be of typename T but there's no way to enforce
//that T is always compatible with basic arithmetic ops
//so I'm assuming that we only ever want floats as our values
//this can't possibly go wrong.
public class ScentList
{
    //The index of the ScentTag is maps 1-for-1 the index in the Values List.
    List<UrbScentTag> ScentIndexes;
    public List<float> Values { get; protected set; }
    public bool HasScents { get; protected set; }

    public ScentList(int Capacity = 0)
    {
        ScentIndexes = new List<UrbScentTag>(Capacity);
        Values = new List<float>(Capacity);
    }
    
    public UrbScentTag GetScentTag(int idx)
    {
        if (ScentIndexes.Count == 0 || !HasScents || idx >= ScentIndexes.Count)
        {
            return UrbScentTag.MaxScentTag;
        }

        return ScentIndexes[idx];
    }
    
    public void AddScent(UrbScentTag tag, float value)
    {
        if (value > 0)
        {
            HasScents = true;
        }

        var idx = ScentIndexes.IndexOf(tag);
        if (idx < 0)
        {
            ScentIndexes.Add(tag);
            Values.Add(value);
            return;
        }
        
        Values[idx] += value;
    }

    public void DetractScent(UrbScentTag tag, float value = 0.0f)
    {
        var idx = ScentIndexes.IndexOf(tag);
        if (idx < 0)
        {
            return;
        }

        var newVal = Values[idx] = value;

        if (newVal < 0)
        {
            newVal = 0.0f;
        }
        
        Values[idx] = newVal;
    }

    public void ClearValues()
    {
        for (int i = 0; i < Values.Count; ++i)
        {
            Values[i] = 0;
        }

        HasScents = false;
    }
}
