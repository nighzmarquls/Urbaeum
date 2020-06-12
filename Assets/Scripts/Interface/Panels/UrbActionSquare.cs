using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbActionSquare : UrbUIPanel
{
    protected override void Initialize()
    {
        base.Initialize();

        InterfaceInputs[0].UserAction = new UrbCreateTool();

    }
}
