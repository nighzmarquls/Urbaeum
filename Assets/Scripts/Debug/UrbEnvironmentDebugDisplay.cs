using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UrbMap))]
public class UrbEnvironmentDebugDisplay : MonoBehaviour
{
    public Sprite DebugTileSprite;

    public Image DisplayOption;

    public UrbAgent[] AgentPallete = null;

    UrbAgent currentAgent = null;

    GameObject[][] debugTiles;
    public List<IEnumerator> scentRoutines;
    public List<IEnumerator> environmentRoutine;

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
        scentRoutines = new List<IEnumerator>();
        environmentRoutine = new List<IEnumerator>();

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
                UrbTile tile = targetMap.GetTile(i, ii);
                Debug.Log("Initializing Scent coroutine in EnvDebugDisplay");
                IEnumerator routine = tile.ScentCoroutine();
                StartCoroutine(routine);
                scentRoutines.Add(routine);

                routine = tile.Environment.EnvironmentCoroutine();
                StartCoroutine(routine);
                environmentRoutine.Add(routine);
            }
        }
        targetMap.RefreshAllPathableSize();
        
        if (Debug.isDebugBuild || Debug.developerConsoleVisible)
        {
            Debug.LogWarning("EnvironmentDebug Display, Destroying exemplar");
        }
        Destroy(Exemplar);
    }

    static ProfilerMarker s_UrbEnvironmentDebugDisplay_p = new ProfilerMarker("UrbEnvDbgDisp.SpawnAgent");
    public void SpawnAgent(UrbAgent input, int xPoint, int yPoint)
    {
        s_UrbEnvironmentDebugDisplay_p.Begin(this);
        UrbTile Tile = targetMap.GetTile(xPoint, yPoint);

        GameObject spawned;
        if (!UrbAgentSpawner.SpawnAgent(input, Tile, out spawned))
        {
            UrbAgent[] Occupants = Tile.Occupants.ToArray();
            foreach(UrbAgent Occupant in Occupants)
            {
                UrbTile[] AdjacentTiles = Occupant.Tileprint.GetBorderingTiles(Occupant);
                foreach(UrbTile AdjacentTile in AdjacentTiles)
                {
                    AdjacentTile.Environment[UrbEnvironmentCondition.Heat] += 1;
                }
                Occupant.Remove();
            }
        }

        s_UrbEnvironmentDebugDisplay_p.End();
    }

    public void HandleButton(int slot)
    {
        if (DisplayOption == null)
            return;
        if (slot > -1 && AgentPallete != null && AgentPallete.Length > slot)
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
            setBlock = tile.Blocked ? false : true;
            if (currentAgent == null)
            {
                HandleBlocked(tile,setBlock);
            }
            else
            {
                SpawnAgent(currentAgent, xPoint, yPoint);
            }
            clickDown = true;
        }
        else if (currentAgent == null)
        {
            if (tile.CurrentContent != null)
            {
                tile.CurrentContent.Remove();
            }

            HandleBlocked(tile, setBlock);
        }
    }

    void HandleBlocked(UrbTile tile,bool setBlock)
    {
        if (setBlock)
        {
            tile.Environment.SetTransfer(UrbEnvironmentCondition.Heat, 0.1f);
        }
        else
        {
            tile.Environment.SetTransfer(UrbEnvironmentCondition.Heat, 1.0f);
        }

        tile.Blocked = setBlock;
    }

    void HandleLink()
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

    void SetPause(bool input)
    {
        paused = input;
        if (input)
        {
            foreach (IEnumerator routine in scentRoutines)
            {
                StopCoroutine(routine);
            }
            foreach(IEnumerator routine in environmentRoutine)
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
            foreach (IEnumerator routine in environmentRoutine)
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

        Ray mouseray = Camera.main.ScreenPointToRay(Input.mousePosition);
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

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (targetMap.LocationToTileAddress(Location, out xTarget, out yTarget))
            {
                tile = targetMap.GetTile(xTarget, yTarget);
                tile.Environment[UrbEnvironmentCondition.Heat] += 1;
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (targetMap.LocationToTileAddress(Location, out xTarget, out yTarget))
            {
                tile = targetMap.GetTile(xTarget, yTarget);
                tile.Environment[UrbEnvironmentCondition.Heat] -= 1;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            clickDown = false;
        }

        if (Input.GetKeyDown(KeyCode.P))
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
                else if (linkA == tile)
                {
                    linkA = null;
                }
                else if (linkB == null)
                {
                    linkB = tile;
                    HandleLink();
                }
            }
        }

        if (true)
        {
            for (int i = 0; i < targetMap.Xsize; i++)
            {
                for (int ii = 0; ii < targetMap.Ysize; ii++)
                {
                    SpriteRenderer spriteRender = debugTiles[i][ii].GetComponent<SpriteRenderer>();
                    if (spriteRender)
                    {
                        tile = targetMap.GetTile(i, ii);

                        if (tile == linkA)
                        {
                            spriteRender.color = Color.yellow;
                        }
                        else if (tile.Blocked)
                        {
                            spriteRender.color = Color.black;
                        }
                        else
                        {

                            Color tileColor = Color.black;

                            if (tile.Environment[UrbEnvironmentCondition.Heat] > 0)
                            {
                                tileColor = (Color.cyan * (tile.Environment[UrbEnvironmentCondition.Heat]));
                            }
                            else if(tile.Environment[UrbEnvironmentCondition.Heat] < 0)
                            {
                                tileColor = (Color.yellow * (tile.Environment[UrbEnvironmentCondition.Heat]*-1.0f));
                            }

                            if (tile.GetLinked().Length > 0)
                            {
                                tileColor += Color.red * 0.25f;
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

