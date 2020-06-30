﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbComposition
{
    protected UrbComposition ContainingComposion;

    public UrbMembrane Membrane { get; protected set; }

    public float MaxCapacity { get; protected set; } = 1000;
    public float UsedCapacity { get; protected set; } = 0;
    public float Mass => UsedCapacity;

    public void SetSize(float Size)
    {
        if (ContainingComposion == null)
        {
            if (Size == 1)
            {
                MaxCapacity = 1000;
            }
            else
            {
                MaxCapacity = 1000 * (Size);
            }
        }
    }

    public float Emptiness {
        get {
            return AvailableCapacity / MaxCapacity;
        }
    }

    public float Fullness {
        get {
            return UsedCapacity / MaxCapacity;
        }
    }

    public float AvailableCapacity {
        get {
            return MaxCapacity - UsedCapacity;
        }
    }

    public float this[UrbSubstanceTag[] Tags] {
        get {
            float Total = 0;

            for (int i = 0; i < Tags.Length; i++)
            {
                if (Tags[i] == UrbSubstanceTag.All)
                {
                    return UsedCapacity;
                }
                Total += this[Tags[i]];
            }

            return Total;
        }
    }

    public float this[UrbSubstanceTag Tag] {
        get {
            if (Tag == UrbSubstanceTag.All)
            {
                return UsedCapacity;
            }
            else if (Substances.ContainsKey(Tag))
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
    protected List<UrbComposition> Compositions;

    public virtual void Initialize()
    {
        Substances = new Dictionary<UrbSubstanceTag, float>();
        Compositions = new List<UrbComposition>();
        Membrane = new UrbMembrane(this);
    }

    public UrbComposition()
    {
        Initialize();
    }

    public UrbComposition(UrbSubstance[] CompositionRecipe)
    {
        Initialize();

        for (int i = 0; i < CompositionRecipe.Length; i++)
        {
            if (Substances.ContainsKey(CompositionRecipe[i].Substance))
            {
                Substances[CompositionRecipe[i].Substance] += CompositionRecipe[i].SubstanceAmount;
            }
            else
            {
                Substances.Add(CompositionRecipe[i].Substance, CompositionRecipe[i].SubstanceAmount);
            }
            UsedCapacity += CompositionRecipe[i].SubstanceAmount;
        }
    }

    public float GetProportionOfTotal(UrbSubstanceTag[] Tags)
    {
        if(UsedCapacity <= 0)
        {
            return 0.0f;
        }

        return this[Tags] / UsedCapacity;
    }

    public float GetProportionOf(UrbSubstanceTag Tag)
    {
        if (Tag == UrbSubstanceTag.None || UsedCapacity <= 0)
        {
            return 0.0f;
        }
        else if (Tag == UrbSubstanceTag.All)
        {
            return 1.0f;
        }

        return this[Tag] / UsedCapacity;
    }

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

    public void AddComposition(UrbComposition input)
    {
        input.ContainingComposion = this;
        input.Membrane.ChangeComposition(this);
        input.MaxCapacity = input.UsedCapacity + AvailableCapacity;
        Compositions.Add(input);
    }

    public bool RemoveComposition(UrbComposition input)
    {
        if (Compositions.Contains(input))
        {
            input.Membrane.ChangeComposition(input);
            input.ContainingComposion = null;
        }
        return Compositions.Remove(input);
    }

    bool Dirty = true;
    UrbScentTag[] CachedScents;
    public UrbScentTag[] GetScent()
    {
        if (Dirty)
        {
            List<UrbScentTag> ScentList = new List<UrbScentTag>();
            /* TODO: This may never be the prefered solution but it has some logic to it.
             * if (Membrane != null && Membrane.Layers != null && Membrane.Layers.Length > 0)
            {
                for(int i = 0; i < Membrane.Layers.Length; i++)
                {
                    if (this[Membrane.Layers[i]] > 0.0)
                    {
                        UrbScentTag[] Scents = UrbSubstances.Scent(Membrane.Layers[i]);
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
            }
            else*/
            {
                foreach (UrbSubstanceTag tag in Substances.Keys)
                {
                    if (this[tag] > 0.0)
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
            }
            CachedScents = ScentList.ToArray();
            Dirty = false;
        }

        return CachedScents;
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
        if (ContainingComposion != null)
        {
            MaxCapacity = ContainingComposion.AvailableCapacity + UsedCapacity;
        }

        if (Amount <= 0.0f)
        {
            return 0.0f;
        }

        float TransferAmount = Amount;

        float CapacityAfter = UsedCapacity + Amount;
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
        
        UsedCapacity += TransferAmount;
    
        if(ContainingComposion != null)
        {
            ContainingComposion.UsedCapacity += TransferAmount;
        }
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

        if (ContainingComposion != null)
        {
            MaxCapacity = ContainingComposion.AvailableCapacity + UsedCapacity;
        }

        float TransferAmount = Amount;
        //Debug.Log("Attempting Transfer " + TransferAmount + " from " + Tag.ToString());

        float CapacityAfter = UsedCapacity + Amount;
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
        UsedCapacity -= TransferAmount;
        if (ContainingComposion != null)
        {
            ContainingComposion.UsedCapacity -= TransferAmount;
        }
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

    public float DecomposeRecipe(UrbRecipe Recipe, float Amount)
    {
        if (Amount > UsedCapacity)
        {
            Amount = UsedCapacity;
        }
        float Result = Amount;

        Result = RemoveSubstance(Recipe.Product, Result);

        if(Result > 0)
        {
            UrbSubstance[] Proportions = UrbSubstances.GetIngredientProportions(Recipe);
            for (int r = 0; r < Proportions.Length; r++)
            {
                AddSubstance(Proportions[r].Substance, Proportions[r].SubstanceAmount * Result);
            }
        }

        return Result;
    }

    public float DecomposeRecipeInto(UrbComposition Target, UrbRecipe Recipe, float Amount)
    {
        float Result = (Target.Emptiness < Amount) ? Target.Emptiness : Amount;

        TransferTo(Target, Recipe.Product, Result);

        Result = Target.DecomposeRecipe(Recipe, Result);

        return Result;
    }

    public float MixRecipe(UrbRecipe Recipe, float Amount)
    {
        if(Amount > UsedCapacity)
        {
            Amount = UsedCapacity;
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
        float Result = (Target.Emptiness < Amount) ? Target.Emptiness : Amount;

        Result = MixRecipe(Recipe, Result);

        return TransferTo(Target, Recipe.Product,Result);
    }

    public float EmptyInto(UrbComposition Target)
    {
        float Result = 0;
        UrbSubstanceTag[] tags = new UrbSubstanceTag[Substances.Keys.Count];
        int i = 0;
        foreach(UrbSubstanceTag tag in Substances.Keys)
        {
            tags[i] = tag;
            i++;
        }

        for(int t = 0; t < tags.Length; t++)
        {
            float Amount = Substances[tags[t]];
            Result += TransferTo(Target, tags[t], Amount);
        }
        return Result;
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