using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;

public class UrbDisplayWindow : MonoBehaviour
    , IDragHandler
    , IBeginDragHandler
    , IEndDragHandler
    , IPointerEnterHandler
    , IPointerExitHandler
{
    public UrbInterfaceInput CloseInput;
    protected Vector2 offset;
    protected RectTransform WindowRect;

    protected float WidthOffset;

    protected float HeightOffset;

    protected static UrbDisplayWindow CurrentFocusedWindow = null;

    public bool InFocus { get; protected set; } = false;

    public bool MouseOver { get; protected set; } = false;

    public void Awake()
    {
        WindowRect = GetComponent<RectTransform>();

        WindowRect.pivot = new Vector2(0.5f, 0.5f);

        WidthOffset = WindowRect.rect.width * 0.5f;
        HeightOffset = WindowRect.rect.height * 0.5f;

        if (CloseInput != null)
        {
            CloseInput.UserAction = new UrbDisplayWindowClose { OwningWindow = this, };
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
        UpdateOffset(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
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
        CloseInput.Disabled = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MouseOver = false;
        if(InFocus)
        {
            return;
        }

        CloseInput.Disabled = true;
    }
}

public class UrbDisplayWindowClose : UrbUserAction
{
    public override string Name => "Close Window";
    public override string MapDisplayAssetPath => "";

    public UrbDisplayWindow OwningWindow;

    public override void SelectAction()
    {
        if (OwningWindow == null)
            return;

        if (OwningWindow.InFocus || OwningWindow.MouseOver)
        {
            base.SelectAction();
            Object.Destroy(OwningWindow.gameObject);
        }
    }
}