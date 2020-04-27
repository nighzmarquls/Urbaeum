using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbAgent : UrbBase
{
    public static int TotalAgents;

    const float LocationThreshold = 0.01f;
    public UrbMovement MovementSystem { get; protected set; }
    UrbPathfinder Pathfinder;
    UrbThinker Mind;

    UrbBodyDisplay BodyDisplay;

    SpriteRenderer mSpriteRenderer;
    public float BirthTime;

    public UrbDisplay Display { get; private set; }
  
    public UrbMap CurrentMap;
    public UrbTile CurrentGoal;
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

            return Body.BodyComposition.CurrentCapacty;
        } }

    public float MassPerTile { get {
            if(tileprint.TileCount > 1)
            {
                return Mass / tileprint.TileCount;
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
        MovementSystem = GetComponent<UrbMovement>();
        Pathfinder = GetComponent<UrbPathfinder>();
        Body = GetComponent<UrbBody>();

        CurrentGoal = null;
        mSpriteRenderer = GetComponent<SpriteRenderer>();
        this.transform.rotation = Camera.main.transform.rotation;
        tileprint = new UrbTileprint(TileprintString);

        Mind = GetComponent<UrbThinker>();
        BodyDisplay = GetComponent<UrbBodyDisplay>();

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


    public float FidgetTime = 1.0f;
    float NextFidget = 0;

    public void Tick()
    {
        if (CurrentMap != null)
        {
            if (Mind != null)
            {
                Mind.CheckUrges();
            }
            if (Pathfinder != null)
            {
                if (!Moving && !Interacting)
                {
                    CurrentGoal = Pathfinder.GetNextGoal(CurrentMap);
                }
            }
            if (MovementSystem != null && CurrentGoal != null && CurrentGoal != CurrentTile)
            {
                Moving = true;
                if (Display != null)
                {
                    Display.Express(UrbDisplayFace.Expression.Default);
                }
                MovementSystem.MoveTo(CurrentGoal);
            }
            else
            {
                Moving = false;
                if (Display != null)
                {
                    if (TargetLocation != transform.position && Shuffle)
                    {
                        Vector3 Direction = (TargetLocation - transform.position);

                        transform.position = (Direction.magnitude > LocationThreshold) ? transform.position + (Direction.normalized * Time.deltaTime) : TargetLocation;
                    }

                    if (Time.time > NextFidget)
                    {
                        NextFidget = Time.time + FidgetTime + Random.Range(0, FidgetTime);
                        float Probability = Random.value;

                        if (Probability > 0.85f)
                        {
                            Display.Flip = !Display.Flip;
                        }
                        else if (Probability > 0.75)
                        {
                            Display.Express(UrbDisplayFace.Expression.Closed);
                        }
                        else if (Probability > 0.70)
                        {
                            Display.Express(UrbDisplayFace.Expression.Default);
                        }
                    }
                }
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

            if (Display != null && !Display.Invisible && LastCheckedMass != Mass && Shuffle)
            {
                LastCheckedMass = Mass;
                CurrentTile.ReorderContents();
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
            new UrbStringData{Name= "Name", Value = gameObject.name}
        };
        return Data;
    }

    override public bool SetComponentData(UrbComponentData Data)
    {
        BirthTime = UrbEncoder.GetField("BirthTime", Data);
        gameObject.name = UrbEncoder.GetString("Name", Data);
        return true;
    }
}
