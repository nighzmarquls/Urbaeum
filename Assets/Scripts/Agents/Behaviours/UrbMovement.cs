using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
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

    static ProfilerMarker s_Moving_p = new ProfilerMarker("UrbMovement.Moving");

    public IEnumerator Moving(UrbTile Goal)
    {
        s_Moving_p.Begin();
        Vector3 StartingPosition = transform.position;
        float Complete = 0;

        if (Speed > 0)
        {
            if (mAgent.Display)
            {
                Vector3 Direction = Goal.Location - StartingPosition;

                mAgent.Display.Flip = Direction.x > 0;
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
                //This is absolutely atrocious but I'm not sure how else to manage this
                s_Moving_p.End();
                yield return new WaitForEndOfFrame();
                s_Moving_p.Begin();
                mAgent.transform.position = Vector3.Lerp(StartingPosition, ArrivalLocation, Complete);
                Complete = (Time.time - StartTime) / TravelTime;
            }
        }
        Movement = null;
        s_Moving_p.End();
    }

    static ProfilerMarker s_ExecuteMove_p = new ProfilerMarker("UrbMovement.ExecuteMove");
    public void ExecuteMove()
    {
        s_ExecuteMove_p.Begin();
        MoveAction.Execute(mAgent,null);
        s_ExecuteMove_p.End();
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

        static ProfilerMarker s_MoveAction_p = new ProfilerMarker("UrbMoveAction.Execute");

        static ProfilerMarker s_MoveAction_p_e = new ProfilerMarker("UrbMoveAction.Execute.Pathfinder");
        public override float Execute(UrbAgent Instigator, UrbAgent Target, float Modifier = 0)
        {
            s_MoveAction_p.Begin(Instigator);
            float Result = Test(Instigator, Modifier);

            Result = Instigator.Body.UtilizeBody(Result);

            if (Result > 0)
            {
                
                s_MoveAction_p_e.Begin();
                UrbPathfinder Pathfinder = Instigator.GetComponent<UrbPathfinder>();
                UrbMovement Movement = Instigator.GetComponent<UrbMovement>();
                UrbTile Goal = null;
                if (!Pathfinder.WasDestroyed && Pathfinder.isActiveAndEnabled)
                {
                    Goal = Pathfinder.GetNextGoal();
                }
                if (!Movement.WasDestroyed && Movement.isActiveAndEnabled)
                {
                    if (Goal != null && Goal != Instigator.CurrentTile)
                    {
                        DisplayActionIcon(Instigator, Instigator.Location);
                        float AdjustedResult = Result / 10;
                        Movement.Speed = AdjustedResult;
                        Movement.EnergyCost = AdjustedResult;
                        Movement.MoveTo(Goal);
                    }
                    
                    s_MoveAction_p_e.End();
                }
                else
                {
                    s_MoveAction_p_e.End();
                    s_MoveAction_p.End();
                    //Debug.LogWarning("Illegal Action: Move Action Called on " + Instigator.name + " this Agent has no UrbMovement Component");
                    return 0;
                }
            }

            s_MoveAction_p.End();
            return Result;
        }
    }
}

