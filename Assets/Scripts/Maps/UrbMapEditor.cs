using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(UrbMap))]
public class UrbMapEditor : Editor
{
    private void OnSceneGUI()
    {
        //if (Event.current.type == EventType.Repaint)

        Vector3 Position = ((UrbMap)target).transform.position;

        {
            Handles.color = ((UrbMap)target).Color;

            float width = ((UrbMap)target).TileSize * ((UrbMap)target).Xsize;
            float height = ((UrbMap)target).TileSize * ((UrbMap)target).Ysize;

           

            Rect MapRect = new Rect(Position, new Vector2(width,height));
            Handles.DrawSolidRectangleWithOutline(MapRect, Color.clear, ((UrbMap)target).Color);
        }

        if(((UrbMap)target).DebugDisplay)
        {
            UrbMap DisplayMap = (UrbMap)target;
            
            for (int x = 0; x < DisplayMap.Xsize; x++)
            {
                for(int y = 0; y < DisplayMap.Ysize; y++)
                {
                    UrbTile Tile = DisplayMap.GetTile(x, y);
                    if(Tile == null)
                    {
                        continue;
                    }

                    UrbTile[] LinkedTiles = Tile.GetLinked();

                    for(int l = 0; l < LinkedTiles.Length; l++)
                    {
                        if (LinkedTiles[l] == null)
                        {
                            continue;
                        }
                        Handles.color = Color.yellow;
                        Handles.DrawLine( Tile.Location, LinkedTiles[l].Location);
                    }
                }
            }
        }
    }
}
#endif