using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbGrasper : UrbBehaviour
{
    UrbComposition HeldComposition;
    UrbAgent PileAgent;

    public override bool ShouldInterval => false;

    public override void Awake()
    {
        HeldComposition = new UrbComposition();

        base.Awake();
    }

    public virtual float TakeFrom(UrbAgent Target, UrbSubstanceTag Substance)
    {
        if(!Target.HasBody)
        {
            return 0;
        }

        float Amount = 0;

        UrbAction HoldAction = mAgent.PickAction(UrbTestCategory.Hold);

        if(HoldAction != null)
        {
            Amount = HoldAction.Execute(mAgent, Target, -HeldComposition.Mass);
            if (Target.IsGrasper)
            {
                Target.Grasper.HeldComposition.TransferTo(HeldComposition, Substance, Amount);
            }
            else
            {
                Amount = Target.mBody.BodyComposition.TransferTo(HeldComposition, Substance, Amount);
            }
        }
        return Amount;
    }

    public virtual float GiveTo(UrbAgent Target, UrbSubstanceTag Substance)
    {
        if(!Target.HasBody)
        {
            return 0;
        }

        float Amount = 0;

        UrbAction HoldAction = mAgent.PickAction(UrbTestCategory.Hold);

        if (HoldAction != null)
        {
            Amount = HoldAction.Execute(mAgent, Target);
            if (Target.IsGrasper)
            {
                Target.Grasper.TakeFrom(mAgent, Substance);
            }
            else if (Target.IsEater)
            {
                Amount = HeldComposition.TransferTo(Target.Eater.Stomach,Substance, Amount);
            }
            else
            {
                Amount = HeldComposition.TransferTo(Target.mBody.BodyComposition, Substance, Amount);
                
            }
        }

        return Amount;
    }

    UrbAgent CurrentPile = null;
    UrbTile LastPileTile = null;
    public virtual float Drop(UrbSubstance Substance, float Amount)
    {
        if (LastPileTile != mAgent.CurrentTile)
        {
            for (int o = 0; o < mAgent.CurrentTile.Occupants.Count; o++)
            {
                if (mAgent.CurrentTile.Occupants[o].TemplatesMatch(PileAgent))
                {
                    CurrentPile = mAgent.CurrentTile.Occupants[o];
                }
            }
            LastPileTile = mAgent.CurrentTile;
        }
        if(CurrentPile == null)
        {

        }
        float Dropped = Amount;

        return Dropped;
    }

    public override void Start()
    {
        Assert.IsNotNull(mAgent, "mAgent must not be null");
        Assert.IsNotNull(mAgent.mBody, "mBody must not be null");
        Assert.IsNotNull(mAgent.mBody.BodyComposition, "mBody.BodyComposition must not be null");

        mAgent.mBody.BodyComposition.AddComposition(HeldComposition);

        base.Start();
    }

}
