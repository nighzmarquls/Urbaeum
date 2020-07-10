using UnityEngine;

public class UrbScentInvestigator : UrbInvestigatorTool
{
 
    public UrbScentTag DisplayScentTag = UrbScentTag.Plant;
    public override float Sensitivity => 0.02f;

    public override Color GetColorFromTile(UrbTile Tile)
    {
        Color TileColor = DetectionColor;
        TileColor.a = 0;

        var val = Tile.TerrainFilter[0][2].tagList[(int) DisplayScentTag].value;
        if (val > 0)
        {
            TileColor.a = (val * Sensitivity);
            return TileColor;
        }

        val = Tile.TerrainFilter[0][1].tagList[(int) DisplayScentTag].value;
        if (val > 0)
        {
            TileColor.a = (val * Sensitivity);
            return TileColor;
        }

        val = Tile.TerrainFilter[0][0].tagList[(int) DisplayScentTag].value;
        //If val is 0, it will multiply out to 0 anyway.
        TileColor.a = (val * Sensitivity);

        return TileColor;
    }
}
