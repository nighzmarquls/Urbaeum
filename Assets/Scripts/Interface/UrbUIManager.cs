using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbUIManager : MonoBehaviour
{
    public static UrbUIManager Instance { get; protected set; }
    public UrbTile CurrentCursorTile { get; protected set; } = null;
    public UrbUserAction CurrentAction { get; protected set; } = null;
    public UrbToolBar Toolbar;
    public UrbActionSquare ActionSquare;

    public UrbAtlas Atlas;

    static bool MapActionInvalid {  get { return Instance == null || Instance.Atlas == null || Instance.CurrentAction == null || Instance.CurrentCursorTile == null; } }

    public static void SetCurrentAction(UrbUserAction Action)
    {
        if(Instance == null)
        {
            return;
        }

        if(Instance.CurrentAction != null)
        {
            Instance.CurrentAction.UnselectAction();
        }

        Instance.CurrentAction = Action;
    }

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

    public static bool MouseOver { get; protected set; } = false;

    public static void OnMapMouseEnter()
    {
        MouseOver = true;
        if (MapActionInvalid)
        {
            return;
        }
        Instance.CurrentAction.MouseEnter(Instance.CurrentCursorTile);
    }

    public static void OnMapMouseExit()
    {
        MouseOver = false;
        if (MapActionInvalid)
        {
            return;
        }
        Instance.CurrentAction.MouseExit(Instance.CurrentCursorTile);
    }


    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Atlas = GetComponent<UrbAtlas>();
        }
        else
        {
            Destroy(this);
        }
    }

    private Vector3 LastMousePosition = Vector3.zero;
    protected void GetMouseTile()
    {
        float Distance = (LastMousePosition - Input.mousePosition).magnitude;

        if (Distance < 0.5f)
        {
            return;
        }
        
        LastMousePosition = Input.mousePosition;

        Ray mouseray = Camera.main.ScreenPointToRay(LastMousePosition);
        Vector3 Location = mouseray.origin + (mouseray.direction * (Vector3.Distance(mouseray.origin, transform.position)));
        CurrentCursorTile = Atlas.GetTile(Location);
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
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
}
