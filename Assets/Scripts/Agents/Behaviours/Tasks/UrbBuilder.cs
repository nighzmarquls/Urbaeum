using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbBuilder : UrbWorker
{

}

public class UrbGatherTask : UrbTask
{
    public override string Name { get; set; } = "Gather Task";
    public override string Description => "Resource Gather Job";

    bool NoTarget = true;

    public float HeldAmount { get; protected set; } = 0;

    public UrbSubstance GoalSubstance;

    protected UrbTile _TargetTile = null;
    public UrbTile TargetTile { get {
            return _TargetTile;
        }
        set {
            NoTarget = false;
            _TargetTile = TargetTile;
        }
    }

    public override float PerformTask(UrbAgent Instigator, UrbTile WorkTile)
    {
        if (NoTarget || WorkTile.Occupants.Count == 0)
        {
            return 0;
        }

        if (Instigator.HasAction(UrbTestCategory.Hold))
        {

            UrbAction Action = Instigator.PickAction(UrbTestCategory.Hold);
            float CarryAmount = 0;

            for(int o = 0; o < WorkTile.Occupants.Count; o++)
            {

            }
        }

        return 0;
    }

    public override float TaskCompletionCostEstimate(UrbAgent Instigator, UrbTile WorkTile)
    {
        if (NoTarget)
        {
            return float.MaxValue;
        }

        if(Instigator.HasAction(UrbTestCategory.Hold))
        {
            UrbAction Action = Instigator.PickAction(UrbTestCategory.Hold);
            float Estimate = Action.CostEstimate(Instigator);
            float CarryAmount = Action.Test(Instigator);
            float TargetAmount = GoalSubstance.SubstanceAmount - _TargetTile[GoalSubstance.Substance];
            if (CarryAmount >= TargetAmount)
            {
                return Estimate;
            }
            else
            {
                return Estimate * (TargetAmount / CarryAmount);
            }
        }

        return float.MaxValue;
    }

    public override float TaskSuitableTileCheck(UrbTile Input)
    {
        return Input[GoalSubstance.Substance];
    }

    public override bool TaskValid(UrbAgent TaskedAgent)
    {
        if(NoTarget)
        {
            return false;
        }

        return (TaskedAgent.HasAction(UrbTestCategory.Hold));
    }

    public override bool TaskComplete()
    {
        if(NoTarget)
        {
            return false;
        }

        return _TargetTile[GoalSubstance.Substance] >= GoalSubstance.SubstanceAmount;
    }
}