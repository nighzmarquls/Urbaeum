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

    public virtual bool UseMapDisplay { get; set; } = true;
    public virtual string MapDisplayAssetPath { get; set; } = "Sprites/blank";
    public virtual Sprite MapDisplaySprite { get; set; } = null;
    public bool MapDisplayInitialized { get; protected set; }

    protected GameObject MapDisplay = null;
    
    protected virtual void InitializeMapDisplaySprite()
    {
        //This should only occur once maybe twice a frame tops if the User is being silly with clicking.
        //Probably not a performance risk but important that we don't clear the MapDisplay like we were.
        if (MapDisplay == null && (!string.IsNullOrEmpty(MapDisplayAssetPath) || MapDisplaySprite != null))
        {
            MapDisplay = new GameObject();
            var Renderer = MapDisplay.AddComponent<SpriteRenderer>();
            if (Renderer == null)
            {
                return;
            }
            
            Renderer.sortingOrder = 10;
            Sprite DisplaySprite = (MapDisplaySprite == null) ? Resources.Load<Sprite>(MapDisplayAssetPath) : MapDisplaySprite;
            Renderer.sprite = DisplaySprite;
        }

        if (MapDisplayInitialized)
        {
            return;
        }
        
        Assert.IsNotNull(MapDisplay);
        Assert.IsFalse(MapDisplayInitialized);
        MapDisplay.name = "DisplayPivot";
        MapDisplay.SetActive(true);
        MapDisplayInitialized = true;
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
        if (UseMapDisplay)
        {
            InitializeMapDisplaySprite();
        }

        UrbUIManager.SetCurrentAction(this);
    }

    public virtual void UnselectAction() {
        if (UseMapDisplay)
        {
            UninitializeMapDisplaySprites();
        }
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
