using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbActionSquare : UrbUIPanel
{
    protected override void Awake()
    {
        base.Awake();

        InterfaceInputs[0].UserAction = new UrbSpawnMenu();
        InterfaceInputs[1].UserAction = new UrbInvestigatorMenu { Icon = InterfaceInputs[3].GetIcon()};
        InterfaceInputs[2].UserAction = new UrbGetAgentDetails();

        InterfaceInputs[3].UserAction = new UrbKillAgent();

        InterfaceInputs[6].UserAction = new UrbSaveAction();
        InterfaceInputs[7].UserAction = new UrbLoadAction();
        InterfaceInputs[8].UserAction = new UrbQuitAction();
    }
}
