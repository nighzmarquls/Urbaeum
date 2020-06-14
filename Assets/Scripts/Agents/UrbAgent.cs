using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbAgent : UrbBase
{
    public static int TotalAgents;

    const float LocationThreshold = 0.01f;
    UrbThinker Mind;

    UrbBodyDisplay BodyDisplay;

    SpriteRenderer mSpriteRenderer;
    public float BirthTime;

    public UrbDisplay Display { get; private set; }
  
    public UrbMap CurrentMap;
    public UrbTile CurrentTile;
    public bool Shuffle = true;

    public bool Interacting = false;

    public float SizeOffset = 1.0f;
    public bool Moving { get; protected set; } = false;

    protected Vector3 TargetLocation;
    public Vector3 Location {

        get { return transform.position; }

        set {
            if(TargetLocation != value)
            {
                TargetLocation = value;
            }
        }
    }

    protected float LastCheckedMass = 0;
    public float Mass {  get {
            if(Body == null || Body.BodyComposition == null)
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

    public bool TemplatesMatch(UrbAgent input)
    {
        string LocalName = gameObject.name.Split('(')[0];
        string InputName = input.gameObject.name.Split('(')[0];

        return string.Compare(LocalName, InputName, true) == 0;
    }

    public virtual void AddAction(UrbAction Action)
    {
        if(AvailableActions == null)
        {
            AvailableActions = new UrbAction[]
            {
                Action
            };
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

    public UrbAction PickAction(UrbTestCategory Test, float Result = 0, UrbTestCategory Exclude = UrbTestCategory.None)
    {
        if(Removing)
        {
            return null;
        }

        if(Mind == null)
        {
            if (AvailableActions != null)
            {
                for (int i = 0; i < AvailableActions.Length; i++)
                {
                    bool Valid = (Test & AvailableActions[i].Category) == Test &&
                    (Exclude == UrbTestCategory.None || (Exclude & AvailableActions[i].Category) == 0);
                    if (Valid)
                    {

                        return AvailableActions[i];
                    }
                }
            }
            return null;
        }

        return Mind.PickAction(Test, Result, Exclude);
    }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }

        Display = GetComponentInChildren<UrbDisplay>();

        if (Display == null)
        {
            Debug.LogError("No DisplayObject Present: Make sure a Display Object is attached to " + gameObject.name);
        }

        BirthTime = Time.time;
        Body = GetComponent<UrbBody>();
        Metabolism = GetComponent<UrbMetabolism>();
        mSpriteRenderer = GetComponent<SpriteRenderer>();
        this.transform.rotation = Camera.main.transform.rotation;
        tileprint = new UrbTileprint(TileprintString);

        Mind = GetComponent<UrbThinker>();
        BodyDisplay = GetComponent<UrbBodyDisplay>();

        LastCheckedMass = 0;

        UrbAgentManager.RegisterAgent(this);
        IsPaused = UrbAgentManager.IsPaused;
        base.Initialize();

    }

    bool Removing = false;
    public void Remove()
    {
        if (Removing)
        {
            return;
        }
        Removing = true;

        UrbAgentManager.UnregisterAgent(this);

        if (CurrentTile != null)
        {
            CurrentTile.OnAgentLeave(this);
        }
        CurrentMap = null;
        if(TotalAgents > 0)
        {
            TotalAgents--;
        }
        
        Destroy(gameObject);
    }

    public bool IsPaused { get; protected set; } = false;
    public bool Pause {
        get { return IsPaused; }
        set {
            if (value != IsPaused)
            {
                UrbBehaviour[] AgentBehaviours = GetComponents<UrbBehaviour>();
                if(value == true)
                {
                    for(int i = 0; i < AgentBehaviours.Length; i++)
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
    }

    float RepositionInterval = 0.1f;
    float NextReposition = 0;
    public void Tick()
    {
        if (IsPaused)
        {
            return;
        }

        if (CurrentMap != null)
        {
            if (Mind != null)
            {
                Mind.CheckUrges();
                Mind.ChooseBehaviour();
            }
               
            if(Body != null)
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

            if (Display != null && !Display.Invisible && Shuffle)
            {
                if (LastCheckedMass != Mass && Shuffle)
                {
                    LastCheckedMass = Mass;
                    CurrentTile.ReorderContents();
                }

                if(Time.time > NextReposition && Display.Significance > UrbDisplay.FeatureSignificance)
                {
                    if (TargetLocation != transform.position)
                    {
                        NextReposition = Time.time + RepositionInterval;
                        Vector3 Direction = (TargetLocation - transform.position);

                        transform.position = (Direction.magnitude > LocationThreshold) ? transform.position + (Direction.normalized * Time.deltaTime) : TargetLocation;
                    }
                }
            }
        }
        else
        {
            Debug.Log(gameObject.name + " Is Invalid!");
            Destroy(gameObject);
        }


    }


    // Update is called once per frame
    void Update()
    {
        Tick();
    }

    override public UrbComponentData GetComponentData()
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

    override public bool SetComponentData(UrbComponentData Data)
    {
        BirthTime = UrbEncoder.GetField("BirthTime", Data);
        gameObject.name = UrbEncoder.GetString("Name", Data);
        TileprintString = UrbEncoder.GetString("TileprintString", Data);
        return true;
    }
}
