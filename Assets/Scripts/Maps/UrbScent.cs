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

public class UrbScent
{
    public const float ScentDecay = 0.5f;
    const float DecayLimit = 0.0001f;
    public const float ScentDiffusion = 0.95f;
    public const float ScentInterval = 1.2f;
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