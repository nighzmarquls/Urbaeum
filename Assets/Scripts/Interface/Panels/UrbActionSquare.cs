using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbActionSquare : UrbUIPanel
{
    protected override void Initialize()
    {
        base.Initialize();

        InterfaceInputs[0].UserAction = new UrbCreateTool();
        InterfaceInputs[1].UserAction = new UrbPauseAction();
        InterfaceInputs[2].UserAction = new UrbResumeAction();
        InterfaceInputs[3].UserAction = new UrbInvestigatorTool();
    }
}
