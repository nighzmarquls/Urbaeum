using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UrbMembrane
{
    public UrbComposition MembraneComposition { get; protected set; }

    public UrbSubstanceTag[] MembraneLayers;

    protected float[] LayerDamage;

    const float ThreeFourths = 4 / 3;
    const float Third = 1 / 3;

    public static float RadiusFromVolume(float Volume)
    {
        return Mathf.Pow(Volume / Mathf.PI * ThreeFourths, Third);
    }

    public static float SphereArea(float Radius)
    {
        return 4 * CircleArea(Radius);
    }

    public static float CircleArea(float Radius)
    {
        return Mathf.PI * Radius * Radius;
    }

    float LastMass = 0;
    float LastRadius = 0;
    public float Radius {
        get {
            if(MembraneComposition.Mass == LastMass)
            {
                return LastRadius;
            }
            LastRadius = RadiusFromVolume(LastMass);
            LastMass = MembraneComposition.Mass;
            return LastRadius;
        }
    }

    float LastArea = 0;
    public float Area {
        get {
            if(MembraneComposition.Mass == LastMass)
            {
                return LastArea;
            }
            LastArea = SphereArea(Radius);
            return LastArea;
        }
    }

    public float TotalThickness()
    {
        return MembraneComposition.GetProportionOfTotal(MembraneLayers) * Radius;
    }

    public float Thickness(UrbSubstanceTag Tag)
    {
        return MembraneComposition.GetProportionOf(Tag) * Radius;
    }

    public UrbSubstanceTag DepthSample(float Depth)
    {
        if(MembraneLayers == null || MembraneLayers.Length == 0)
        {
            return UrbSubstanceTag.None;
        }

        if(Depth > TotalThickness())
        {
            return UrbSubstanceTag.All;
        }

        float Penetration = 0;

        for(int DepthIndex = 0; DepthIndex < MembraneLayers.Length; DepthIndex++)
        {
            Penetration += Thickness(MembraneLayers[DepthIndex]);
            if(Penetration >= Depth)
            {
               return MembraneLayers[DepthIndex];
            }
        }
        return UrbSubstanceTag.All;
    }

    public float Impact(UrbComposition ImpactSource, UrbSubstanceTag ImpactSubstance, float Force)
    {
        float Result = ImpactSource[ImpactSubstance];

        Result = PenetrationCheck(ImpactSubstance, Result, Force);

        return Result;
    }

    public float PenetrationCheck( UrbSubstanceTag Penetrator, float Amount, float Force)
    {
        float Result = Amount;

        return Result;
    }

    UrbMembrane(UrbComposition Composition)
    {
        MembraneComposition = Composition;
    }

}
