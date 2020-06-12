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

        UrbTile Result = LandMap.GetInboundsTile(Location);

        return Result;
    }

}
