using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbUserAction
{
    public virtual Sprite Icon { get; set; } = null;
    public virtual string Name { get; set; }  = "";
    public virtual string Description { get; set; } = "";

    public virtual void SelectAction()
    {
        Debug.Log("Selected Action " + Name);
        UrbUIManager.SetCurrentAction(this);
    }

    public virtual void UnselectAction() { }

    public virtual void MouseClick(UrbTile currentCursorTile) { }

    public virtual void MouseDown(UrbTile input) { }

    public virtual void MouseUp(UrbTile input) { }

    public virtual void MouseOver(UrbTile input) { }

    public virtual void MouseEnter(UrbTile input) { }

    public virtual void MouseExit(UrbTile input) { }
}
