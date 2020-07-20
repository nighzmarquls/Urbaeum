﻿using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public enum UrbBreedTag
{
    Plant,
    Bun,
    MaxBreedTag
}

[RequireComponent(typeof(UrbAgent))]
[RequireComponent(typeof(UrbBody))]
public class UrbBreeder : UrbBehaviour
{
    public int MateRequirement = 0;
    public int MateCrowding = 8;
    public int OffspringCount = 1;
    public float OffspringRequiredSpace = 500;

    public float Gestation = 1.0f;

    public int DispersalDistance = -1;

    public UrbBreedTag BreedType = UrbBreedTag.Plant;
    public UrbPathTerrain RequiredOffspringTerrain = UrbPathTerrain.Land;

    public UrbScentTag[] MateScents;
    public UrbScentTag[] RivalScents;

    public UrbSubstance[] GestationRecipe;

    public GameObject[] OffspringObjects;
    protected UrbObjectData[] OffspringData = null;

    protected static UrbLoveAction LoveAction = new UrbLoveAction();

    protected int Crowd = 0;
    public bool Gestating { get; protected set; }

    public override UrbUrgeCategory UrgeSatisfied => UrbUrgeCategory.Breed;

    int breedCheckedFrame;
    bool _potentiallyBreed;
    bool _canBreed;
    public bool CanBreed
    {
        get
        {
            if (!_potentiallyBreed)
            {
                return false;
            }
            
            if (!mAgent.Alive)
            {
                _potentiallyBreed = false;
                return false;
            }
            
            int currentFrame = Time.frameCount;
            if (breedCheckedFrame == currentFrame)
            {
                return _canBreed;
            }
            
            //TODO: (IMO) Breeding should mean the Agent is well-off, compositionally-speaking
            //So we should consider choosing a ratio of Composition excess.
            _canBreed = mAgent.mBody.BodyComposition.ContainsMoreOrEqualThan(GestationRecipe);
            breedCheckedFrame = currentFrame;
            
            return false;
        }
    }

    public override void OnEnable()
    {
        Gestating = false;
        Crowd = 0;

        if (OffspringData == null || OffspringData.Length <= 0)
        {
            EncodeOffspringData();
        }
        
        base.OnEnable();

        _potentiallyBreed = HasAgent && HasBody && mBody.HasComposition;
    }

    protected void SetOffspringData(UrbAgent Offspring)
    {
        if (Offspring == null || !Offspring.IsBreeder)
        {
            return;
        }
        
        UrbObjectData[] ChildOffspringData = new UrbObjectData[OffspringData.Length];
        
        OffspringData.CopyTo(ChildOffspringData, 0);

        Offspring.Breeder.OffspringData = ChildOffspringData;
    }

    protected void EncodeOffspringData()
    {
        OffspringData = new UrbObjectData[OffspringObjects.Length];
        for (int o = 0; o < OffspringObjects.Length; o++)
        {
            OffspringData[o] = UrbEncoder.Read(OffspringObjects[o]);
        }
    }

    protected override bool ValidToInterval()
    {
        return base.ValidToInterval() && mAgent.CurrentTile != null && OffspringData != null && OffspringData.Length > 0 && mAgent.mBody != null;
    }

    static ProfilerMarker s_TileEvaluateCheck_p = new ProfilerMarker("UrbBreeder.TileEvaluateCheck");

    public override float TileEvaluateCheck(UrbTile Target , bool Contact = false)
    {
        //"Temporary" using for now
        using (s_TileEvaluateCheck_p.Auto())
        {
            if (Gestating || Target?.Occupants == null || Crowd > MateCrowding)
            {
                return 0;
            }

            float Evaluation = (MateRequirement == 0)? mAgent.Mass : 0;

            for (int c = 0; c < Target.Occupants.Count; c++)
            {
                var occupant = Target.Occupants[c];
                if (occupant.WasDestroyed || !occupant.isActiveAndEnabled)
                {
                    Target.Occupants.Remove(occupant);
                    continue;
                }
                
                UrbAgent MateCandidate = Target.Occupants[c];

                if (!MateCandidate || MateCandidate.WasDestroyed || !MateCandidate.IsBreeder)
                {
                    continue;
                }

                var BreedCandidate = MateCandidate.Breeder;
                
                if (!BreedCandidate.WasDestroyed && BreedCandidate != this && BreedCandidate.BreedType == BreedType)
                {
                    Crowd++;
                    Evaluation += Target.Occupants[c].Mass;
                }

                if(Crowd >= MateCrowding)
                {
                    return 0;
                }
            }
            return Evaluation;
        }
    }

