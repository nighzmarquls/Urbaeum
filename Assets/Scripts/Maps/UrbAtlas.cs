using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbAtlas : MonoBehaviour
{
    List<UrbMap> Maps;

    public int AtlasX = 64;
    public int AtlasY = 64;

    public int SkySizeFactor = 2;
    

    private UrbMap LandMap;
    private UrbMap SkyMap;

    // Start is called before the first frame update
    public float TimeMultiplier {
        set {
            for(int i = 0; i < Maps.Count;i++)
            {
                Maps[i].TimeMultiplier = value;
            }
        }
    }
    void Start()
    {
        if(AtlasX <= 0 && AtlasY <= 0)
        {
            return;
        }

        Maps = new List<UrbMap>();

        GameObject LandObject = new GameObject("LandMap");
        LandObject.transform.position = Vector3.forward * (AtlasY + AtlasX);

        LandMap = LandObject.AddComponent<UrbMap>();
        LandMap.SetNewMap(AtlasX, AtlasY, 1, UrbPathTerrain.Land);

        GameObject SkyObject = new GameObject("SkyMap");
        
        SkyMap = SkyObject.AddComponent<UrbMap>();
        SkyMap.SetNewMap(AtlasX / SkySizeFactor, AtlasY / SkySizeFactor, SkySizeFactor, UrbPathTerrain.Air);

        LinkSkyToLand();

        Maps.Add(LandMap);
        UrbSystemIO.RegisterMap(LandMap);
        Maps.Add(SkyMap);
        UrbSystemIO.RegisterMap(SkyMap);

        RefreshAllMaps();
        StartBehaviours();
    }

    void RefreshAllMaps()
    {
        for (int i = 0; i < Maps.Count; i++)
        {
            Maps[i].RefreshAllPathableSize();
        }
    }
    void LinkSkyToLand()
    {
        for(int x = 0; x < AtlasX; x++)
        {
            for(int y = 0; y < AtlasY; y++)
            {
                int skyX = x / SkySizeFactor;
                int skyY = y / SkySizeFactor;

                LandMap.GetTile(x, y).AddLink(SkyMap.GetTile(skyX, skyY));
            }
        }
    }

    public UrbTile GetTile(Vector3 Location)
    {
        if (LandMap == null)
        {
            return null;
        }
        UrbTile Result = null;
        bool EmptySkyTile = true;
        if (SkyMap != null)
        {
            Result = SkyMap.GetInboundsTile(Location);


            if(Result != null && Result.Occupants != null && Result.Occupants.Count > 0)
            {
                EmptySkyTile = false;
            }
        }

        if (EmptySkyTile)
        {
            Result = LandMap.GetInboundsTile(Location);
        }

        return Result;
    }

    bool IsPaused;
    public void StartBehaviours()
    {
        IsPaused = false;

        for (int i = 0; i < Maps.Count; i++)
        {
            for (int c = 0; c < Maps[i].MapCoroutines.Length; c++)
            {
                StartCoroutine(Maps[i].MapCoroutines[c]);
            }
        }
    }

    public void PauseBehaviours()
    {
        if(IsPaused)
        {
            return;
        }
        IsPaused = true;

        for (int i = 0; i < Maps.Count; i++)
        {
            for(int c = 0; c < Maps[i].MapCoroutines.Length; c++)
            {
                StopCoroutine(Maps[i].MapCoroutines[c]);
            }
        }
        
    }

    public void ResumeBehaviours()
    {
        if (!IsPaused)
        {
            return;
        }

        IsPaused = false;

        for (int i = 0; i < Maps.Count; i++)
        {
            for (int c = 0; c < Maps[i].MapCoroutines.Length; c++)
            {
                StartCoroutine(Maps[i].MapCoroutines[c]);
            }
        }
        
    }
}
