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
            if(Body != null)
            {
                if(Body.BodyCritical())
                {
                    Remove();
                }
                if (Display != null)
                {
                    Display.UpdateSprites(Body.BodyComposition);
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
        return Data;
    }

    override public bool SetComponentData(UrbComponentData Data)
    {
        BirthTime = UrbEncoder.GetField("BirthTime", Data);
        return true;
    }
}