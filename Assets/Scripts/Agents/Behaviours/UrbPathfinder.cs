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

        float bestValue;
        if (IsMindNull)
        {
            bestValue = currentTile.TerrainFilter[TerrainType][Size][GoalTag];
        }
        else
        {
            bestValue = Mind.EvaluateTile(currentTile, TerrainType, Size);
        }
        
        UrbTile goalTile = currentTile;
        float currentValue = 0.0f;
        
        for(int t = 0; t < Adjacent.Length; t++)
        {
            if (Adjacent[t] == null || Adjacent[t].Blocked)
            {
                continue;
            }

            if (mAgent.MassPerTile > Adjacent[t].RemainingCapacity)
            {
                continue;
            }

            if (!Adjacent[t].TerrainPassable(PassableTerrain))
            {
                continue;
            }

            if (IsMindNull)
            {
                currentValue = Adjacent[t].TerrainFilter[TerrainType][Size][GoalTag];
            }
            else
            {
                currentValue = Mind.EvaluateTile(Adjacent[t], TerrainType, Size);
            }
            
            if (currentValue <= 0)
            {
                continue;
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
