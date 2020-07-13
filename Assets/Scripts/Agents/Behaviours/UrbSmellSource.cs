using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbSmellSource : UrbBehaviour
{

    public UrbScentTag[] SmellTag;
    public float SmellStrength = 1.0f;
    public override bool LivingBehaviour => false;

    // Obsolete. Scents now pull from the occupants list
    public override IEnumerator FunctionalCoroutine()
     {
         if(mAgent.HasBody && mAgent.mBody.BodyComposition != null)
         {
             SmellTag = mAgent.mBody.BodyComposition.GetScent();
             SmellStrength = mAgent.MassPerTile;
         }
    
         // if (LastSmellTile != mAgent.CurrentTile)
         // {
         //     TileCache = mAgent.Tileprint.GetBorderingTiles(mAgent, true);
         // }
         
         if (Interval < UrbScent.ScentInterval)
         {
             yield return new WaitForSeconds(UrbScent.ScentInterval - Interval);
         }
     }

    public override UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();

        Data.Fields = new UrbFieldData[]
        {
            new UrbFieldData{ Name = "SmellStrength", Value = SmellStrength}
        };

        Data.StringArrays = new UrbStringArrayData[]
        {
            UrbEncoder.EnumsToArray<UrbScentTag>("SmellTag",SmellTag) 
        };
        
        return Data;
    }

    public override bool SetComponentData(UrbComponentData Data)
    {
        SmellStrength = UrbEncoder.GetField("SmellStrength", Data);
        SmellTag = UrbEncoder.GetEnumArray<UrbScentTag>("SmellTag", Data);

        return true;
    }
}
