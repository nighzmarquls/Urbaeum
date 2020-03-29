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
public class UrbPathfinder : UrbBase
{
    public UrbScentTag GoalTag = UrbScentTag.Goal;
    public int Size = 0;
    public UrbPathTerrain PassableTerrain = UrbPathTerrain.Land;

    protected UrbAgent mAgent;
    protected UrbThinker mThinker;
    void Start()
    {
        mAgent = GetComponent<UrbAgent>();
        mThinker = GetComponent<UrbThinker>();
    }

    public UrbTile GetNextGoal(UrbMap input)
    {
        Vector3 currentPosition = transform.position;
        UrbTile currentTile = input.GetNearestTile(currentPosition);
        UrbTile[] Adjacent = mAgent.Tileprint.GetBorderingTiles(mAgent, true);

        UrbTile goalTile = currentTile;
        int TerrainType = (int)PassableTerrain;
        float bestValue = (mThinker == null)? currentTile.TerrainFilter[TerrainType][Size][GoalTag] : mThinker.EvaluateTile(currentTile, TerrainType, Size);

        if (Adjacent != null)
        {
            for(int t = 0; t < Adjacent.Length; t++)
            {
                if (Adjacent[t] == null)
                {
                    continue;
                }

                float currentValue = (mThinker == null) ? Adjacent[t].TerrainFilter[TerrainType][Size][GoalTag] : mThinker.EvaluateTile(Adjacent[t], TerrainType, Size);

                if (currentValue <= 0)
                {
                    continue;
                }

                if (Adjacent[t].CurrentContent != null && Adjacent[t].CurrentContent != mAgent)
                {
                    continue;
                }

                if (currentValue >= bestValue)
                {
                    bestValue = currentValue;
                    goalTile = Adjacent[t];
                }

            }
        }
        return goalTile;
    }

    override public UrbComponentData GetComponentData()
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

    override public bool SetComponentData(UrbComponentData Data)
    {
        GoalTag = UrbEncoder.GetEnum<UrbScentTag>("GoalTag", Data);
        PassableTerrain = UrbEncoder.GetEnum<UrbPathTerrain>("PassableTerrain", Data);
        Size = (int)UrbEncoder.GetField("Size", Data);
        return true;
    }
}
