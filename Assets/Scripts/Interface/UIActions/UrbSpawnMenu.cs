using System.Collections;
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

            var template = UrbSystemIO.Instance.AgentTypes[i];
            var sprite = template.GetComponent<SpriteRenderer>().sprite;
            UrbSpawnAction CreateAction = new UrbSpawnAction {
                AgentTemplate = template,
                Icon = sprite,
                Name = template.gameObject.name,
                MapDisplaySprite = sprite
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
