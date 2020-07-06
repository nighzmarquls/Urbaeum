using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

[System.Serializable]
public enum UrbTileprintFill
{
    Empty = 0,
    Occupy,
    Block,
    MaxTileprintFill
}

public class UrbTileprint
{
    UrbTileprintFill[][] Print;

    const int MaxTileprint = (int)UrbTileprintFill.MaxTileprintFill;

    int X, Y;

    public Vector2 CenterOffset => new Vector2(X / 2, Y / 2);

    public int TileCount { get; protected set; }

    public UrbTileprint(string PrintMap = "")
    {
        Initialize(PrintMap);
    }

    protected void Initialize(string PrintMap = "")
    {
        if (string.IsNullOrEmpty(PrintMap))
        {
            Print = new UrbTileprintFill[1][];
            Print[0] = new UrbTileprintFill[1];
            Print[0][0] = UrbTileprintFill.Occupy;
            SynchronizeDimensions();
        }
        else
        {
            LoadPrintMapFromString(PrintMap);
        }
    }

    protected void LoadPrintMapFromString(string PrintMap)
    {
        if(string.IsNullOrEmpty(PrintMap))
        {
            Debug.LogError("Attempting to Read Empty or Null String for Printmap");
            return;
        }
        string[] Rows = PrintMap.Split(new string[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        char[] testCharacters = new char[MaxTileprint];

        for(int i = 0; i < MaxTileprint; i++)
        {
            testCharacters[i] = i.ToString()[0];
        }


        Print = new UrbTileprintFill[Rows.Length][];

        Y = Rows.Length;
        X = 0;

        for (int y = 0; y < Rows.Length; y++)
        {
            char[] row = Rows[y].ToCharArray();
            Print[y] = new UrbTileprintFill[row.Length];
            for(int x = 0; x < row.Length; x ++)
            {
                X = (row.Length > X) ? row.Length : X;

                Print[y][x] = UrbTileprintFill.Empty;
                for(int i = 0; i < MaxTileprint; i++)
                {
                    if(row[x] == testCharacters[i])
                    {
                        Print[y][x] = (UrbTileprintFill)i;

                        if(i > 0)
                        {
                            TileCount++;
                        }
                        break;
                    }
                }
            }
        }
    }

    protected void SynchronizeDimensions()
    {
        Y = Print.Length;
        X = Print[0].Length;
    }

    public UrbTile[] GetAdjacentPrintTiles(UrbTile Tile, bool GetLinked = false)
    {
        return Tile.GetAdjacent(GetLinked, X, Y);
    }

    public UrbTile[] GetAdjacentPrintTiles(UrbAgent Agent, bool GetLinked = false)
    {
        if (Agent.IsCurrentMapNull || Agent.CurrentTile == null)
        {
            return new UrbTile[0];
        }

        return GetAdjacentPrintTiles(Agent.CurrentTile, GetLinked);
    }

    public UrbTile[] GetBorderingTiles(UrbAgent Agent, bool GetLinked = false, int Distance = 0)
    {
        if(Agent.IsCurrentMapNull  || Agent.CurrentTile == null)
        {
            return new UrbTile[0];
        }

        return GetBorderingTiles(Agent.CurrentTile, GetLinked, Distance);
    }

    public UrbTile[] GetBorderingTiles(UrbTile Tile, bool GetLinked = false, int Distance = 0)
    {
        if (X < 2 && Y < 2 && Distance == 0)
        {
            return Tile.GetAdjacent(GetLinked);
        }


        Vector2 Voffset = GetOffset();
        int Xoffset = (int)Voffset.x;
        int Yoffset = (int)Voffset.y;

        List<UrbTile> ReturnList = new List<UrbTile>();

        for(int i = -1; i <= X; i++)
        {
            UrbTile CheckedTile = Tile.GetRelativeTile(i + Xoffset, Yoffset -(1+ Distance));
            if (CheckedTile == null)
            {
                continue;
            }

            ReturnList.Add(CheckedTile);
        }

        for(int i = 0; i <= Y; i++)
        {
            UrbTile CheckedTile = Tile.GetRelativeTile(X + Xoffset + Distance, i + Yoffset);
            if (CheckedTile == null)
            {
                continue;
            }

            ReturnList.Add(CheckedTile);
        }

        for (int i = -1; i <= X; i++)
        {
            UrbTile CheckedTile = Tile.GetRelativeTile(i + Xoffset, Y + Yoffset + Distance);
            if (CheckedTile == null)
            {
                continue;
            }

            ReturnList.Add(CheckedTile);
        }

        for(int i = 0; i < Y; i++)
        {
            UrbTile CheckedTile = Tile.GetRelativeTile(Xoffset - (1+Distance), Yoffset + i);
            if (CheckedTile == null)
            {
                continue;
            }

            ReturnList.Add(CheckedTile);
        }

        if (GetLinked)
        {
            UrbTile[] Tiles = GetAllPrintTiles(Tile);

            foreach (UrbTile PrintTile in Tiles)
            {
                if (PrintTile != null)
                {
                    ReturnList.AddRange(PrintTile.GetLinked());
                }
            }
        }

        return ReturnList.ToArray();
    }

    public UrbTile[] GetAllPrintTiles(UrbAgent Agent)
    {
        if (Agent.IsCurrentMapNull || Agent.CurrentTile == null)
        {
            return new UrbTile[0];
        }

        return GetAllPrintTiles(Agent.CurrentTile);
    }

    public UrbTile[] GetAllPrintTiles(UrbTile Tile)
    {
        int Xoffset = 0;
        int Yoffset = 0;

        if (X > 2 && Y > 2)
        {
            Vector2 Voffset = GetOffset();
            Xoffset = (int)Voffset.x;
            Yoffset = (int)Voffset.y;
        }
        else
        {
            return new UrbTile[] { Tile };
        }

        UrbTile[] ReturnTiles = new UrbTile[X * Y];

        int Index = 0;
        for (int y = 0; y < Print.Length; y++)
        {
            for (int x = 0; x < Print[y].Length; x++)
            {
                UrbTile CheckedTile = Tile.GetRelativeTile(x + Xoffset, y + Yoffset);
                ReturnTiles[Index] = CheckedTile;
                Index++;
            }
        }

        return ReturnTiles;
    }

    public bool TilePrintCollisionCheck(UrbTile Tile)
    {
        int Xoffset = 0;
        int Yoffset = 0;

        if (X > 2 && Y > 2)
        {
            Vector2 Voffset = GetOffset();
            Xoffset = (int)Voffset.x;
            Yoffset = (int)Voffset.y;
        }

        for (int y = 0; y < Print.Length; y++)
        {
            for (int x = 0; x < Print[y].Length; x++)
            {
                UrbTile CheckedTile = Tile.GetRelativeTile(x + Xoffset, y + Yoffset);

                if (CheckedTile == null)
                    return true;

                UrbTileprintFill Fill = Print[y][x];
                switch (Fill)
                {
                    case UrbTileprintFill.Block:
                    case UrbTileprintFill.Occupy:
                        if (CheckedTile.Blocked)
                            return true;
                        break;
                    default:
                        break;
                }
            }
        }

        return false;
    }


    protected static void PrintAtTile(UrbTileprintFill Fill, UrbTile Tile, UrbAgent Agent)
    {
        switch (Fill)
        {
            case UrbTileprintFill.Block:
                if (!Tile.Occupants.Contains(Agent))
                {
                    Tile.Occupants.Add(Agent);
                    Tile.Blocked = true;
                }
                break;
            case UrbTileprintFill.Occupy:
                if (!Tile.Occupants.Contains(Agent))
                {
                    Tile.Occupants.Add(Agent);
                }
                break;
            default:
                break;
        }
    }

    protected void UnprintAtTile(UrbTileprintFill Fill, UrbTile Tile, UrbAgent Agent)
    {
        switch(Fill){
            case UrbTileprintFill.Block:
                if(Tile.Occupants.Contains(Agent))
                {
                    Tile.Occupants.Remove(Agent);
                    Tile.Blocked = false;
                }
                break;    
            case UrbTileprintFill.Occupy:
                if (Tile.Occupants.Contains(Agent))
                {
                    Tile.Occupants.Remove(Agent);
                }
                break;
            default:
                break;
        }
    }

    protected Vector2 GetOffset()
    {
        int Xoffset = (X == 3)? -1 : Mathf.FloorToInt((2-(float)X)/2.0f);
        int Yoffset = (Y == 3)? -1 : Mathf.FloorToInt((2-(float)Y)/2.0f);

        return new Vector2(Xoffset,Yoffset);
    }

    static ProfilerMarker s_ArriveAtTile_p = new ProfilerMarker("UrbTilePrint.ArriveAtTile"); 
    public void ArriveAtTile(UrbTile Tile, UrbAgent Agent)
    {
        s_ArriveAtTile_p.Begin();
        int Xoffset = 0;
        int Yoffset = 0;

        if ( X > 2 && Y > 2)
        {
            Vector2 Voffset = GetOffset();
            Xoffset = (int)Voffset.x;
            Yoffset = (int)Voffset.y;
        }
    
        for (int y = 0; y < Print.Length; y++)
        {
            for (int x = 0; x < Print[y].Length; x++)
            {
                UrbTile FillingTile = Tile.GetRelativeTile(x + Xoffset, y + Yoffset);

                if (FillingTile == null)
                    continue;

                UrbTileprintFill Fill = Print[y][x];
                PrintAtTile(Fill, FillingTile, Agent);
            }
        }

        s_ArriveAtTile_p.End();
    }

    public void DepartFromTile(UrbTile Tile, UrbAgent Agent)
    {
        int Xoffset = 0;
        int Yoffset = 0;

        if (X > 2 && Y > 2)
        {
            Vector2 Voffset = GetOffset();
            Xoffset = (int)Voffset.x;
            Yoffset = (int)Voffset.y;
        }

        for (int y = 0; y < Print.Length; y++)
        {
            for (int x = 0; x < Print[y].Length; x++)
            {
                UrbTile FillingTile = Tile.GetRelativeTile(x + Xoffset, y + Yoffset);

                if (FillingTile == null)
                    continue;

                UrbTileprintFill Fill = Print[y][x];
                UnprintAtTile(Fill, FillingTile, Agent);
            }
        }
    }
}
