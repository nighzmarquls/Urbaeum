using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbMap))]
public class UrbMapDebugDisplay : MonoBehaviour
{
    public Sprite DebugTileSprite;

   GameObject[][] debugTiles;

    UrbMap targetMap;
    bool needsInit = true;
    // Start is called before the first frame update
    void Start()
    {
        targetMap = GetComponent<UrbMap>();

    }

    void Initialize()
    {
        needsInit = false;
        debugTiles = new GameObject[targetMap.Xsize][];

        GameObject Exemplar = new GameObject("DebugSprite");
        SpriteRenderer spriteRender = Exemplar.AddComponent<SpriteRenderer>();
        spriteRender.sprite = DebugTileSprite;
        Exemplar.transform.localScale = new Vector3(targetMap.TileSize, targetMap.TileSize, targetMap.TileSize);
        for (int i = 0; i < targetMap.Xsize; i++)
        {
            debugTiles[i] = new GameObject[targetMap.Ysize];

            for (int ii = 0; ii < targetMap.Ysize; ii++)
            {
                debugTiles[i][ii] = Instantiate<GameObject>(Exemplar, targetMap.TileAddressToLocation(i, ii), Quaternion.identity);
            }
        }
        targetMap.RefreshAllPathableSize();
        Destroy(Exemplar);
    }

    int xTarget, yTarget = 0;
    // Update is called once per frame

    bool setBlock;
    bool clickDown;
    void Update()
    {
        if(needsInit)
        {
            Initialize();
            return;
        }

        Ray mouseray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 Location = mouseray.origin + (mouseray.direction * (Vector3.Distance(mouseray.origin, transform.position)));
        int xPoint, yPoint;

        UrbTile tile;
        bool dirty = false;
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if(Input.GetMouseButton(0))
        {
            targetMap.LocationToTileAddress(Location, out xPoint, out yPoint);
            
            tile = targetMap.GetTile(xPoint, yPoint);
            if(!clickDown)
            {
                setBlock = tile.Blocked ? false : true;
                clickDown = true;
            }
            tile.Blocked = setBlock; 


            dirty = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            clickDown = false;
        }

        if (Input.GetMouseButtonDown(1))
        {
            targetMap.LocationToTileAddress(Location, out xTarget, out yTarget);

            dirty = true;
        }

        if (dirty)
        {
            tile = targetMap.GetTile(xTarget, yTarget);
            targetMap.RefreshAllScent();
            tile.AddScent(UrbScentTag.Goal, 1.0f);
            tile.PropogateScent();
            
            for (int i = 0; i < targetMap.Xsize; i++)
            {
                for (int ii = 0; ii < targetMap.Ysize; ii++)
                {
                    SpriteRenderer spriteRender = debugTiles[i][ii].GetComponent<SpriteRenderer>();
                    if (spriteRender)
                    {
                        tile = targetMap.GetTile(i, ii);

                        if (tile.Blocked)
                        {
                            spriteRender.color = Color.black;
                        }
                        else
                        {

                            Color tileColor = Color.black;

                            if(tile.CurrentContent)
                            {
                                tileColor = Color.red;
                            }
                            else if (tile.TerrainFilter[0][2][UrbScentTag.Goal] > 0)
                            {
                                tileColor = (Color.magenta * (tile.TerrainFilter[0][2][UrbScentTag.Goal]));
                            }
                            else if (tile.TerrainFilter[0][1][UrbScentTag.Goal] > 0)
                            {
                                tileColor = (Color.blue * (tile.TerrainFilter[0][1][UrbScentTag.Goal]));
                            }
                            else if (tile.TerrainFilter[0][0][UrbScentTag.Goal] > 0)
                            {
                                tileColor = (Color.cyan * (tile.TerrainFilter[0][0][UrbScentTag.Goal]));
                            }
                            
                            tileColor.a = 0.0f;

                            spriteRender.color = Color.white - tileColor;
                        }
                    }
                }
            }
            // */
        }
        
    }
}
