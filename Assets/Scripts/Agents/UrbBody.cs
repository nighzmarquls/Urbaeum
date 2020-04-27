using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbBody : UrbBase
{
    public UrbComposition BodyComposition;

    public UrbSubstance[] BodyRecipe;
    public UrbSubstance[] CriticalBodyPartAmounts;
    protected UrbAgent mAgent;

    public override void Initialize()
    {
        if(bInitialized)
        {
            return;
        }

        if (BodyComposition == null)
        {
            BodyComposition = new UrbComposition(BodyRecipe);
        }
        mAgent = GetComponent<UrbAgent>();

        BodyComposition.SetSize(mAgent.Tileprint.TileCount);
        base.Initialize();
    }

    public bool BodyCritical()
    {
        if(BodyComposition == null)
        {
            return false;
        }
        for(int i = 0; i < CriticalBodyPartAmounts.Length; i++)
        {
            if(BodyComposition[CriticalBodyPartAmounts[i].Substance] < CriticalBodyPartAmounts[i].SubstanceAmount)
            {
                return true;
            }
        }
        return false;
    }

    override public UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();

        
        UrbSubstance[] BodyContents = (BodyComposition == null)? BodyRecipe : BodyComposition.GetCompositionIngredients();

        Data.FieldArrays = new UrbFieldArrayData[]
        {
            UrbEncoder.GetArrayFromSubstances("BodyRecipe" , BodyRecipe),
            UrbEncoder.GetArrayFromSubstances("BodyContents" , BodyContents),
            UrbEncoder.GetArrayFromSubstances("CriticalBodyPartAmounts" , CriticalBodyPartAmounts),
        };


        return Data;
    }



    override public bool SetComponentData(UrbComponentData Data)
    {
        BodyRecipe = UrbEncoder.GetSubstancesFromArray("BodyRecipe",Data);
        BodyComposition = new UrbComposition(UrbEncoder.GetSubstancesFromArray("BodyContents", Data));
        CriticalBodyPartAmounts = UrbEncoder.GetSubstancesFromArray("CriticalBodyPartAmounts", Data);
        return true;
    }
}
