using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbSmellSource : UrbBehaviour
{

    public UrbScentTag[] SmellTag;
    public float SmellStrength = 1.0f;

    protected UrbTile[] TileCache = null;
    protected UrbTile LastSmellTile = null;

    override public IEnumerator FunctionalCoroutine()
    {
        if(mAgent.Body != null && mAgent.Body.BodyComposition != null)
        {
            SmellTag = mAgent.Body.BodyComposition.GetScent();
            SmellStrength = mAgent.MassPerTile;
        }

        if (LastSmellTile != mAgent.CurrentTile)
        {
            TileCache = mAgent.Tileprint.GetBorderingTiles(mAgent, true);
        }

        for(int s = 0; s < TileCache.Length; s++)
        {
            if (TileCache[s] == null)
            {
                continue;
            }
            for (int t = 0; t < SmellTag.Length; t++)
            {
                TileCache[s].AddScent(SmellTag[t], SmellStrength / TileCache.Length);
            }
        }

        if (Interval < UrbScent.ScentInterval)
        {
            yield return new WaitForSeconds(UrbScent.ScentInterval - Interval);
        }
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
