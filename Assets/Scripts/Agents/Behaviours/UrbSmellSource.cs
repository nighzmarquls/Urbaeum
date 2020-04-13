using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbSmellSource : UrbBehaviour
{

    public UrbScentTag[] SmellTag;
    public float SmellStrength = 1.0f;

    override public IEnumerator FunctionalCoroutine()
    {
        if(mAgent.Body != null && mAgent.Body.BodyComposition != null)
        {
            SmellTag = mAgent.Body.BodyComposition.GetScent();
        }
        UrbTile[] Tiles = mAgent.Tileprint.GetBorderingTiles(mAgent, true);
        float DiffusedSmell = SmellStrength / Tiles.Length;
        for(int s = 0; s < Tiles.Length; s++)
        {
            if (Tiles[s] == null)
            {
                continue;
            }
            for (int t = 0; t < SmellTag.Length; t++)
            {
                Tiles[s].AddScent(SmellTag[t], DiffusedSmell);
            }
        }

        yield return null;

    }

    override public UrbComponentData GetComponentData()
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

    override public bool SetComponentData(UrbComponentData Data)
    {
        SmellStrength = UrbEncoder.GetField("SmellStrength", Data);
        SmellTag = UrbEncoder.GetEnumArray<UrbScentTag>("SmellTag", Data);

        return true;
    }
}
