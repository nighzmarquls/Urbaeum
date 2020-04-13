﻿using System.Collections;
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

    public float MateCost = 10.0f;
    public float Gestation = 1.0f;

    public UrbBreedTag BreedType = UrbBreedTag.Plant;

    public UrbScentTag[] MateScents;
    public UrbScentTag[] RivalScents;

    public UrbSubstance[] GestationRecipe;

    public GameObject[] OffspringObjects;
    protected UrbObjectData[] OffspringData = null;
    protected UrbMetabolism mMetabolism;



    public bool Gestating { get; protected set; }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }
        Gestating = false;

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
        if(OffspringBreeder)
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
        return base.ValidToInterval() && OffspringObjects.Length > 0;
    }

    override public IEnumerator FunctionalCoroutine()
    {
        if (!ValidToInterval())
            yield return null;

        UrbTile[] Adjacent = mAgent.CurrentMap.GetNearestAdjacent(transform.position, true);

        int MateCount = 0;

        foreach(UrbTile Tile in Adjacent)
        {
            yield return BehaviourThrottle.PerformanceThrottle();
            if(Gestating)
            {    
                break;
            }

            if (Tile == null)
            {
                continue;
            }

            if(Tile.CurrentContent == null)
            {
                continue;
            }

            UrbBreeder MateCandidate = Tile.CurrentContent.GetComponent<UrbBreeder>();

            if(MateCandidate != null && MateCandidate != this && MateCandidate.BreedType == BreedType)
            {
                if(mMetabolism != null && MateRequirement > 0)
                {
                    mMetabolism.SpendEnergy(MateCost);
                }
                MateCount++;
            }
        }

        yield return null;

        if (Gestating || MateCount >= MateRequirement && MateCount < MateCrowding && OffspringCount > 0 && mAgent.Body.BodyComposition.ContainsMoreOrEqualThan(GestationRecipe))
        {
            Gestating = true;
            yield return new WaitForSeconds(Gestation);

            if (!ValidToInterval())
                yield return null;

            Adjacent = mAgent.CurrentMap.GetNearestAdjacent(transform.position);
            int NumberOffspring = 0;
            int Delay = Random.Range((int)0, (int)3);

            foreach (UrbTile Tile in Adjacent)
            {
                yield return BehaviourThrottle.PerformanceThrottle();

                if (Tile == null || Tile.Blocked)
                {
                    continue;
                }

                if (Tile.CurrentContent != null)
                    continue;

                if (Delay > 0)
                {
                    Delay--;
                    continue;
                }

                int OffspringChoice = Random.Range(0, OffspringObjects.Length);

                GameObject OffspringTemplate = OffspringObjects[OffspringChoice];

                GameObject OffspringObject;

                if (UrbAgentSpawner.SpawnAgent(OffspringTemplate, Tile, out OffspringObject, OffspringData[OffspringChoice]))
                {
                    SetOffspringData(OffspringObject);

                    Delay = Random.Range((int)0, (int)2);
                    NumberOffspring++;
                    mAgent.Body.BodyComposition.RemoveRecipe(GestationRecipe);
                }
                
                if (OffspringCount <= NumberOffspring || mAgent.Body.BodyComposition.ContainsLessThan(GestationRecipe))
                {
                    Gestating = false;
                    yield break;
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

            new UrbFieldData{ Name = "MateCost", Value = MateCost},
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
        MateRequirement = (int) UrbEncoder.GetField("MateRequirement", Data);
        MateCrowding = (int)UrbEncoder.GetField("MateCrowding", Data);
        OffspringCount = (int)UrbEncoder.GetField("OffspringCount", Data);

        MateCost = UrbEncoder.GetField("MateCost", Data);
        Gestation = UrbEncoder.GetField("Gestation", Data);
        Gestating = (UrbEncoder.GetField("Gestating", Data) > 0.0f);

        BreedType = UrbEncoder.GetEnum<UrbBreedTag>("BreedType", Data);
        OffspringData = UrbEncoder.GetObjectDataArray("OffspringData", Data);

        MateScents = UrbEncoder.GetEnumArray<UrbScentTag>("MateScents", Data);
        RivalScents = UrbEncoder.GetEnumArray<UrbScentTag>("RivalScents", Data);

        GestationRecipe = UrbEncoder.GetSubstancesFromArray("GestationRecipe", Data);
        
   
        return true;
    }
}
