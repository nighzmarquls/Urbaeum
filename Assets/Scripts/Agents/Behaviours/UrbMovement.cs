using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbMovement : UrbBehaviour
{
    protected float Speed = 2;
    protected float EnergyCost = 5;
    public Coroutine Movement = null;

    protected static UrbMoveAction MoveAction = new UrbMoveAction();

    public override bool ShouldInterval => false;

    public override void Initialize()
    {
        base.Initialize();
        mAgent.AddAction(MoveAction);
    }

    public IEnumerator Moving(UrbTile Goal)
    {
        Vector3 StartingPosition = transform.position;
        float Complete = 0;

        if (Speed > 0)
        {
            if (mAgent.Display)
            {
                Vector3 Direction = Goal.Location - StartingPosition;

                if (Direction.x > 0)
                {
                    mAgent.Display.Flip = true;
                }
                else
                {
                    mAgent.Display.Flip = false;
                }
            }

            if (mAgent.CurrentTile != null)
            {
                mAgent.CurrentTile.OnAgentLeave(mAgent);
                if (Goal != mAgent.CurrentTile)
                {
                    if (mAgent.Metabolism != null)
                    {
                        mAgent.Metabolism.SpendEnergy(EnergyCost * mAgent.Mass * mAgent.Mass);
                    }
                }
            }
            Goal.OnAgentArrive(mAgent);

            float TravelTime = (1.0f / (Speed* mAgent.TimeMultiplier));
            float ArrivalTime = Time.time + TravelTime;
            float StartTime = Time.time;
            Vector3 ArrivalLocation = Goal.Location;
            while (Complete < 1.0f)
            {
                yield return new WaitForEndOfFrame();
                mAgent.transform.position = Vector3.Lerp(StartingPosition, ArrivalLocation, Complete);
                Complete = (Time.time - StartTime) / TravelTime;
            }
        }
        Movement = null;
          
    }

    public void ExecuteMove()
    {
        MoveAction.Execute(mAgent,null);
    }

    public void MoveTo(UrbTile Goal)
    {
        if(Movement == null)
        {
            Movement = StartCoroutine(Moving(Goal));
        }
    }

    public class UrbMoveAction : UrbAction
    {
        protected override string IconPath => IconDiretory + "Dodge";
        public override UrbTestCategory Category => UrbTestCategory.Mobility | UrbTestCategory.Defense;

        public override float Test(UrbAgent target, float Modifier = 0)
        {
            return MobilityTest(target.Body);
        }

        public override float CostEstimate(UrbAgent Instigator)
        {
            return Test(Instigator) * Instigator.Mass * Instigator.Mass;
        }

        public override float Execute(UrbAgent Instigator, UrbAgent Target, float Modifier = 0)
        {
            float Result = Test(Instigator, Modifier);

            if (Result > 0)
            {
                UrbPathfinder Pathfinder = Instigator.GetComponent<UrbPathfinder>();
                UrbMovement Movement = Instigator.GetComponent<UrbMovement>();
                UrbTile Goal = null;
                if (Pathfinder != null)
                {
                    Goal = Pathfinder.GetNextGoal();
                }
                if (Movement != null)
                {
                    if (Goal != null && Goal != Instigator.CurrentTile)
                    {
                        DisplayActionIcon(Instigator, Instigator.Location);
                        float AdjustedResult = Result / 10;
                        Movement.Speed = AdjustedResult;
                        Movement.EnergyCost = AdjustedResult;
                        Movement.MoveTo(Goal);
                    }
                }
                else
                {
                    Debug.LogWarning("Illegal Action: Move Action Called on " + Instigator.name + " this Agent has no UrbMovement Component");
                    return 0;
                }
            }

            return Result;
        }
    }
}

