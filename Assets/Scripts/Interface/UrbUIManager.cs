using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEngine;

[RequireComponent(typeof(UrbAtlas))]
public class UrbUIManager : MonoBehaviour
{
    public static UrbUIManager Instance { get; protected set; }

    [TextArea(0, 5)] public string OverlayPrintString;
    public UrbTileprint OverlayPrint { get; protected set; }

    public UrbTile CurrentCursorTile { get; protected set; } = null;
    public UrbUserAction CurrentAction { get; protected set; } = null;
    public UrbToolBar Toolbar;
    public UrbActionSquare ActionSquare;

    public UrbWindowManager WindowManager;

    public UrbAgentDetailWindow AgentDisplayPrefab;

    public UrbAtlas Atlas;

    Vector3 LastMousePosition = Vector3.zero;
    static Camera mainCam;
    static bool isDisabled = true;
    static bool MapActionInvalid
    {
        get { return (isDisabled) || Instance.CurrentAction == null || Instance.CurrentCursorTile == null; }//|| Instance.IsPaused; }
    }

    public float TimeMultiplier {
        set {
            Time.timeScale = value;
        }
    }
    
    public static void SetCurrentAction(UrbUserAction Action)
    {
        if (Instance == null)
        {
            return;
        }

        if (Instance.CurrentAction != null)
        {
            Instance.CurrentAction.UnselectAction();
        }

        Instance.CurrentAction = Action;
    }

    #region MouseEvents
    public static bool MouseOver { get; protected set; } = false;

    public static void OnMapMouseClick()
    {
        if (MapActionInvalid)
        {
            return;
        }

        Instance.CurrentAction.MouseClick(Instance.CurrentCursorTile);
    }

    public static void OnMapMouseDown()
    {
        if (MapActionInvalid)
        {
            return;
        }

        Instance.CurrentAction.MouseDown(Instance.CurrentCursorTile);
    }

    public static void OnMapMouseUp()
    {
        if (MapActionInvalid)
        {
            return;
        }

        Instance.CurrentAction.MouseUp(Instance.CurrentCursorTile);
    }
    
    public static void OnMapMouseOver()
    {
        if(MapActionInvalid)
        {
            return;
        }
        
        Instance.CurrentAction.MouseOver(Instance.CurrentCursorTile);
    }
    
    public static void OnMapMouseEnter()
    {
        MouseOver = true;
        // Debug.Log("OnMapMouse Enter");
        if (MapActionInvalid)
        {
            return;
        }
        Instance.CurrentAction.MouseEnter(Instance.CurrentCursorTile);
    }
    public static void OnMapMouseExit()
    {
        // Debug.Log("OnMapMouse Exit");

        MouseOver = false;
        if (MapActionInvalid)
        {
            return;
        }
        Instance.CurrentAction.MouseExit(Instance.CurrentCursorTile);
    }
#endregion

#region LifetimeEvents
    
    public void OnEnable()
    {
        mainCam = Camera.main;
        Assert.IsNotNull(mainCam);
        
        if (Instance == null)
        {
            Instance = this;
            
            Atlas = GetComponent<UrbAtlas>();

            Assert.IsNotNull(Atlas);
            
            if (!string.IsNullOrEmpty(OverlayPrintString))
            {
                OverlayPrint = new UrbTileprint(OverlayPrintString);
            }

            isDisabled = false;
        }
        else
        {
            Debug.LogWarning("UrbUI Manager OnEnable self-destruct");
            Destroy(this);
        }
    }

    public void Update()
    {
        if (MouseOver)
        {
            GetMouseTile();
            OnMapMouseOver();
        }
    }
    
    public void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
            isDisabled = true;
        }
    }
    
#endregion
    
    protected void GetMouseTile()
    {
        float Distance = (LastMousePosition - Input.mousePosition).magnitude;

        if (Distance < 0.5f)
        {
            return;
        }
        
        LastMousePosition = Input.mousePosition;

        Ray mouseray = mainCam.ScreenPointToRay(LastMousePosition);
        Vector3 Location = mouseray.origin + (mouseray.direction * (Vector3.Distance(mouseray.origin, transform.position)));
        CurrentCursorTile = Atlas.GetTile(Location);
    }
    
    public bool IsPaused { get; protected set; } = false;
    public void SetPause(bool Pause)
    {
        if(Pause == IsPaused)
        {
            return;
        }

        IsPaused = Pause;
        if (Pause)
        {
            Atlas.PauseBehaviours();
        }
        else
        {
            Atlas.ResumeBehaviours();
        }

        UrbAgentManager.SetPauseOnAllAgents(Pause);
    }
}

public class UrbPauseAction : UrbUserAction
{
    public override string Name => "Pause";
    public override string MapDisplayAssetPath => "";

    public override void SelectAction()
    {
        UrbUIManager.Instance.SetPause(true);
        
        base.SelectAction();
    }
}

public class UrbResumeAction : UrbUserAction
{
    public override string Name => "Resume";
    public override string MapDisplayAssetPath => "";

    public override void SelectAction()
    {
        UrbUIManager.Instance.SetPause(false);

        base.SelectAction();
    }
}

public class UrbQuitAction : UrbUserAction
{
    public override string Name => "Quit";
    public override string MapDisplayAssetPath => "";

    public override void SelectAction()
    {
        Application.Quit();
        base.SelectAction();
    }
}