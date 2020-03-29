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
        {
            Handles.color = ((UrbMap)target).Color;

            float width = ((UrbMap)target).TileSize * ((UrbMap)target).Xsize;
            float height = ((UrbMap)target).TileSize * ((UrbMap)target).Ysize;

            Vector3 Position = ((UrbMap)target).transform.position;

          

            Position.x -= width / 2;
            Position.y -= height / 2;

            Rect MapRect = new Rect(Position, new Vector2(width,height));
            Handles.DrawSolidRectangleWithOutline(MapRect, Color.clear, ((UrbMap)target).Color);
        }
    }
}
#endif