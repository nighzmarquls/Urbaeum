using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UrbUtility;

public class UrbAgent : UrbBase
{
    public static int TotalAgents;
    
    public bool HasPathfinder { get; protected set; } = false;
    const float LocationThreshold = 0.01f;
    UrbThinker Mind;

    UrbBodyDisplay BodyDisplay;

    SpriteRenderer mSpriteRenderer;
    public float BirthTime;

    UrbDisplay _display = null; 
    public UrbDisplay Display
    {
        get { return _display; }
        private set
        {
            _display = value;
            // ReSharper disable once Unity.PerformanceCriticalCodeNullComparison
            
            HasDisplay = _display != null;
        }
    }
    public bool HasDisplay { get; private set; }

    public bool IsCurrentMapNull = true;
    bool IsBodyNotNull;
    UrbMap _currentMap;
    public UrbMap CurrentMap
    {
        get
        {
            return _currentMap;
        }
        set
        {
            _currentMap = value;
            // ReSharper disable once Unity.PerformanceCriticalCodeNullComparison
            IsCurrentMapNull = _currentMap == null;
        }
    }

    public UrbTile CurrentTile;
    public bool Shuffle = true;
    
    public float SizeOffset = 1.0f;
    public bool Moving { get; protected set; } = false;

    protected Vector3 TargetLocation;

    void Start()
    {
        IsCurrentMapNull = CurrentMap == null;
        IsBodyNotNull = Body != null;
    }
    
    protected override void OnDestroy()
    {
        CurrentTile.Occupants.Remove(this);
        UrbAgentManager.UnregisterAgent(this);
        base.OnDestroy();
    }

    public float TimeMultiplier {
        get {
            return IsCurrentMapNull ? 0 : CurrentMap.TimeMultiplier;
        }
    }

    public Vector3 Location {

        get { return transform.position; }

        set {
            TargetLocation = value;
        }
    }

    protected float LastCheckedMass = 0;
    public float Mass {  get {
            if(!IsBodyNotNull || Body.BodyComposition == null)
            {
                return 0;
            }

            return Body.BodyComposition.UsedCapacity;
        } }

    public float MassPerTile { get {
            if(tileprint.TileCount > 1)
            {
                return Mass / ((Body.Height > 1)? tileprint.TileCount*Body.Height : tileprint.TileCount );
            }
            else
            {
                return Mass;
            }
    } }

    [TextArea(0, 5)]
    public string TileprintString;
    protected UrbTileprint tileprint;
    public UrbTileprint Tileprint {
        get {
            if(tileprint != null)
            {
                return tileprint;
            }
            tileprint = new UrbTileprint(TileprintString);
            return tileprint;
        }
    }

    public UrbBody Body { get; private set; }
    public UrbMetabolism Metabolism { get; private set; }

    public Sprite CurrentSprite {
        get {
            if(mSpriteRenderer != null)
            {
                return mSpriteRenderer.sprite;
            }
          
            mSpriteRenderer = GetComponent<SpriteRenderer>();
            if (mSpriteRenderer == null)
            {
                return null;
            }

            return mSpriteRenderer.sprite;
        }
        set {
            if (mSpriteRenderer != null)
            {
                mSpriteRenderer.sprite = value;
            }
            else
            {
                mSpriteRenderer = GetComponent<SpriteRenderer>();
                if (mSpriteRenderer != null)
                {
                    mSpriteRenderer.sprite = value;
                }
            }
        }
    }

    //TemplatesMatch tells us if these are the same type of entity or not. 
    public bool TemplatesMatch(UrbAgent input)
    {
        if(Removing || WasDestroyed || input.WasDestroyed)
        {
            return false;
        }
        
        string LocalName = gameObject.name.Split('(')[0];
        string InputName = input.gameObject.name.Split('(')[0];

        return string.Compare(LocalName, InputName, true) == 0;
    }

    public virtual void AddAction(UrbAction Action)
    {
        if(AvailableActions == null)
        {
            AvailableActions = new []{ Action };
            return;
        }

        UrbAction[] newActions = new UrbAction[AvailableActions.Length + 1];
        AvailableActions.CopyTo(newActions, 1);
        newActions[0] = Action;

    }

    public virtual void RemoveAction(UrbAction Action)
    {
        if (AvailableActions == null)
        {
            return;
        }

        List<UrbAction> TempList = new List<UrbAction>(AvailableActions);

        if(TempList.Contains(Action))
        {
            TempList.Remove(Action);
            AvailableActions = TempList.ToArray();
        }
    }

    public UrbAction[] AvailableActions { get; private set; }

