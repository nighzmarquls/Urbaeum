using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbInvestigatorTool : UrbUserAction
{
    public override string Name => "Investigate Tool";
    public override string MapDisplayAssetPath => "Sprites/UI/Examine";
    public UrbScentTag DisplayScentTag = UrbScentTag.Plant;
    public float Sensitivity = 0.5f;
    protected bool Uninitialized = true;

    protected const string InvestigationDisplayPath = "Sprites/blank";

    protected SpriteRenderer[] InvestigationDisplay;

    protected override void InitializeMapDisplaySprite()
    {
        base.InitializeMapDisplaySprite();

        if(Uninitialized)
        {
            Uninitialized = false;

            List<SpriteRenderer> WorkingList = new List<SpriteRenderer>();
            if (UrbUIManager.Instance.OverlayPrint != null)
            {
                Sprite DisplaySprite = Resources.Load<Sprite>(InvestigationDisplayPath);
                for (int i = 0; i < UrbUIManager.Instance.OverlayPrint.TileCount; i++)
                {
                    GameObject Display = new GameObject();
                    Display.name = "InvestigationDisplay";
                    SpriteRenderer Renderer = Display.AddComponent<SpriteRenderer>();
                    Renderer.sprite = DisplaySprite;
                    Renderer.sortingOrder = -1;
                    Renderer.enabled = false;
                    Display.transform.SetParent(MapDisplay.transform);
                    WorkingList.Add(Renderer);
                }
            }
            InvestigationDisplay = WorkingList.ToArray();
        }

    }

    public override void SelectAction()
    {
        base.SelectAction();
    }

    public virtual void HideDisplay()
    {
        for(int i = 0; i < InvestigationDisplay.Length; i++)
        {
            InvestigationDisplay[i].enabled = false;
        }
    }

    public virtual void AssignDisplayFromTile(int DisplayIndex, UrbTile Tile)
    {
        if(Tile == null || DisplayIndex >= InvestigationDisplay.Length)
        {
            return;
        }

        InvestigationDisplay[DisplayIndex].transform.position = Tile.RawLocation;
        InvestigationDisplay[DisplayIndex].enabled = true;

        InvestigationDisplay[DisplayIndex].color = GetColorFromTile(Tile);
    }

    public virtual Color GetColorFromTile(UrbTile Tile)
    {
        Color TileColor = Color.black;

        if (Tile.TerrainFilter[0][2][UrbScentTag.Plant] > 0)
        {
            TileColor = (Color.magenta * (Tile.TerrainFilter[0][2][DisplayScentTag] * Sensitivity));
        }
        else if (Tile.TerrainFilter[0][1][UrbScentTag.Plant] > 0)
        {
            TileColor = (Color.blue * (Tile.TerrainFilter[0][1][DisplayScentTag] * Sensitivity));
        }
        else if (Tile.TerrainFilter[0][0][UrbScentTag.Plant] > 0)
        {
            TileColor = (Color.cyan * (Tile.TerrainFilter[0][0][DisplayScentTag] * Sensitivity));
        }

        TileColor.a = 0;

        return Color.white - TileColor;
    }

    public override void MouseOver(UrbTile input)
    {
        if (UrbUIManager.Instance.OverlayPrint != null)
        {
            HideDisplay();
            UrbTile[] OverlayTiles = UrbUIManager.Instance.OverlayPrint.GetAllPrintTiles(input);

            for(int i = 0; i < OverlayTiles.Length; i++)
            {
                AssignDisplayFromTile(i, OverlayTiles[i]);
            }
        }

        base.MouseOver(input);
    }

    public override void MouseEnter(UrbTile input)
    {

        base.MouseEnter(input);
    }

    public override void MouseExit(UrbTile input)
    {
        base.MouseExit(input);
    }

    public override void UnselectAction()
    {
        base.UnselectAction();
    }
}
