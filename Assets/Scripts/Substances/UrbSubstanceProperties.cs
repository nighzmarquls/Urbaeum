using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class UrbSubstanceProperty
{
    public float Hardness = -1;
    public float Flexibility = -1;
    public float Maleability = -1;
    public UrbSubstance[] Components = new UrbSubstance[0];
    public UrbScentTag[] PersonalScent { get; protected set; } = new UrbScentTag[0];

    protected UrbScentTag[] CachedScent;
    public UrbScentTag[] Scent {
        get {
            if(CachedScent != null)
            {
                return CachedScent;
            }

            List<UrbScentTag> WorkingList = new List<UrbScentTag>();
            WorkingList.AddRange(PersonalScent);
            if(Components.Length > 0)
            {
                for(int c = 0; c < Components.Length; c++)
                {
                    WorkingList.AddRange(UrbSubstanceProperties.Get(Components[c].Substance).Scent);
                }
            }
            CachedScent = WorkingList.ToArray();

            return CachedScent;
        }
        set {

            PersonalScent = value;
            CachedScent = null;
        }
    } 
}

public class UrbSubstanceProperties
{
    public static UrbSubstanceProperty Get(UrbSubstanceTag Tag) {

        if (Properties == null || !Properties.ContainsKey(Tag))
        {
            return Default;
        }

        return Properties[Tag];
    }

    public static bool Remove(UrbSubstanceTag Tag)
    {
        if (Properties == null || !Properties.ContainsKey(Tag))
        {
            return false;
        }
        return Properties.Remove(Tag);
    }

    public static void Set(UrbSubstanceTag Tag, UrbSubstanceProperty Property)
    {
        if(Properties == null)
        {
            Properties = new Dictionary<UrbSubstanceTag, UrbSubstanceProperty>();
        }

        if(Properties.ContainsKey(Tag))
        {
            if(Property.Hardness > -1)
            {
                Properties[Tag].Hardness = Property.Hardness;
            }
            if(Property.Flexibility > -1)
            {
                Properties[Tag].Flexibility = Property.Flexibility;
            }
            if(Property.Maleability > -1)
            {
                Properties[Tag].Flexibility = Property.Maleability;
            }
            if(Property.PersonalScent.Length > 0)
            {
                for(int i = 0; i < Properties[Tag].Scent.Length; i++)
                {
                    UnregisterScent(Tag, Properties[Tag].Scent[i]);
                }
                Properties[Tag].Scent = Property.PersonalScent;
            }
            if(Property.Components.Length > 0)
            {
                Properties[Tag].Components = Property.Components;
            }
        }
        else
        {
            Properties.Add(Tag, Property);
        }

        for (int i = 0; i < Properties[Tag].Scent.Length; i++)
        {
            RegisterScent(Tag, Properties[Tag].Scent[i]);
        }

        return;
    }

    protected static bool RegisterScent(UrbSubstanceTag Substance, UrbScentTag Scent)
    {
        if(!SubstanceByScent.ContainsKey(Scent))
        {
            SubstanceByScent.Add(Scent, new List<UrbSubstanceTag>());
        }
    
        if(SubstanceByScent[Scent].Contains(Substance))
        {
            return false;
        }

        SubstanceByScent[Scent].Add(Substance);

        return true;
    }

    protected static bool UnregisterScent(UrbSubstanceTag Substance, UrbScentTag Scent)
    {
        if (SubstanceByScent.ContainsKey(Scent))
        {
            if (SubstanceByScent[Scent].Contains(Substance))
            {
                return SubstanceByScent[Scent].Remove(Substance);
            }
        }

        return false;
    }

    public static bool CheckScent(UrbSubstanceTag Substance, UrbScentTag Scent)
    {
        if (SubstanceByScent.ContainsKey(Scent))
        {
            return SubstanceByScent[Scent].Contains(Substance);
        }
        return false;
    }

    protected static Dictionary<UrbScentTag, List<UrbSubstanceTag>> SubstanceByScent = new Dictionary<UrbScentTag, List<UrbSubstanceTag>>();
    protected static Dictionary<UrbSubstanceTag, UrbSubstanceProperty> Properties;
    public static UrbSubstanceProperty Default = new UrbSubstanceProperty { Hardness = 1, Scent = new UrbScentTag[0] };
}
