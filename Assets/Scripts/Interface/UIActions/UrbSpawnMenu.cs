﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbSpawnMenu : UrbUserAction
{
    public override string Name => "Spawn Menu";
    public override string MapDisplayAssetPath => "";

    UrbSpawnAction[] Creatable = null;

    protected void InitializeCreatable()
    {

        List<UrbSpawnAction> WorkingList = new List<UrbSpawnAction>();
        for(int i = 0; i < UrbSystemIO.Instance.AgentTypes.Count; i++)
        {
            UrbBody TestBody = UrbSystemIO.Instance.AgentTypes[i].GetComponent<UrbBody>();

            if(TestBody == null || TestBody.BodyRecipe == null || TestBody.BodyRecipe.Length == 0)
            {
                continue;
            }

            UrbSpawnAction CreateAction = new UrbSpawnAction {
                AgentTemplate = UrbSystemIO.Instance.AgentTypes[i],
                Icon = UrbSystemIO.Instance.AgentTypes[i].CurrentSprite,
                Name = UrbSystemIO.Instance.AgentTypes[i].gameObject.name,
                MapDisplaySprite = UrbSystemIO.Instance.AgentTypes[i].CurrentSprite
            };
            WorkingList.Add(CreateAction);
        }

        Creatable = WorkingList.ToArray();
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
