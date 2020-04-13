using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UrbMap))]
public class UrbPerformanceDebugDisplay : MonoBehaviour
{
    public Image DisplayOption;

    public UrbAgent[] AgentPallete = null;

    GameObject currentAgent = null;

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
        scentRoutines = new List<IEnumerator>();
        environmentRoutine = new List<IEnumerator>();

        for (int i = 0; i < targetMap.Xsize; i++)
        {
            for (int ii = 0; ii < targetMap.Ysize; ii++)
            {
               
                UrbTile tile = targetMap.GetTile(i, ii);
                IEnumerator routine = tile.ScentCoroutine();
                StartCoroutine(routine);
                scentRoutines.Add(routine);

                routine = tile.Environment.EnvironmentCoroutine();
                StartCoroutine(routine);
                environmentRoutine.Add(routine);
            }
        }
        targetMap.RefreshAllPathableSize();
    }

    public void SpawnAgent(GameObject input, int xPoint, int yPoint)
    {
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
    }

    bool ButtonPress = false;
    public void HandleButton(int slot)
    {
        ButtonPress = true;
        if (DisplayOption == null)
            return;
        if (slot > -1 && AgentPallete != null && AgentPallete.Length > slot)
        {
            DisplayOption.color = Color.white;
            DisplayOption.sprite = AgentPallete[slot].CurrentSprite;
            currentAgent = AgentPallete[slot].gameObject;
        }
        else
        {
            DisplayOption.color = Color.black;
            currentAgent = null;
        }
    }

    void HandleClick(int xPoint, int yPoint)
    {
        if (ButtonPress)
            return;

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

    public void TogglePause()
    {
        SetPause(!paused);
    }
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
            ButtonPress = false;
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

    }
}

