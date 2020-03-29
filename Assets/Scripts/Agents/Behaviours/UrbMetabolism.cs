
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
[RequireComponent(typeof(UrbBody))]
public class UrbMetabolism : UrbBehaviour
{
    public UrbSubstance[] BodyGrowthRecipe;
    public UrbSubstanceTag BodyEnergyReserveStorage;
    public float GrowthRate = 10.0f;

    protected float EnergyDebt = 0;

    protected UrbRecipe[] ReserveToGrowth;
    protected UrbRecipe[] FoodToReserves;

    UrbEater mEater;

    public bool Healing { get; protected set; }
    public bool Starving { get; protected set; }

    public override void Initialize()
    {
        if(bInitialized)
        {
            return;
        }

        mEater = GetComponent<UrbEater>();
        InitializeGrowthRecipes();
        if(mEater != null)
        {
            InitializeReserveRecipes();
        }
        base.Initialize();
    }

    protected void InitializeGrowthRecipes()
    {
        ReserveToGrowth = new UrbRecipe[BodyGrowthRecipe.Length];

        for (int g = 0; g < BodyGrowthRecipe.Length; g++)
        {
            UrbRecipe Recipe = new UrbRecipe();
            Recipe.Ingredients = new UrbSubstance[1];
            Recipe.Product = BodyGrowthRecipe[g].Substance ;

            Recipe.Ingredients[0] = new UrbSubstance();
            Recipe.Ingredients[0].Substance = BodyEnergyReserveStorage;
            Recipe.Ingredients[0].SubstanceAmount = 1.0f;

            ReserveToGrowth[g] = Recipe;
        }
    }
    protected void InitializeReserveRecipes()
    {
        FoodToReserves = new UrbRecipe[mEater.FoodSubstances.Length];

        for (int e = 0; e < mEater.FoodSubstances.Length; e++)
        { 
            UrbRecipe Recipe = new UrbRecipe();
            Recipe.Ingredients = new UrbSubstance[1];
            Recipe.Product = BodyEnergyReserveStorage;

            //TODO: Define Nutrition requirements somewhere.
            Recipe.Ingredients[0] = new UrbSubstance();
            Recipe.Ingredients[0].Substance = mEater.FoodSubstances[e];
            Recipe.Ingredients[0].SubstanceAmount = 1.0f;


            FoodToReserves[e] = Recipe;
        }
    }

    public void SpendEnergy(float cost)
    {
        EnergyDebt += cost;
    }

    public override IEnumerator FunctionalCoroutine()
    {
        BehaviourThrottle.PerformanceThrottle();
        BuildReserves();
        BehaviourThrottle.PerformanceThrottle();

        if (EnergyDebt > 0)
        {
            Starving = false;
            float SpentCost = mAgent.Body.BodyComposition.RemoveSubstance(BodyEnergyReserveStorage, EnergyDebt);

            if(SpentCost < EnergyDebt)
            {
                Starving = true;
                float RemainingDebt = EnergyDebt - SpentCost;
                for(int b = 0; b < BodyGrowthRecipe.Length; b++)
                {
                    RemainingDebt -= mAgent.Body.BodyComposition.RemoveSubstance(BodyGrowthRecipe[b].Substance, RemainingDebt);
                    if(RemainingDebt <= 0)
                    {
                        break;
                    }
                }
            }

            EnergyDebt = 0;
        }

        GrowBody();

       
        yield return null;
    }

    protected float BuildReserves()
    {
        if(GrowthRate <= 0.0f || BodyEnergyReserveStorage == UrbSubstanceTag.None)
        {
            return 0.0f;
        }

        float Growth = 0.0f;

        if (mEater != null && mEater.Stomach != null)
        {
            for (int e = 0; e < mEater.FoodSubstances.Length; e++)
            {

                if (mEater.Stomach[mEater.FoodSubstances[e]] <= 0)
                {
                    continue;
                }

                Growth += mEater.Stomach.MixRecipe(FoodToReserves[e], GrowthRate);

                float GrowthSuccess = mEater.Stomach.TransferTo(mAgent.Body.BodyComposition, FoodToReserves[e].Product, Growth);

                if (GrowthSuccess < Growth)
                {
                    mEater.Stomach.RemoveSubstance(FoodToReserves[e].Product, Growth - GrowthSuccess);
                }

                if (Growth >= GrowthRate)
                {
                    break;
                }
            }
        }
        else
        {
            Growth += mAgent.Body.BodyComposition.AddSubstance(BodyEnergyReserveStorage, GrowthRate);
        }

        return Growth;
    }

    protected float GrowBody()
    {
        float Growth = 0.0f;

        Healing = false;
        for (int g = 0; g < BodyGrowthRecipe.Length; g++)
        {
            if (mAgent.Body.BodyComposition[BodyGrowthRecipe[g].Substance] < BodyGrowthRecipe[g].SubstanceAmount)
            {
                Healing = true;
                if (BodyEnergyReserveStorage == UrbSubstanceTag.None)
                {
                    Growth += mAgent.Body.BodyComposition.AddSubstance(BodyGrowthRecipe[g].Substance, GrowthRate);
                }
                else
                {
                    Growth += mAgent.Body.BodyComposition.MixRecipe(ReserveToGrowth[g], GrowthRate);
                }
                break;
            }
        }
        return Growth;
    }

    override public UrbComponentData GetComponentData()
    {
        
        UrbComponentData Data = base.GetComponentData();

        Data.Fields = new UrbFieldData[]
        {
            new UrbFieldData{ Name = "EnergyDebt", Value = EnergyDebt}
        };

        Data.Strings = new UrbStringData[]
        {
            new UrbStringData{ Name = "BodyEnergyReserveStorage", Value = BodyEnergyReserveStorage.ToString() }
        };

        Data.FieldArrays = new UrbFieldArrayData[]
        {
            UrbEncoder.GetArrayFromSubstances("BodyGrowthRecipe" , BodyGrowthRecipe),
        };

        Data.RecipeArrays = new UrbRecipeArrayData[]
        {
            new UrbRecipeArrayData{ Name = "ReserveToGrowth" , Value = ReserveToGrowth},
            new UrbRecipeArrayData{ Name = "FoodToReserves" , Value = FoodToReserves }

        };

        return Data;
    }

    override public bool SetComponentData(UrbComponentData Data)
    {
        EnergyDebt = UrbEncoder.GetField("EnergyDebt", Data);
        BodyEnergyReserveStorage = UrbEncoder.GetEnum<UrbSubstanceTag>("BodyEnergyReserveStorage", Data);
        BodyGrowthRecipe = UrbEncoder.GetSubstancesFromArray("BodyGrowthRecipe", Data);
        ReserveToGrowth = UrbEncoder.GetRecipeArray("ReserveToGrowth", Data);
        FoodToReserves = UrbEncoder.GetRecipeArray("FoodToReserves", Data);

        return true;
    }
}
