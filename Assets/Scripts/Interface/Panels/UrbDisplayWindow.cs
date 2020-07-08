using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;

public abstract class UrbDisplayWindow : MonoBehaviour
    , IDragHandler
    , IBeginDragHandler
    , IEndDragHandler
    , IPointerEnterHandler
    , IPointerExitHandler
{
    public UrbInterfaceInput CloseInput;
    public UrbInterfaceInput MinimizeInput;
    
    protected Vector2 offset;
    protected RectTransform WindowRect;
    
    protected float WidthOffset;
    protected float HeightOffset;
    
    protected static UrbDisplayWindow CurrentFocusedWindow = null;
    public bool CanClose { get; protected set; }
    public bool CanMinimize { get; protected set; }
    
    public bool AllowDragging = true;
    public bool InFocus { get; protected set; } = false;

    public bool MouseOver { get; protected set; } = false;

    public virtual void OnEnable()
    {
        Debug.Log("Enabling UrbDisplayWindow");
        
        if (AllowDragging)
        {
            WindowRect = GetComponent<RectTransform>();

            WindowRect.pivot = new Vector2(0.5f, 0.5f);

            var rect = WindowRect.rect;
            WidthOffset = rect.width * 0.5f;
            HeightOffset = rect.height * 0.5f;
        }

        CanClose = CloseInput != null;
        if (CanClose)
        {
            Debug.Log("Added CloseInput UserAction to Window");
            CloseInput.UserAction = new UrbDisplayWindowClose { OwningWindow = this };
        }

        CanMinimize = MinimizeInput != null;
        if (CanMinimize)
        {
            Debug.Log("Added MinimizeInput UserAction to Window");
            MinimizeInput.UserAction = new UrbDisplayWindowMinimize { OwningWindow = this };
        }
    }

    protected void TakeFocus()
    {
        if(CurrentFocusedWindow != this)
        {
            if(CurrentFocusedWindow != null)
            { 
                CurrentFocusedWindow.InFocus = false;
            }

            CurrentFocusedWindow = this;
            InFocus = true;
        }
    }

    protected void UpdateOffset(PointerEventData eventData)
    {
        offset = (Vector2)transform.position - eventData.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!AllowDragging)
        {
            return;
        }
        
        UpdateOffset(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!AllowDragging)
        {
            return;
        }
        
        Vector2 NewPosition = offset + eventData.position;

        if(NewPosition.x - WidthOffset < 0)
        {
            NewPosition.x -= NewPosition.x - WidthOffset;
        }
        else if(NewPosition.x + WidthOffset > Screen.width)
        {
            NewPosition.x += Screen.width - (NewPosition.x + WidthOffset); 
        }
        
        if(NewPosition.y - HeightOffset < 0)
        {
            NewPosition.y -= NewPosition.y - HeightOffset;
        }
        else if(NewPosition.y + HeightOffset > Screen.height)
        {
            NewPosition.y += Screen.height - (NewPosition.y + HeightOffset);
        }

        transform.position = NewPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
       
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MouseOver = true;
        if (CanClose)
        {
            CloseInput.Disabled = false;
        }

        if (CanMinimize)
        {
            MinimizeInput.Disabled = false;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MouseOver = false;
        if(InFocus)
        {
            return;
        }

        if (CanClose)
        {
            CloseInput.Disabled = true;
        }

        if (CanMinimize)
        {
            MinimizeInput.Disabled = true;
        }
    }

    public virtual void OnClose()
    {
        Debug.Log("UrbDisplayWindow OnClose");
        return;
    }
    public virtual void OnMinimize()
    {
        Debug.Log("UrbDisplayWindow OnMinimize");
        return;
    }
}

public class UrbDisplayWindowClose : UrbUserAction
{
    public override string Name => "Close Window";
    public override string MapDisplayAssetPath => "";

    public UrbDisplayWindow OwningWindow;

    public override void SelectAction()
    {
        if (OwningWindow == null) { return; }

        if (OwningWindow.InFocus || OwningWindow.MouseOver)
        {
            base.SelectAction();
            OwningWindow.OnClose();
            Object.Destroy(OwningWindow.gameObject);
        }
    }
}

public class UrbDisplayWindowMinimize : UrbUserAction
{
    public override string Name => "Minimize Window";
    public override string MapDisplayAssetPath => "";

    public UrbDisplayWindow OwningWindow;

    public override void SelectAction()
    {
        if (OwningWindow == null) { return; }

        if (OwningWindow.InFocus || OwningWindow.MouseOver)
        {
            base.SelectAction();
            Debug.Log("Minimize Window was pressed!");
        }
    }
}