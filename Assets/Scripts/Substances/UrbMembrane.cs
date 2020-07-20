using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UrbMembrane
{
    protected UrbComposition ContainingComposition;

    //MembraneLayers is a property that is sampled from the Skin Recipes.
    protected UrbSubstanceTag[] MembraneLayers;
    public UrbSubstanceTag[] Layers {
        get { return MembraneLayers; }
        set { MembraneLayers = value; LayerDamage = new float[value.Length]; }
    }

    public float Damage {
        get {
            if (LayerDamage == null)
            {
                return 0;
            }

            float Amount = 0;
            
            for (int i = 0; i < LayerDamage.Length; i++)
            {
                Amount += LayerDamage[i];
            }
            return Amount;
        }
    }

    public float Restore(float Input)
    {
        float Result = 0;
        for(int i = LayerDamage.Length -1; i > -1; i++)
        {
            float Healing = Mathf.Min(LayerDamage[i], Input);
            Input -= Healing;
            Result += Healing;
            if(Input <= 0)
            {
                break;
            }
        }
        return Result;
    }

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
            if (ContainingComposition.Mass == LastMass)
            {
                return LastRadius;
            }
            LastRadius = RadiusFromVolume(LastMass);
            LastMass = ContainingComposition.Mass;
            return LastRadius;
        }
    }

    float LastArea = 0;
    public float Area {
        get {
            if (ContainingComposition.Mass == LastMass)
            {
                return LastArea;
            }
            LastArea = SphereArea(Radius);
            return LastArea;
        }
    }

    public float TotalThickness()
    {
        return ContainingComposition.GetProportionOfTotal(MembraneLayers) * Radius;
    }

    public float Thickness(UrbSubstanceTag Tag)
    {
        return ContainingComposition.GetProportionOf(Tag) * Radius;
    }

    public UrbSubstanceTag DepthSample(float Depth)
    {
        if (MembraneLayers == null || MembraneLayers.Length == 0)
        {
            return UrbSubstanceTag.None;
        }

        if (Depth > TotalThickness())
        {
            return UrbSubstanceTag.All;
        }

        float Penetration = 0;

        for (int DepthIndex = 0; DepthIndex < MembraneLayers.Length; DepthIndex++)
        {
            Penetration += Thickness(MembraneLayers[DepthIndex]);
            if (Penetration >= Depth)
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

    public float PenetrationCheck(UrbSubstanceTag Penetrator, float Amount, float Force)
    {
        if (Layers == null || Layers.Length == 0)
        {
            return Amount;
        }

        float Result = 0;
        float Depth = Force;
        float PenHardness = UrbSubstanceProperties.Get(Penetrator).Hardness;

        for (int i = 0; i < Layers.Length; i++)
        {
            float LayerThickness = Thickness(Layers[i]);
            float LayerHardness = UrbSubstanceProperties.Get(Layers[i]).Hardness;
            float LayerFlexibility = UrbSubstanceProperties.Get(Layers[i]).Flexibility;

            Depth *= PenHardness / LayerHardness;
         

            if(Depth > LayerThickness)
            {
                float Damage = LayerFlexibility > 0 ? (Amount*LayerThickness)/LayerFlexibility : Amount*LayerThickness;
                LayerDamage[i] += Damage;
                Result += Damage;
                Depth -= LayerThickness;
            }
            else
            {

                float Damage = LayerFlexibility > 0 ? (Amount * Depth) / LayerFlexibility : Amount * Depth;
                LayerDamage[i] += Damage;
                Result += Damage;
                break;
            }
        }

        return Result;
    }

    public UrbMembrane(UrbComposition Composition)
    {
        ContainingComposition = Composition;
    }

    public void ChangeComposition(UrbComposition Composition)
    {
        if(Composition == null)
        {
            Debug.LogError("Illegal Assignment: Cannot Change Composition to Null on UrbMembrane");
            return;
        }

        if (MembraneLayers != null)
        {
            for (int i = 0; i < MembraneLayers.Length; i++)
            {
                ContainingComposition.TransferTo(Composition, MembraneLayers[i], float.MaxValue);
            }
        }
        ContainingComposition = Composition;
    }
}