    public override float BehaviourEvaluation { get {
            if (Crowd >= MateCrowding)
            {
                return 0;
            }
            return base.BehaviourEvaluation;
    } protected set => base.BehaviourEvaluation = value; }

    public override void RegisterTileForBehaviour(float Evaluation, UrbTile Target, int Index)
    {
        if (Crowd < MateCrowding)
        {
            base.RegisterTileForBehaviour(Evaluation, Target, Index);
        }
    }

    public override void ClearBehaviour()
    {
        Crowd = 0;
        base.ClearBehaviour();
    }

    static ProfilerMarker executeTileBehaviour = new ProfilerMarker("UrbBreeder.ExecuteTileBehaviour");

    public override void ExecuteTileBehaviour()
    {
        executeTileBehaviour.Begin();

        if (IsPaused || !mAgent.Alive)
        {
            executeTileBehaviour.End();
            return;
        }

        if (MateRequirement == 0 && CanBreed)
        {
            Gestating = true;
            executeTileBehaviour.End();
            base.ExecuteTileBehaviour();
            return;
        }

        int MateCount = 0;

        for (int i = 0; i < RegisteredTiles.Length; i++)
        {
            if (RegisteredTiles[i] == null)
                continue;

            if (RegisteredTiles[i].Occupants == null)
                continue;

            for (int o = 0; o < RegisteredTiles[i].Occupants.Count; o++)
            {
                UrbAgent occupant = RegisteredTiles[i].Occupants[o];

                if (occupant.WasDestroyed)
                {
                    continue;
                }

                if (!occupant.IsBreeder || occupant.WasDestroyed || occupant == mAgent)
                {
                    continue;
                }

                UrbBreeder PossibleMate = occupant.Breeder;
                if (PossibleMate.BreedType != BreedType)
                {
                    continue;
                }

                float Result = LoveAction.Execute(mAgent, PossibleMate.mAgent, 0);
                if (Result <= 0)
                {
                    continue;
                }

                mAgent.Express(UrbDisplayFace.Expression.Joy);
                Result = LoveAction.Execute(PossibleMate.mAgent, mAgent, 0);

                if (Result <= 0)
                {
                    continue;
                }

                PossibleMate.mAgent.Express(UrbDisplayFace.Expression.Joy);
                if (++MateCount >= MateRequirement)
                {
                    Gestating = true;
                    break;
                }
            }
        }
        
        executeTileBehaviour.End();
        base.ExecuteTileBehaviour();
    }

    // Cached Values for Functional Coroutine;
    private UrbTile LastBreedTile = null;
    private UrbTile[] SearchCache;
    private GameObject OffspringTemplate;
    private UrbAgent OffspringAgent;
    private GameObject OffspringObject;

    private int Delay;
    private int NumberOffspring;
    private int OffspringChoice = -1;

    private void SetOffspringTemplate(int Choice)
    {
        if (Choice != OffspringChoice && Choice > -1 && Choice < OffspringObjects.Length)
        {
            OffspringChoice = Choice;
            OffspringTemplate = OffspringObjects[OffspringChoice];
            OffspringAgent = OffspringTemplate.GetComponent<UrbAgent>();
        }
    }

    public override IEnumerator FunctionalCoroutine()
    {
        if (!ValidToInterval())
        {
            yield break;
        }

        if (!Gestating || !CanBreed)
        {
            yield break;
        }
        
        yield return new WaitForSeconds(Gestation);

        if (!ValidToInterval())
        {
            yield break;
        }

        SetOffspringTemplate(Random.Range(0, OffspringObjects.Length));

        if (LastBreedTile != mAgent.CurrentTile)
        {
            LastBreedTile = mAgent.CurrentTile;
            SearchCache = DispersalDistance < 0 ? mAgent.Tileprint.GetAllPrintTiles(mAgent) : mAgent.Tileprint.GetBorderingTiles(mAgent, true, DispersalDistance);
        }

        Delay = Random.Range((int)0, (int)3);
        NumberOffspring = 0;

        for (int t = 0; t < SearchCache.Length; t++)
        {
            if (SearchCache[t] == null || SearchCache[t].Blocked || !SearchCache[t].TerrainPassable(RequiredOffspringTerrain) || SearchCache[t].RemainingCapacity < OffspringRequiredSpace)
            {
                continue;
            }

            if (Delay > 0)
            {
                Delay--;
                continue;
            }

            yield return BehaviourThrottle.PerformanceThrottle();

            if (UrbAgentSpawner.SpawnAgent(OffspringAgent, SearchCache[t], out OffspringObject, OffspringData[OffspringChoice]))
            {
                SetOffspringData(OffspringObject.GetComponent<UrbAgent>());

                Delay = Random.Range((int)0, (int)2);
                NumberOffspring++;
                mAgent.mBody.BodyComposition.RemoveRecipe(GestationRecipe);
                if(mAgent.Metabolism != null && OffspringAgent != null)
                {
                    mAgent.Metabolism.SpendEnergy(OffspringAgent.Mass);
                }

                SetOffspringTemplate(Random.Range(0, OffspringObjects.Length));
            }

            yield return new WaitForSeconds(Interval);
            
            if (NumberOffspring >= OffspringCount || mAgent.mBody.BodyComposition.ContainsLessThan(GestationRecipe))
            {
                break;
            }
        }
        Gestating = false;
    }

