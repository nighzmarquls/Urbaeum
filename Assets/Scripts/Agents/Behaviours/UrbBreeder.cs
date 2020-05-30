using System.Collections;
using System.Collections.Generic;
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

    public float Gestation = 1.0f;

    public int DispersalDistance = -1;

    public UrbBreedTag BreedType = UrbBreedTag.Plant;

    public UrbScentTag[] MateScents;
    public UrbScentTag[] RivalScents;

    public UrbSubstance[] GestationRecipe;

    public GameObject[] OffspringObjects;
    protected UrbObjectData[] OffspringData = null;
    protected UrbMetabolism mMetabolism;

    protected static UrbLoveAction LoveAction = new UrbLoveAction();

    protected int Crowd = 0;
    public bool Gestating { get; protected set; }

    public override UrbUrgeCategory UrgeSatisfied => UrbUrgeCategory.Breed;

    public bool CanBreed { get {
            if (mAgent == null || mAgent.Body == null || mAgent.Body.BodyComposition == null)
                return false;

            return mAgent.Body.BodyComposition.ContainsMoreOrEqualThan(GestationRecipe);
        } }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }
        Gestating = false;
        Crowd = 0;

        if (OffspringData == null || OffspringData.Length <= 0)
        {
            EncodeOffspringData();
        }

        mMetabolism = GetComponent<UrbMetabolism>();

        base.Initialize();

    }

    protected void SetOffspringData(GameObject Offspring)
    {
        UrbBreeder OffspringBreeder = Offspring.GetComponent<UrbBreeder>();
        if (OffspringBreeder)
        {
            UrbObjectData[] ChildOffspringData = new UrbObjectData[OffspringData.Length];

            OffspringData.CopyTo(ChildOffspringData, 0);

            OffspringBreeder.OffspringData = ChildOffspringData;
        }
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
        return base.ValidToInterval() && mAgent.CurrentTile != null && OffspringData != null && OffspringData.Length > 0 && mAgent.Body != null;
    }

    public override float TileEvaluateCheck(UrbTile Target , bool Contact = false)
    {
        if (Gestating || Target == null || Target.Occupants == null || Crowd > MateCrowding)
        {
            return 0;
        }

        float Evaluation = (MateRequirement == 0)? mAgent.Mass : 0;

        for (int c = 0; c < Target.Occupants.Count; c++)
        {
            UrbBreeder MateCandidate = Target.Occupants[c].GetComponent<UrbBreeder>();

            if (MateCandidate != null && MateCandidate != this && MateCandidate.BreedType == BreedType)
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

    public override void ExecuteTileBehaviour()
    {
        if(MateRequirement == 0)
        {
            Gestating = true;
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
                UrbBreeder PossibleMate = RegisteredTiles[i].Occupants[o].GetComponent<UrbBreeder>();
                if (PossibleMate == null || PossibleMate == this)
                {
                    continue;
                }

                if (PossibleMate.BreedType == this.BreedType)
                {

                    float Result = LoveAction.Execute(mAgent, PossibleMate.mAgent, 0);
                    if (Result > 0)
                    {
                        Result = LoveAction.Execute(PossibleMate.mAgent, mAgent, 0);

                        if (Result > 0)
                        {
                            MateCount++;
                        }
                    }

                }
            }
        }

        if (MateCount >= MateRequirement)
        {
            Gestating = true;
        }

        base.ExecuteTileBehaviour();
    }

    override public IEnumerator FunctionalCoroutine()
    {
        if (!ValidToInterval())
            yield break;

        if (Gestating && CanBreed)
        {
            yield return new WaitForSeconds(Gestation);

            if (!ValidToInterval())
                yield break;

            UrbTile[] Search = DispersalDistance < 0 ? mAgent.Tileprint.GetAllPrintTiles(mAgent) : mAgent.Tileprint.GetBorderingTiles(mAgent, true, DispersalDistance);
            int NumberOffspring = 0;
            int Delay = Random.Range((int)0, (int)3);

            yield return BehaviourThrottle.PerformanceThrottle();

            for (int t = 0; t < Search.Length; t++)
            {
                if (Search[t] == null || Search[t].Blocked)
                {
                    continue;
                }

                if (Delay > 0)
                {
                    Delay--;
                    continue;
                }

                int OffspringChoice = Random.Range(0, OffspringObjects.Length);

                GameObject OffspringTemplate = OffspringObjects[OffspringChoice];

                GameObject OffspringObject;

                if (UrbAgentSpawner.SpawnAgent(OffspringTemplate, Search[t], out OffspringObject, OffspringData[OffspringChoice]))
                {
                    SetOffspringData(OffspringObject);

                    Delay = Random.Range((int)0, (int)2);
                    NumberOffspring++;
                    mAgent.Body.BodyComposition.RemoveRecipe(GestationRecipe);
                    if(mAgent.Metabolism != null)
                    {
                        UrbAgent OffspringAgent = OffspringObject.GetComponent<UrbAgent>();
                        if (OffspringAgent != null)
                        {
                            mAgent.Metabolism.SpendEnergy(OffspringAgent.Mass);
                        }
                    }
                }

                if (OffspringCount <= NumberOffspring || mAgent.Body.BodyComposition.ContainsLessThan(GestationRecipe))
                {
                    Gestating = false;
                    yield break;
                }
                else
                {
                    yield return new WaitForSeconds(Interval);
                }
            }
            Gestating = false;
        }
    }

    override public UrbComponentData GetComponentData()
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
            new UrbFieldData{ Name = "DispersalDistance", Value = DispersalDistance},
            new UrbFieldData{ Name = "Gestation", Value = Gestation},
            new UrbFieldData{ Name = "Gestating", Value = (Gestating)? 1 : 0},
        };

        Data.Strings = new UrbStringData[]
        {
            new UrbStringData{ Name = "BreedType", Value = BreedType.ToString()}
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



    override public bool SetComponentData(UrbComponentData Data)
    {
        MateRequirement = (int)UrbEncoder.GetField("MateRequirement", Data);
        MateCrowding = (int)UrbEncoder.GetField("MateCrowding", Data);
        OffspringCount = (int)UrbEncoder.GetField("OffspringCount", Data);

        DispersalDistance = (int)UrbEncoder.GetField("DispersalDistance", Data);
        Gestation = UrbEncoder.GetField("Gestation", Data);
        Gestating = (UrbEncoder.GetField("Gestating", Data) > 0.0f);

        BreedType = UrbEncoder.GetEnum<UrbBreedTag>("BreedType", Data);
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
            float Sex = target.Body.BodyComposition[UrbSubstanceTag.Female] + target.Body.BodyComposition[UrbSubstanceTag.Male];
            return MobilityTest(target.Body) + Sex + Modifier;
        }
    }
}
