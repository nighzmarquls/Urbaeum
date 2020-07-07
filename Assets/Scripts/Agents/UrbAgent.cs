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

    [NonSerialized]
    public UrbMerge[] UrbMerges = null;

    public bool IsMergeable { get; private set; } = false;

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

    protected string AgentLocalName;
    public bool IsCurrentMapNull = true;
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

    protected UrbTile LastTile;
    public UrbTile[] OccupiedTiles { get; protected set; }

    public UrbTile CurrentTile { get { return LastTile; }
        set {
            if (value == LastTile)
            {
                return;
            }
            LastTile = value;
            OccupiedTiles = Tileprint.GetAllPrintTiles(this);
        }
    }

    public bool Shuffle = true;
    
    public float SizeOffset = 1.0f;
    public bool Moving { get; protected set; } = false;

    protected Vector3 TargetLocation;
    
    protected override void OnDestroy()
    {
        if (CurrentTile != null && CurrentTile.Occupants != null)
        {
            CurrentTile.Occupants.Remove(this);
        }
        //generally we don't actually care if 
        //the underlying objects are non-null
        //only if it's null and we don't know about it
        IsCurrentMapNull = true;
        CurrentTile = null;
        IsMindNull = true;
        CurrentMap = null;
        UrbAgentManager.UnregisterAgent(this);
        base.OnDestroy();
    }

    public float TimeMultiplier {
        get {
            if (CurrentMap == null)
            {
                return 0;
            }
            return CurrentMap.TimeMultiplier;
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
            if(!HasBody || mBody.BodyComposition == null)
            {
                return 0;
            }

            return mBody.BodyComposition.UsedCapacity;
        } }

    public float MassPerTile { get {
            if(tileprint.TileCount > 1)
            {
                return Mass / ((mBody.Height > 1)? tileprint.TileCount*mBody.Height : tileprint.TileCount );
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
            logger.Log("Cannot merge component because Agent was removing/Destroyed or input was destroyed", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(AgentLocalName))
        {
            AgentLocalName = gameObject.name.Split('(')[0];
        }
        
        var templatesMatch = string.Compare(AgentLocalName, input.AgentLocalName, true) == 0;
        
        if (!templatesMatch)
        {
            logger.Log("Local Name: " + AgentLocalName + " doesn't match input name: " + input.AgentLocalName, this);
        }

        return templatesMatch;
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
        if (AvailableActions == null || AvailableActions.Length == 0)
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

    public virtual void AddDeathBehaviour(UrbBehaviour Behaviour)
    {
        if (DeathBehaviours == null)
        {
            DeathBehaviours = new[] { Behaviour };
            return;
        }

        UrbBehaviour[] newBehaviours = new UrbBehaviour[DeathBehaviours.Length + 1];
        DeathBehaviours.CopyTo(newBehaviours, 1);
        newBehaviours[0] = Behaviour;

    }

    public virtual void RemoveDeathBehaviour(UrbBehaviour Behaviour)
    {
        if (DeathBehaviours == null || DeathBehaviours.Length == 0)
        {
            return;
        }

        List<UrbBehaviour> TempList = new List<UrbBehaviour>(DeathBehaviours);

        if (TempList.Contains(Behaviour))
        {
            TempList.Remove(Behaviour);
            DeathBehaviours = TempList.ToArray();
        }
    }

    public UrbBehaviour[] DeathBehaviours { get; private set; }

    static ProfilerMarker s_PickAction_p = new ProfilerMarker("UrbAgent.PickAction");
    public UrbAction PickAction(UrbTestCategory Test, float Result = 0, UrbTestCategory Exclude = UrbTestCategory.None)
    {
        using (s_PickAction_p.Auto())
        {
            if (Removing)
            {
                logger.Log("Chose not to pick an action because removing", this);
                return null;
            }

            if (!IsMindNull)
            {
                logger.Log("Trying to pick an action");
                return Mind.PickAction(Test, Result, Exclude);
            }

            if (AvailableActions == null)
            {
                logger.Log("No actions available!", this);
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

            Debug.unityLogger.Log("PickAction", "Could not find any actions to pick!", this);
            return null;
        }
    }

    public override void OnEnable()
    {
        Display = GetComponentInChildren<UrbDisplay>();

        if (!HasDisplay)
        {
            logger.LogError("No DisplayObject Present: Make sure a Display Object is attached!", gameObject);
        }

        BirthTime = Time.time;
        
        Metabolism = GetComponent<UrbMetabolism>();
        mSpriteRenderer = GetComponent<SpriteRenderer>();
        Camera = Camera.main;
        if (Camera == null)
        {
            Debug.LogError("Camera for UrbAgent is NULL", this);
            return;
        }
        
        this.transform.rotation = Camera.transform.rotation;
        tileprint = new UrbTileprint(TileprintString);
        
        Mind = GetComponent<UrbThinker>();
        BodyDisplay = GetComponent<UrbBodyDisplay>();
        IsMindNull = Mind == null;
        LastCheckedMass = 0;

        UrbMerges = GetComponents<UrbMerge>();

        if (UrbMerges != null && UrbMerges.Length > 0)
        {
            IsMergeable = true;
        }
        
        
        UrbAgentManager.RegisterAgent(this);
        IsPaused = UrbAgentManager.IsPaused;
        base.OnEnable();

        if (gameObject.name.Contains("("))
        {
            AgentLocalName = gameObject.name.Split('(')[0];
            return;
        }

        AgentLocalName = gameObject.name;
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

    const float MassChangeToReorder = 50f;
    const float RepositionInterval = 0.1f;
    float NextReposition = 0;
    bool IsMindNull;
    Camera Camera;

    public void Express(UrbDisplayFace.Expression Expression)
    {
        Display.Express(Expression);
    }

    public void Die()
    {
        Express(UrbDisplayFace.Expression.Dead);

        if (DeathBehaviours != null)
        {
            for (int d = 0; d < DeathBehaviours.Length; d++)
            {
                DeathBehaviours[d].ExecuteTileBehaviour();
            }
        }
        Remove();
    }

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
            return;
        }

        if (CurrentTile == null)
        {
            logger.Log("Agent has no Tile!");
        }
        
        if (!IsMindNull)
        {
            Mind.CheckUrges();
            Mind.ChooseBehaviour();
        }

        s_TickToMind_p.End();
        
        s_TickToBody_p.Begin();
        if(HasBody)
        {
            if(mBody.BodyCritical())
            {
                Die();
            }

            //TODO: Move this into some other behaviour/into a different cadence.
            mBody.RecoverUtilization();

            if (BodyDisplay != null)
            {
                BodyDisplay.UpdateDisplay(mBody.BodyComposition);
            }
        }
        
        s_TickToBody_p.End();

        s_TickToDisplay_p.Begin();
        if (HasDisplay && !Display.Invisible && Shuffle)
        {
            var massChange = Math.Abs(LastCheckedMass - Mass);
            if (massChange > MassChangeToReorder && Shuffle)
            {
                LastCheckedMass = Mass;
                CurrentTile?.VisualShuffle();
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
    public override void Update()
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
