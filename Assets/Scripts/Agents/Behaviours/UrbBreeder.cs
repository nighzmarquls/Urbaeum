using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Unity.Assertions;
using Unity.Profiling;
using UnityEngine;
using Random = UnityEngine.Random;

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
    //TODO: Consider Gestation as an abstraction over certain ingredients mixing?
    public bool Gestating { get; protected set; }

    public override UrbUrgeCategory UrgeSatisfied => UrbUrgeCategory.Breed;
    
//This pattern is useful for high-frequency events   
// #if DEBUG
//BreedReason is relevant when CanBreed
    protected string _breedReason = "Coroutine not run";
//Relevant primarily when Gestating    
    protected string _birthStatus = "Not yet set";
// #endif
    
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
            
            //Currently, breeding requires agents to all be alive
            //But this may not work for agents that spawn from corpses 
            //Phoenixes and Zombies come to mind. 
            if (!mAgent.Alive)
            {
                // _potentiallyBreed = false;
                return false;
            }
            
            //Defined as an external call, it will be difficult for the compiler/JIT to 
            //optimize multiple references to the frameCount.
            int currentFrame = Time.frameCount;
            if (breedCheckedFrame == currentFrame)
            {
                return _canBreed;
            }

            breedCheckedFrame = currentFrame;
            
            if (Gestating)
            {
                _canBreed = false;
                return false;
            }
            
            //TODO: (IMO) Breeding should mean the Agent is well-off, compositionally-speaking
            //So we should consider choosing a ratio of Composition excess.
            _canBreed = mAgent.mBody.BodyComposition.ContainsMoreOrEqualThan(GestationRecipe);
            return _canBreed;
        }
    }

    public override void OnEnable()
    {
        if (OffspringData == null || OffspringData.Length <= 0)
        {
            EncodeOffspringData();
        }
        
        Gestating = false;
        Crowd = 0;
        _canBreed = false;
        breedCheckedFrame = 0;
        
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
        Assert.IsTrue(mAgent.Breeder == this);
        
        if (!base.ValidToInterval())
        {
            _birthStatus = _breedReason = "UrbBase interval invalid";
            return false;
        }
        
        if (mAgent.CurrentTile == null || OffspringData == null)
        {
            _birthStatus = _breedReason = "Current Tile/OffSpringData == Null";
            return false;
        }

        if (!HasBody)
        {
            _birthStatus = _breedReason = "HasBody False";
            return false;
        }

        if (OffspringData.Length == 0)
        {
            _birthStatus = _breedReason = "OffspringData len == 0";
            return false;
        }

        return true;
    }

    static ProfilerMarker s_TileEvaluateCheck_p = new ProfilerMarker("UrbBreeder.TileEvaluateCheck");

    public override float TileEvaluateCheck(UrbTile Target , bool Contact = false)
    {
        //"Temporary" using for now
        using (s_TileEvaluateCheck_p.Auto())
        {
            if (Gestating)
            {
                _breedReason = "currently gestating";
                return 0;
            }

            Assert.IsNotNull(Target, "Target != null");
            
            if (Target == null)
            {
                _breedReason = "TargetTile is null";
                return 0;
            }
            
            Assert.IsNotNull(Target.Occupants, "Target.Occupants != null");

            if (Target.Occupants == null)
            {
                _breedReason = "Tile Occupants list is null";
                return 0;
            }
            
            if (Crowd > MateCrowding)
            {
                _breedReason = "Crowd > Mate Crowding";
                return 0;
            }
            
            float Evaluation = (MateRequirement == 0)? mAgent.Mass : 0;

            for (int c = 0; c < Target.Occupants.Count; c++)
            {
                var occupant = Target.Occupants[c];
                if (occupant.WasDestroyed)
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

                if (Crowd >= MateCrowding)
                {
                    _breedReason = "Crowd >= MateCrowding";
                    return 0;
                }
            }
            return Evaluation;
        }
    }

    public override float BehaviourEvaluation
    {
        get {
            if (Crowd >= MateCrowding)
            {
                return 0;
            }

            float retVal = base.BehaviourEvaluation;
            return retVal;
    }
        protected set
        {
            var val = value;
            base.BehaviourEvaluation = val;
        }
    }

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
            _breedReason = "Game paused or agent is dead";
            executeTileBehaviour.End();
            return;
        }

        if (MateRequirement == 0 && CanBreed)
        {
            _breedReason = "MateRequirement is 0 and can breed";
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

    public string BreedReason()
    {
        return Gestating ? _birthStatus : _breedReason;
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

    protected IEnumerator AttemptSpawn()
    {
        //TODO: Make spawning cost energy ?
        SetOffspringTemplate(Random.Range(0, OffspringObjects.Length));

        if (LastBreedTile != mAgent.CurrentTile)
        {
            LastBreedTile = mAgent.CurrentTile;
            SearchCache = DispersalDistance < 0 ? mAgent.Tileprint.GetAllPrintTiles(mAgent) : mAgent.Tileprint.GetBorderingTiles(mAgent, true, DispersalDistance);
        }

        Delay = Random.Range((int)0, (int)(SearchCache.Length / 2));
        NumberOffspring = 0;

        for (int t = 0; t < SearchCache.Length; t++)
        {
            if (--Delay > 0)
            {
                _birthStatus = "not ready to spawn";
                continue;
            }
            
            var current = SearchCache[t];

            if (current == null)
            {
                _birthStatus = $"tile {t} was null";
                continue;
            }

            if (current.Blocked)
            {
                _birthStatus = "tile was blocked";
                continue;
            }
            
            if (!current.TerrainPassable(RequiredOffspringTerrain) || current.RemainingCapacity < OffspringRequiredSpace)
            {
                _birthStatus = "terrain not passable or doesnt match required space";
                continue;
            }
            
            if (NumberOffspring >= OffspringCount)
            {
                _birthStatus = "Too many offspring";
                break;
            }

            if (mAgent.mBody.BodyComposition.ContainsLessThan(GestationRecipe))
            {
                _birthStatus = "Not enough resources to spawn baby";
                break;
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
            else
            {
                _birthStatus = "Spawn attempt failed";
                logger.LogWarning("Failed to spawn offspring", mAgent);
            }
        }
        
        Gestating = false;
    }
    
    public override IEnumerator FunctionalCoroutine()
    {
        yield return new WaitUntil(ValidToInterval);
        
        if (!Gestating)
        {
            _birthStatus = "not gestating";
            yield break;
        }
        
        //TODO: Better feedback for debugging purposes.
        // TimeUntilSpawn = Gestation - (CurrentTime - TimeOfImpregnation) ? 
        _birthStatus = "to sleeping intervals";
        yield return new WaitForSeconds(Interval);
        _birthStatus = "GestationBake Interval";
        yield return new WaitForSeconds(Gestation - Interval);
        _birthStatus = "after sleeping intervals";

        if (!ValidToInterval())
        {
            _birthStatus = "secondary ValidToInterval check failed";
            yield break;
        }

        if (Gestating && mAgent.Alive)
        {
            yield return AttemptSpawn();
        }
        else
        {
            _birthStatus = "gestation check failed";
        }
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
