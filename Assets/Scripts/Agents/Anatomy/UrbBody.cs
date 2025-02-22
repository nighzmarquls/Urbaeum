﻿using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbBody : UrbBase
{

    UrbComposition _bodyComposition;
    public UrbComposition BodyComposition {
        get
        {
            if (_bodyComposition == null)
            {
                _bodyComposition = new UrbComposition(BodyRecipe);
                _bodyComposition.Membrane.Layers = SkinRecipe;
                HasComposition = true;
            }

            return _bodyComposition;
        }
        private set
        {
            HasComposition = value != null;
            _bodyComposition = value;
        }
    }
    public bool HasComposition { get; protected set; }
    public UrbSubstance[] BodyRecipe;
    public UrbSubstanceTag[] SkinRecipe;
    public UrbSubstance[] CriticalBodyPartAmounts;
    public float SurfaceArea { get {
            if(BodyComposition != null)
            {
                return BodyComposition.Membrane.Area;
            }
            return 0;
        }
    }

    public float Height { get; protected set; }  = 1;

    protected static UrbRecoverBodyAction RecoverAction = new UrbRecoverBodyAction();

    public override void Awake()
    {
        if (_bodyComposition == null)
        {
            _bodyComposition = new UrbComposition(BodyRecipe);
            _bodyComposition.Membrane.Layers = SkinRecipe;
            HasComposition = true;
        }
        
        base.Awake();
    }
    
    public override void OnEnable()
    {
        Assert.IsNotNull(BodyComposition);
        BodyComposition.SetSize(mAgent.Tileprint.TileCount * Height);
        base.OnEnable();
    }

    public bool BodyEmpty()
    {
        return BodyComposition.Emptiness > 0.9f; //TODO: Fix this;
    }

    public bool BodyCritical()
    {
        if(!HasComposition)
        {
            return false;
        }

        for(int i = 0; i < CriticalBodyPartAmounts.Length; i++)
        {
            if(BodyComposition[CriticalBodyPartAmounts[i].Substance] < CriticalBodyPartAmounts[i].SubstanceAmount)
            {
                return true;
            }
        }
        return false;
    }

    public float Utilization { get; protected set; }
    public float UtilizeBody(float Amount)
    {
        float TotalBody = BodyComposition.Mass; 
        if(Utilization >= TotalBody)
        {
            mAgent.Express(UrbDisplayFace.Expression.Closed);
            return 0;
        }

        Utilization += Amount;
        if(Utilization > TotalBody)
        {
            mAgent.Express(UrbDisplayFace.Expression.Closed);
            Amount -= Utilization - TotalBody;
        }
        return Amount;
    }

    public void RecoverUtilization()
    {
        if (Utilization > 0)
        {
            Utilization -= RecoverAction.Execute(mAgent, mAgent);
        }
    }

    public override UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();


        Data.Fields = new UrbFieldData[]
        {
            new UrbFieldData{ Name = "Height", Value = Height}
        };

        UrbSubstance[] BodyContents = (BodyComposition == null)? BodyRecipe : BodyComposition.GetCompositionIngredients();

        Data.FieldArrays = new UrbFieldArrayData[]
        {
            UrbEncoder.GetArrayFromSubstances("BodyRecipe" , BodyRecipe),
            UrbEncoder.GetArrayFromSubstances("BodyContents" , BodyContents),
            UrbEncoder.GetArrayFromSubstances("CriticalBodyPartAmounts" , CriticalBodyPartAmounts),
        };

        Data.StringArrays = new UrbStringArrayData[]
        {
            UrbEncoder.EnumsToArray("SkinRecipe" , SkinRecipe),
        };

        return Data;
    }

    public override bool SetComponentData(UrbComponentData Data)
    {
        Height = UrbEncoder.GetField("Height", Data);
        BodyRecipe = UrbEncoder.GetSubstancesFromArray("BodyRecipe",Data);
        SkinRecipe = UrbEncoder.GetEnumArray<UrbSubstanceTag>("SkinRecipe", Data);
        BodyComposition = new UrbComposition(UrbEncoder.GetSubstancesFromArray("BodyContents", Data));
        CriticalBodyPartAmounts = UrbEncoder.GetSubstancesFromArray("CriticalBodyPartAmounts", Data);
        return true;
    }
}

public class UrbRecoverBodyAction : UrbAction
{
    public override float Test(UrbAgent target, float Modifier = 0)
    {
        return MobilityTest(target.mBody);
    }

    public override float Execute(UrbAgent Instigator, UrbAgent Target, float Modifier = 0)
    {
        if(Instigator != Target)
        {
            Debug.LogWarning("Illegal Action: " + Instigator.name + " trying to recover for " + Target.name);
            return 0;
        }

        float Result = Mathf.Min(Instigator.mBody.Utilization, Test(Instigator));

        if (Instigator.HasMetabolism)
        {
            Instigator.Metabolism.SpendEnergy(Result);
        }

        Assert.IsFalse(float.IsInfinity(Result) || float.IsNaN(Result));

        return Result;
    }
}
