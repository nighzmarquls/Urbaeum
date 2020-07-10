using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UrbPathTerrain
{
    Land = 0,
    Air,
    MaxPathTerrain
}

[RequireComponent(typeof(UrbAgent))]
public class UrbPathfinder : UrbBehaviour
{
    public UrbScentTag GoalTag = UrbScentTag.Goal;
    public int Size = 0;
    public UrbPathTerrain PassableTerrain = UrbPathTerrain.Land;
    
    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override bool ShouldInterval => false;

    public UrbTile GetNextGoal()
    {
        UrbTile currentTile = mAgent.CurrentTile;
        UrbTile[] Adjacent = mAgent.Tileprint.GetBorderingTiles(mAgent, true);

        if (Adjacent == null || Adjacent.Length == 0)
        {
            return currentTile;
        }

        int TerrainType = (int)PassableTerrain;

        float bestValue = 0.0f;
        
        if (IsMindNull)
        {
            var scent = currentTile.TerrainFilter[TerrainType][Size]; 
            bestValue = scent.tagList[(int)GoalTag].value;
        }
        else
        {
           bestValue = Mind.EvaluateTile(currentTile, TerrainType, Size);
        }

        UrbTile goalTile = currentTile;
        for(int t = 0; t < Adjacent.Length; t++)
        {
            if (Adjacent[t] == null)
            {
                continue;
            }

            if(Adjacent[t].Blocked)
            {
                continue;
            }

            if(Adjacent[t].FreeCapacity < mAgent.MassPerTile)
            {
                continue;
            }

            if(!Adjacent[t].TerrainPassable(PassableTerrain))
            {
                continue;
            }

            var currentScent = Adjacent[t].TerrainFilter[TerrainType][Size];
            
            
            float currentValue = 0f;
            if (IsMindNull)
            {
                currentValue = currentScent.tagList[(int) GoalTag].value;
            }
            else
            {
               currentValue = Mind.EvaluateTile(Adjacent[t], TerrainType, Size);
            }
            
            if (currentValue >= bestValue)
            {
                bestValue = currentValue;
                goalTile = Adjacent[t];
            }
        }

        return goalTile;
    }

    public override UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();

        Data.Strings = new UrbStringData[]
        {
            new UrbStringData{ Name = "GoalTag", Value = GoalTag.ToString() },
            new UrbStringData{ Name = "PassableTerrain", Value = PassableTerrain.ToString()}
        };

        Data.Fields = new UrbFieldData[]
        {
             new UrbFieldData{ Name = "Size", Value = Size }
        };
        return Data;
    }

    public override bool SetComponentData(UrbComponentData Data)
    {
        GoalTag = UrbEncoder.GetEnum<UrbScentTag>("GoalTag", Data);
        PassableTerrain = UrbEncoder.GetEnum<UrbPathTerrain>("PassableTerrain", Data);
        Size = (int)UrbEncoder.GetField("Size", Data);
        return true;
    }
}
