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
    Muscle,
    Fat,
    Fluff,
    Male,
    Female,
    Nerves,
    Teeth,
    Claw,
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
    public static void RegisterSubstanceProperties()
    {
        UrbSubstanceProperties.Set(UrbSubstanceTag.Stem, new UrbSubstanceProperty { Scent = new UrbScentTag[] { UrbScentTag.Plant } } );
        UrbSubstanceProperties.Set(UrbSubstanceTag.Leaf, new UrbSubstanceProperty { Scent = new UrbScentTag[] { UrbScentTag.Plant } } );
        UrbSubstanceProperties.Set(UrbSubstanceTag.Flower, new UrbSubstanceProperty { Scent = new UrbScentTag[] { UrbScentTag.Plant, UrbScentTag.Sweet } } );
        UrbSubstanceProperties.Set(UrbSubstanceTag.Muscle, new UrbSubstanceProperty { Scent = new UrbScentTag[] { UrbScentTag.Meat } } );
        UrbSubstanceProperties.Set(UrbSubstanceTag.Nerves, new UrbSubstanceProperty { Scent = new UrbScentTag[] { UrbScentTag.Meat } });
        UrbSubstanceProperties.Set(UrbSubstanceTag.Fat, new UrbSubstanceProperty { Scent = new UrbScentTag[] { UrbScentTag.Meat } });
        UrbSubstanceProperties.Set(UrbSubstanceTag.Fluff, new UrbSubstanceProperty { Scent = new UrbScentTag[] { UrbScentTag.Fluff } });
        UrbSubstanceProperties.Set(UrbSubstanceTag.Male, new UrbSubstanceProperty { Scent = new UrbScentTag[] { UrbScentTag.Male } });
        UrbSubstanceProperties.Set(UrbSubstanceTag.Female, new UrbSubstanceProperty { Scent = new UrbScentTag[] { UrbScentTag.Female } });
    }

    public static UrbScentTag[] Scent(UrbSubstanceTag input) {
        return UrbSubstanceProperties.Get(input).Scent;
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

    public static bool SubstanceSmellsLike(UrbSubstanceTag Substance, UrbScentTag Scent)
    {
        return UrbSubstanceProperties.CheckScent(Substance, Scent);
    }

    public static UrbSubstance[] GetIngredientProportions(UrbRecipe Recipe)
    {
        
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
