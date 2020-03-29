using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbMap : MonoBehaviour
{
    public Color Color = Color.white;
    public float TileSize = 1.0f;
    public int Xsize;
    public int Ysize;
    public UrbPathTerrain DefaultTerrain = UrbPathTerrain.Land;

    UrbTile[][] MapTiles;

    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
    }

    public void SetNewMap(int newX, int newY, float newTileSize, UrbPathTerrain TerrainType = UrbPathTerrain.Land)
    {
        ClearMap();
        TileSize = newTileSize;
        Xsize = newX;
        Xsize = newY;
        DefaultTerrain = TerrainType;
        GenerateMap();
    }

    void ClearMap()
    {
        for (int i = 0; i < Xsize; i++)
        {
            for (int ii = 0; ii < Ysize; ii++)
            {
                MapTiles[i][ii].ClearTile();
            }
        }
    }

    void GenerateMap()
    {
        Vector3 positionOffset = new Vector3(Xsize * TileSize * 0.5f, Ysize * TileSize * 0.5f);
        transform.position -= positionOffset;
        MapTiles = new UrbTile[Xsize][];
        for (int i = 0; i < Xsize; i++)
        {
            MapTiles[i] = new UrbTile[Ysize];
            for (int ii = 0; ii < Ysize; ii++)
            {
                MapTiles[i][ii] = new UrbTile(this, i, ii);
                MapTiles[i][ii].TerrainTypes = new UrbPathTerrain[] { DefaultTerrain };
            }
        }
    }

    public UrbMapData GetMapData()
    {
        UrbMapData output = new UrbMapData();

        output.X = Xsize;
        output.Y = Ysize;

        output.Tiles = new UrbTileData[output.X * output.Y];

        for (int i = 0; i < output.X; i++)
        {
            for (int ii = 0; ii < output.Y; ii++)
            {
                output.Tiles[i*output.X + ii] = MapTiles[i][ii].GetTileData();
            }
        }

        return output;
    }

    public bool LoadMapFromData(UrbMapData input)
    {
        if(input.X != Xsize || input.Y != Ysize)
        {
            Debug.LogError("UrbMapData size mismatch. Load Canceled");
            return false;
        }

        for (int i = 0; i < input.X; i++)
        {
            for (int ii = 0; ii < input.Y; ii++)
            {
                bool success = MapTiles[i][ii].LoadTileFromData(input.Tiles[i * input.X + ii]);

                if(!success)
                {
                    return false;
                }

            }
        }

        return true;
    }

    //Returns Loction of Tile Address
    public Vector2 TileAddressToLocation(int Xaddress, int Yaddress)
    {
        Vector3 offSetLocation = new Vector3(Xaddress*TileSize, Yaddress*TileSize) + transform.position;

        return offSetLocation;
    }

    //Returns true if the location is within the map and gives the correct address, returns false and outs closest tile address if it is not within the map.
    public bool LocationToTileAddress(Vector3 Location, out int Xaddress, out int Yaddress)
    {
        Vector3 offsetLocation = (Location - transform.position)/TileSize;
        Xaddress = Mathf.RoundToInt(Mathf.Min(Mathf.Max(0.0f, offsetLocation.x), Xsize - 1));
        Yaddress = Mathf.RoundToInt(Mathf.Min(Mathf.Max(0.0f, offsetLocation.y), Ysize - 1));

        return (offsetLocation.x > 0 && offsetLocation.x < Xsize && offsetLocation.y > 0 && offsetLocation.y < Ysize);
    }

    //Will always return a tile even if the location is out of bounds for the map.
    public UrbTile GetNearestTile(Vector2 Location)
    {
        int Xaddress = 0;
        int Yaddress = 0;

        LocationToTileAddress(Location, out Xaddress, out Yaddress);

        return MapTiles[Xaddress][Yaddress];
    }

    //Will return null is the location is out of bounds.
    public UrbTile GetInboundsTile(Vector2 Location)
    {
        int Xaddress = 0;
        int Yaddress = 0;

        if(LocationToTileAddress(Location, out Xaddress, out Yaddress))
        {
            return MapTiles[Xaddress][Yaddress];
        }
        return null;
    }

    //Will return Tile if address is valid. otherwise null
    public UrbTile GetTile(int Xaddress, int Yaddress)
    {
        if(Xaddress > -1 && Xaddress < Xsize && Yaddress > -1 && Yaddress < Ysize)
        {
            return MapTiles[Xaddress][Yaddress];
        }

        return null;
    }


    public UrbTile[] GetAdjacent(Vector2 Location, bool GetLinked = false, int xOffset = 1, int yOffset = 1)
    {
        int Xaddress = 0;
        int Yaddress = 0;
        if (LocationToTileAddress(Location, out Xaddress, out Yaddress))
        {
            return GetAdjacent(Xaddress, Yaddress, GetLinked, xOffset, yOffset);
        }

        return null;
    }

    public UrbTile[] GetNearestAdjacent(Vector2 Location, bool GetLinked = false, int xOffset = 1, int yOffset = 1)
    {
        int Xaddress = 0;
        int Yaddress = 0;
        LocationToTileAddress(Location, out Xaddress, out Yaddress);
       
        return GetAdjacent(Xaddress, Yaddress, GetLinked, xOffset, yOffset);
    }

    public UrbTile[] GetAdjacent(int Xaddress, int Yaddress, bool GetLinked = false, int xOffset = 1, int yOffset = 1)
    {
        UrbTile targetTile = GetTile(Xaddress, Yaddress);

        if(!targetTile.LinksDirty && GetLinked && xOffset == 1 && yOffset == 1)
        {
            return targetTile.CachedAdjacent;
        }
        UrbTile[] targetLinked = (GetLinked) ? targetTile.GetLinked() : new UrbTile[0] { };

        UrbTile[] Adjacency = new UrbTile[8 + targetLinked.Length];

        int[] XScan = new int[] {   0,   1,  1,  1,
                                    0,  -1, -1, -1   };
        int[] YScan = new int[] {  -1,  -1,  0,  1,
                                    1,   1,  0, -1   };

        /*
                7 0 1
                6   2
                5 4 3
        */

        for (int i = 0; i < 8; i++)
        {
            int Xtest = (XScan[i]*xOffset) + Xaddress;
            int Ytest = (YScan[i]*yOffset) + Yaddress;

            UrbTile TestTile = GetTile(Xtest, Ytest);

            Adjacency[i] = TestTile;

        }

        if (GetLinked && targetLinked.Length > 0)
        {
            targetLinked.CopyTo(Adjacency, 8);
        }

        return Adjacency;
    }

    public void RefreshAllPathableSize()
    {
        for (int i = 0; i < Xsize; i++)
        {
            for (int ii = 0; ii < Ysize; ii++)
            {
                MapTiles[i][ii].EvaluateSizeLimit();
            }
        }
    }

    public void RefreshAllScent()
    {
        for (int i = 0; i < Xsize; i++)
        {
            for (int ii = 0; ii < Ysize; ii++)
            {
                MapTiles[i][ii].ClearScent();
            }
        }
    }

}
