using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbBehaviour : UrbBase
{
    [SerializeField]
    protected float Interval = 1.0f;
    private IEnumerator mCoroutine;
    protected UrbAgent mAgent;

    public virtual UrbUrgeCategory UrgeSatisfied { get { return UrbUrgeCategory.None; } }

    public virtual bool ShouldInterval { get { return true; } }

    public virtual bool ContactBehaviour { get { return true; } }
    public virtual bool SenseBehaviour { get { return false; } }
    public virtual bool DeathBehaviour { get { return false; } }

    public void PauseBehaviour()
    {
        if (mCoroutine == null || !isActiveAndEnabled)
            return;

        StopCoroutine(mCoroutine);
    }

    public void ResumeBehaviour()
    {
        if (mCoroutine == null || !isActiveAndEnabled)
            return;

        StartCoroutine(mCoroutine);
    }

    public override void OnEnable()
    {
        mAgent = GetComponent<UrbAgent>();
        mCoroutine = IntervalCoroutine();
        Eater = GetComponent<UrbEater>();
        IsEater = Eater != null;
        
        base.OnEnable();
        if (ShouldInterval && isActiveAndEnabled)
        {
            StartCoroutine(mCoroutine);
        }
        if(DeathBehaviour)
        {
            mAgent.AddDeathBehaviour(this);
        }
    }

    UrbTile LastOriginTile = null;
    UrbTile[] CachedSearchTile = null;

    protected UrbTile[] GetSearchTiles(bool GetLinked)
    {
        if (LastOriginTile == null || LastOriginTile != mAgent.CurrentTile)
        {
            UrbTile[] Self = mAgent.Tileprint.GetAllPrintTiles(mAgent);

            UrbTile[] Adjacent = mAgent.Tileprint.GetBorderingTiles(mAgent, GetLinked);

            UrbTile[] SearchTiles = new UrbTile[Adjacent.Length + Self.Length];

            Self.CopyTo(SearchTiles, 0);

            Adjacent.CopyTo(SearchTiles, Self.Length);

            LastOriginTile = mAgent.CurrentTile;
            CachedSearchTile = SearchTiles;
        }
        return CachedSearchTile;
    }

    protected UrbTile[] RegisteredTiles;

    public virtual float TileEvaluateCheck(UrbTile Target, bool Contact = false)
    {
        return -1f;
    }
    
    protected void ExpandRegisteredTiles(int Index)
    {
        UrbTile[] ExpandedArray = new UrbTile[Index + 1];
        RegisteredTiles.CopyTo(ExpandedArray, 0);
        RegisteredTiles = ExpandedArray;
    }
    
    static ProfilerMarker s_RegisterTileBehaviour_p = new ProfilerMarker("UrbBehavior.RegisterTileBehaviour");
    public virtual void RegisterTileForBehaviour(float Evaluation ,UrbTile Target, int Index)
    {
        s_RegisterTileBehaviour_p.Begin();
        if(RegisteredTiles == null)
        {
            RegisteredTiles = new UrbTile[Index+1];
        }

        if(Index >= RegisteredTiles.Length)
        {
            ExpandRegisteredTiles(Index);
        }

        RegisteredTiles[Index] = Target;
        BehaviourEvaluation += Evaluation;
        
        s_RegisterTileBehaviour_p.End();

    }

    static ProfilerMarker s_ClearBehavior_p = new ProfilerMarker("UrbBehavior.ClearBehaviour");

    UrbTile LastBehaviourOrigin = null;
    public virtual void ClearBehaviour()
    {
        s_ClearBehavior_p.Begin();
        if(LastBehaviourOrigin == null || LastBehaviourOrigin != mAgent.CurrentTile)
        {
            if (RegisteredTiles != null)
            {
                for (int t = 0; t < RegisteredTiles.Length; t++)
                {
                    RegisteredTiles[t] = null;
                }
            }
            LastBehaviourOrigin = mAgent.CurrentTile;
         
        }
        BehaviourEvaluation = 0;
        s_ClearBehavior_p.End();
    }

    public virtual float BehaviourEvaluation { get; protected set; }

    public virtual void ExecuteTileBehaviour()
    {
        ClearBehaviour();
    }

    protected virtual bool ValidToInterval()
    {
        return !mAgent.WasDestroyed && mAgent.isActiveAndEnabled && mAgent.CurrentMap != null;
    }

    protected UrbUtility.UrbThrottle BehaviourThrottle = new UrbUtility.UrbThrottle();

    static ProfilerMarker s_IntervalCoroutine_p = new ProfilerMarker("UrbBehavior.FunctionalCoroutine");

    protected float behaviourInterval;
    
    public IEnumerator IntervalCoroutine()
    {
        IEnumerator retVal;

        while (true)
        {
            behaviourInterval = Interval * mAgent.TimeMultiplier;
            if (ValidToInterval())
            {
                //The profiler has a tendency to freak out w/ yields between marked points 
                s_IntervalCoroutine_p.Begin();
                retVal = FunctionalCoroutine();
                s_IntervalCoroutine_p.End();
                yield return retVal;
            }

            //yield return BehaviourThrottle.PerformanceThrottle();
            yield return new WaitForSeconds(behaviourInterval);
        }
    }

    public virtual IEnumerator FunctionalCoroutine()
    {
        yield return null;
    }
}
