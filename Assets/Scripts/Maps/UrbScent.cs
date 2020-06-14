﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

    public IEnumerator DecayScent()
    {
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
        yield return ScentThrottle.PerformanceThrottle();
    }

    public static UrbUtility.UrbThrottle ScentThrottle = new UrbUtility.UrbThrottle(7);

    public IEnumerator ReceiveScent(UrbScent input, float Diffusion = 1.0f)
    {
        for (int i = 0; i < MaxTag; i++)
        {
            if (input.DirtyTags[i])
            {
                if (Tags[i] < input.Tags[i] * Diffusion)
                {
                    this[i] = input.Tags[i] * Diffusion;
                }
            }
        }
        yield return ScentThrottle.PerformanceThrottle();
    }
}