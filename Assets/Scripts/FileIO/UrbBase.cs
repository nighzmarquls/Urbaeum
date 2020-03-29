
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbBase : MonoBehaviour
{
    protected bool bInitialized = false;
    virtual public UrbComponentData GetComponentData()
    {
        UrbComponentData Data = new UrbComponentData
        {
            Type = this.GetType().ToString(),
        };
        return Data;
    }

    virtual public bool SetComponentData(UrbComponentData Data)
    {
        Debug.Log(this.GetType() + " Using Base SetComponentData");
        return true;
    }

    virtual public void Initialize()
    {
       bInitialized = true;
    }
}
