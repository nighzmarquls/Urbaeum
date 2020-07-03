using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UrbMapMouseInterface : MonoBehaviour
    , IPointerClickHandler
    , IPointerDownHandler
    , IPointerUpHandler
    , IPointerEnterHandler
    , IPointerExitHandler
{
    protected bool PointerOver = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        UrbUIManager.OnMapMouseClick();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        UrbUIManager.OnMapMouseUp();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UrbUIManager.OnMapMouseDown();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UrbUIManager.OnMapMouseEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UrbUIManager.OnMapMouseExit();
    }
}
