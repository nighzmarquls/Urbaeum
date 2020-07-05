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

public struct ScentTag : IBufferElementData
{
    public float Value;
    public bool IsDirty;

    public void SetValue(float val)
    {
        Value = val;
        IsDirty = val > 0;
    }
}

public struct UrbScent
{
    public const float ScentDecay = 0.5f;
    // public const float DecayLimit = 0.0001f;
    public const float ScentDiffusion = 0.95f;
    public const float ScentInterval = 1.2f;
    
    public ScentTag[] Tags;

    public float this[UrbScentTag i] {
        get { return Tags[(int)i].Value; }
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
        var tags = scent.Tags;
        ScentTag inputTag;
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
                scent.Tags[i].SetValue(diffusedVal);
            }

            passTo[index] = scent;
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
        var tags = scent.Tags;
        ScentTag tag; 
        for (int i = 0; i < tags.Length; i++)
        {
            tag = tags[i];
            if (!tag.IsDirty)
            {
                continue;
            }

            scent.Tags[i].SetValue(tag.Value * UrbScent.ScentDecay);
        }

        scents[index] = scent;
    }
}