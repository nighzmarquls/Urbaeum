using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
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

    protected List<UrbAgent> Contents;
    public List<UrbAgent> Occupants; 

    public UrbEnvironment Environment;

    protected int PathableSize = 0;

    protected bool mBlocked = false;
    public bool Blocked { get { return mBlocked; }
        set {
            if (value != mBlocked)
            {
                mBlocked = value;
                PropagateSizeLimit();
            }
        }
    }

    protected float ScentDiffusion = UrbScent.ScentDiffusion;

    public float FreeCapacity {
        get {
            float Capacity = TileCapacity;
            for (int o = 0; o < Occupants.Count; o++)
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

    static ProfilerMarker s_ReorderContents_p = new ProfilerMarker("UrbTile.ReorderContents.ToTileWithBiggestMass");
    static ProfilerMarker s_ReorderContents_p2_p = new ProfilerMarker("UrbTile.ReorderContents.AfterTileWithBiggestMass");

    public int LinkCount { get; protected set; }
    bool Ordering = false;
    public void ReorderContents()
    {
        s_ReorderContents_p.Begin();
        if(Ordering)
        {
            s_ReorderContents_p.End();
            return;
        }

        Ordering = true;
        LocationOffset = Vector3.zero;
        if (Occupants.Count <= 0)
        {
            s_ReorderContents_p.End();
            return;
        }
        
        Vector3 Center = OwningMap.TileAddressToLocation(XAddress, YAddress);
        List<UrbAgent> OrderedOccupants = new List<UrbAgent>(Occupants.Count);
        float BiggestMass = Occupants[0].MassPerTile;
        for (int i = 0; i < Occupants.Count; i++)
        {
            var occupant = Occupants[i];
            if (occupant.WasDestroyed || !occupant.isActiveAndEnabled)
            {
                continue;
            }
            
            if(occupant.MassPerTile > BiggestMass)
            {
                OrderedOccupants.Insert(0, occupant);
                BiggestMass = occupant.MassPerTile;
            }
            else
            {
                OrderedOccupants.Add(occupant);
            }
        }
        
        s_ReorderContents_p.End();
        s_ReorderContents_p2_p.Begin();
       
        float Turn = 0;
        float TurnAdjust = (Mathf.PI);
        float Radius = 0;
        float RadiusAdjust = 3;

        float TileCapacityOffset = TileCapacity / 4f;

        for (int i = 0; i < OrderedOccupants.Count; i++)
        {
            if(i > MaximumOccupants)
            {
                if (Debug.developerConsoleVisible || Debug.isDebugBuild)
                {
                    Debug.Log("Max entities on a tile have been reached forcibly removing.");
                }
                OrderedOccupants[i].Remove(false);
                continue;
            }
            float X = Mathf.Sin(Turn);
            float Y = Mathf.Cos(Turn);
            LocationOffset = new Vector3(X, Y, 0) * (Radius * this.OwningMap.TileSize);
            if (OrderedOccupants[i].Shuffle)
            {
                OrderedOccupants[i].Location = Center + LocationOffset + new Vector3(0, 0, (LocationOffset.y * DepthPush) - LocationOffset.x);
            }
            Turn += (OrderedOccupants[i].MassPerTile / TileCapacityOffset) * TurnAdjust;
            TurnAdjust *= 0.85f;
            Radius += (OrderedOccupants[i].MassPerTile / TileCapacityOffset) / RadiusAdjust;
            RadiusAdjust += 5f;
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
        s_ReorderContents_p2_p.End();
    }

    void Constructor(UrbMap CreatingMap)
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
                TerrainFilter[i][ii] = new UrbScent{
                    Tags = new DirtyableTag[UrbScent.MaxTag],
                };
            }
        }
        
        Environment = new UrbEnvironment(this);
    }

    public UrbTile(UrbMap CreatingMap, int CreatedX, int CreatedY)
    {
        Constructor(CreatingMap);
        XAddress = CreatedX;
        YAddress = CreatedY;
        Links = new UrbTile[] { null, null, null };
        LinkCount = 0;
    }

    public UrbTile(UrbMap CreatingMap, int CreatedX, int CreatedY, UrbTile[] LinkedTiles)
    {
        Constructor(CreatingMap);
        XAddress = CreatedX;
        YAddress = CreatedY;
        Links = LinkedTiles;
        LinkCount = 0;
        
        foreach (var tile in LinkedTiles)
        {
            if (tile != null)
            {
                ++LinkCount;
            }
        }
    }

    public UrbTile(UrbMap CreatingMap, Vector2 MapLocation)
    {
        Constructor(CreatingMap);
        OwningMap.LocationToTileAddress(MapLocation, out XAddress, out YAddress);
        Links = new UrbTile[] { null, null, null };
    }

    public UrbTile(UrbMap CreatingMap, Vector2 MapLocation, UrbTile[] LinkedTiles)
    {
        Constructor(CreatingMap);
        OwningMap.LocationToTileAddress(MapLocation, out XAddress, out YAddress);
        Links = LinkedTiles;
        LinkCount = 0;
        foreach (var tile in LinkedTiles)
        {
            if (tile != null)
            {
                ++LinkCount;
            }
        }
    }

    public bool LoadTileFromData(UrbTileData input)
    {
        LinksDirty = true;
        if (input.Links.Length > 0)
        {
            Links = new UrbTile[input.Links.Length];

            for (int i = 0; i < Links.Length; i++)
            {
                UrbMap LinkedMap = UrbSystemIO.GetMapFromID(input.Links[i].MapID);
                if (LinkedMap != null)
                {
                    UrbTile LinkedTile = LinkedMap.GetTile(input.Links[i].X, input.Links[i].Y);
                    Links[i] = LinkedTile;
                }
            }
        }
        else
        {
            Links = new UrbTile[0];
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
            List<UrbTileLinkData> WorkingList = new List<UrbTileLinkData>();
            foreach (var link in Links)
            {
                if (link == null)
                {
                    continue;
                }
                
                UrbTileLinkData TempLink = new UrbTileLinkData();
                TempLink.MapID = UrbSystemIO.GetMapID(link.OwningMap);
                TempLink.X = link.XAddress;
                TempLink.Y = link.YAddress;
                WorkingList.Add(TempLink);
            }
            output.Links = WorkingList.ToArray();
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
    
    static ProfilerMarker s_GetAdjacent_p = new ProfilerMarker("UrbTile.GetAdjacent");
    public UrbTile[] GetAdjacent(bool GetLinked = false, int xOffset = 1, int yOffset = 1)
    {
        //Nominally, this GetAdjacent section would be using Auto, but
        //Issues with `using` on ProfileMarkers seems to have blow
        using (s_GetAdjacent_p.Auto())
        {
            return OwningMap.GetAdjacent(XAddress, YAddress, GetLinked, xOffset, yOffset);
        }
    }

    public UrbTile GetRelativeTile(int Xdistance, int Ydistance)
    {
        if(Xdistance == 0 && Ydistance == 0)
        {
            return this;
        }
        return OwningMap.GetTile(XAddress + Xdistance, YAddress - Ydistance);

    }

    protected void PropagateSizeLimit()
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
    static ProfilerMarker s_OnAgentArrive_p = new ProfilerMarker("UrbTile.OnAgentArrive");

    public void OnAgentArrive(UrbAgent input)
    {
        s_OnAgentArrive_p.Begin(input);
        Ordering = false;
        Contents.Add(input);
        input.Tileprint.ArriveAtTile(this, input);
        input.CurrentTile = this;

        //TODO: What is a map, and why are we assigning it so frequently
        //I can only find OwningMap being set once, at game initialization...
        //Can we move CurrentMap to object initialization?
        input.CurrentMap = OwningMap;
        
        input.transform.localScale = new Vector3(this.OwningMap.TileSize, this.OwningMap.TileSize, this.OwningMap.TileSize)*input.SizeOffset;
        ReorderContents();
        s_OnAgentArrive_p.End();
    }

    static ProfilerMarker s_OnAgentLeave_p = new ProfilerMarker("UrbTile.OnAgentLeave");
    public void OnAgentLeave(UrbAgent input, bool reorder = true)
    {
        Ordering = !reorder;
        Contents.Remove(input);
        
        s_OnAgentLeave_p.Begin();
        input.Tileprint.DepartFromTile(this, input);
        if (reorder)
        {
            ReorderContents();
        }

        s_OnAgentLeave_p.End();
    }

    public void ToggleLink(UrbTile input)
    {
        bool containedLink = RemoveLink(input);
        
        if (!containedLink)
        {
            AddLink(input);
        }
    }

    public void AddLink(UrbTile input, bool firstCall =true)
    {
        var linkIdx = GetLinkIndex(input);
        
        //sanity check that the link doesn't already exist.
        if (linkIdx != -1)
        {
            //handle the edge-case where we have a link to
            //input, but input has no link to us
            input.AddLink(this, false);
            return;
        }

        var emptySlot = GetLinkIndex(null);
        if (emptySlot == -1)
        {
            emptySlot = Links.Length;
            //Far too fancy, but shorthand syntax for saying that we already know that the list will grow a bit
            //so let's allocate a couple new object refs to the array for future reuse.
            Links = (new List<UrbTile>(Links) {null, null, null}).ToArray();
        }

        Links[emptySlot] = input;

        ScentDirty = true;
        LinksDirty = true;
        Environment.MakeDirty();

        if (firstCall)
        {
            input.AddLink(this, false);
        }
    }

    public int GetLinkIndex(UrbTile toCheck)
    {
        UrbTile link;
        for (int i = 0; i < Links.Length; i++)
        {
            link = Links[i];
            if (link != toCheck)
            {
                continue;
            }
            
            //LOG.Log("Already Linked");
                
            return i;
        }
        
        //Debug.Log("Not Linked");
        return -1;
    }

    public bool RemoveLink(UrbTile input, bool firstCall = true)
    {
        var idx = GetLinkIndex(input);
        if (idx == -1)
        {
            return false;
        }

        Links[idx] = null;

        ScentDirty = true;
        LinksDirty = true;
        Environment.MakeDirty();

        if (firstCall)
        {
            input.RemoveLink(this, false);
        }

        return true;
    }

    public void ClearTile()
    {
        if(Contents == null || Contents.Count == 0)
        {
            return;
        }
        
        for (int i = 0; i < Contents.Count; i++)
        {
            Contents[i].Remove();
        }
    }

    static ProfilerMarker s_TileScentCoroutineLoop_p = new ProfilerMarker("UrbTile.ScentCoTerrainLoop");
    public IEnumerator ScentCoroutine()
    {
        Debug.Log("Initializing Scent coroutine in UrbTile");

        while(true)
        {
            yield return new WaitForSeconds(UrbScent.ScentInterval * TimeMultiplier);
            Ordering = false;
            
            if (!UrbSystemIO.HasInstance || UrbSystemIO.Instance.Loading)
            {
                //UrbSystemIO can take a second or two to load, so may as well make sure we give it the chance. 
                //yield return new WaitForSeconds(0.2f);
                continue;
            }

            if (!ScentDirty)
            {
                continue;
            }

            s_TileScentCoroutineLoop_p.Begin();
            for (int t = 0; t < TerrainTypes.Length; t++)
            {
                for (int s = 0; s < SizeLimit; s++)
                {
                    var terrainType = (int)TerrainTypes[t];
                    var terrainFilter = TerrainFilter[terrainType][s];
                    if (!terrainFilter.dirty)
                    {
                        continue;
                    }
                    
                    terrainFilter.dirty = false;
                    s_TileScentCoroutineLoop_p.End();
                    yield return terrainFilter.DecayScent();
                    s_TileScentCoroutineLoop_p.Begin();
                    if (ScentDirty)
                    {
                        continue;
                    }

                    //Want to use the terrainFilter var above, but 
                    //I need to make sure that's not going to change what happens
                    //here for this ScentDirty.
                    ScentDirty = TerrainFilter[(int)TerrainTypes[t]][s].dirty || ScentDirty;
                }
            }
            s_TileScentCoroutineLoop_p.End();

            if (!ScentDirty)
            {
                continue;
            }
            
            yield return DiffuseScent();
            ScentDirty = false;
        }
    }

    static ProfilerMarker s_PropagateScent_p = new ProfilerMarker("UrbTile.PropagateScent");
    //TODO: Optimize this
    public void PropagateScent()
    {
        s_PropagateScent_p.Begin();
        //TODO: The array-copying here seems non-performant.
        var ToAdd = new List<UrbTile>(OwningMap.GetAdjacent(XAddress, YAddress));
        var ToDiffuse = new List<UrbTile>(ToAdd.Count);
        
        while (ToAdd.Count > 0)
        {
            var scent = ToAdd[0];
            if(scent == null || scent.Blocked || ToDiffuse.Contains(scent))
            {
                ToAdd.RemoveAt(0);
                continue; 
            }
            
            ToDiffuse.Add(ToAdd[0]);
            ToAdd.AddRange(OwningMap.GetAdjacent(ToAdd[0].XAddress, ToAdd[0].YAddress));
            ToAdd.RemoveAt(0);
        }

        foreach(UrbTile Tile in ToDiffuse)
        {
            // ReSharper disable once IteratorMethodResultIsIgnored
            Tile.DiffuseScent();
        }

        s_PropagateScent_p.End();
    }

    
    static ProfilerMarker s_DiffuseScent_p = new ProfilerMarker("UrbTile.DiffuseScent");

    IEnumerator DiffuseScent()
    {
        s_DiffuseScent_p.Begin();
        if (LinksDirty)
        {
            Adjacent = OwningMap.GetAdjacent(XAddress, YAddress, true);
            LinksDirty = false;
        }

        for (int i = 0; i < TerrainTypes.Length; i++)
        {
            for(int t = 0; t < Adjacent.Length; t++)
            {
                var adj = Adjacent[t];
                if (adj == null || adj.Blocked)
                {
                    continue;
                }

                for (int check = 0; check < Adjacent[t].TerrainTypes.Length; check++)
                {
                    var terrainType = (int) TerrainTypes[i];
                    
                    if (check != terrainType)
                        continue;

                    adj.ScentDirty = true;
                    for(int s = 0; s < adj.SizeLimit; s++)
                    {
                        var filter = adj.TerrainFilter[terrainType][s];
                        var scent = filter.ReceiveScent(TerrainFilter[terrainType][s], ScentDiffusion);
                        s_DiffuseScent_p.End();
                        
                        yield return scent;
                        s_DiffuseScent_p.Begin();
                    }
                }     
            }
        }

        s_DiffuseScent_p.End();
    }
}
