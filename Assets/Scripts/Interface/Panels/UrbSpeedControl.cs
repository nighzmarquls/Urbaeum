using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UrbSpeedControl : UrbUIPanel
{
    public Text SpeedDisplay;

    public float SpeedStep = 0.5f;

    [SerializeField]
    protected float LastSpeed = 1.0f;

    public float CurrentSpeed { get { return LastSpeed; }
        set {

            if(value == LastSpeed)
            {
                return;
            }
            LastSpeed = value;
            SpeedDisplay.text = "x" + (float)Mathf.RoundToInt(LastSpeed*100)/100;
        }
    }
    protected override void Awake()
    {
        base.Awake();

        if(SpeedDisplay == null)
        {
            Debug.LogError("No Speed Display Assigned to UrbSpeedControl on " + gameObject.name);
        }
        SpeedDisplay.text = "x" +  LastSpeed;

        InterfaceInputs[0].UserAction = new UrbPauseAction();
        InterfaceInputs[1].UserAction = new UrbResumeAction();
        InterfaceInputs[2].UserAction = new UrbChangeSpeed { SpeedControl = this, SpeedChange = SpeedStep };
        InterfaceInputs[3].UserAction = new UrbChangeSpeed { SpeedControl = this, SpeedChange = 1.0f/ SpeedStep };
    }
}

public class UrbChangeSpeed : UrbUserAction
{
    public override string Name => "Change Speed";
    public override bool UseMapDisplay => false;

    public UrbSpeedControl SpeedControl;

    public float SpeedChange = 1.0f;

    public override void SelectAction()
    {
        if (SpeedControl == null)
            return;

        float ChangedSpeed = SpeedControl.CurrentSpeed * SpeedChange;

        UrbUIManager.Instance.TimeMultiplier = ChangedSpeed; //1.0f/ChangedSpeed;

        SpeedControl.CurrentSpeed = ChangedSpeed;
    }
}