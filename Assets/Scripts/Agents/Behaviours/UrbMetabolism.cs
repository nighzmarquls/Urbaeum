
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug_1 = Debug;

[RequireComponent(typeof(UrbAgent))]
[RequireComponent(typeof(UrbBody))]
public class UrbMetabolism : UrbBehaviour
{
    public const float EnergyConversionRatio = 0.00006f ;
    public UrbSubstance[] BodyGrowthRecipe;
    public UrbSubstanceTag BodyEnergyReserveStorage;
    public float GrowthRate = 10.0f;

    public bool MetabolismReady = false;

    protected float EnergyDebt = 0;

    protected UrbRecipe[] ReserveToGrowth;
    protected UrbRecipe[] FoodToReserves;
    
    public float EnergyBudget {
        get {
            if (!HasBody)
            {
                return 0;
            }
            return (mBody.BodyComposition[BodyEnergyReserveStorage] - EnergyDebt);
        }
    }

    public bool Healing { get; protected set; }
    public bool Starving { get; protected set; }

    public override void OnEnable()
    {
        MetabolismReady = false;
        base.OnEnable();
        InitializeGrowthRecipes();
        if(IsEater)
        {
            Debug.Log(mAgent.ID + " Metabolism OnEnable");
            InitializeReserveRecipes();
        }
        
        if (HasBody && mBody.HasEnableBeenCalled == false)
        {
            mBody.OnEnable();  
        }
        MetabolismReady = true;
    }

    protected void InitializeGrowthRecipes()
    {
        ReserveToGrowth = new UrbRecipe[BodyGrowthRecipe.Length];

        //We don't need a wholly-new-struct every time b/c they're copy by value. 
        UrbRecipe Recipe = new UrbRecipe();
        for (int g = 0; g < BodyGrowthRecipe.Length; g++)
        {
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
        FoodToReserves = new UrbRecipe[Eater.FoodSubstances.Length];

        for (int e = 0; e < Eater.FoodSubstances.Length; e++)
        { 
            UrbRecipe Recipe = new UrbRecipe();
            Recipe.Ingredients = new UrbSubstance[1];
            Recipe.Product = BodyEnergyReserveStorage;

            //TODO: Define Nutrition requirements somewhere.
            Recipe.Ingredients[0] = new UrbSubstance();
            Recipe.Ingredients[0].Substance = Eater.FoodSubstances[e];
            Recipe.Ingredients[0].SubstanceAmount = 1.0f;
            
            FoodToReserves[e] = Recipe;
        }
    }

    public void SpendEnergy(float cost)
    {
        EnergyDebt += cost*EnergyConversionRatio;
    }

    public override IEnumerator FunctionalCoroutine()
    {
        if (!HasEnableBeenCalled || !MetabolismReady)
        {
            yield return new WaitForFixedUpdate();
        }

        //SpendEnergy(mAgent.Body.BodyComposition.Mass);
        BuildReserves();
        GrowBody(GrowthRate);

        if (EnergyDebt > 0)
        {
            Starving = false;
            float SpentCost = mAgent.mBody.BodyComposition.RemoveSubstance(BodyEnergyReserveStorage, EnergyDebt);

            if (SpentCost < EnergyDebt)
            {
                Starving = true;
                mAgent.Express(UrbDisplayFace.Expression.Cry);

                float RemainingDebt = EnergyDebt - SpentCost;
                for (int b = 0; b < BodyGrowthRecipe.Length; b++)
                {
                    RemainingDebt -= mAgent.mBody.BodyComposition.RemoveSubstance(BodyGrowthRecipe[b].Substance, RemainingDebt);
                    if (RemainingDebt <= 0)
                    {
                        EnergyDebt = 0;
                        break;
                    }
                }
                EnergyDebt = RemainingDebt;
            }
            else
            {
                mAgent.Express(UrbDisplayFace.Expression.Default);
            }
        }

        yield return null;
    }

    protected float BuildReserves()
    {
        if(GrowthRate <= 0.0f || BodyEnergyReserveStorage == UrbSubstanceTag.None)
        {
            return 0.0f;
        }

        float Growth = 0.0f;

        if (!IsEater || Eater.Stomach == null)
        {
            return mBody.BodyComposition.AddSubstance(BodyEnergyReserveStorage, GrowthRate);
        }
        
        for (int e = 0; e < Eater.FoodSubstances.Length; e++)
        {
            if (Eater.Stomach[Eater.FoodSubstances[e]] <= 0)
            {
                continue;
            }

            Growth += Eater.Stomach.MixRecipe(FoodToReserves[e], GrowthRate);

            float GrowthSuccess = Eater.Stomach.TransferTo(mBody.BodyComposition, FoodToReserves[e].Product, Growth);

            if (GrowthSuccess < Growth)
            {
                Eater.Stomach.RemoveSubstance(FoodToReserves[e].Product, Growth - GrowthSuccess);
            }

            if (Growth >= GrowthRate)
            {
                return Growth;
            }
        }

        return Growth;
    }

    public float GrowBody(float Amount = 0)
    {
        float Growth = 0.0f;

        Healing = false;
        
        if(HasBody && mBody.BodyComposition.Membrane.Damage > 0)
        {
            Growth = mBody.BodyComposition.Membrane.Restore(GrowthRate); Healing = true;
            mAgent.Express(UrbDisplayFace.Expression.Cry);
        }

        for (int g = 0; g < BodyGrowthRecipe.Length; g++)
        {
            if(Growth > Amount)
            {
                break;
            }

            if (!(mAgent.mBody.BodyComposition[BodyGrowthRecipe[g].Substance] < BodyGrowthRecipe[g].SubstanceAmount))
            {
                continue;
            }

            if (BodyEnergyReserveStorage == UrbSubstanceTag.None)
            {
                Growth += mAgent.mBody.BodyComposition.AddSubstance(BodyGrowthRecipe[g].Substance, Amount);
            }
            else
            {
                Growth += mAgent.mBody.BodyComposition.MixRecipe(ReserveToGrowth[g], Amount);
                SpendEnergy(Growth);
            }
        }

        return Growth;
    }

    public override UrbComponentData GetComponentData()
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

    public override bool SetComponentData(UrbComponentData Data)
    {
        EnergyDebt = UrbEncoder.GetField("EnergyDebt", Data);
        BodyEnergyReserveStorage = UrbEncoder.GetEnum<UrbSubstanceTag>("BodyEnergyReserveStorage", Data);
        BodyGrowthRecipe = UrbEncoder.GetSubstancesFromArray("BodyGrowthRecipe", Data);
        ReserveToGrowth = UrbEncoder.GetRecipeArray("ReserveToGrowth", Data);
        FoodToReserves = UrbEncoder.GetRecipeArray("FoodToReserves", Data);

        return true;
    }
}
