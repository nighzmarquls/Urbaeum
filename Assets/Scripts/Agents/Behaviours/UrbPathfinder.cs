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

    protected UrbThinker mThinker;

    public override void Initialize()
    {
        mThinker = GetComponent<UrbThinker>();
        base.Initialize();
    }

    public override bool ShouldInterval => false;

    public UrbTile GetNextGoal()
    {
        UrbTile currentTile = mAgent.CurrentTile;
        UrbTile[] Adjacent = mAgent.Tileprint.GetBorderingTiles(mAgent, true);

        UrbTile goalTile = currentTile;
        int TerrainType = (int)PassableTerrain;
        float bestValue = (mThinker == null)? currentTile.TerrainFilter[TerrainType][Size][GoalTag] : mThinker.EvaluateTile(currentTile, TerrainType, Size);

        if (Adjacent == null) return goalTile;
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

            float currentValue = (mThinker == null) ? Adjacent[t].TerrainFilter[TerrainType][Size][GoalTag] : mThinker.EvaluateTile(Adjacent[t], TerrainType, Size);

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
