using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    protected ScentList AgentScentCache = new ScentList(5);
    
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
    protected int LastCapacityCalculationFrame = 0;
    protected float _remainingCapacity = TileCapacity;
    public float RemainingCapacity {
        get
        {
            //Helps ensure that we only calculating RemainingCapacity once per frame.
            if (LastCapacityCalculationFrame != Time.frameCount)
            {
                CalculateRemainingCapacity();
            }
            
            return _remainingCapacity;
        }
    }

    //This is also called from OnAgentEnter and OnAgentLeave
    void CalculateRemainingCapacity()
    {
        LastCapacityCalculationFrame = Time.frameCount;
        float Capacity = TileCapacity;
        for (int o = 0; o < Occupants.Count; o++)
        {
            Capacity -= Occupants[o].MassPerTile;
        }
            
        if (Capacity <= 0)
        {
            Capacity = 0;
        }
            
        _remainingCapacity = Capacity;
    }
    
    public int SizeLimit { get { return PathableSize; } }

    public int X { get { return XAddress; } }
    public int Y { get { return YAddress; } }

    protected Vector3 LocationOffset = Vector3.zero;

    public Vector3 Location { get { return OwningMap.TileAddressToLocation(XAddress, YAddress) + LocationOffset;} }
    public Vector3 RawLocation { get { return OwningMap.TileAddressToLocation(XAddress, YAddress); } }

    public bool TerrainPassable(UrbPathTerrain Input)
    {
        bool Passable = false;
        for(int tt = 0; tt < TerrainTypes.Length; tt++)
        {
            if(Input == TerrainTypes[tt])
            {
                Passable = true;
                break;
            }
        }
        return Passable;
    }

    public void UpdateClearance()
    {
        if (Occupants.Count <= 0)
        {
            Blocked = false;
            return;
        }
        
        float Free = RemainingCapacity / TileCapacity;
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
    }

    public float this[UrbSubstanceTag[] Tags] { get {
            if (Occupants.Count <= 0)
            {
                return 0;
            }

            float Amount = 0;

            for (int i = 0; i < Occupants.Count; i++)
            {
                if (Occupants[i].HasBody)
                {
                    Amount += Occupants[i].mBody.BodyComposition[Tags];
                }
            }
            return Amount;
        }
    }

    public float this[UrbSubstanceTag Tag] {
        get {
            if (Occupants.Count <= 0)
            {
                return 0;
            }

            float Amount = 0;

            for (int i = 0; i < Occupants.Count; i++)
            {
                if (Occupants[i].HasBody)
                {
                    Amount += Occupants[i].mBody.BodyComposition[Tag];
                }
            }
            return Amount;
        }
    }

    public void VisualShuffle()
    {
        LocationOffset = Vector3.zero;
        if (Occupants.Count <= 0)
        {
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

            if (occupant.MassPerTile > BiggestMass)
            {
                OrderedOccupants.Insert(0, occupant);
                BiggestMass = occupant.MassPerTile;
            }
            else
            {
                OrderedOccupants.Add(occupant);
            }
        }

        float Turn = 0;
        float TurnAdjust = (Mathf.PI);
        float Radius = 0;
        float RadiusAdjust = 3;
        float TileCapacityOffset = TileCapacity / 4f;
        
        for (int i = 0; i < OrderedOccupants.Count; i++)
        {
            if(OrderedOccupants[i].CurrentTile != this)
            {
                continue;
            }

            LocationOffset = new Vector3(Mathf.Sin(Turn), Mathf.Cos(Turn), 0);
            LocationOffset *= (Radius * OwningMap.TileSize);
            var summedLocationOffset = new Vector3(0, 0, (LocationOffset.y * DepthPush) - LocationOffset.x) + LocationOffset;

            if (OrderedOccupants[i].Shuffle)
            {
                OrderedOccupants[i].Location = Center + summedLocationOffset;
            }
            else
            {
                OrderedOccupants[i].Location = Center;
            }
            
            Turn += (OrderedOccupants[i].MassPerTile / TileCapacityOffset) * TurnAdjust;
            TurnAdjust *= 0.85f;
            Radius += (OrderedOccupants[i].MassPerTile / TileCapacityOffset) / RadiusAdjust;
            RadiusAdjust += 5f;
        }

        Occupants = OrderedOccupants;

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
        Links = new UrbTile[] { null, null, null };
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
        Links = new UrbTile[] { null, null, null };
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
    
    public void ClearScent()
    {
        ScentDirty = true;
    }
    
    static ProfilerMarker s_OnAgentArrive_p = new ProfilerMarker("UrbTile.OnAgentArrive");

    public void OnAgentArrive(UrbAgent input)
    {
        Assert.IsNotNull(input);
        
        s_OnAgentArrive_p.Begin(input);
        Contents.Add(input);
        
        input.Tileprint.ArriveAtTile(this, input);
        input.CurrentTile = this;
        
        //TODO: What is a map, and why are we assigning it so frequently
        //I can only find OwningMap being set once, at game initialization...
        //Can we move CurrentMap to object initialization?
        input.CurrentMap = OwningMap;
        
        input.transform.localScale = new Vector3(this.OwningMap.TileSize, this.OwningMap.TileSize, this.OwningMap.TileSize)*input.SizeOffset;

        if(input.IsSmelly)
        {
            ScentDirty = true;
        }

        if (input.HasEnableBeenCalled)
        {
            CalculateRemainingCapacity();
            UpdateClearance();
            
            if (input.Shuffle)
            {
                VisualShuffle();
            }
        }
        
        s_OnAgentArrive_p.End();
    }

    static ProfilerMarker s_OnAgentLeave_p = new ProfilerMarker("UrbTile.OnAgentLeave");
    public void OnAgentLeave(UrbAgent input, bool reorder = true)
    {
        Contents.Remove(input);
        
        s_OnAgentLeave_p.Begin();
        
        input.Tileprint.DepartFromTile(this, input);

        if (input.IsSmelly)
        {
            ScentDirty = true;
        }

        CalculateRemainingCapacity();
        UpdateClearance();

        if (input.Shuffle)
        {
            VisualShuffle();
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

    public void SynchronizeScents()
    {
        //Normalize the scent values we use
        AgentScentCache.ClearValues();

        foreach (var occupant in Occupants)
        {
            if (!occupant.IsSmelly)
            {
                continue;
            }

            Assert.IsNotNull(occupant.SmellSource);
            
            foreach (var tag in occupant.SmellSource.SmellTag)
            {
                AgentScentCache.AddScent(tag, occupant.SmellSource.SmellStrength / occupant.Tileprint.TileCount);
            }
        }
    }
    
    public IEnumerator ScentCoroutine()
    {
        float LastReorderTime = 0.0f;
        const float MinimumTimeSinceLastReorder = .25f;

        float LastScentUpdate = 0.0f;
        const float MinimumScentUpdate = 1.0f;

        while(true)
        {
            yield return new WaitForSeconds(UrbScent.ScentInterval * TimeMultiplier);

            if (!UrbSystemIO.HasInstance || UrbSystemIO.Instance.Loading)
            {
                continue;
            }

            //Now we update ordering on similar timescales as our Scents
            if (Time.fixedTime - LastReorderTime > MinimumTimeSinceLastReorder)
            {
                VisualShuffle();
                LastReorderTime = Time.fixedTime;
            }

            if (LinksDirty)
            {
                Adjacent = OwningMap.GetAdjacent(XAddress, YAddress, true);
                LinksDirty = false;
            }

            UpdateClearance();
            
            //Don't need to call this _that_ often, but I'm still not sure how often we SHOULD call it 
            SynchronizeScents();
            
            for (int t = 0; t < TerrainTypes.Length; t++)
            {
                var terrainType = (int)TerrainTypes[t];
                var currentTerrainScents = TerrainFilter[terrainType];

                for (int s = 0; s < SizeLimit; s++)
                {
                    var terrainFilter = TerrainFilter[terrainType][s];
                    if(terrainFilter == null)
                    {
                        continue;
                    }

                    int idx = 0;
                    var currentTag = AgentScentCache.GetScentTag(idx);
                    //This enumerations' kinda terrible
                    while (currentTag != UrbScentTag.MaxScentTag)
                    {
                        var value = AgentScentCache.Values[idx];
                        //In the "OG" version, the equivalent of these was set 
                        //by dividing the value of the scent by the number of tiles that
                        //were expected to get touched.
                        //This "base" value seems to be somewhat strong even so.
                        currentTerrainScents[s][currentTag] = value * ScentDiffusion;
                        currentTag = AgentScentCache.GetScentTag(++idx);
                    }

                    if (!terrainFilter.dirty)
                    {
                        continue;
                    }

                    terrainFilter.dirty = false;
                    yield return terrainFilter.DecayScent();

                    ScentDirty = ScentDirty || terrainFilter.dirty;
                }
                if (ScentDirty)
                {
                    yield return DiffuseScent(terrainType);
                }
            }
            ScentDirty = false;

            if (!ScentDirty && Occupants.Count > 0)
            {
                if (Time.fixedTime - LastScentUpdate > MinimumScentUpdate)
                {
                    ScentDirty = true;
                    LastScentUpdate = Time.fixedTime;
                }
            }
        }
    }

    static ProfilerMarker s_DiffuseScent_p = new ProfilerMarker("UrbTile.DiffuseScent");

    IEnumerator DiffuseScent(int terrainType)
    {
        s_DiffuseScent_p.Begin();
        
        for (int t = 0; t < Adjacent.Length; t++)
        {
            var adj = Adjacent[t];
            if (adj == null || adj.Blocked)
            {
                continue;
            }

            bool sentScent = false;
            
            for (int check = 0; check < Adjacent[t].TerrainTypes.Length; check++)
            {
                if (check != terrainType)
                {
                    continue;
                }
                
                for (int s = 0; s < adj.SizeLimit; s++)
                {
                    var sendScent = TerrainFilter[terrainType][s];
                    if (sendScent.dirty == false)
                    {
                        continue;
                    }

                    sentScent = true;
                    var filter = adj.TerrainFilter[terrainType][s];
                    var scent = filter.ReceiveScent(sendScent, ScentDiffusion);
                    s_DiffuseScent_p.End();
                    yield return scent;
                    s_DiffuseScent_p.Begin();
                }
            }
            //Leave ScentDirty alone if both are false or if ScentDirty is already true and sentScent is false
            adj.ScentDirty |= sentScent;
        }

        s_DiffuseScent_p.End();
    }

    #region Obsolete or obsoleting
    // Was once used by FunctionalCoroutine in UrbSmellSource
    // public void AddScent(UrbScentTag tag, float value)
    // {
    //     ScentDirty = true;
    //     for (int i = 0; i < TerrainTypes.Length; i++)
    //     {
    //         int TerrainType = (int)TerrainTypes[i];
    //
    //         for (int s = 0; s < SizeLimit; s++)
    //         {
    //             TerrainFilter[TerrainType][s][tag] = value;
    //         }
    //     }
    // }

    //Unused methods. Just read from Contents for now
    // public UrbAgent CurrentContent {
    //
    //     get {
    //         if(Contents == null || Contents.Count == 0)
    //         {
    //             return null;
    //         }
    //
    //         return Contents[Contents.Count-1];
    //     }
    //
    // }
    //
    // public UrbAgent[] CurrentContents {
    //     get {
    //         if(Contents == null || Contents.Count == 0)
    //         {
    //             return new UrbAgent[0];
    //         }
    //
    //         return Contents.ToArray();
    //     }
    // }
    
    #endregion
}
