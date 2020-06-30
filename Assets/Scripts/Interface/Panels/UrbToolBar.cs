using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class UrbToolBar : UrbUIPanel
{
    UrbUserAction[] AssignedActions = null;
    int BarDisplayIndex = 0;

    public void ClearButtons()
    {
        BarDisplayIndex = 0;
        for (int i = 0; i < InterfaceInputs.Length; i++)
        {
            InterfaceInputs[i].UserAction = null;
            InterfaceInputs[i].SetIcon(null, Color.clear);
        }
    }

    public void AssignActions(UrbUserAction[] Actions)
    {
        ClearButtons();
        AssignedActions = Actions;
        DisplayActions();
    }

    public void DisplayActions(int Index = 0)
    {
        if(Index >= AssignedActions.Length)
        {
            DisplayActions(Index - AssignedActions.Length);
            return;
        }
        if(Index < 0)
        {
            DisplayActions(Index + AssignedActions.Length);
            return;
        }

        for(int i =0; i+Index < AssignedActions.Length && i < InterfaceInputs.Length; i++)
        {
            InterfaceInputs[i].UserAction = AssignedActions[Index+i];
            InterfaceInputs[i].SetIcon(AssignedActions[Index + i].Icon, AssignedActions[Index + i].IconColor);
            
        }
    }
}
