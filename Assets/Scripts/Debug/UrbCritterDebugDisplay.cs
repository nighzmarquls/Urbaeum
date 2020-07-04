using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UrbMap))]
public class UrbCritterDebugDisplay : MonoBehaviour
{
    public Sprite DebugTileSprite;

    public Image DisplayOption;

    public UrbAgent[] AgentPallete = null;

    UrbAgent currentAgent = null;

    GameObject[][] debugTiles;
    public List<IEnumerator> scentRoutines;

    UrbMap targetMap;
    bool needsInit = true;
    // Start is called before the first frame update
    void Start()
    {
        Camera = Camera.main;
        targetMap = GetComponent<UrbMap>();

    }

    void Initialize()
    {
        needsInit = false;
        debugTiles = new GameObject[targetMap.Xsize][];
        scentRoutines = new List<IEnumerator>();
        GameObject Exemplar = new GameObject("DebugSprite");
        SpriteRenderer spriteRender = Exemplar.AddComponent<SpriteRenderer>();
        spriteRender.sprite = DebugTileSprite;
        spriteRender.sortingOrder = -1;
        Exemplar.transform.localScale = new Vector3(targetMap.TileSize, targetMap.TileSize, targetMap.TileSize);
        for (int i = 0; i < targetMap.Xsize; i++)
        {
            debugTiles[i] = new GameObject[targetMap.Ysize];

            for (int ii = 0; ii < targetMap.Ysize; ii++)
            {
                debugTiles[i][ii] = Instantiate<GameObject>(Exemplar, targetMap.TileAddressToLocation(i, ii), Quaternion.identity);
                UrbTile tile = targetMap.GetTile(i, ii);
                IEnumerator routine = tile.ScentCoroutine();
                StartCoroutine(routine);
                scentRoutines.Add(routine);
            }
        }
        targetMap.RefreshAllPathableSize();

        if (Debug.isDebugBuild || Debug.developerConsoleVisible)
        {
            Debug.LogWarning("Destroying exemplar");
        }

        Destroy(Exemplar);
    }

    public void SpawnAgent(UrbAgent input, int xPoint, int yPoint)
    {
        UrbTile Tile = targetMap.GetTile(xPoint, yPoint);

        GameObject spawned;
        if (UrbAgentSpawner.SpawnAgent(input, Tile, out spawned))
        {
           
        }
        else
        {
            Debug.Log("Destroying tileCurrent after agent spawn returned false");

            Destroy(Tile.CurrentContent.gameObject);
        }
    }

    public void HandleButton(int slot)
    {
        if (DisplayOption == null)
            return;
        if(slot > -1 && AgentPallete != null && AgentPallete.Length > slot)
        {
            DisplayOption.color = Color.white;
            DisplayOption.sprite = AgentPallete[slot].CurrentSprite;
            currentAgent = AgentPallete[slot];
        }
        else
        {
            DisplayOption.sprite = DebugTileSprite;
            DisplayOption.color = Color.black;
            currentAgent = null;
        }
    }

    void HandleClick(int xPoint, int yPoint)
    {
        UrbTile tile = targetMap.GetTile(xPoint, yPoint);
        if (!clickDown)
        {
            setBlock = !tile.Blocked;
            if (currentAgent == null)
            {
                tile.Blocked = setBlock;
            }
            else
            {
                SpawnAgent(currentAgent, xPoint, yPoint);
            }
            clickDown = true;
        }
        else if(currentAgent == null)
        {
            if(tile.CurrentContent != null)
            {
                Debug.Log("Post-Click Destroy");
                Destroy(tile.CurrentContent.gameObject);
            }

            tile.Blocked = setBlock;
        }
    }

    static void HandleLink()
    {
        if (linkA != null && linkB != null)
        {
            linkA.ToggleLink(linkB);
            linkA = null;
            linkB = null;
        }
    }

    int xTarget, yTarget = 0;
    // Update is called once per frame

    bool setBlock;
    bool clickDown;

    static UrbTile linkA = null;
    static UrbTile linkB = null;
    bool paused = false;
    Camera Camera;

    void SetPause(bool input)
    {
        if (paused == input)
            return;

        paused = input;
        if (input)
        {
            foreach (IEnumerator routine in scentRoutines)
            {
                StopCoroutine(routine);
            }
        }
        else
        {
            foreach (IEnumerator routine in scentRoutines)
            {
                StartCoroutine(routine);
            }
        }
    }
    void Update()
    {
        if (needsInit)
        {
            Initialize();
            return;
        }

        SetPause(paused || UrbSystemIO.Instance.Loading);
        
        Ray mouseray = Camera.ScreenPointToRay(Input.mousePosition);
        Vector3 Location = mouseray.origin + (mouseray.direction * (Vector3.Distance(mouseray.origin, transform.position)));
        int xPoint, yPoint;

        UrbTile tile;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetMouseButton(0))
        {
            if (targetMap.LocationToTileAddress(Location, out xPoint, out yPoint))
            {
                HandleClick(xPoint, yPoint);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            clickDown = false;
        }

        if(Input.GetKeyDown(KeyCode.P))
        {
            SetPause(!paused);
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (targetMap.LocationToTileAddress(Location, out xTarget, out yTarget))
            {
                tile = targetMap.GetTile(xTarget, yTarget);

                if (linkA == null)
                {
                    linkA = tile;
                }
                else if(linkA == tile)
                {
                    linkA = null;
                }
                else if(linkB == null)
                {
                    linkB = tile;
                    HandleLink();
                }
            }
        }
        
        for (int i = 0; i < targetMap.Xsize; i++)
        {
            for (int ii = 0; ii < targetMap.Ysize; ii++)
            {
                SpriteRenderer spriteRender = debugTiles[i][ii].GetComponent<SpriteRenderer>();
                if (!spriteRender)
                {
                    continue;
                }

                tile = targetMap.GetTile(i, ii);

                if (tile == linkA)
                {
                    spriteRender.color = Color.yellow;
                    continue;
                }

                if (tile.Blocked)
                {
                    spriteRender.color = Color.black;
                    continue;
                }

                Color tileColor = Color.black;

                if (tile.TerrainFilter[0][2][UrbScentTag.Plant] > 0)
                {
                    tileColor = (Color.magenta * (tile.TerrainFilter[0][2][UrbScentTag.Plant]));
                }
                else if (tile.TerrainFilter[0][1][UrbScentTag.Plant] > 0)
                {
                    tileColor = (Color.blue * (tile.TerrainFilter[0][1][UrbScentTag.Plant]));
                }
                else if (tile.TerrainFilter[0][0][UrbScentTag.Plant] > 0)
                {
                    tileColor = (Color.cyan * (tile.TerrainFilter[0][0][UrbScentTag.Plant]));
                }

                if (tile.GetLinked().Length > 0)
                {
                    tileColor += Color.red * 0.25f;
                }
                
                tileColor.a = 0.0f;

                spriteRender.color = Color.white - tileColor;
            }
        }
        // */

    }
}
