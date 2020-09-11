using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbKillAgent : UrbUserAction
{
    public override string Name => "Kill";
    public override bool UseMapDisplay => false;
    public override void MouseClick(UrbTile currentCursorTile)
    {

        Ray mouseray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 Location = mouseray.origin;

        Collider2D Result = Physics2D.OverlapCircle(Location, 0.1f);
        if (Result != null)
        {
            UrbAgent SelectedAgent = Result.GetComponentInParent<UrbAgent>();
            if (SelectedAgent != null)
            {
                SelectedAgent.Remove();
            }
        }

        base.MouseClick(currentCursorTile);
    }
}