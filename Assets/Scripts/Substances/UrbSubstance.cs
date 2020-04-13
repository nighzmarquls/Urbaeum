using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum UrbSubstanceTag
{
    None = 0,
    Seed,
    Stem,
    Leaf,
    Flower,
    Meat,
    Fat,
    Fluff,
    Male,
    Female,
    All
}

[System.Serializable]
public struct UrbRecipe
{
    public UrbSubstance[] Ingredients; 
    public UrbSubstanceTag Product;
}

[System.Serializable]
public struct UrbSubstance
{
    public UrbSubstanceTag Substance;
    public float SubstanceAmount;
}

public class UrbSubstances
{
    public const uint MaxTag = (uint)UrbSubstanceTag.All;

    //TODO: Make this driven by data
    static protected UrbScentTag[][] ScentsBySubstance = new UrbScentTag[][]
    {
        new UrbScentTag[0]
        , new UrbScentTag[0]
        , new UrbScentTag[]{ UrbScentTag.Plant}
        , new UrbScentTag[]{ UrbScentTag.Plant}
        , new UrbScentTag[]{ UrbScentTag.Plant, UrbScentTag.Sweet}
        , new UrbScentTag[]{ UrbScentTag.Meat}
        , new UrbScentTag[]{ UrbScentTag.Meat}
        , new UrbScentTag[]{ UrbScentTag.Fluff}
        , new UrbScentTag[]{ UrbScentTag.Male }
        , new UrbScentTag[]{ UrbScentTag.Female }
    };

    public static UrbScentTag[] Scent(UrbSubstanceTag input) {
        return ScentsBySubstance[(uint)input];
    }

    public static UrbScentTag[] Scent(UrbSubstanceTag[] input)
    {
        List<UrbScentTag> ScentList = new List<UrbScentTag>();
        for (int i = 0; i < input.Length; i++)
        {
            UrbScentTag[] Scents = UrbSubstances.Scent(input[i]);
            for (int s = 0; s < Scents.Length; s++)
            {
                if (ScentList.Contains(Scents[s]))
                {
                    continue;
                }
                ScentList.Add(Scents[s]);
            }
        }
        return ScentList.ToArray();
    }

    public static bool SubstanceSmellsLike(UrbSubstanceTag substance, UrbScentTag scent)
    {
        uint address = (uint)substance;
        for (int s = 0; s < ScentsBySubstance[address].Length; s++)
        {
            if(ScentsBySubstance[address][s] == scent)
            {
                return true;
            }
        }
        return false;
    }

    public static UrbSubstance[] GetIngredientProportions(UrbRecipe Recipe)
    {
        /*if(Recipe.Ingredients == null)
        {
            return new UrbRecipeIngredient[0];
        }*/
        UrbSubstance[] Proportions = new UrbSubstance[Recipe.Ingredients.Length];
        float Quantity = 0;
        for (int i = 0; i < Recipe.Ingredients.Length; i++)
        { 
            Quantity += Recipe.Ingredients[i].SubstanceAmount;
        }

        for (int i = 0; i < Recipe.Ingredients.Length; i++)
        {
            Proportions[i].Substance = Recipe.Ingredients[i].Substance;
            Proportions[i].SubstanceAmount = Recipe.Ingredients[i].SubstanceAmount/Quantity;
        }

        return Proportions;
    }
}
