using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbInvestigatorMenu : UrbUserAction
{
    [System.Serializable]
    protected struct ScentInfo
    {
        public UrbScentTag ScentTag;
        public Color ScentColor;
    }

    protected static ScentInfo[] Info = 
    {
        new ScentInfo{ ScentTag = UrbScentTag.Plant, ScentColor =  Color.green},
        new ScentInfo{ ScentTag = UrbScentTag.Meat, ScentColor =  Color.red},
        new ScentInfo{ ScentTag = UrbScentTag.Sweet, ScentColor =  Color.white},
        new ScentInfo{ ScentTag = UrbScentTag.Fluff, ScentColor =  Color.gray},
        new ScentInfo{ ScentTag = UrbScentTag.Male, ScentColor =  Color.blue},
        new ScentInfo{ ScentTag = UrbScentTag.Female, ScentColor =  Color.magenta},
        new ScentInfo{ ScentTag = UrbScentTag.Violence, ScentColor =  Color.red + Color.yellow},
    };

    public override string Name => "Investigator Menu";
    public override bool UseMapDisplay => false;

    UrbInvestigatorTool[] Investigators = null;

    protected void InitializeCreatable()
    {

        List<UrbInvestigatorTool> WorkingList = new List<UrbInvestigatorTool>();

        WorkingList.Add( new UrbInvestigatorTool { Icon = this.Icon}
        );

        for (int i = 0; i < Info.Length; i++)
        {

            UrbInvestigatorTool InvestigatorAction = new UrbScentInvestigator
            {
                DisplayScentTag = Info[i].ScentTag,
                Icon = this.Icon,
                Name = Info[i].ScentTag.ToString() + " Investigator",
                MapDisplaySprite = this.MapDisplaySprite,
                DetectionColor = Info[i].ScentColor,
                
            };

            WorkingList.Add(InvestigatorAction);
        }

        Investigators = WorkingList.ToArray();
    }

    public override void SelectAction()
    {
        if (UrbUIManager.Instance.CurrentAction != null && UrbUIManager.Instance.CurrentAction == this)
        {
            return;
        }

        if (Investigators == null)
        {
            InitializeCreatable();
        }

        UrbUIManager.Instance.Toolbar.AssignActions(Investigators);
        base.SelectAction();
    }

    public override void UnselectAction()
    {

    }
}
