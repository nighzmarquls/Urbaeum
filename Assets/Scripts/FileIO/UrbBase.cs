using Unity.Profiling;
using Unity.Serialization;
using UnityEngine;
using UrbUtility;

public class UrbBase : MonoBehaviour
{
    protected readonly UrbLogger logger = new UrbLogger(UnityEngine.Debug.unityLogger.logHandler);

    #region OptionalComponents
    //Optional Urb Components which may find themselves on a given UrbAgent 
    [DontSerialize]     public UrbEater Eater { get; protected set; }

    [DontSerialize] public UrbBreeder Breeder { get; protected set; }
    
    [DontSerialize] public UrbBody mBody { get; protected set; }
    [DontSerialize] public UrbMetabolism Metabolism { get; private set; }
    
    [DontSerialize] public UrbSmellSource SmellSource { get; protected set; }
    
    [DontSerialize] public UrbAgent mAgent { get; protected set; }
    [DontSerialize] public UrbThinker Mind;
    #endregion
    #region OptionalComponentIndicators
    
    [DontSerialize] public bool IsSmelly { get; protected set; }
    
    [DontSerialize] public bool HasBody { get; protected set; }
    
    [DontSerialize] public bool IsBreeder { get; protected set; }
    
    [DontSerialize] public bool IsEater { get; protected set; }
    [DontSerialize] public bool HasAgent { get; protected set; }
    
    [DontSerialize] public bool IsMindNull;
    [DontSerialize] public bool HasMetabolism;
    #endregion

    [DontSerialize] public bool HasEnableBeenCalled { get; protected set; }
    [DontSerialize] public bool LogMe = false;

    
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

        SetUrbComponents();
        
        IsEater = Eater != null;
        IsBreeder = Breeder != null;
        IsSmelly = SmellSource != null;
        IsMindNull = Mind == null;
        HasAgent = mAgent != null;
        HasBody = mBody != null;
        //Strange situation where UrbBody isn't getting enabled
        if (HasBody && !mBody.HasEnableBeenCalled)
        {
            mBody.OnEnable();
        }
        
        HasMetabolism = Metabolism != null;
        
        LogMe = false;
        logger.logEnabled = false;
        
        WasDestroyed = false;
        enabled = true;
    }
    
    void SetUrbComponents()
    {
        Metabolism = GetComponent<UrbMetabolism>();
        if (Metabolism == this)
        {
            Metabolism = null;
        }
        
        Eater = GetComponent<UrbEater>();
        if (Eater == this)
        {
            Eater = null;
        }
        
        Breeder = GetComponent<UrbBreeder>();
        if (Breeder == this)
        {
            Breeder = null;
        }
        
        SmellSource = GetComponent<UrbSmellSource>();
        if (SmellSource == this)
        {
            SmellSource = null;
        }
        
        Mind = GetComponent<UrbThinker>();
        if (Mind == this)
        {
            Mind = null;
        }
        
        mBody = GetComponent<UrbBody>();
        if (mBody == this)
        {
            mBody = null;
        }
        
        mAgent = GetComponent<UrbAgent>();
        if (mAgent == this)
        {
            mAgent = null;
        }
    }
}
