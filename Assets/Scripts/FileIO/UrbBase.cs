using Unity.Profiling;
using UnityEngine;
using UrbUtility;

public class UrbBase : MonoBehaviour
{
    protected readonly UrbLogger logger = new UrbLogger(UnityEngine.Debug.unityLogger.logHandler);

    //Optional Urb Components which may find themselves on a given UrbAgent 
    public UrbEater Eater { get; protected set; }
    public UrbBreeder Breeder { get; protected set; }
    
    public bool IsBreeder { get; protected set; }
    public bool IsEater { get; protected set; }
    
    public bool LogMe = false;
    
    public virtual void Update()
    {
        if (!bInitialized)
        {
            Initialize();
        }
        
        if (LogMe != logger.shouldBeLogging)
        {
            logger.ToggleDebug();
        }
    }
    
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
        Eater = GetComponent<UrbEater>();
        IsEater = Eater != null;

        Breeder = GetComponent<UrbBreeder>();
        IsBreeder = Breeder != null;
        
        LogMe = false;
        logger.logEnabled = false;
        
        WasDestroyed = false;
        bInitialized = true;
        enabled = true;
    }
}
