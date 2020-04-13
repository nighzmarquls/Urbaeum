using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbTile
{
    public const int MaxSize = 3;
    public const int MaxTerrain = (int)UrbPathTerrain.MaxPathTerrain;
    UrbMap OwningMap;
    int XAddress;
    int YAddress;


    public bool LinksDirty = true;
    UrbTile[] Links;
    public UrbTile[] CachedAdjacent { get { return Adjacent; } }
    protected UrbTile[] Adjacent;

    bool ScentDirty = false;
    public UrbPathTerrain[] TerrainTypes;
    public UrbScent[][] TerrainFilter;

    protected UrbAgent Content;
    public List<UrbAgent> Occupants; 

    public UrbEnvironment Environment;
    public float cost;

    protected int PathableSize = 0;

    protected bool mBlocked = false;
    public bool Blocked { get { return mBlocked; }
        set {
            if(value != mBlocked)
            {
                mBlocked = value;
                PropogateSizeLimit();
            }
        }
    }

    public int SizeLimit { get { return PathableSize; } }

    public int X { get { return XAddress; } }
    public int Y { get { return YAddress; } }

    public Vector3 Location { get { return OwningMap.TileAddressToLocation(XAddress, YAddress); } }

    private void Constructor(UrbMap CreatingMap)
    {
        Occupants = new List<UrbAgent>();
        OwningMap = CreatingMap;

        TerrainFilter = new UrbScent[MaxTerrain][];
        for(int i = 0; i < MaxTerrain; i++)
        {
            TerrainFilter[i] = new UrbScent[MaxSize];
            for (int ii = 0; ii < MaxSize; ii++)
            {
                TerrainFilter[i][ii] = new UrbScent();
            }
           
        }
        
        Environment = new UrbEnvironment(this);
    }

    public UrbTile(UrbMap CreatingMap, int CreatedX, int CreatedY)
    {
        Constructor(CreatingMap);
        XAddress = CreatedX;
        YAddress = CreatedY;
        Links = new UrbTile[] { };
    }

    public UrbTile(UrbMap CreatingMap, int CreatedX, int CreatedY, UrbTile[] LinkedTiles)
    {
        Constructor(CreatingMap);
        XAddress = CreatedX;
        YAddress = CreatedY;
        Links = LinkedTiles;
    }

    public UrbTile(UrbMap CreatingMap, Vector2 MapLocation)
    {
        Constructor(CreatingMap);
        OwningMap.LocationToTileAddress(MapLocation, out XAddress, out YAddress);
        Links = new UrbTile[] { };
    }

    public UrbTile(UrbMap CreatingMap, Vector2 MapLocation, UrbTile[] LinkedTiles)
    {
        Constructor(CreatingMap);
        OwningMap.LocationToTileAddress(MapLocation, out XAddress, out YAddress);
        Links = LinkedTiles;
    }

    public bool LoadTileFromData(UrbTileData input)
    {
        LinksDirty = true;
        Links = new UrbTile[input.Links.Length];
 
        for(int i = 0; i < Links.Length; i++)
        {
            UrbMap LinkedMap = UrbSystemIO.GetMapFromID(input.Links[i].MapID);
            if (LinkedMap != null)
            {
                UrbTile LinkedTile = LinkedMap.GetTile(input.Links[i].X, input.Links[i].Y);
                Links[i] = LinkedTile;
            }
        }

        ClearTile();

        if (input.Content > -1)
        {
            UrbSystemIO.LoadAgentFromID(input.Content,this, input.Objects[0]);
        }

        Blocked = input.Blocked;
        Environment.LoadEnvironmentFromData(input.Environment);
        // This is broken somehow, fix it.
        /*TerrainTypes = new UrbPathTerrain[input.TerrainTypes.Length];

        for (int i = 0; i < TerrainTypes.Length; i++)
        {
            TerrainTypes[i] = input.TerrainTypes[i];
        }*/
        return true;
    }

    public UrbTileData GetTileData()
    {
        UrbTileData output = new UrbTileData();

        if(Links.Length > 0)
        {
            output.Links = new UrbTileLinkData[Links.Length];

            for(int i = 0; i < Links.Length; i++)
            {
                UrbTileLinkData TempLink = new UrbTileLinkData();
                TempLink.MapID = UrbSystemIO.GetMapID(Links[i].OwningMap);
                TempLink.X = Links[i].XAddress;
                TempLink.Y = Links[i].YAddress;
                output.Links[i] = TempLink;
            }
        }
        else
        {
            output.Links = new UrbTileLinkData[0];
        }

        if(Content != null)
        {
            output.Content = UrbSystemIO.GetAgentID(Content);
            output.Objects = new UrbObjectData[]
            {
                UrbEncoder.Read(Content.gameObject)
            };
        }
        else
        {
            output.Content = -1;
        }

        output.Blocked = Blocked;

        output.Environment = Environment.GetEnvironmentData();

        return output;
    }

    public UrbTile[] GetLinked()
    {
        return Links;
    }

    public UrbTile[] GetAdjacent(bool GetLinked = false, int xOffset = 1, int yOffset = 1)
    {
        return OwningMap.GetAdjacent(XAddress, YAddress, GetLinked, xOffset, yOffset);
    }

    public UrbTile GetRelativeTile(int Xdistance, int Ydistance)
    {
        if(Xdistance == 0 && Ydistance == 0)
        {
            return this;
        }
        return OwningMap.GetTile(XAddress + Xdistance, YAddress - Ydistance);

    }

    protected void PropogateSizeLimit()
    {
        UrbTile[] Adjacent = OwningMap.GetAdjacent(XAddress, YAddress);
        EvaluateSizeLimit();
        for(int i = 0; i < Adjacent.Length; i ++) 
        {
            if(Adjacent[i] == null)
            {
                continue;
            }

            Adjacent[i].EvaluateSizeLimit();
        }
    }

    public void EvaluateSizeLimit()
    {
        if (Blocked)
        {
            PathableSize = 0;
            return;
        }
        UrbTile[] Adjacent = OwningMap.GetAdjacent(XAddress, YAddress);
        
        int fullOpen = 0;
        int mediumOpen = 0;

        for( int i = 0; i < Adjacent.Length; i++)
        {
            UrbTile Tile = Adjacent[i];

            if (Tile == null || Tile.Blocked)
            {
                if (i > 4)
                {
                    break;
                }
                
            }
            else
            {
                fullOpen ++;
                if( i > 1 && i < 5)
                {
                    mediumOpen++;
                }
            }
        }

        if (fullOpen > 7) // all eight directions are open
        {
            PathableSize = 3;
        }
        else if (mediumOpen > 2 )
        {
            PathableSize = 2;
        }
        else
        {
            PathableSize = 1;
        }
        ScentDirty = true;
    }

    public void AddScent(UrbScentTag tag, float value)
    {
        ScentDirty = true;
        for (int i = 0; i < TerrainTypes.Length; i++)
        {
            int TerrainType = (int)TerrainTypes[i];

            for (int s = 0; s < SizeLimit; s++)
            {
                TerrainFilter[TerrainType][s][tag] = value;
            }
            
        }

    }

    public void ClearScent()
    {
        ScentDirty = true;

    }

    public UrbAgent CurrentContent {

        get { return Content; }

    }

    public void OnAgentArrive(UrbAgent input)
    {
        if(Content == null)
        {
            Content = input;
            input.Tileprint.ArriveAtTile(this, input);
            input.CurrentMap = this.OwningMap;
            input.transform.localScale = new Vector3(this.OwningMap.TileSize, this.OwningMap.TileSize, this.OwningMap.TileSize)*input.SizeOffset;
        }
    }

    public void OnAgentLeave(UrbAgent input)
    {
        if(Content == input)
        {
            Content = null;
            input.Tileprint.DepartFromTile(this, input);
        }
    }

    public void ToggleLink(UrbTile input)
    {
        List<UrbTile> ListofLinks = new List<UrbTile>(Links);

        if(ListofLinks.Contains(input))
        {
            input.RemoveLink(this);
            RemoveLink(input);
        }
        else
        {
            AddLink(input);
        }

        ScentDirty = true;
        LinksDirty = true;
        Environment.MakeDirty();
    }

    public void AddLink(UrbTile input)
    {
        foreach(UrbTile link in Links)
        {
            if(link == input)
            {
                Debug.Log("Already Linked");
                return;
            }
        }
        UrbTile[] temp = new UrbTile[Links.Length + 1];
        Links.CopyTo(temp, 0);
        temp[Links.Length] = input;
        Links = temp;

        ScentDirty = true;
        LinksDirty = true;
        Environment.MakeDirty();

        foreach (UrbTile link in input.Links)
        {
            if (link == input)
            {
                Debug.Log("Already Linked");
                return;
            }
        }
        temp = new UrbTile[input.Links.Length + 1];
        input.Links.CopyTo(temp, 0);
        temp[input.Links.Length] = this;
        input.Links = temp;

        input.ScentDirty = true;
        input.LinksDirty = true;
        input.Environment.MakeDirty();
    }

    public void RemoveLink(UrbTile input)
    {
        UrbTile linked = null;
        foreach (UrbTile link in Links)
        {
            if (link == input)
            {
                linked = link;
                break;
            }
        }
        if(linked == null)
        {
            Debug.Log("Not Linked");
            return;
        }

        List<UrbTile> ListofLinks = new List<UrbTile>(Links);

        ListofLinks.Remove(input);

        Links = ListofLinks.ToArray();

        ScentDirty = true;
        LinksDirty = true;
        Environment.MakeDirty();

        input.ScentDirty = true;
        input.LinksDirty = true;
        input.Environment.MakeDirty();
    }

    public void ClearTile()
    {
        if(Content == null )
        {
            return;
        }
        else
        {
            Content.Remove();
        }
    }

    UrbUtility.UrbThrottle ScentThrottle = new UrbUtility.UrbThrottle();
    
    public IEnumerator ScentCoroutine()
    {
        while(true)
        {
            if(UrbSystemIO.Instance.Loading)
            {
                continue;
            }
            if (ScentDirty)
            {

                for (int t = 0; t < TerrainTypes.Length; t++)
                {
                    for (int s = 0; s < SizeLimit; s++)
                    {
                        yield return ScentThrottle.PerformanceThrottle();

                        if(TerrainFilter[(int)TerrainTypes[t]][s] == null)
                        {
                            continue;
                        }

                        if (TerrainFilter[(int)TerrainTypes[t]][s].dirty)
                        {
                            TerrainFilter[(int)TerrainTypes[t]][s].dirty = false;
                            TerrainFilter[(int)TerrainTypes[t]][s].DecayScent();
                            ScentDirty = true;
                        }
                    }

                    if (ScentDirty)
                    {
                        DiffuseScent();
                        ScentDirty = false;
                    }
                }
               
            }
            else
            {
                yield return new WaitForSeconds(0.25f);
            }

            yield return new WaitForSeconds(0.25f);
            
        }
    }

    //TODO: Optimize this
    public void PropogateScent()
    {
        List<UrbTile> ToDiffuse = new List<UrbTile>();

        ToDiffuse.Add(this);

        List<UrbTile> ToAdd = new List<UrbTile>();
        ToAdd.AddRange(OwningMap.GetAdjacent(XAddress, YAddress));
        while (ToAdd.Count > 0)
        {
            if(ToAdd[0] == null || ToAdd[0].Blocked || ToDiffuse.Contains(ToAdd[0]))
            {
                ToAdd.RemoveAt(0);
            }
            else
            {
                ToDiffuse.Add(ToAdd[0]);
                ToAdd.AddRange(OwningMap.GetAdjacent(ToAdd[0].XAddress, ToAdd[0].YAddress));
                ToAdd.RemoveAt(0);
            }
        }

        foreach(UrbTile Tile in ToDiffuse)
        {
            Tile.DiffuseScent();
        }
    }

    void DiffuseScent()
    {
        float diffusion = UrbScent.ScentDiffusion;
        if (LinksDirty)
        {
            Adjacent = OwningMap.GetAdjacent(XAddress, YAddress, true);
            LinksDirty = false;
        }

        for (int i = 0; i < TerrainTypes.Length; i++)
        {
            int TerrainType = (int)TerrainTypes[i];

            for(int t = 0; t < Adjacent.Length; t++)
            {
                if (Adjacent[t] == null || Adjacent[t].Blocked)
                {
                    continue;
                }

                for (int check = 0; check < Adjacent[t].TerrainTypes.Length; check++)
                {
                    if (check != TerrainType)
                        continue;

                    Adjacent[t].ScentDirty = true;
                    for(int s = 0; s < Adjacent[t].SizeLimit; s++)
                    {
                        Adjacent[t].TerrainFilter[TerrainType][s].ReceiveScent(TerrainFilter[TerrainType][s], diffusion);
                    }
                }                   
            }
        }
    }
}
