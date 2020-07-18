using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEngine;

[RequireComponent(typeof(UrbAgent), typeof(UrbBody))]
public class UrbSmellSource : UrbBehaviour
{
    UrbScentTag[] _smellTag;
    public UrbScentTag[] SmellTag {
        get {
            Assert.IsNotNull(_smellTag);
            return _smellTag;
        }}
    public float SmellStrength { get; protected set; }  = 1.0f;
    public override bool LivingBehaviour => false;

    public override void OnEnable()
    {
        _smellTag = mBody.BodyComposition.GetScent();
        SmellStrength = mAgent.MassPerTile;
        base.OnEnable(); 
    }

    // Obsolete. Scents now pull from the occupants list
    public override IEnumerator FunctionalCoroutine()
     {
         Assert.IsFalse(IsPaused);
         Assert.IsTrue(HasBody && mBody.HasComposition);

         //SmellSources are derived from aspects of the Body 
         _smellTag = mBody.BodyComposition.GetScent();
         SmellStrength = mAgent.MassPerTile;
    
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

        //SmellStrength is (currently) computed based on MassPerTile.
        // Data.Fields = new UrbFieldData[]
        // {
        //     new UrbFieldData{ Name = "SmellStrength", Value = SmellStrength}
        // };

        //SmellTag is (currently) computed based on the Body
        // Data.StringArrays = new UrbStringArrayData[]
        // {
        //     UrbEncoder.EnumsToArray<UrbScentTag>("SmellTag",SmellTag) 
        // };
        
        return Data;
    }

    public override bool SetComponentData(UrbComponentData Data)
    {
        //SmellStrength is (currently) computed based on MassPerTile.
        // SmellStrength = UrbEncoder.GetField("SmellStrength", Data);
        // SmellTag = UrbEncoder.GetEnumArray<UrbScentTag>("SmellTag", Data);

        return true;
    }
}