    static ProfilerMarker s_PickAction_p = new ProfilerMarker("UrbAgent.PickAction");
    public UrbAction PickAction(UrbTestCategory Test, float Result = 0, UrbTestCategory Exclude = UrbTestCategory.None)
    {
        using (s_PickAction_p.Auto())
        {
            if (Removing)
            {
                return null;
            }

            if (!IsMindNull)
            {
                return Mind.PickAction(Test, Result, Exclude);
            }

            if (AvailableActions == null)
            {
                return null;
            }

            for (int i = 0; i < AvailableActions.Length; i++)
            {
                bool Valid = (Test & AvailableActions[i].Category) == Test &&
                             (Exclude == UrbTestCategory.None || (Exclude & AvailableActions[i].Category) == 0);
                if (Valid)
                {

                    return AvailableActions[i];
                }
            }

            return null;
        }
    }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }

        Display = GetComponentInChildren<UrbDisplay>();
        
        if (!HasDisplay)
        {
            logger.LogError("No DisplayObject Present: Make sure a Display Object is attached!", gameObject);
        }

        BirthTime = Time.time;
        Body = GetComponent<UrbBody>();
        Metabolism = GetComponent<UrbMetabolism>();
        mSpriteRenderer = GetComponent<SpriteRenderer>();
        Camera = Camera.main;
        this.transform.rotation = Camera.transform.rotation;
        tileprint = new UrbTileprint(TileprintString);

        Mind = GetComponent<UrbThinker>();
        BodyDisplay = GetComponent<UrbBodyDisplay>();

        IsMindNull = Mind == null;
        LastCheckedMass = 0;

        UrbAgentManager.RegisterAgent(this);
        IsPaused = UrbAgentManager.IsPaused;
        base.Initialize();
    }

    bool Removing = false;
    public void Remove(bool reorder = true)
    {
        if (Removing)
        {
            logger.Log("Attempting to remove an object already being removed");
            return;
        }
        Removing = true;

        UrbAgentManager.UnregisterAgent(this);

        CurrentTile?.OnAgentLeave(this, reorder);
        
        CurrentMap = null;
        if(TotalAgents > 0)
        {
            TotalAgents--;
        }

        if (Debug.developerConsoleVisible)
        {
            logger.Log(LogType.Log, "Remove entity, destroying game object", context:this);
        }
        Destroy(gameObject);
    }

    public bool IsPaused { get; protected set; } = false;

    public bool Pause {
        get { return IsPaused; }
        set
        {
            if (value == IsPaused)
            {
                return;
            }
            
            UrbBehaviour[] AgentBehaviours = GetComponents<UrbBehaviour>();
            if (value)
            {
                for (int i = 0; i < AgentBehaviours.Length; i++)
                {
                    AgentBehaviours[i].PauseBehaviour();
                }
            }
            else
            {
                for (int i = 0; i < AgentBehaviours.Length; i++)
                {
                    AgentBehaviours[i].ResumeBehaviour();
                }
            }

            IsPaused = value;
        }
    }

    const float RepositionInterval = 0.1f;
    float NextReposition = 0;
    bool IsMindNull;
    Camera Camera;

    static ProfilerMarker s_TickToMind_p = new ProfilerMarker("UrbAgent.TickToMind");
    static ProfilerMarker s_TickToBody_p = new ProfilerMarker("UrbAgent.TickToBody");
    static ProfilerMarker s_TickToDisplay_p = new ProfilerMarker("UrbAgent.TickDisplay");
    public void Tick()
    {
        s_TickToMind_p.Begin(this);
        if (IsPaused)
        {
            s_TickToMind_p.End();
            return;
        }

        if (IsCurrentMapNull)
        {
            logger.Log("missing a map!", gameObject);
            // s_TickToMind_p.End();
            // Destroy(gameObject);
            // return;
        }
        if (!IsMindNull)
        {
            Mind.CheckUrges();
            Mind.ChooseBehaviour();
        }

        s_TickToMind_p.End();
        
        s_TickToBody_p.Begin();
        if(IsBodyNotNull)
        {
            if(Body.BodyCritical())
            {
                Remove();
            }
            if (BodyDisplay != null)
            {
                BodyDisplay.UpdateDisplay(Body.BodyComposition);
            }
        }

        s_TickToBody_p.End();

        s_TickToDisplay_p.Begin();
        if (HasDisplay && !Display.Invisible && Shuffle)
        {
            if (Math.Abs(LastCheckedMass - Mass) > 0.01f && Shuffle)
            {
                LastCheckedMass = Mass;
                CurrentTile.ReorderContents();
            }

            if (!(Time.time > NextReposition) || !(Display.Significance > UrbDisplay.FeatureSignificance))
            {
                s_TickToDisplay_p.End();
                return;
            }
            var position = transform.position;

            if (TargetLocation == position)
            {
                s_TickToDisplay_p.End();
                return;
            }
            NextReposition = Time.time + RepositionInterval;
            Vector3 Direction = (TargetLocation - position);

            position = (Direction.magnitude > LocationThreshold) ? position + (Direction.normalized * Time.deltaTime) : TargetLocation;
            transform.position = position;
        }

        s_TickToDisplay_p.End();
    }
    static ProfilerMarker s_UpdateUrbAgent_p = new ProfilerMarker("UrbAgent.Update");
    
    // Update is called once per frame
    void Update()
    {
        s_UpdateUrbAgent_p.Begin(this);
        Tick();
        s_UpdateUrbAgent_p.End();
    }

    public override UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();

        Data.Fields = new UrbFieldData[]
        {
            new UrbFieldData{ Name = "BirthTime", Value = BirthTime}
        };

        Data.Strings = new UrbStringData[]
        {
            new UrbStringData{Name= "Name", Value = gameObject.name},
            new UrbStringData{Name= "TileprintString", Value = TileprintString}
        };
        return Data;
    }

    public override bool SetComponentData(UrbComponentData Data)
    {
        BirthTime = UrbEncoder.GetField("BirthTime", Data);
        gameObject.name = UrbEncoder.GetString("Name", Data);
        TileprintString = UrbEncoder.GetString("TileprintString", Data);
        return true;
    }
}
