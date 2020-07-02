
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class UrbBase : MonoBehaviour
{
    public bool WasDestroyed { get; protected set;  } = false;
    protected bool bInitialized { get; private set; } = false;
    
    static ProfilerMarker s_UrbBaseGetCompData_p = new ProfilerMarker("UrbBase.GetComponentData");
    public virtual UrbComponentData GetComponentData()
    {
        s_UrbBaseGetCompData_p.Begin(this);
        if(!bInitialized)
        {
            Initialize();
        }
        UrbComponentData Data = new UrbComponentData
        {
            Type = this.GetType().ToString(),
        };
        s_UrbBaseGetCompData_p.End();
        return Data;
    }

    protected virtual void OnDestroy()
    {
        bInitialized = false;
        enabled = false;
        WasDestroyed = true;
    }

    public virtual bool SetComponentData(UrbComponentData Data)
    {
        //Debug.Log(this.GetType() + " Using Base SetComponentData");
        return true;
    }
    public virtual void Initialize()
    {
        WasDestroyed = false;
        bInitialized = true;
        enabled = true;
    }

}
