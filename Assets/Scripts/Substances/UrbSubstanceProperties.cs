using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class UrbSubstanceProperty
{
    public float Hardness;
    public float Flexibility;
    public float Maleability;
    public UrbSubstance[] Components = new UrbSubstance[0];

    protected UrbScentTag[] PersonalScent = new UrbScentTag[0];
    protected UrbScentTag[] CachedScent;
    public UrbScentTag[] Scent {
        get {
            if(CachedScent != null)
            {
                return CachedScent;
            }

            List<UrbScentTag> WorkingList = new List<UrbScentTag>();
            if(Components.Length > 0)
            {
                for(int c = 0; c < Components.Length; c++)
                {
                    //WorkingList.AddRange()
                }
            }



            return CachedScent;
        }
        set {
            PersonalScent = value;
        }
    } 
}

public class UrbSubstanceProperties
{
    public static UrbSubstanceProperty GetProperties(UrbSubstanceTag Tag) {

        return Default;
    }
    public static UrbSubstanceProperty Default = new UrbSubstanceProperty { Hardness = 0, Scent = new UrbScentTag[0] };
}
