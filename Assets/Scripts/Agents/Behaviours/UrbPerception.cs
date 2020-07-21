using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Profiling;
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
    bool contactsSynced;
    UrbBehaviour[] Components;
    public override void OnEnable()
    {
        Components = GetComponents<UrbBehaviour>();
        contactsSynced = false;
        base.OnEnable();
    }

    public override void OnDisable()
    {
        contactsSynced = false;
        base.OnDisable();
    }
    
    //The reason this is here is because it has a dependency which is
    //not necessarily available until after OnEnable, etc, have been called. 
    protected void SyncContacts()
    {
        List<UrbBehaviour> ContactList = new List<UrbBehaviour>();
        List<UrbBehaviour> SenseList = new List<UrbBehaviour>();

        UrbTile currentTile = mAgent.CurrentTile; 
        Assert.IsNotNull(currentTile, $"AgentLocalName: {mAgent.AgentLocalName}, ID: {mAgent.ID}");
        
        for (int c = 0; c < Components.Length; c++)
        {
            if (!(Components[c].TileEvaluateCheck(currentTile) > -1f))
            {
                continue;
            }

            if (Components[c].ContactBehaviour)
            {
                ContactList.Add(Components[c]);
            }
            else if (Components[c].SenseBehaviour)
            {
                SenseList.Add(Components[c]);
            }
        }
        cBehaviours = ContactList.ToArray();
        sBehaviours = SenseList.ToArray();
        ContactList.Clear();
        
        contactsSynced = true;
    }
    
    static ProfilerMarker s_ContactCheck_p = new ProfilerMarker("UrbPerception.ContactCheck_afterThrottle");

    UrbTile[] ContactSearchCache;
    UrbTile LastContactTile = null;
    float Evaluation;
    protected IEnumerator ContactCheck()
    {
        //This happens if ContactCheck gets called too soon
        if (mAgent.CurrentTile == null)
        {
            yield return new WaitUntil(() => mAgent.CurrentTile != null);
            Assert.IsNotNull(mAgent.CurrentTile);
        }

        if (!contactsSynced && Components != null)
        {
            SyncContacts();
            yield return BehaviourThrottle;
        }
        
        if (LastContactTile != mAgent.CurrentTile)
        {
            LastContactTile = mAgent.CurrentTile;
            if (ContactPrint == null)
            {
                ContactSearchCache = GetSearchTiles(true);
            }
            else
            {
                ContactSearchCache = ContactPrint.GetAllPrintTiles(mAgent);
            }
        }

        for(int b = 0; b < ContactBehaviours.Length; b++)
        {
            ContactBehaviours[b].ClearBehaviour();
        }

        yield return BehaviourThrottle;

        s_ContactCheck_p.Begin();
        UrbTile tile;
        for (int i = 0; i < ContactSearchCache.Length; i++)
        {
            tile = ContactSearchCache[i];
            if (tile == null)
            {
                continue;
            }

            for (int b = 0; b < ContactBehaviours.Length; b++)
            {
                Evaluation = ContactBehaviours[b].TileEvaluateCheck(tile, true);
                if (Evaluation > float.Epsilon)
                {
                    ContactBehaviours[b].RegisterTileForBehaviour(Evaluation, tile, i);
                }
            }

            s_ContactCheck_p.End();
            yield return BehaviourThrottle;
            s_ContactCheck_p.Begin();
        }

        s_ContactCheck_p.End();
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

        UrbTile tile;
        for (int i = 0; i < SenseSearchCache.Length; i++)
        {
            tile = SenseSearchCache[i];
            if (tile == null)
            {
                continue;
            }
            for (int b = 0; b < SenseBehaviours.Length; b++)
            {
                float Evaluation = SenseBehaviours[b].TileEvaluateCheck(tile, true);
                if (Evaluation > float.Epsilon)
                {
                    SenseBehaviours[b].RegisterTileForBehaviour(Evaluation, tile, i);
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



    public override UrbComponentData GetComponentData()
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

    public override bool SetComponentData(UrbComponentData Data)
    {
        PerceptionPrintString = UrbEncoder.GetString("PerceptionPrintString", Data);
        ContactPrintString = UrbEncoder.GetString("ContactPrintString", Data);
        return true;
    }
}
