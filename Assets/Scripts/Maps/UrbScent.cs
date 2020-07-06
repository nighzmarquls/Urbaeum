using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
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

public class UrbScent
{
    public const float ScentDecay = 0.5f;
    const float DecayLimit = 0.0001f;
    public const float ScentDiffusion = 0.95f;
    public const float ScentInterval = 0.9f;
    const uint MaxTag = (uint)UrbScentTag.MaxScentTag;
    float[] Tags;
    bool[] DirtyTags;

    public bool dirty = false;

    public float this[UrbScentTag i] {
        get { return this[(int)i]; }
        set { this[(int)i] = value;
            
        }
    }

    public float this[int i] {
        get { return this.Tags[i]; }
        set {
            this.Tags[i] = value;
            if (value > 0)
            {
                dirty = true;
                DirtyTags[i] = true;
                
                return;
            }
            DirtyTags[i] = false;

        }
    }

    public UrbScent()
    {
        Tags = new float[MaxTag];
        DirtyTags = new bool[MaxTag];
        for (int i = 0; i < MaxTag; i++)
        {
            Tags[i] = 0;
            DirtyTags[i] = false;
        }
    }

    static ProfilerMarker s_DecayScent_p = new ProfilerMarker("UrbScent.DecayScent");
    public IEnumerator DecayScent()
    {
        s_DecayScent_p.Begin();
        for (int i = 0; i < MaxTag; i++)
        {
            if (DirtyTags[i])
            {
                if (Tags[i] > DecayLimit)
                {
                    this[i] = Tags[i] * ScentDecay;
                }
                else
                {
                    this[i] = 0.0f;
                }
                
            }
        }
        s_DecayScent_p.End();
        yield return ScentThrottle.PerformanceThrottle();
    }

    public static UrbThrottle ScentThrottle = new UrbThrottle(7);

    static ProfilerMarker s_ReceiveScent_p = new ProfilerMarker("UrbScent.ReceiveScent");

    public IEnumerator ReceiveScent(UrbScent input, float Diffusion = 1.0f)
    {
        s_ReceiveScent_p.Begin();
        for (int i = 0; i < MaxTag; i++)
        {
            if (!input.DirtyTags[i])
            {
                continue;
            }
            
            if (Tags[i] < input.Tags[i] * Diffusion)
            {
                this[i] = input.Tags[i] * Diffusion;
            }
        }

        s_ReceiveScent_p.End();
        yield return ScentThrottle.PerformanceThrottle();
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
