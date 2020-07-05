using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbScentInvestigator : UrbInvestigatorTool
{
 
    public UrbScentTag DisplayScentTag = UrbScentTag.Plant;
    public override float Sensitivity => 0.01f;

    public override Color GetColorFromTile(UrbTile Tile)
    {
        Color TileColor = DetectionColor;
        TileColor.a = 0;
        if (Tile.TerrainFilter[0][2][UrbScentTag.Plant] > 0)
        {
            TileColor.a = (Tile.TerrainFilter[0][2][DisplayScentTag] * Sensitivity);
        }
        else if (Tile.TerrainFilter[0][1][UrbScentTag.Plant] > 0)
        {
            TileColor.a = (Tile.TerrainFilter[0][1][DisplayScentTag] * Sensitivity);
        }
        else if (Tile.TerrainFilter[0][0][UrbScentTag.Plant] > 0)
        {
            TileColor.a = (Tile.TerrainFilter[0][0][DisplayScentTag] * Sensitivity);
        }

        return TileColor;
    }
}
