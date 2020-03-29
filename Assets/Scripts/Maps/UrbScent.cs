using System.Collections;
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
    MaxScentTag
}

public class UrbScent
{
    public const float ScentDecay = 0.5f;
    const float DecayLimit = 0.0001f;
    public const float ScentDiffusion = 0.95f;
    const uint MaxTag = (uint)UrbScentTag.MaxScentTag;
    float[] Tags;

    public bool dirty = false;

    public float this[UrbScentTag i] {
        get { return this.Tags[(uint)i]; }
        set { this.Tags[(uint)i] = value; dirty = true; }
    }

    public UrbScent()
    {
        Tags = new float[MaxTag];
        for (int i = 0; i < MaxTag; i++)
        {
            Tags[i] = 0;
        }
    }

    public void DecayScent()
    {
        for (int i = 0; i < MaxTag; i++)
        {
            if (Tags[i] > DecayLimit)
            {
                Tags[i] = Tags[i] * ScentDecay;
                dirty = true;
            }
            else
            {
                Tags[i] = 0.0f;
            }
        }
    }

    public void ReceiveScent(UrbScent input, float Diffusion = 1.0f)
    {
        dirty = true;
        for (int i = 0; i < MaxTag; i++)
        {
            Tags[i] = Tags[i] < input.Tags[i] * Diffusion ? input.Tags[i] * Diffusion : Tags[i];
        }
    }
}