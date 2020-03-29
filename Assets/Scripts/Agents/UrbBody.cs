using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbBody : UrbBase
{
    public UrbComposition BodyComposition;

    [Range(1, 3)]
    public int Size = 1;
    public UrbSubstance[] BodyRecipe;
    public UrbSubstance[] CriticalBodyPartAmounts;

    public override void Initialize()
    {
        if(bInitialized)
        {
            return;
        }

        BodyComposition = new UrbComposition(BodyRecipe);
        BodyComposition.SetSize(Size);
        base.Initialize();
    }

    public bool BodyCritical()
    {
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

        UrbSubstance[] BodyContents = BodyComposition.GetCompositionIngredients();

        Data.FieldArrays = new UrbFieldArrayData[]
        {
            UrbEncoder.GetArrayFromSubstances("BodyRecipe" , BodyRecipe),
            UrbEncoder.GetArrayFromSubstances("BodyContents" , BodyContents),
            UrbEncoder.GetArrayFromSubstances("CriticalBodyPartAmounts" , CriticalBodyPartAmounts),
        };

        Data.Fields = new UrbFieldData[]
        {
            new UrbFieldData { Name = "Size", Value = Size}
        } ;

        return Data;
    }



    override public bool SetComponentData(UrbComponentData Data)
    {
        BodyRecipe = UrbEncoder.GetSubstancesFromArray("BodyRecipe",Data);
        BodyComposition = new UrbComposition(UrbEncoder.GetSubstancesFromArray("BodyContents", Data));
        CriticalBodyPartAmounts = UrbEncoder.GetSubstancesFromArray("CriticalBodyPartAmounts", Data);
        Size = (int)UrbEncoder.GetField("Size", Data);
        return true;
    }
}
