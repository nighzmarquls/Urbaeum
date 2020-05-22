using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbMerge : UrbBehaviour
{
    public int MinimumMergeCount = 2;
    public float MinimumMass = 0;
    public int MinimumTiles = 0;
    public int MaximumTiles = 9;
    public UrbAgent MergeProduct;

    protected void MergeIntoTarget(UrbAgent Target)
    {
        mAgent.Body.BodyComposition.EmptyInto(Target.Body.BodyComposition);
        UrbEater AgentEater = Target.GetComponent<UrbEater>();
        if (AgentEater != null)
        {
            UrbEater mEater = GetComponent<UrbEater>();
            if (mEater != null)
            {
                mEater.Stomach.EmptyInto(AgentEater.Stomach);
            }
        }

        mAgent.Remove();
    }

    override public IEnumerator FunctionalCoroutine()
    {
        if (mAgent.Tileprint.TileCount > 1)
        {
            UrbTile[] SelfTiles = mAgent.Tileprint.GetAllPrintTiles(mAgent);
            for (int s = 0; s < SelfTiles.Length; s++)
            {
                if (SelfTiles[s] == null)
                {
                    continue;
                }

                for (int o = 0; o < SelfTiles[s].Occupants.Count; o++)
                {
                    if (MergeProduct.TemplatesMatch(SelfTiles[s].Occupants[o]))
                    {
                        MergeIntoTarget(SelfTiles[s].Occupants[o]);
                        yield break;
                    }
                }
            }
        }
        else
        {
            for (int o = 0; o < mAgent.CurrentTile.Occupants.Count; o++)
            {
                if (MergeProduct.TemplatesMatch(mAgent.CurrentTile.Occupants[o]))
                {
                    MergeIntoTarget(mAgent.CurrentTile.Occupants[o]);
                    yield break;
                }
            }
        }
        UrbTile[] Search = GetSearchTiles(true);
        List<UrbMerge> MergeCandidates = new List<UrbMerge>();
        float MyMass = mAgent.Mass;
        float CandidateMass = MyMass;
        float CandidateTiles = 0;
        bool CenterCandidate = true;
        for (int t = 0; t < Search.Length; t++)
        {
            if (Search[t] == null)
            {
                continue;
            }

            bool FoundMatch = false;
            for(int c = 0; c < Search[t].Occupants.Count; c++)
            {
                UrbMerge[] MergeComponents = Search[t].Occupants[c].GetComponents<UrbMerge>();
                for (int i = 0; i < MergeComponents.Length; i++)
                {
                    if (MergeComponents[i] != this)
                    {
                        if (MergeProduct.TemplatesMatch(MergeComponents[i].MergeProduct))
                        {
                            FoundMatch = true;
                            if (MergeCandidates.Contains(MergeComponents[i]))
                            {
                                continue;
                            }

                            MergeCandidates.Add(MergeComponents[i]);
                            float ContentMass = Search[t].Occupants[c].Mass;
                            if (ContentMass > MyMass)
                            {
                                CenterCandidate = false;
                            }
                            CandidateMass += Search[t].Occupants[c].Mass;
                        }
                    }
                    
                }
            }
            if(FoundMatch)
            {
                CandidateTiles++;
                if(CandidateTiles >= MaximumTiles)
                {
                    break;
                }
            }
        }

        yield return BehaviourThrottle.PerformanceThrottle();

        if (CenterCandidate && MergeCandidates.Count >= MinimumMergeCount && CandidateMass >= MinimumMass && CandidateTiles >= MinimumTiles)
        {
            GameObject Spawned = null;
            if (UrbAgentSpawner.SpawnAgent(MergeProduct.gameObject, mAgent.CurrentTile, out Spawned))
            {
                UrbAgent SpawnedAgent = Spawned.GetComponent<UrbAgent>();

                for (int i = 0; i < MergeCandidates.Count; i++)
                {
                    MergeCandidates[i].MergeIntoTarget(SpawnedAgent);
                    
                    
                }
                MergeIntoTarget(SpawnedAgent);
                yield break;
            }
        }
        yield return null;
    }
}
