using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbComposition
{
    public float MaxCapacity { get; protected set; } = 1000;
    public float CurrentCapacty { get; protected set; }  = 0;

    public void SetSize(int Size)
    {
        MaxCapacity = 1000 * (Size * Size);
    }

    public float Emptiness {
        get {
            return AvailableCapacity / MaxCapacity;
        }
    }

    public float Fullness {
        get {
            return CurrentCapacty / MaxCapacity;
        }
    }

    public float AvailableCapacity {
        get {
            return MaxCapacity - CurrentCapacty;
        }
    }

    public float this[UrbSubstanceTag Tag] {
        get {
            if(Substances.ContainsKey(Tag))
            {
                return Substances[Tag];
            }
            else
            {
                return 0.0f;
            }
        }
    }

    protected Dictionary<UrbSubstanceTag, float> Substances;

    public UrbSubstance[] GetCompositionIngredients()
    {
        UrbSubstance[] Ingredients = new UrbSubstance[Substances.Keys.Count];

        int i = 0;
        foreach(UrbSubstanceTag tag in Substances.Keys)
        {
            UrbSubstance Ingredient = new UrbSubstance();
            Ingredient.Substance = tag;
            Ingredient.SubstanceAmount = Substances[tag];

            Ingredients[i] = Ingredient;
            i++;
        }
        return Ingredients;
    }

    bool Dirty = true;
    UrbScentTag[] CachedScents;
    public UrbScentTag[] GetScent()
    {
        if (Dirty)
        {
            List<UrbScentTag> ScentList = new List<UrbScentTag>();

            foreach (UrbSubstanceTag tag in Substances.Keys)
            {
                if (Substances[tag] > 0.0)
                {
                    UrbScentTag[] Scents = UrbSubstances.Scent(tag);
                    for (int s = 0; s < Scents.Length; s++)
                    {
                        if (ScentList.Contains(Scents[s]))
                        {
                            continue;
                        }
                        ScentList.Add(Scents[s]);
                    }
                }
            }
            CachedScents = ScentList.ToArray();
            Dirty = false;
        }

        return CachedScents;
    }

    public UrbComposition()
    {
        Substances = new Dictionary<UrbSubstanceTag, float>();
    }

    public UrbComposition(UrbSubstance[] CompositionRecipe)
    {
        Substances = new Dictionary<UrbSubstanceTag, float>();

        for(int i = 0; i < CompositionRecipe.Length; i++)
        {
            if (Substances.ContainsKey(CompositionRecipe[i].Substance))
            {
                Substances[CompositionRecipe[i].Substance] += CompositionRecipe[i].SubstanceAmount;
            }
            else
            {
                Substances.Add(CompositionRecipe[i].Substance, CompositionRecipe[i].SubstanceAmount);
            }
            CurrentCapacty += CompositionRecipe[i].SubstanceAmount;
        }
    }

    public float AddRecipe(UrbSubstance[] Recipe)
    {
        float TransferAmount = 0;
        for (int r = 0; r < Recipe.Length; r++)
        {
            TransferAmount += AddSubstance(Recipe[r].Substance, Recipe[r].SubstanceAmount);
        }
        return TransferAmount;
    }

    public float AddSubstance(UrbSubstanceTag Tag, float Amount)
    {
        if (Amount <= 0.0f)
        {
            return 0.0f;
        }

        float TransferAmount = Amount;

        float CapacityAfter = CurrentCapacty + Amount;
        if (CapacityAfter > MaxCapacity)
        {
            TransferAmount -= CapacityAfter - MaxCapacity;
        }

        if(Substances.ContainsKey(Tag))
        {
            Substances[Tag] += TransferAmount;
        }
        else
        {
            Substances.Add(Tag, TransferAmount);
            Dirty = true;
        }
        
        CurrentCapacty += TransferAmount;
    
        return TransferAmount;
    }

    public float RemoveRecipe(UrbSubstance[] Recipe)
    {
        float TransferAmount = 0;
        for(int r = 0; r < Recipe.Length; r++)
        {
            TransferAmount += RemoveSubstance(Recipe[r].Substance, Recipe[r].SubstanceAmount);
        }
        return TransferAmount;
    }

    public float RemoveSubstance(UrbSubstanceTag Tag, float Amount)
    {
        if (Amount <= 0.0f)
        {
            return 0.0f;
        }

        float TransferAmount = Amount;
        //Debug.Log("Attempting Transfer " + TransferAmount + " from " + Tag.ToString());

        float CapacityAfter = CurrentCapacty + Amount;
        if (CapacityAfter < 0)
        {
            TransferAmount += CapacityAfter;
        }
        //Debug.Log("After Capacity Check Transfer " + TransferAmount);

        if (Substances.ContainsKey(Tag))
        {
            if (Substances[Tag] >= TransferAmount)
            {
                Substances[Tag] -= TransferAmount;
            }
            else
            {
                TransferAmount += Substances[Tag] - TransferAmount;
                Substances[Tag] = 0;
                Dirty = true;
            } 
           // Debug.Log("After Quantity Check " + TransferAmount);
        }
        else
        {
            TransferAmount = 0;
        }
        CurrentCapacty -= TransferAmount;
        //Debug.Log(TransferAmount + " removed from " + Tag.ToString());
        return TransferAmount;
    }

    public float TransferTo(UrbComposition Target, UrbSubstanceTag Tag, float Amount)
    {
        if(Amount <= 0.0f)
        {
            return 0.0f;
        }

        float TransferAmount = Mathf.Min(Amount, Target.AvailableCapacity);

        TransferAmount = RemoveSubstance(Tag, Amount);

        if(TransferAmount > 0)
        {
            Target.AddSubstance(Tag, TransferAmount);
        }
        else
        {
            //Debug.Log( Tag.ToString() + " Not Available to Transfer.");
        }

        return TransferAmount;
    }

    public float MixRecipe(UrbRecipe Recipe, float Amount)
    {
        if(Amount > CurrentCapacty)
        {
            Amount = CurrentCapacty;
        }

        UrbSubstance[] Proportions = UrbSubstances.GetIngredientProportions(Recipe);

        float Result = Amount;

        for(int r = 0; r < Proportions.Length; r++)
        {
            float PossibleAmount = this[Proportions[r].Substance] / Proportions[r].SubstanceAmount;
            if(PossibleAmount < Result)
            {
                Result = PossibleAmount;
            }
        }

        if(Result > 0.0f)
        {
            for(int c = 0; c < Proportions.Length; c++)
            {
                RemoveSubstance(Proportions[c].Substance, Proportions[c].SubstanceAmount * Result);
            }

            AddSubstance(Recipe.Product, Result);
        }
        else
        {
            Result = 0.0f;
        }

        return Result;    
    }

    public float MixRecipeInto(UrbComposition Target, UrbRecipe Recipe, float Amount)
    {
        float Result = MixRecipe(Recipe, Amount);

        return TransferTo(Target, Recipe.Product,Result);
    }

    //TODO: Optimize this
    public bool ContainsEqualTo(UrbComposition composition)
    {
        foreach (UrbSubstanceTag tag in Substances.Keys)
        {
            if (composition.Substances.ContainsKey(tag))
            {
                if (Substances[tag] != composition[tag])
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public bool ContainsEqualTo(UrbSubstance[] Recipe)
    {
        for(int i = 0; i < Recipe.Length; i++)
        {
            if (Substances.ContainsKey(Recipe[i].Substance))
            {
                if (Substances[Recipe[i].Substance] != Recipe[i].SubstanceAmount)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public bool ContainsLessThan(UrbComposition composition)
    {
        foreach (UrbSubstanceTag tag in Substances.Keys)
        {
            if (composition.Substances.ContainsKey(tag))
            {
                if (Substances[tag] >= composition[tag])
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool ContainsLessThan(UrbSubstance[] Recipe)
    {
        for (int i = 0; i < Recipe.Length; i++)
        {
            if (Substances.ContainsKey(Recipe[i].Substance))
            {
                if (Substances[Recipe[i].Substance] >= Recipe[i].SubstanceAmount)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool ContainsMoreOrEqualThan(UrbComposition composition)
    {
        foreach (UrbSubstanceTag tag in Substances.Keys)
        {
            if (composition.Substances.ContainsKey(tag))
            {
                if (Substances[tag] < composition[tag])
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public bool ContainsMoreOrEqualThan(UrbSubstance[] Recipe)
    {
        for (int i = 0; i < Recipe.Length; i++)
        {
            if (Substances.ContainsKey(Recipe[i].Substance))
            {
                if (Substances[Recipe[i].Substance] < Recipe[i].SubstanceAmount)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public bool ContainsMoreThan(UrbComposition composition)
    {
        foreach (UrbSubstanceTag tag in Substances.Keys)
        {
            if (composition.Substances.ContainsKey(tag))
            {
                if (Substances[tag] <= composition[tag])
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public bool ContainsMoreThan(UrbSubstance[] Recipe)
    {
        for (int i = 0; i < Recipe.Length; i++)
        {
            if (Substances.ContainsKey(Recipe[i].Substance))
            {
                if (Substances[Recipe[i].Substance] <= Recipe[i].SubstanceAmount)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }
}