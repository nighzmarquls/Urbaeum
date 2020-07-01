
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbBase : MonoBehaviour
{
    protected bool bInitialized { get; private set; } = false;
    public virtual UrbComponentData GetComponentData()
    {
        if(!bInitialized)
        {
            Initialize();
        }
        UrbComponentData Data = new UrbComponentData
        {
            Type = this.GetType().ToString(),
        };
        return Data;
    }

    public virtual bool SetComponentData(UrbComponentData Data)
    {
        //Debug.Log(this.GetType() + " Using Base SetComponentData");
        return true;
    }

    public virtual void Initialize()
    {
        bInitialized = true;
        enabled = true;
    }

}
