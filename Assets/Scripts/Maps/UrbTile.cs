using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbTile
{
    public const int MaximumOccupants = 4;
    public const float TileCapacity = 1000;
    public const int MaxSize = 3;
    public const int MaxTerrain = (int)UrbPathTerrain.MaxPathTerrain;
    public const float DepthPush = 3;
    public UrbMap OwningMap { get; protected set; }
    int XAddress;
    int YAddress;


    public bool LinksDirty = true;
    UrbTile[] Links;
    public UrbTile[] CachedAdjacent { get { return Adjacent; } }
    protected UrbTile[] Adjacent;

    public float TimeMultiplier { get { return OwningMap.TimeMultiplier;  } }

    bool ScentDirty = false;
    public UrbPathTerrain[] TerrainTypes;
    public UrbScent[][] TerrainFilter;

    protected UrbAgent Content;
    protected List<UrbAgent> Contents;
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

    protected float ScentDiffusion = UrbScent.ScentDiffusion;

    public float FreeCapacity {
        get {
            float Capacity = TileCapacity;
            for(int o = 0; o < Occupants.Count; o++)
            {
                Capacity -= Occupants[o].MassPerTile;
                if (Capacity <= 0)
                {
                    return 0;
                }
            }
            return Capacity;
        }
    }

    public int SizeLimit { get { return PathableSize; } }

    public int X { get { return XAddress; } }
    public int Y { get { return YAddress; } }

    protected Vector3 LocationOffset = Vector3.zero;

    public Vector3 Location { get { return OwningMap.TileAddressToLocation(XAddress, YAddress) + LocationOffset;} }
    public Vector3 RawLocation { get { return OwningMap.TileAddressToLocation(XAddress, YAddress); } }

    bool Ordering = false;
    public void ReorderContents()
    {
        if(Ordering)
        {
            return;
        }

        Ordering = true;
        LocationOffset = Vector3.zero;
        if (Occupants.Count > 0)
        {
            Vector3 Center = OwningMap.TileAddressToLocation(XAddress, YAddress);
            UrbAgent Biggest = Occupants[0];
            List<UrbAgent> OrderedOccupants = new List<UrbAgent>();
            float BiggestMass = Biggest.MassPerTile;
            for (int i = 0; i < Occupants.Count; i++)
            {
                if(Occupants[i].MassPerTile > BiggestMass)
                {
                    OrderedOccupants.Insert(0, Occupants[i]);
                    Biggest = Occupants[i];
                    BiggestMass = Occupants[i].MassPerTile;
                }
                else
                {
                    OrderedOccupants.Add(Occupants[i]);
                }
            }
            if (Biggest != null)
            {
                float Turn = 0;
                float TurnAdjust = (Mathf.PI);
                float Radius = 0;
                float RadiusAdjust = 3;

                float TileCapacityOffset = TileCapacity / 4f;

                for (int i = 0; i < OrderedOccupants.Count; i++)
                {
                    if(i > MaximumOccupants)
                    {
                        OrderedOccupants[i].Remove();
                        continue;
                    }
                    float X = Mathf.Sin(Turn);
                    float Y = Mathf.Cos(Turn);
                    LocationOffset = new Vector3(X, Y, 0) * Radius * this.OwningMap.TileSize;
                    if (OrderedOccupants[i].Shuffle)
                    {
                        OrderedOccupants[i].Location = Center + LocationOffset + new Vector3(0, 0, (LocationOffset.y * DepthPush) - LocationOffset.x);
                    }
                    Turn += (OrderedOccupants[i].MassPerTile / TileCapacityOffset) * TurnAdjust;
                    TurnAdjust *= 0.85f;
                    Radius += (OrderedOccupants[i].MassPerTile / TileCapacityOffset) / RadiusAdjust;
                    RadiusAdjust += 5f;
                    
                }

            }

            float Free = FreeCapacity / TileCapacity;
            if (Free <= 0)
            {
                ScentDirty = false;
                Blocked = true;
            }
            else
            {
                Blocked = false;
            }

            ScentDiffusion = UrbScent.ScentDiffusion * Free;
            Occupants = OrderedOccupants;
        }
    }

    private void Constructor(UrbMap CreatingMap)
    {
        Occupants = new List<UrbAgent>();
        Contents = new List<UrbAgent>();
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

        if (input.Contents != null && input.Contents.Length > 0)
        {
            for (int c = 0; c < input.Contents.Length; c++)
            {
                UrbSystemIO.LoadAgentFromID(input.Contents[c], this, input.Objects[c]);
            }
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

        if(Contents.Count > 0)
        {
            output.Contents = new int[Contents.Count];
            output.Objects = new UrbObjectData[Contents.Count];
            for (int c = 0; c < Contents.Count; c++)
            {
                output.Contents[c] = UrbSystemIO.GetAgentID(Contents[c]);
                output.Objects[c] = UrbEncoder.Read(Contents[c].gameObject);
            }
        }
        else
        {

            output.Contents = new int[0];
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

        get {
            if(Contents == null || Contents.Count == 0)
            {
                return null;
            }

            return Contents[Contents.Count-1];
        }

    }

    public UrbAgent[] CurrentContents {
        get {
            if(Contents == null || Contents.Count == 0)
            {
                return new UrbAgent[0];
            }

            return Contents.ToArray();
        }
    }

    public void OnAgentArrive(UrbAgent input)
    {
            Ordering = false;
            Contents.Add(input);
            input.Tileprint.ArriveAtTile(this, input);
            input.CurrentTile = this;
            input.CurrentMap = this.OwningMap;
            input.transform.localScale = new Vector3(this.OwningMap.TileSize, this.OwningMap.TileSize, this.OwningMap.TileSize)*input.SizeOffset;
            ReorderContents();
    }

    public void OnAgentLeave(UrbAgent input)
    {
        Ordering = false;
        Contents.Remove(input);
        input.Tileprint.DepartFromTile(this, input);
        ReorderContents();
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
        if(Contents == null || Contents.Count == 0)
        {
            return;
        }
        else
        {
            for (int i = 0; i < Contents.Count; i++)
            {
                Contents[i].Remove();
            }
        }
    }

    public IEnumerator ScentCoroutine()
    {
        while(true)
        {
            Ordering = false;
            if (UrbSystemIO.Instance == null || UrbSystemIO.Instance.Loading)
            {
                ///continue;
            }
            else if (ScentDirty)
            {

                for (int t = 0; t < TerrainTypes.Length; t++)
                {
                    for (int s = 0; s < SizeLimit; s++)
                    {
                        if(TerrainFilter[(int)TerrainTypes[t]][s] == null)
                        {
                            continue;
                        }

                        if (TerrainFilter[(int)TerrainTypes[t]][s].dirty)
                        {
                            TerrainFilter[(int)TerrainTypes[t]][s].dirty = false;
                            yield return TerrainFilter[(int)TerrainTypes[t]][s].DecayScent();
                            ScentDirty = (TerrainFilter[(int)TerrainTypes[t]][s].dirty)? true : ScentDirty;
                        }
                    }

                    
                }

                if (ScentDirty)
                {
                    yield return DiffuseScent();
                    ScentDirty = false;
                }

            }
            else
            {
                yield return new WaitForSeconds(UrbScent.ScentInterval * TimeMultiplier);
            }

            yield return new WaitForSeconds(UrbScent.ScentInterval * TimeMultiplier);
            
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

    IEnumerator DiffuseScent()
    {
        if (LinksDirty)
        {
            Adjacent = OwningMap.GetAdjacent(XAddress, YAddress, true);
            LinksDirty = false;
        }

        for (int i = 0; i < TerrainTypes.Length; i++)
        {

            for(int t = 0; t < Adjacent.Length; t++)
            {
                if (Adjacent[t] == null || Adjacent[t].Blocked)
                {
                    continue;
                }

                for (int check = 0; check < Adjacent[t].TerrainTypes.Length; check++)
                {
                    if (check != (int)TerrainTypes[i])
                        continue;

                    Adjacent[t].ScentDirty = true;
                    for(int s = 0; s < Adjacent[t].SizeLimit; s++)
                    {
                        yield return Adjacent[t].TerrainFilter[(int)TerrainTypes[i]][s].ReceiveScent(TerrainFilter[(int)TerrainTypes[i]][s], ScentDiffusion);
                    }
                }                   
            }
        }
    }
}