    public override UrbComponentData GetComponentData()
    {
        UrbComponentData Data = new UrbComponentData
        {
            Type = this.GetType().ToString(),
        };

        Data.Fields = new UrbFieldData[]
        {
            new UrbFieldData{ Name = "MateRequirement", Value = MateRequirement},
            new UrbFieldData{ Name = "MateCrowding", Value = MateCrowding},
            new UrbFieldData{ Name = "OffspringCount", Value = OffspringCount},
            new UrbFieldData{ Name = "OffspringRequiredSpace", Value = OffspringRequiredSpace},
            new UrbFieldData{ Name = "DispersalDistance", Value = DispersalDistance},
            new UrbFieldData{ Name = "Gestation", Value = Gestation},
            new UrbFieldData{ Name = "Gestating", Value = (Gestating)? 1 : 0},
        };

        Data.Strings = new UrbStringData[]
        {
            new UrbStringData{ Name = "BreedType", Value = BreedType.ToString()},
            new UrbStringData{ Name = "RequiredOffspringTerrain", Value = RequiredOffspringTerrain.ToString()}
        };

        Data.StringArrays = new UrbStringArrayData[]
        {
            UrbEncoder.EnumsToArray<UrbScentTag>("MateScents", MateScents),
            UrbEncoder.EnumsToArray<UrbScentTag>("RivalScents", RivalScents),
            UrbEncoder.ObjectsDataToArray("OffspringData", OffspringData)
        };

        Data.FieldArrays = new UrbFieldArrayData[]
        {
            UrbEncoder.GetArrayFromSubstances("GestationRecipe", GestationRecipe)
        };

        return Data;
    }



    public override bool SetComponentData(UrbComponentData Data)
    {
        MateRequirement = (int)UrbEncoder.GetField("MateRequirement", Data);
        MateCrowding = (int)UrbEncoder.GetField("MateCrowding", Data);
        OffspringCount = (int)UrbEncoder.GetField("OffspringCount", Data);
        OffspringRequiredSpace = UrbEncoder.GetField("OffspringRequiredSpace", Data);

        DispersalDistance = (int)UrbEncoder.GetField("DispersalDistance", Data);
        Gestation = UrbEncoder.GetField("Gestation", Data);
        Gestating = (UrbEncoder.GetField("Gestating", Data) > 0.0f);

        BreedType = UrbEncoder.GetEnum<UrbBreedTag>("BreedType", Data);
        RequiredOffspringTerrain = UrbEncoder.GetEnum<UrbPathTerrain>("RequiredOffspringTerrain", Data);
        OffspringData = UrbEncoder.GetObjectDataArray("OffspringData", Data);

        MateScents = UrbEncoder.GetEnumArray<UrbScentTag>("MateScents", Data);
        RivalScents = UrbEncoder.GetEnumArray<UrbScentTag>("RivalScents", Data);

        GestationRecipe = UrbEncoder.GetSubstancesFromArray("GestationRecipe", Data);


        return true;
    }

    public class UrbLoveAction : UrbAction
    {
        static Color Pink = new Color(1f, 0.0f, 0.25f);
        protected override string IconPath => IconDiretory + "Love";
        public override Color IconColor => Pink;

        public override float Test(UrbAgent target, float Modifier = 0)
        {
            float Sex = target.mBody.BodyComposition[UrbSubstanceTag.Female] + target.mBody.BodyComposition[UrbSubstanceTag.Male];
            return MobilityTest(target.mBody) + Sex + Modifier;
        }
    }
}
