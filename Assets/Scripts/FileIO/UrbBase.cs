using Unity.Profiling;
using Unity.Serialization;
using UnityEngine;
using UrbUtility;

public class UrbBase : MonoBehaviour
{
    protected readonly UrbLogger logger = new UrbLogger(UnityEngine.Debug.unityLogger.logHandler);

    //Optional Urb Components which may find themselves on a given UrbAgent 
    [DontSerialize]

    public UrbEater Eater { get; protected set; }

    [DontSerialize]
    public UrbBreeder Breeder { get; protected set; }
    
    [DontSerialize]
    public UrbBody mBody { get; protected set; }
    
    [DontSerialize]
    public UrbSmellSource SmellSource { get; protected set; }
    
    [DontSerialize]
    public bool IsSmelly { get; protected set; }
    
    [DontSerialize]
    public bool HasBody { get; protected set; }
    
    [DontSerialize]
    public bool IsBreeder { get; protected set; }
    
    [DontSerialize]
    public bool IsEater { get; protected set; }
    
    [DontSerialize]
    public bool LogMe = false;

    public bool HasEnableBeenCalled = false;
    public virtual void Update()
    {
        if (LogMe != logger.shouldBeLogging)
        {
            logger.ToggleDebug();
        }
    }
    
    public bool WasDestroyed { get; protected set;  } = false;
    
    static ProfilerMarker s_UrbBaseGetCompData_p = new ProfilerMarker("UrbBase.GetComponentData");
    public virtual UrbComponentData GetComponentData()
    {
        s_UrbBaseGetCompData_p.Begin(this);

        UrbComponentData Data = new UrbComponentData
        {
            Type = this.GetType().ToString(),
        };
        s_UrbBaseGetCompData_p.End();
        return Data;
    }

    protected virtual void OnDestroy()
    {
        enabled = false;
        WasDestroyed = true;
        
        IsEater = false;
        IsBreeder = false;
        IsSmelly = false;
        
        HasBody = false;
    }

    public virtual bool SetComponentData(UrbComponentData Data)
    {
        //Debug.Log(this.GetType() + " Using Base SetComponentData");
        return true;
    }
    
    public virtual void OnEnable()
    {
        HasEnableBeenCalled = true;
        
        Eater = GetComponent<UrbEater>();
        IsEater = Eater != null;

        Breeder = GetComponent<UrbBreeder>();
        IsBreeder = Breeder != null;

        SmellSource = GetComponent<UrbSmellSource>();
        IsSmelly = SmellSource != null;

        //Strange situation where UrbBody isn't getting enabled
        mBody = GetComponent<UrbBody>();
        //Protect from recursive nonsense
        if (mBody == this)
        {
            mBody = null;
        }
        
        HasBody = mBody != null;
        if (HasBody && !mBody.HasEnableBeenCalled)
        {
            mBody.OnEnable();
        }
        
        LogMe = false;
        logger.logEnabled = false;
        
        WasDestroyed = false;
        enabled = true;
    }
}
