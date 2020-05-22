using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbPerception : UrbBehaviour
{
    [TextArea(0, 5)]
    public string PerceptionPrintString;
    protected UrbTileprint senseprint;
    public UrbTileprint SensePrint {
        get {
            if (string.IsNullOrEmpty(PerceptionPrintString))
            {
                return null;
            }
            if (senseprint != null)
            {
                return senseprint;
            }
            senseprint = new UrbTileprint(PerceptionPrintString);
            return senseprint;
        }
    }

    [TextArea(0, 5)]
    public string ContactPrintString;
    protected UrbTileprint contactprint;
    public UrbTileprint ContactPrint {
        get {
            if (string.IsNullOrEmpty(ContactPrintString))
            {
                return null;
            }

            if (contactprint != null)
            {
                return contactprint;
            }

            contactprint = new UrbTileprint(PerceptionPrintString);
            
            return contactprint;
        }
    }

    public UrbBehaviour[] mBehaviours;
    public UrbBehaviour[] Behaviours { get {
            if (mBehaviours == null)
            {
                return new UrbBehaviour[0];
            }
            return mBehaviours;
        }
    }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }
       
        base.Initialize();
        List<UrbBehaviour> BehaviourList = new List<UrbBehaviour>();

        UrbBehaviour[] Components = GetComponents<UrbBehaviour>();

        UrbTile[] Search = (ContactPrint == null) ? GetSearchTiles(true) : ContactPrint.GetAllPrintTiles(mAgent);

        int MaxIndex = Search.Length;
        for (int c = 0; c < Components.Length; c++)
        {
            if (Components[c].TileEvaluateCheck(mAgent.CurrentTile) > -1f)
            {
                BehaviourList.Add(Components[c]);
            }
        }
        mBehaviours = BehaviourList.ToArray();
    }

    protected IEnumerator ContactCheck()
    {
        UrbTile[] Search = (ContactPrint == null) ? GetSearchTiles(true) : ContactPrint.GetAllPrintTiles(mAgent);
 
        for(int b = 0; b < Behaviours.Length; b++)
        {
            Behaviours[b].ClearBehaviour();
        }

        yield return BehaviourThrottle;

        for (int i = 0; i < Search.Length; i++)
        {
            for(int b = 0; b < Behaviours.Length; b++)
            {
                float Evaluation = Behaviours[b].TileEvaluateCheck(Search[i]);
                if (Evaluation > float.Epsilon)
                {
                    Behaviours[b].RegisterTileForBehaviour(Evaluation, Search[i], i);
                }
            }
        }
    }
    protected IEnumerator SenseCheck()
    {
        if (SensePrint == null)
            yield break;

        yield return BehaviourThrottle;
    }

    public override IEnumerator FunctionalCoroutine()
    {
        yield return ContactCheck();
        yield return SenseCheck();
    }



    override public UrbComponentData GetComponentData()
    {
        UrbComponentData Data = new UrbComponentData
        {
            Type = this.GetType().ToString(),
        };

        Data.Strings = new UrbStringData[]
        {
            new UrbStringData{Name= "PerceptionPrintString", Value = PerceptionPrintString},
            new UrbStringData{Name= "ContactPrintString", Value = ContactPrintString}
        };
        return Data;
    }

    override public bool SetComponentData(UrbComponentData Data)
    {
        PerceptionPrintString = UrbEncoder.GetString("PerceptionPrintString", Data);
        ContactPrintString = UrbEncoder.GetString("ContactPrintString", Data);
        return true;
    }
}
