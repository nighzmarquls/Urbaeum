using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbMovement : UrbBase
{
    public float Speed = 2;
    public float EnergyCost = 5;
    public Coroutine Movement = null;

    protected UrbAgent mAgent;
    protected Renderer mRenderer;
    protected UrbMetabolism mMetabolism;
    void Start()
    {
        mAgent = GetComponent<UrbAgent>();
        mRenderer = GetComponent<Renderer>();
        mMetabolism = GetComponent<UrbMetabolism>();
    }

    public IEnumerator Moving(UrbTile Goal)
    {
        Vector3 StartingPosition = transform.position;
        float Complete = 0;

        if (mAgent.DisplayObject)
        {
            Vector3 Direction = Goal.Location - StartingPosition;

            if (Direction.x > 0 )
            {
                mAgent.DisplayObject.Flip = true;
            }
            else
            {
                mAgent.DisplayObject.Flip = false;
            }
        }

        if (mAgent.CurrentTile != null)
        {
            mAgent.CurrentTile.OnAgentLeave(mAgent);
            if (Goal != mAgent.CurrentTile)
            {
                if (mMetabolism != null)
                {
                    mMetabolism.SpendEnergy(EnergyCost * mAgent.Mass*mAgent.Mass * UrbMetabolism.EnergyConversionRatio );
                }
            }
        }
        Goal.OnAgentArrive(mAgent);

        float TravelTime = (1.0f / Speed);
        float ArrivalTime = Time.time + TravelTime;
        float StartTime = Time.time;
        while ( Complete < 1.0f)
        {
            yield return new WaitForEndOfFrame();
            mAgent.Location = Vector3.Lerp(StartingPosition, Goal.Location, Complete);
            Complete = (Time.time - StartTime) / TravelTime;
        }

        

        Movement = null;
    }

    public void MoveTo(UrbTile Goal)
    {
        if(Movement == null)
        {
            Movement = StartCoroutine(Moving(Goal));
        }
    }

    override public UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();

        Data.Fields = new UrbFieldData[]
        {
            new UrbFieldData{ Name = "Speed", Value = Speed },
            new UrbFieldData{ Name = "EnergyCost", Value = EnergyCost}
        };
        return Data;
    }

    override public bool SetComponentData(UrbComponentData Data)
    {
        Speed = UrbEncoder.GetField("Speed", Data);
        EnergyCost = UrbEncoder.GetField("EnergyCost", Data);
        return true;
    }
}
