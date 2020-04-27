using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbEater : UrbBehaviour
{
    public UrbSubstanceTag[] FoodSubstances;
    public UrbComposition Stomach;

    [SerializeField]
    protected UrbBiteInteraction Interaction;

    public UrbScentTag[] FoodScents { get; protected set; }

    protected UrbMetabolism mMetabolism;

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }
        mMetabolism = GetComponent<UrbMetabolism>();
        Stomach = new UrbComposition();
        FoodScents = UrbSubstances.Scent(FoodSubstances);

        
        base.Initialize();
        if (mAgent.Body.BodyComposition == null)
        {
            mAgent.Body.Initialize();
        }

        mAgent.Body.BodyComposition.AddComposition(Stomach);
    }

    protected override bool ValidToInterval()
    {
        return base.ValidToInterval() && FoodSubstances.Length > 0 && Stomach != null;
    }

    override public IEnumerator FunctionalCoroutine()
    {
        UrbTile[] Search = GetSearchTiles(true);
        mAgent.Interacting = false;
        for (int t = 0; t < Search.Length; t++)
        {
            if (Stomach.AvailableCapacity > 0)
            {
                if(mMetabolism != null)
                {
                    if (mMetabolism.EnergyBudget < Interaction.CostEstimate(mAgent) && Stomach.CurrentCapacty > 0)
                    {
                        Debug.Log("Energy Budget: " + mMetabolism.EnergyBudget + " And Digesting");
                        break;
                    }
                }
                if (Search[t] == null)
                {
                    continue;
                }

                if (Search[t].CurrentContent == null)
                {
                    continue;
                }

                for (int o = 0; o < Search[t].Occupants.Count; o++)
                {
                    if (Search[t].Occupants[o] == mAgent)
                    {
                        continue;
                    }
                    UrbBody PossibleFood = Search[t].Occupants[o].Body;
                    
                    if (PossibleFood != null)
                    {
                        bool ContainsFood = false;
                        for (int f = 0; f < FoodSubstances.Length; f++)
                        {
                            if (PossibleFood.BodyComposition == null)
                            {
                                break;
                            }
                           

                            if (PossibleFood.BodyComposition[FoodSubstances[f]] > 0)
                            {
                                ContainsFood = true;
                                break;
                            }
                        }

                        if(ContainsFood)
                        {
                            float BiteSize;
                            if (Interaction.AttemptInteraction(mAgent, Search[t].Occupants[o], out BiteSize))
                            {
                                mAgent.Interacting = true;
                                for (int f = 0; f < FoodSubstances.Length; f++)
                                {
                                    float Eaten = PossibleFood.BodyComposition.TransferTo(Stomach, FoodSubstances[f], BiteSize);
                                    
                                    if (Eaten >= BiteSize)
                                    {
                                        yield break;
                                    }

                                }
                            }
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }

        yield return null;
    }

    override public UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();

       
        UrbSubstance[] StomachContents = (Stomach == null)? new UrbSubstance[0] : Stomach.GetCompositionIngredients();

        Data.FieldArrays = new UrbFieldArrayData[]
        {
            UrbEncoder.GetArrayFromSubstances("StomachContents" , StomachContents),
        };

        Data.StringArrays = new UrbStringArrayData[]
        {
            UrbEncoder.EnumsToArray("FoodSubstances", FoodSubstances),
            UrbEncoder.EnumsToArray("FoodScents",FoodScents)
        };

        return Data;
    }

    override public bool SetComponentData(UrbComponentData Data)
    {
        FoodSubstances = UrbEncoder.GetEnumArray<UrbSubstanceTag>("FoodSubstances", Data);
        FoodScents = UrbEncoder.GetEnumArray<UrbScentTag>("FoodScents", Data);
        Stomach = new UrbComposition(UrbEncoder.GetSubstancesFromArray("StomachContents", Data));
        return true;
    }
}

[System.Serializable]
public class UrbBiteInteraction : UrbInteraction
{
    public UrbTest HitTest;
    public UrbTest DodgeTest;
    public UrbTest BiteTest;
    public UrbTest SoakTest;

    public override float CostEstimate(UrbAgent Instigator)
    {
        return Test(Instigator, HitTest.Category) + Test(Instigator,BiteTest.Category);
    }

    public override bool AttemptInteraction(UrbAgent Instigator,UrbAgent Target, out float Result)
    {
        UrbMetabolism BiterMetabolism = Instigator.GetComponent<UrbMetabolism>();
        UrbMetabolism TargetMetabolism = Target.GetComponent<UrbMetabolism>();
        Result = 0;

        float HitCheck = Test(Instigator, HitTest.Category);
       
        if (BiterMetabolism != null)
        {
            BiterMetabolism.SpendEnergy(HitCheck);
            Instigator.Display.QueueEffectDisplay(HitTest, Instigator.transform.position);
        }

        if (HitCheck <= 0)
        {
            return false;
        }

        float DodgeCheck = Test(Target, DodgeTest.Category);

        if (TargetMetabolism != null)
        {
            TargetMetabolism.SpendEnergy(DodgeCheck);
        }

        if (DodgeCheck > HitCheck)
        {
            Target.Display.QueueEffectDisplay(DodgeTest, Target.transform.position);
            return false;
        }

        float BiteCheck = Test(Instigator, BiteTest.Category, HitCheck - DodgeCheck);

        if (BiterMetabolism != null)
        {
            BiterMetabolism.SpendEnergy(BiteCheck);
        }

        Result = BiteCheck;

        float SoakCheck = Test(Target, SoakTest.Category);

        if(SoakCheck > HitCheck)
        {
            Target.Display.QueueEffectDisplay(SoakTest, Target.transform.position);
            return false;
        }

        Instigator.Display.QueueEffectDisplay(BiteTest, Target.transform.position);
        return true;
    }
}
