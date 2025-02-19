﻿using System;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbEater : UrbBehaviour
{
    public UrbSubstanceTag[] FoodSubstances;
    public UrbComposition Stomach;

    protected static UrbBiteAttack BiteAttack = new UrbBiteAttack();

    public UrbScentTag[] FoodScents { get; protected set; }
    
    protected List<UrbBody> DetectedFood;

    public override UrbUrgeCategory UrgeSatisfied => UrbUrgeCategory.Hunger;

    public override bool ShouldInterval => false;
    
    public override void Awake()
    {
        Stomach = new UrbComposition();
        FoodScents = UrbSubstances.Scent(FoodSubstances);
        DetectedFood = new List<UrbBody>();

        base.Awake();
    }

    //Moving these asserts to Start() is a complete and total hack. 
    //Until I figure out what the heck is going on with the hecking
    //ordering here- Awake for the UrbBody should be getting called
    //before the OnEnable of UrbEater but of course it turns out unity
    //doesn't guarantee that between separate components?
    //  WHY, UNITY, WHY!?!
    //  I'm going to continue being stubborn about using the built-in
    //methods b/c I feel like this is the better way, long-term, than
    //making our own from scratch.
    //  It's still ridiculously stupid.
    //  https://docs.unity3d.com/Manual/ExecutionOrder.html
    
    public override void Start()
    {
        Assert.IsNotNull(mAgent, "mAgent must not be null");
        Assert.IsNotNull(mAgent.mBody, "mBody must not be null");
        Assert.IsNotNull(mAgent.mBody.BodyComposition, "mBody.BodyComposition must not be null");
        
        mAgent.mBody.BodyComposition.AddComposition(Stomach);
        mAgent.AddAction(BiteAttack);
        
        base.Start();
    }

    public override void Update()
    {
        base.Update();
    }
    
    static ProfilerMarker s_TileEvaluateCheck_p = new ProfilerMarker("UrbEater.TileEvaluateCheck");

    public override float TileEvaluateCheck(UrbTile Target, bool Contact = false)
    {
        if (Stomach == null || Target == null)
            return 0;

        s_TileEvaluateCheck_p.Begin();
        float Evaluation = 0;

        for (int o = 0; o < Target.Occupants.Count; o++)
        {
            if (Target.Occupants[o] == mAgent)
            {
                continue;
            }
            UrbBody PossibleFood = Target.Occupants[o].mBody;

            if (PossibleFood != null)
            {
                for (int f = 0; f < FoodSubstances.Length; f++)
                {
                    if (PossibleFood.BodyComposition == null)
                    {
                        break;
                    }
                    Evaluation += PossibleFood.BodyComposition[FoodSubstances[f]];
                }
            }
        }

        s_TileEvaluateCheck_p.End();
        return Evaluation * Stomach.Emptiness;
    }

    static ProfilerMarker s_RegisterTileForBehaviour_p = new ProfilerMarker("UrbEater.RegisterTileForBehaviour");
    public override void RegisterTileForBehaviour(float Evaluation, UrbTile Target, int Index)
    {
        s_RegisterTileForBehaviour_p.Begin();
        for (int o = 0; o < Target.Occupants.Count; o++)
        {
            if (Target.Occupants[o] == mAgent)
            {
                continue;
            }
            UrbBody PossibleFood = Target.Occupants[o].mBody;

            if (PossibleFood != null)
            {
                for (int f = 0; f < FoodSubstances.Length; f++)
                {
                    if (PossibleFood.BodyComposition == null)
                    {
                        break;
                    }
                    if (PossibleFood.BodyComposition[FoodSubstances[f]] > 0)
                    {
                        DetectedFood.Add(PossibleFood);
                        break;
                    }
                }
            }
        }
        base.RegisterTileForBehaviour(Evaluation, Target, Index);
        s_RegisterTileForBehaviour_p.End();
    }

    public override void ClearBehaviour()
    {
        DetectedFood.Clear();
        base.ClearBehaviour();
    }
    static ProfilerMarker s_ExecuteTileBehaviour = new ProfilerMarker("UrbEater.ExecuteTileBehaviour");
    public override void ExecuteTileBehaviour()
    {
        mAgent.Express(UrbDisplayFace.Expression.Default);
        using (s_ExecuteTileBehaviour.Auto())
        {
            float BiteSize = BiteAttack.Test(mAgent);
            float Eaten = 0;
            for (int d = 0; d < DetectedFood.Count; d++)
            {
                float Result = BiteAttack.Execute(mAgent, DetectedFood[d].mAgent, -Eaten);
                if (Result > 0)
                {
                    mAgent.Express(UrbDisplayFace.Expression.Joy);
                    for (int f = 0; f < FoodSubstances.Length; f++)
                    {
                        Eaten += DetectedFood[d].BodyComposition.TransferTo(Stomach, FoodSubstances[f], Result);

                        if (Eaten >= BiteSize)
                        {
                            return;
                        }

                    }
                }
            }

            base.ExecuteTileBehaviour();
        }
    }

    public override UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();

       
        UrbSubstance[] StomachContents = (Stomach == null)? new UrbSubstance[0] : Stomach.GetCompositionIngredients();

        Data.FieldArrays = new UrbFieldArrayData[]
        {
            UrbEncoder.GetArrayFromSubstances("StomachContents" , StomachContents),
        };

        Data.StringArrays = new UrbStringArrayData[]
        {
            UrbEncoder.EnumsToArray("FoodSubstances", FoodSubstances),
            UrbEncoder.EnumsToArray("FoodScents",FoodScents)
        };

        return Data;
    }

    public override bool SetComponentData(UrbComponentData Data)
    {
        FoodSubstances = UrbEncoder.GetEnumArray<UrbSubstanceTag>("FoodSubstances", Data);
        FoodScents = UrbEncoder.GetEnumArray<UrbScentTag>("FoodScents", Data);
        Stomach = new UrbComposition(UrbEncoder.GetSubstancesFromArray("StomachContents", Data));
        return true;
    }
}

public class UrbBiteAttack : UrbAttack
{
    protected override string IconPath => IconDiretory + "BiteAttack";
    public override Color IconColor => base.IconColor;
    protected override UrbDamage DamageAction { get; set; } = new UrbBiteDamage();
    public override UrbTestCategory Category => base.Category | UrbTestCategory.Hold;

    public override float Test(UrbAgent target, float Modifier = 0)
    {
        float Teeth = target.mBody.BodyComposition[UrbSubstanceTag.Teeth];

        Assert.IsFalse(float.IsInfinity(Teeth) || float.IsNaN(Teeth));
        
        return Mathf.Max(0,Teeth + MobilityTest(target.mBody) + Modifier);
    }

}

public class UrbBiteDamage : UrbDamage
{
    protected override UrbSubstanceTag DamageSubstance => UrbSubstanceTag.Teeth; 
    protected override string IconPath => IconDiretory + "BiteHit";
    public override Color IconColor => Color.red;
    public override UrbTestCategory Category => UrbTestCategory.Pierce;
}