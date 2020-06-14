using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbCreateTool : UrbUserAction
{
    public override string Name => "Create Tool";
    public override string MapDisplayAssetPath => "";

    UrbSpawnAction[] Creatable = null;

    protected void InitializeCreatable()
    {
        Creatable = new UrbSpawnAction[UrbSystemIO.Instance.AgentTypes.Count];

        for(int i = 0; i < UrbSystemIO.Instance.AgentTypes.Count; i++)
        {
            UrbSpawnAction CreateAction = new UrbSpawnAction {
                SpawnedTemplate = UrbSystemIO.Instance.AgentTypes[i].gameObject,
                Icon = UrbSystemIO.Instance.AgentTypes[i].CurrentSprite,
                Name = UrbSystemIO.Instance.AgentTypes[i].gameObject.name,
                MapDisplaySprite = UrbSystemIO.Instance.AgentTypes[i].CurrentSprite
            };

            Creatable[i] = CreateAction;
        }
    }

    public override void SelectAction()
    {
        if(UrbUIManager.Instance.CurrentAction != null && UrbUIManager.Instance.CurrentAction == this)
        {
            return;
        }

        if(Creatable == null)
        {
            InitializeCreatable();
        }

        UrbUIManager.Instance.Toolbar.AssignActions(Creatable);
        base.SelectAction();
    }

    public override void UnselectAction()
    {
       
    }
}
