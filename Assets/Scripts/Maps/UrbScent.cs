using System.Collections;
using System.Collections.Generic;
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

public struct DirtyableTag
{
    public float Value;
    public bool IsDirty;
}

public struct UrbScent : ISharedComponentData
{
    public const float ScentDecay = 0.5f;
    public const float DecayLimit = 0.0001f;
    public const float ScentDiffusion = 0.95f;
    public const float ScentInterval = 1.2f;
    public const uint MaxTag = (uint)UrbScentTag.MaxScentTag;
    
    public DirtyableTag[] Tags;
    public bool dirty;

    public float this[UrbScentTag i] {
        get { return this[(int)i]; }
        set { this[(int)i] = value; }
    }

    public float this[int i] {
        get { return Tags[i].Value; }
        set {
            Tags[i].Value = value;
            if (value > 0)
            {
                dirty = true;
                Tags[i].IsDirty = true;
                return;
            }
            Tags[i].IsDirty = false;
        }
    }
}


[UpdateBefore(typeof(DecayScentJob))]
public struct ReceiveScentJob : IJobParallelFor
{
    public NativeArray<UrbScent> passTo;
    [ReadOnly]
    public NativeArray<UrbScent> toPass;
    public float diffusion;
    public void Execute(int index)
    {
        var scent = passTo[index];
        if (!scent.dirty)
        {
            return;
        }
        
        var tags = scent.Tags;
        DirtyableTag inputTag;
        float diffusedVal;
        for (int i = 0; i < tags.Length; i++)
        {
            inputTag = toPass[index].Tags[i];
            if (!inputTag.IsDirty)
            {
                continue;
            }

            diffusedVal = inputTag.Value * diffusion;
            if (tags[i].Value < diffusedVal)
            {
                scent[i] = diffusedVal;
            }
        }
    }
}

[UpdateAfter(typeof(ReceiveScentJob))]
public struct DecayScentJob : IJobParallelFor
{
    public NativeArray<UrbScent> scents;
    public void Execute(int index)
    {
        var scent = scents[index];
        if (!scent.dirty)
        {
            return;
        }
        
        var tags = scent.Tags;
        DirtyableTag tag; 
        for (int i = 0; i < tags.Length; i++)
        {
            tag = tags[i];
            if (!tag.IsDirty)
            {
                continue;
            }

            if (tag.Value > UrbScent.DecayLimit)
            {
                scent[i] = tag.Value * UrbScent.ScentDecay;
            }
            else
            {
                scent[i] = 0.0f;
            }
        }

        scent.dirty = false;
        scents[index] = scent;
    }
}