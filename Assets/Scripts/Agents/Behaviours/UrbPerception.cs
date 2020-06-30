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

            contactprint = new UrbTileprint(ContactPrintString);
            
            return contactprint;
        }
    }

    protected UrbBehaviour[] cBehaviours;
    public UrbBehaviour[] ContactBehaviours { get {
            if (cBehaviours == null)
            {
                return new UrbBehaviour[0];
            }
            return cBehaviours;
        }
    }

    protected UrbBehaviour[] sBehaviours;
    public UrbBehaviour[] SenseBehaviours {
        get {
            if (sBehaviours == null)
            {
                return new UrbBehaviour[0];
            }
            return sBehaviours;
        }
    }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }
       
        base.Initialize();
        List<UrbBehaviour> ContactList = new List<UrbBehaviour>();
        List<UrbBehaviour> SenseList = new List<UrbBehaviour>();

        UrbBehaviour[] Components = GetComponents<UrbBehaviour>();

        for (int c = 0; c < Components.Length; c++)
        {
            if (Components[c].TileEvaluateCheck(mAgent.CurrentTile) > -1f && Components[c].ContactBehaviour)
            {
                ContactList.Add(Components[c]);
            }
            if (Components[c].TileEvaluateCheck(mAgent.CurrentTile) > -1f && Components[c].SenseBehaviour)
            {
                SenseList.Add(Components[c]);
            }
        }
        cBehaviours = ContactList.ToArray();
        sBehaviours = SenseList.ToArray();
        ContactList.Clear();
    }

    UrbTile[] ContactSearchCache;
    UrbTile LastContactTile = null;
    float Evaluation;
    protected IEnumerator ContactCheck()
    {
        if (LastContactTile != mAgent.CurrentTile)
        {
            LastContactTile = mAgent.CurrentTile;
            ContactSearchCache = (ContactPrint == null) ? GetSearchTiles(true) : ContactPrint.GetAllPrintTiles(mAgent);
        }
        for(int b = 0; b < ContactBehaviours.Length; b++)
        {
            ContactBehaviours[b].ClearBehaviour();
        }

        yield return BehaviourThrottle;

        for (int i = 0; i < ContactSearchCache.Length; i++)
        {
            for(int b = 0; b < ContactBehaviours.Length; b++)
            {
                Evaluation = ContactBehaviours[b].TileEvaluateCheck(ContactSearchCache[i], true);
                if (Evaluation > float.Epsilon)
                {
                    ContactBehaviours[b].RegisterTileForBehaviour(Evaluation, ContactSearchCache[i], i);
                }
            }
        }
    }

    UrbTile[] SenseSearchCache;
    UrbTile LastSenseTile = null;
    protected IEnumerator SenseCheck()
    {
        if (SensePrint == null)
            yield break;

        if (LastSenseTile != mAgent.CurrentTile)
        {
            LastSenseTile = mAgent.CurrentTile;
            SenseSearchCache = SensePrint.GetAllPrintTiles(mAgent);
        }

        for (int b = 0; b < SenseBehaviours.Length; b++)
        {
            SenseBehaviours[b].ClearBehaviour();
        }

        yield return BehaviourThrottle;

        for (int i = 0; i < SenseSearchCache.Length; i++)
        {
            for (int b = 0; b < SenseBehaviours.Length; b++)
            {
                float Evaluation = SenseBehaviours[b].TileEvaluateCheck(SenseSearchCache[i], true);
                if (Evaluation > float.Epsilon)
                {
                    SenseBehaviours[b].RegisterTileForBehaviour(Evaluation, SenseSearchCache[i], i);
                }
            }
        }

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
