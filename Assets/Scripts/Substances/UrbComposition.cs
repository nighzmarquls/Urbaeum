using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Assertions;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

public class UrbComposition
{
    protected UrbComposition ContainingComposition;
    
    public UrbMembrane Membrane { get; protected set; }

    float _maxCapacity = 1000;

    public float MaxCapacity
    {
        get { return _maxCapacity; }
        protected set
        {
            //MaxCapacity should not be zero. So we force it to 1 
            //in case it is..
            if (value <= 0)
            {
                value = 1;
            }
            _maxCapacity = value;
        }
    }

    float _usedCapacity = 0;
    public float UsedCapacity
    {
        get
        {
            return _usedCapacity;
        }
        protected set
        {
            if (value < 1)
            {
                _usedCapacity = 0;
                return;
            }
            
            Assert.IsFalse(float.IsNaN(value) || float.IsInfinity(value));
            _usedCapacity = value;
        }
    }
    public float Mass => UsedCapacity;

    public void SetSize(float Size)
    {
        if (ContainingComposition == null)
        {
            var toAssert = float.IsNaN(Size) || float.IsInfinity(Size);
            Assert.IsFalse(toAssert, "Size must be valid float");
            Assert.IsTrue(Size > 0, "Size must be > 0.");
            MaxCapacity = 1000 * Size;
        }
    }

    public float Emptiness {
        get {
            var emptiness = AvailableCapacity / MaxCapacity;
            
            Assert.IsFalse(float.IsInfinity(emptiness) || float.IsNaN(emptiness));
            
            return emptiness;
        }
    }

    public float Fullness {
        get
        {
            var fullness = UsedCapacity / MaxCapacity;

            var toAssert = float.IsNaN(fullness) || float.IsInfinity(fullness);
            
            Assert.IsFalse(toAssert, "Fullness must be valid value");

            return fullness;
        }
    }

    public float AvailableCapacity {
        get {
            var availCapacity = MaxCapacity - UsedCapacity;
            
            Assert.IsFalse(float.IsNaN(availCapacity) || float.IsInfinity(availCapacity));

            return availCapacity;
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

            if (Substances.TryGetValue(Tag, out float val))
            {
                return val;
            }

            return 0.0f;
        }
    }

    //Dictionaries are random-access memory, so operating on more than one at a time is slow
    //TODO: Replace this mechanism with the dual-list construct.
    protected Dictionary<UrbSubstanceTag, float> Substances = new Dictionary<UrbSubstanceTag, float>();
    protected List<UrbComposition> Compositions = new List<UrbComposition>();
    
    public UrbComposition(UrbSubstance[] CompositionRecipe = null)
    {
        
        Membrane = new UrbMembrane(this);

        if (CompositionRecipe == null)
        {
            return;
        }
        
        Assert.IsFalse(float.IsInfinity(UsedCapacity) || float.IsNaN(UsedCapacity));
        
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
        
        Assert.IsFalse(float.IsInfinity(UsedCapacity) || float.IsNaN(UsedCapacity));
    }

