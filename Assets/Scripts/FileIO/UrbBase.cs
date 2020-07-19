using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Unity.Assertions;
using Unity.Profiling;
using Unity.Serialization;
using UnityEngine;
using UrbUtility;

public class UrbBase : MonoBehaviour
{
    public UrbLogger logger { get; set; }

#region OptionalComponents
    //Optional Urb Components which may find themselves on a given UrbAgent 
    [DontSerialize] public UrbEater Eater { get; protected set; }

    [DontSerialize] public UrbBreeder Breeder { get; protected set; }
    
    [DontSerialize] public UrbBody mBody { get; protected set; }

    [DontSerialize] public UrbMetabolism Metabolism
    {
        get; 
        private set;
    }
    
    [DontSerialize] public UrbSmellSource SmellSource { get; protected set; }
    
    [DontSerialize] public UrbAgent mAgent { get; protected set; }
    [DontSerialize] public UrbThinker Mind { get; protected set; }

    #endregion
    #region OptionalComponentIndicators
    
    [DontSerialize] public bool IsSmelly { get; protected set; }
    
    [DontSerialize] public bool HasBody { get; protected set; }
    
    [DontSerialize] public bool IsBreeder { get; protected set; }
    
    [DontSerialize] public bool IsEater { get; protected set; }
    [DontSerialize] public bool HasAgent { get; protected set; }

    [DontSerialize] public bool IsMindNull { get; protected set; }
    [DontSerialize] public bool HasMetabolism { get; protected set; }
    #endregion

    [DontSerialize] public bool HasAwakeBeenCalled { get; protected set; }
    [DontSerialize] public bool HasEnableBeenCalled { get; protected set; }
    [DontSerialize] public bool LogMe = false;
    [DontSerialize] public bool ValidateMe = false;
    [DontSerialize] public static bool ShouldPause;
    [DontSerialize] public bool IsPaused { get; protected set; }
    
    public bool WasDestroyed { get; protected set;  } = false;
    
    static ProfilerMarker s_UrbBaseGetCompData_p = new ProfilerMarker("UrbBase.GetComponentData");
    public virtual UrbComponentData GetComponentData()
    {
        s_UrbBaseGetCompData_p.Begin(this);

        UrbComponentData Data = new UrbComponentData
        {
            Type = GetType().ToString(),
        };
        s_UrbBaseGetCompData_p.End();
        return Data;
    }
    
    public virtual bool SetComponentData(UrbComponentData Data)
    {
        //Debug.Log(this.GetType() + " Using Base SetComponentData");
        return true;
    }

    //The method call orders go:
    //Awake -> Start -> OnEnable -> Update
    //OnEnable for resetting Urb states.
    //Awake // Start for setting references to components that need to communicate
#region Start-Of-Lifetime methods
    public virtual void Awake()
    {
        Assert.IsFalse(HasAwakeBeenCalled);
        
        HasAwakeBeenCalled = true;

        SetUrbComponents();
        
        IsMindNull = Mind == null;
        
        IsEater = Eater != null;
        IsBreeder = Breeder != null;
        IsSmelly = SmellSource != null;
        
        HasAgent = mAgent != null;
        HasBody = mBody != null;
        HasMetabolism = Metabolism != null;
        
        LogMe = false;
    }
    
    public virtual void OnEnable()
    {
        if (HasAgent)
        {
            logger = mAgent.GetLogger();
            Assert.IsNotNull(logger);
        }
        
        ValidateUrbComponents();
        //Logger is not guaranteed to exist until this point.
        logger.logEnabled = false;

        Assert.IsTrue(HasAwakeBeenCalled);
        Assert.IsFalse(HasEnableBeenCalled);
        HasEnableBeenCalled = true;
        enabled = true;
        WasDestroyed = false;
    }
#endregion

    public virtual void Update()
    {
        if (ValidateMe)
        {
            ValidateUrbComponents();
        }
    }
    
#region End-Of-Lifetime methods
    protected virtual void OnDisable()
    {
        HasEnableBeenCalled = false;
        //TODO: Object-pool-y value resets.
        
        enabled = false;
        WasDestroyed = true;
        
        IsEater = false;
        IsBreeder = false;
        IsSmelly = false;
        
        HasBody = false;
    }
#endregion
    
    [Conditional("UNITY_ASSERTIONS")]
    void ValidateUrbComponents()
    {
        Assert.IsNotNull(logger);
        
        //Keeping these checks only to the most commonly broken and / or problematic assertions
        //In order to reduce perf impact in debug mode.
        if (HasMetabolism)
        {
            Assert.IsNotNull(Metabolism);
        }

        if (HasAgent)
        {
            Assert.IsNotNull(mAgent);
        }

        if (IsSmelly)
        {
            Assert.IsNotNull(SmellSource);
        }
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
