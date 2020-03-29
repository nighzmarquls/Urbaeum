using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct UrbSave
{
    public UrbMapData[] Maps;
}

[System.Serializable]
public struct UrbMapData
{
    public Vector3 Position;
    public int X, Y;
    public UrbTileData[] Tiles;
}

[System.Serializable]
public struct UrbTileData
{
    public int Content;
    public bool Blocked;
    public UrbEnvironmentData Environment;
    public UrbTileLinkData[] Links;
    public UrbPathTerrain[] TerrainTypes;
}

[System.Serializable]
public struct UrbEnvironmentData
{
    public float[] Conditions;
    public float[] Transfer;
    public bool Dirty;
}

[System.Serializable]
public struct UrbTileLinkData
{
    public int MapID;
    public int X, Y;
}