    // Intended to be called when the agent is deceased-- A "magical"
    //means of mass disappearing into the void, and therefore, a bit
    //of a compromise to the system's integrity.
    // Deceased bodies will get removed from the map over time.
    //In the future, this should be replaced with actual decay mechanisms
    //FOR EXAMPLE: Fur/teeth are not eaten by sneks. 
    // Could solve that loophole by having fur first in the ordering?
    public void DecaySubstances(int decay = 2)
    {
        Assert.IsTrue(decay > 0, "decay must be > 0");
        
        ContainingComposition?.DecaySubstances();
        
        UrbSubstanceTag[] subs = Substances.Keys.ToArray();
        float[] values = Substances.Values.ToArray();
        float val;

        for (int i = 0; i < subs.Length; i++)
        {
            val = values[i];
            if (val <= 0)
            {
                continue;
            }

            UrbSubstanceTag tag = subs[i];

            if (val <= 1)
            {
                UsedCapacity -= val;
                Substances[tag] -= val;
                break;
            }
            
            float reduction = val / decay;
            Substances[tag] -= reduction;
            UsedCapacity -= reduction;
            //I was going to have the 
            break;
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
        input.ContainingComposition = this;
        input.Membrane.ChangeComposition(this);
        input.MaxCapacity = input.UsedCapacity + AvailableCapacity;
        
        Assert.IsFalse(float.IsInfinity(input.MaxCapacity) || float.IsNaN(input.MaxCapacity));
        
        UsedCapacity += input.UsedCapacity;
        
        Assert.IsFalse(float.IsInfinity(UsedCapacity) || float.IsNaN(UsedCapacity));
        
        Compositions.Add(input);
    }

    public bool RemoveComposition(UrbComposition input)
    {
        if (Compositions.Contains(input))
        {
            UsedCapacity -= input.UsedCapacity;
            input.Membrane.ChangeComposition(input);
            input.ContainingComposition = null;
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
        if (ContainingComposition != null)
        {
            MaxCapacity = ContainingComposition.AvailableCapacity + UsedCapacity;
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
    
        if(ContainingComposition != null)
        {
            ContainingComposition.UsedCapacity += TransferAmount;
        }
        
        Assert.IsFalse(float.IsInfinity(TransferAmount) || float.IsNaN(TransferAmount));

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

        if (ContainingComposition != null)
        {
            MaxCapacity = ContainingComposition.AvailableCapacity + UsedCapacity;
        }

        float TransferAmount = Amount;
        //Debug.Log("Attempting Transfer " + TransferAmount + " from " + Tag.ToString());

        //Is this Supposed to be addition? 
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
        if (ContainingComposition != null)
        {
            ContainingComposition.UsedCapacity -= TransferAmount;
        }
        
        Assert.IsFalse(float.IsInfinity(TransferAmount) || float.IsNaN(TransferAmount));

        return TransferAmount;
    }

    public float TransferTo(UrbComposition Target, UrbSubstanceTag Tag, float Amount)
    {
        if(Amount <= 0.0f)
        {
            return 0.0f;
        }

        //float TransferAmount = Mathf.Min(Amount, Target.AvailableCapacity);

        float TransferAmount = RemoveSubstance(Tag, Amount);

        if(TransferAmount > 0)
        {
            Target.AddSubstance(Tag, TransferAmount);
        }
        else
        {
            //Debug.Log( Tag.ToString() + " Not Available to Transfer.");
        }

        Assert.IsFalse(float.IsInfinity(TransferAmount) || float.IsNaN(TransferAmount));
        
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

        Assert.IsFalse(float.IsInfinity(Result) || float.IsNaN(Result));
        
        return Result;
    }

    public float DecomposeRecipeInto(UrbComposition Target, UrbRecipe Recipe, float Amount)
    {
        float Result = (Target.Emptiness < Amount) ? Target.Emptiness : Amount;

        TransferTo(Target, Recipe.Product, Result);

        Result = Target.DecomposeRecipe(Recipe, Result);

        Assert.IsFalse(float.IsInfinity(Result) || float.IsNaN(Result));

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

        Assert.IsFalse(float.IsInfinity(Result) || float.IsNaN(Result));

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
        
        Assert.IsFalse(float.IsInfinity(Result) || float.IsNaN(Result));

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
            //My attempt at optimization - Zoru
            if (!Substances.TryGetValue(Recipe[i].Substance, out float value))
            {
                return false;
            }

            if (value != Recipe[i].SubstanceAmount)
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
        UrbSubstanceTag tag;
        for (int i = 0; i < Recipe.Length; i++)
        {
            tag = Recipe[i].Substance; 
            if (!Substances.TryGetValue(tag, out float ourValue))
            {
                return false;
            }

            if (ourValue >= Recipe[i].SubstanceAmount)
            {
                return false;
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
        UrbSubstanceTag tag;
        for (int i = 0; i < Recipe.Length; i++)
        {
            tag = Recipe[i].Substance; 
            if (!Substances.TryGetValue(tag, out float ourValue))
            {
                return false;
            }

            if (ourValue <= Recipe[i].SubstanceAmount)
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