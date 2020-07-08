using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text), typeof(Text))]
public class UrbEventLogger : UrbDisplayWindow
{
    static readonly string TitleText;
    static readonly string[] EventMessages = new string[10];
    //Idea is to use this to create a "scrolling" log on each update cycle
    static int idxOfNextMessage;
    public bool AgentAssigned { get; protected set; } = false;
    protected UrbAgent mAgent = null;

    public UrbAgent TargetAgent
    {
        get { return mAgent; } set { mAgent = value; AgentAssigned = value != null; }
    }
    
    //By using RequireComponent above, they should NEVER be null.
    public Text HeaderText;
    public Text BodyText;
    
    public override void OnEnable()
    {
        Debug.Log("Enabling UrbEventLogger");
        base.OnEnable();
    }

    public override void OnClose()
    {
    }
    
    // Update is called once per frame
    void Update()
    {
        HeaderText.text = "Frame number: " + Time.frameCount;
    }

    public override void OnMinimize()
    {
        base.OnMinimize();
    }
}
