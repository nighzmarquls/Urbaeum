using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbAgent : UrbBase
{

    UrbMovement MovementSystem;
    UrbPathfinder Pathfinder;
    UrbThinker Mind;

    UrbBodyDisplay Display;

    SpriteRenderer mSpriteRenderer;
    public float BirthTime;

    public UrbDisplay DisplayObject { get; private set; }

    public UrbMap CurrentMap;
    public UrbTile CurrentGoal;

    public float SizeOffset = 1.0f;

    [TextArea(0, 5)]
    public string TileprintString;
    protected UrbTileprint tileprint;
    public UrbTileprint Tileprint {
        get {
            if(tileprint != null)
            {
                return tileprint;
            }
            tileprint = new UrbTileprint();
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

        DisplayObject = GetComponentInChildren<UrbDisplay>();

        if (DisplayObject == null)
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
        Display = GetComponent<UrbBodyDisplay>();

        base.Initialize();


    }

    public void Remove()
    {
        UrbTile tile = CurrentMap.GetNearestTile(transform.position);
        tile.OnAgentLeave(this);
        CurrentMap = null;
        Destroy(gameObject);
    }

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
                CurrentGoal = Pathfinder.GetNextGoal(CurrentMap);
            }
            if (MovementSystem != null && CurrentGoal != null)
            {
               MovementSystem.MoveTo(CurrentGoal); 
            }
            else
            {
                if (DisplayObject != null)
                {
                    if (Time.time > NextFidget)
                    {
                        NextFidget = Time.time + FidgetTime + Random.Range(0, FidgetTime);
                        float Probability = Random.value;

                        if (Probability > 0.85f)
                        {
                            DisplayObject.Flip = !DisplayObject.Flip;
                        }
                        else if (Probability > 0.75)
                        {
                            DisplayObject.Express(UrbDisplayFace.Expression.Closed);
                        }
                        else if (Probability > 0.70)
                        {
                            DisplayObject.Express(UrbDisplayFace.Expression.Default);
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
                if (Display != null)
                {
                    Display.UpdateDisplay(Body.BodyComposition);
                }
            }

            
           
        }
        else
        {
            Debug.Log(gameObject.name + " Is Invalid!");
            Destroy(gameObject);
        }


    }

    public float FidgetTime = 1.0f;
    float NextFidget = 0; 
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
