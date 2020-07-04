using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbActionSquare : UrbUIPanel
{
    protected override void Initialize()
    {
        base.Initialize();

        InterfaceInputs[0].UserAction = new UrbSpawnMenu();
        InterfaceInputs[1].UserAction = new UrbPauseAction();
        InterfaceInputs[2].UserAction = new UrbResumeAction();
        InterfaceInputs[3].UserAction = new UrbInvestigatorMenu { Icon = InterfaceInputs[3].GetIcon()};
        InterfaceInputs[4].UserAction = new UrbSaveAction();
        InterfaceInputs[5].UserAction = new UrbLoadAction();
        InterfaceInputs[6].UserAction = new UrbGetAgentDetails();
        InterfaceInputs[7].UserAction = new UrbKillAgent();
        InterfaceInputs[8].UserAction = new UrbQuitAction();
    }
}
