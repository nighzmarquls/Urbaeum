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
    public bool dirty;
}

//If we move to ECS for sure, This would be inheriting a SystemBase
public class UrbScent
{
    public const float ScentDecay = 0.5f;
    public const float DecayLimit = 0.0001f;
    public const float ScentDiffusion = 0.95f;
    public const float ScentInterval = 0.9f;
    public const uint MaxTag = (uint)UrbScentTag.MaxScentTag;

    public NativeArray<scentTagComponent> tagList;
    
    //Can't nest Native components together. 
    public NativeArray<scentTagComponent> input;

    public float DiffusionRate = ScentDiffusion;
    public bool dirty = false;
    public bool hasInput = false;
    
    protected JobHandle currentDecayJob;
    protected JobHandle currentRcvScentJob;
    
    DecayScentJob decayJob;
    PassScentJob receiveScentJob;
    
    public UrbScent()
    {
        tagList = new NativeArray<scentTagComponent>((int)MaxTag, Allocator.Persistent);
        input = new NativeArray<scentTagComponent>((int)MaxTag, Allocator.Persistent);
        decayJob = new DecayScentJob()
        {
            scentTag = tagList,
        };
        receiveScentJob = new PassScentJob();
        receiveScentJob.Diffusion = DiffusionRate;
        
        currentDecayJob = new JobHandle();
        currentRcvScentJob = new JobHandle();
    }
    
    //protected override void when doing this the "correct" way.
    public void OnUpdate()
    {
        if (!currentDecayJob.IsCompleted)
        {
            currentDecayJob.Complete();
        }

        currentDecayJob = decayJob.Schedule(tagList.Length, 12);
        
        if (hasInput)
        {
            hasInput = false;
            receiveScentJob.Diffusion = DiffusionRate;
            receiveScentJob.input.CopyFrom(input);
            currentRcvScentJob = receiveScentJob.Schedule(input.Length, 12);
        }
        
        JobHandle.ScheduleBatchedJobs();
    }

    public IEnumerator ReceiveScent(NativeArray<scentTagComponent> newInput, float diffusion)
    {
        if (currentRcvScentJob.IsCompleted == false || hasInput)
        {
            yield return new WaitUntil(() => ((!hasInput) && currentRcvScentJob.IsCompleted));
        }
        
        DiffusionRate = diffusion;
        input.CopyFrom(newInput);
        hasInput = true;
    }
    
    [BurstCompile]
    public struct DecayScentJob : IJobParallelFor
    {
        public NativeArray<scentTagComponent> scentTag;
        public void Execute(int index)
        {
            var tag = scentTag[index];
            if (!tag.dirty)
            {
                return;
            }
        
            if (tag.value > DecayLimit)
            {
                tag.value *= ScentDecay;
            }
            else
            {
                tag.value = 0.0f;
                tag.dirty = false;
            }
        }
    }

    [BurstCompile]
    public struct PassScentJob : IJobParallelFor
    {
        [ReadOnly] public float Diffusion;
    
        [ReadOnly] public NativeArray<scentTagComponent> input;
        public NativeArray<scentTagComponent> output;

        public void Execute(int i)
        {
            float diffused = 0.0f;
            scentTagComponent current;

            var tag = input[i];

            if (!tag.dirty)
            {
                return;
            }

            diffused = tag.value * Diffusion;
            current = output[i];

            if (current.value < diffused)
            {
                current.value = diffused;
            }

            output[i] = current;
        }
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

    public UrbScentTag[] KnownScents
    {
        get
        {
            return ScentIndexes.ToArray();
        }
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
