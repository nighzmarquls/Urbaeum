using System.Collections;
using System.Collections.Generic;
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

public struct UrbScent
{
    public const float ScentDecay = 0.5f;
    const float DecayLimit = 0.0001f;
    public const float ScentDiffusion = 0.95f;
    public const float ScentInterval = 1.2f;
    public const uint MaxTag = (uint)UrbScentTag.MaxScentTag;
    
    public DirtyableTag[] Tags;

    public bool dirty;

    public float this[UrbScentTag i] {
        get { return this[(int)i]; }
        set { this[(int)i] = value;
        }
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

    static ProfilerMarker s_DecayScent_p = new ProfilerMarker("UrbScent.DecayScent");
    public IEnumerator DecayScent()
    {
        s_DecayScent_p.Begin();
        for (int i = 0; i < Tags.Length; i++)
        {
            if (!Tags[i].IsDirty)
            {
                continue;
            }
            
            if (Tags[i].Value > DecayLimit)
            {
                this[i] = Tags[i].Value * ScentDecay;
            }
            else
            {
                this[i] = 0.0f;
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
        DirtyableTag inputTag;
        float diffusedVal;
        for (int i = 0; i < Tags.Length; i++)
        {
            inputTag = input.Tags[i];
            if (!inputTag.IsDirty)
            {
                continue;
            }

            diffusedVal = inputTag.Value * Diffusion;
            if (Tags[i].Value < diffusedVal)
            {
                this[i] = diffusedVal;
            }
        }

        s_ReceiveScent_p.End();
        yield return ScentThrottle.PerformanceThrottle();
    }
}