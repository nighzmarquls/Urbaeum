using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEngine;

public class UrbUserAction
{
    public virtual Sprite Icon { get; set; } = null;
    public virtual Color IconColor { get; set; } = Color.white;
    public virtual string Name { get; set; } = "";
    public virtual string Description { get; set; } = "";
    public virtual string MapDisplayAssetPath { get; set; } = "Sprites/blank";
    public virtual Sprite MapDisplaySprite { get; set; } = null;
    public bool MapDisplayInitialized { get; protected set; }

    protected GameObject MapDisplay = null;

    protected virtual void InitializeMapDisplaySprite()
    {
        if (MapDisplay == null && (!string.IsNullOrEmpty(MapDisplayAssetPath) || MapDisplaySprite != null))
        {
            MapDisplay = new GameObject();
            MapDisplay.name = "DisplayPivot";
            SpriteRenderer Renderer = MapDisplay.AddComponent<SpriteRenderer>();
            Renderer.sortingOrder = 10;
            Sprite DisplaySprite = (MapDisplaySprite == null) ? Resources.Load<Sprite>(MapDisplayAssetPath) : MapDisplaySprite;
            Renderer.sprite = DisplaySprite;
        }

        if (MapDisplay != null)
        {
            Assert.IsFalse(MapDisplayInitialized);

            MapDisplay.SetActive(true);
            MapDisplayInitialized = true;
        }
    }

    protected virtual void UninitializeMapDisplaySprites()
    {
        if (MapDisplayInitialized)
        {
            Assert.IsTrue(MapDisplayInitialized);

            MapDisplay.SetActive(false);
            MapDisplayInitialized = false;
        }
    }

    public virtual void SelectAction()
    {
        InitializeMapDisplaySprite();

        Debug.Log("Player-Selected Action " + Name);
        UrbUIManager.SetCurrentAction(this);
    }

    public virtual void UnselectAction() {
        UninitializeMapDisplaySprites();
    }
    
    public virtual void MouseClick(UrbTile currentCursorTile) { }

    public virtual void MouseDown(UrbTile input) { }

    public virtual void MouseUp(UrbTile input) { }

    public virtual void MouseOver(UrbTile input)
    {
        if(input == null)
        {
            return;
        }

        if (MapDisplayInitialized)
        {
            MapDisplay.transform.position = input.Location;
            MapDisplay.transform.localScale = new Vector3(input.OwningMap.TileSize, input.OwningMap.TileSize, input.OwningMap.TileSize);
        }
    }

    public virtual void MouseEnter(UrbTile input)
    {
        if (MapDisplay != null)
        {
            MapDisplay.SetActive(true);
        }
    }

    public virtual void MouseExit(UrbTile input)
    {
        if (MapDisplay != null)
        {
            MapDisplay.SetActive(false);
        }
    }
}
