using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class UrbMerge : UrbBehaviour
{
    public int MinimumMergeCount = 2;
    public float MinimumMass = 0;
    public int MinimumTiles = 0;
    public int MaximumTiles = 9;
   
    public UrbAgent MergeProduct;
    public override bool ShouldInterval => false;

    float TotalMass = 0;
    int TotalTiles = 0;
    int TotalCount = 0;

    protected void MergeIntoTarget(UrbAgent Target)
    {
        mAgent.Body.BodyComposition.EmptyInto(Target.Body.BodyComposition);
        UrbEater AgentEater = Target.GetComponent<UrbEater>();
        if (!AgentEater.WasDestroyed && AgentEater.isActiveAndEnabled)
        {
            UrbEater mEater = GetComponent<UrbEater>();
            if (!mEater.WasDestroyed && mEater.isActiveAndEnabled)
            {
                mEater.Stomach.EmptyInto(AgentEater.Stomach);
            }
        }

        mAgent.Remove();
    }

    static ProfilerMarker s_UrbMergeTileEvalCheck_p = new ProfilerMarker("UrbMerge.TileEvaluateCheck");
    //Contact is for saying what KIND of check - only when trying to do something that REQUIRES a contact
    // A contact is done for checks that require sharing a tile with another entity
    // "Roughly things that are roughly in-reach"
    public override float TileEvaluateCheck(UrbTile Target, bool Contact = false)
    {
        if (Target?.Occupants == null)
        {
            return 0;
        }

        s_UrbMergeTileEvalCheck_p.Begin(this);
        //Evaluation tells us to what degree the current behavior of the entity cares
        //about the Target tile.
        float Evaluation = 0;
      
        for (int c = 0; c < Target.Occupants.Count; c++)
        {
            if (MergeProduct.TemplatesMatch(Target.Occupants[c]))
            {
                MergeIntoTarget(Target.Occupants[c]);
                break;
            }
            UrbMerge[] MergeComponents = Target.Occupants[c].GetComponents<UrbMerge>();
            for (int i = 0; i < MergeComponents.Length; i++)
            {
                if (MergeComponents[i] != this)
                {
                    if (MergeProduct.TemplatesMatch(MergeComponents[i].MergeProduct))
                    {
                        if(mAgent.Mass >= Target.Occupants[c].Mass)
                        {
                            Evaluation += Target.Occupants[c].Mass;
                        }
                       
                    }
                }

            }
        }

        s_UrbMergeTileEvalCheck_p.End();
        return Evaluation;
    }

    public override void RegisterTileForBehaviour(float Evaluation, UrbTile Target, int Index)
    {
        for (int c = 0; c < Target.Occupants.Count; c++)
        {
            UrbMerge[] MergeComponents = Target.Occupants[c].GetComponents<UrbMerge>();
            for (int i = 0; i < MergeComponents.Length; i++)
            {
                if (MergeComponents[i] != this && mAgent.Mass >= Target.Occupants[c].Mass)
                {
                    TotalMass += Target.Occupants[c].Mass;
                    TotalCount += 1;
                }
            }
        }
        TotalTiles++;
        base.RegisterTileForBehaviour(Evaluation, Target, Index);
    }

    public override void ClearBehaviour()
    {
        TotalCount = 0;
        TotalMass = 0;
        TotalTiles = 0;
        base.ClearBehaviour();
    }

    public override float BehaviourEvaluation { get {
            if (TotalMass < MinimumMass || TotalTiles > MaximumTiles || TotalTiles < MinimumTiles || TotalCount < MinimumMergeCount)
            {
                return 0;
            }
            return base.BehaviourEvaluation;
        }
        protected set => base.BehaviourEvaluation = value; }

    public override void ExecuteTileBehaviour()
    {
        if(TotalMass < MinimumMass || TotalTiles > MaximumTiles || TotalTiles < MinimumTiles || TotalCount < MinimumMergeCount)
        {
            ClearBehaviour();
        }

        GameObject Spawned = null;
        if (UrbAgentSpawner.SpawnAgent(MergeProduct.gameObject, mAgent.CurrentTile, out Spawned))
        {
            UrbAgent SpawnedAgent = Spawned.GetComponent<UrbAgent>();

            MergeIntoTarget(SpawnedAgent);

            for (int t = 0; t < RegisteredTiles.Length; t++)
            {
                if (RegisteredTiles[t] == null)
                {
                    continue;
                }

                for (int c = 0; c < RegisteredTiles[t].Occupants.Count; c++)
                {
                    if (MergeProduct.TemplatesMatch(RegisteredTiles[t].Occupants[c]))
                    {
                        MergeIntoTarget(RegisteredTiles[t].Occupants[c]);
                        break;
                    }
                    UrbMerge[] MergeComponents = RegisteredTiles[t].Occupants[c].GetComponents<UrbMerge>();
                    for (int i = 0; i < MergeComponents.Length; i++)
                    {
                        if (MergeComponents[i] != this)
                        {
                            if (MergeProduct.TemplatesMatch(MergeComponents[i].MergeProduct))
                            {
                                MergeComponents[i].MergeIntoTarget(SpawnedAgent);
                            }
                        }

                    }
                }
            }
        }
        base.ExecuteTileBehaviour();
    }
}
