using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text), typeof(Text))]
public class UrbEventLogger : UrbDisplayWindow
{
    int lastMessageUpdateTime;
    string messagePool;
    
    public static bool AgentAssigned { get; protected set; } = false;
    protected static UrbAgent mAgent = null;

    public static UrbAgent TargetAgent
    {
        get { return mAgent; } set { mAgent = value; AgentAssigned = value != null; }
    }
    
    //By using RequireComponent above, they should NEVER be null.
    public Text HeaderText;
    public Text BodyText;
    
    public override void OnEnable()
    {
        Assert.IsNotNull(HeaderText);
        Assert.IsNotNull(BodyText);
        base.OnEnable();
    }

    public override void OnClose()
    { 
    }
    
    // Update is called once per frame
    void Update()
    {
        HeaderText.text = "Frame number: " + Time.frameCount;

        if (!AgentAssigned)
        {
            return;
        }
        
        //TODO: Tune the logging to minimize actions.
        var changeFrame = TargetAgent.LastEventLogUpdateFrame();
        if (changeFrame > lastMessageUpdateTime)
        {
            lastMessageUpdateTime = changeFrame;
            BodyText.text = TargetAgent.ReadEventLog();
        }
    }

    public override void OnMinimize()
    {
        base.OnMinimize();
    }
}
