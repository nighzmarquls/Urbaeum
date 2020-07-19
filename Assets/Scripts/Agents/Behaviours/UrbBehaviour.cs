using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbBehaviour : UrbBase
{
    [SerializeField]
    protected float Interval = 1.0f;
    private IEnumerator mCoroutine;

    public virtual UrbUrgeCategory UrgeSatisfied { get { return UrbUrgeCategory.None; } }

    public virtual bool ShouldInterval { get { return true; } }
    public virtual bool LivingBehaviour { get { return true; } }

    public virtual bool ContactBehaviour { get { return true; } }
    public virtual bool SenseBehaviour { get { return false; } }
    public virtual bool DeathBehaviour { get { return false; } }

    public void PauseBehaviour()
    {
        Assert.IsNotNull(mCoroutine);
        Assert.IsTrue(isActiveAndEnabled);
        Assert.IsFalse(IsPaused);

        StopCoroutine(mCoroutine);

        IsPaused = true;
    }

    public void ResumeBehaviour()
    {
        Assert.IsNotNull(mCoroutine);
        Assert.IsTrue(isActiveAndEnabled);
        Assert.IsTrue(IsPaused);

        StartCoroutine(mCoroutine);
        
        IsPaused = false;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        //ensure that base OnEnable is called before we start running
        //the coroutines.
        mCoroutine = IntervalCoroutine();

        if (ShouldInterval && isActiveAndEnabled)
        {
            StartCoroutine(mCoroutine);
        }

        if(DeathBehaviour)
        {
            mAgent.AddDeathBehaviour(this);
        }
    }

    protected override void OnDisable()
    {
        if (mCoroutine != null)
        {
            StopCoroutine(mCoroutine);
        }
        
        if(DeathBehaviour)
        {
            mAgent.RemoveDeathBehaviour(this);
        }

        base.OnDisable();
    }

    UrbTile LastOriginTile = null;
    bool CacheLinked = false;
    UrbTile[] CachedSearchTile = null;
    UrbTile[] CachedSelfTile = null;
    UrbTile[] CachedAdjacent = null;

    protected bool CacheCheck(bool GetLinked)
    {
        return LastOriginTile == null || LastOriginTile != mAgent.CurrentTile || GetLinked != CacheLinked;
    }

    protected void CacheTiles(bool GetLinked)
    {
        UrbTile[] Self = mAgent.Tileprint.GetAllPrintTiles(mAgent);

        UrbTile[] Adjacent = mAgent.Tileprint.GetBorderingTiles(mAgent, GetLinked);

        UrbTile[] SearchTiles = new UrbTile[Adjacent.Length + Self.Length];

        Self.CopyTo(SearchTiles, 0);

        Adjacent.CopyTo(SearchTiles, Self.Length);

        LastOriginTile = mAgent.CurrentTile;
        CachedSearchTile = SearchTiles;
        CachedSelfTile = Self;
        CachedAdjacent = Adjacent;
    }

    protected UrbTile[] GetAdjacentTiles(bool GetLinked)
    {
        if (CacheCheck(GetLinked))
        {
            CacheTiles(GetLinked);
        }
        return CachedAdjacent;
    }

    protected UrbTile[] GetSelfTiles(bool GetLinked)
    {
        if (CacheCheck(GetLinked))
        {
            CacheTiles(GetLinked);
        }
        return CachedSelfTile;
    }

    protected UrbTile[] GetSearchTiles(bool GetLinked)
    {
        if (CacheCheck(GetLinked))
        {
            CacheTiles(GetLinked);
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

    public override void Update()
    {
        if (ShouldPause != IsPaused)
        {
            if (ShouldPause)
            {
                PauseBehaviour();
            }
            else
            {
                ResumeBehaviour();
            }
        }
        base.Update();
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
        return mAgent.WasDestroyed || !mAgent.isActiveAndEnabled || mAgent.CurrentMap == null || (!LivingBehaviour) || mAgent.Alive;
    }

    protected UrbUtility.UrbThrottle BehaviourThrottle = new UrbUtility.UrbThrottle();

    static ProfilerMarker s_IntervalCoroutine_p = new ProfilerMarker("UrbBehavior.FunctionalCoroutine");

    protected float behaviourInterval = 0.2f;
    
    public IEnumerator IntervalCoroutine()
    {
        IEnumerator retVal;

        while (true)
        {
            //yield return BehaviourThrottle.PerformanceThrottle();
            yield return new WaitForSeconds(behaviourInterval);
            
            behaviourInterval = Interval;
            if (ValidToInterval())
            {
                //The profiler has a tendency to freak out w/ yields between marked points 
                s_IntervalCoroutine_p.Begin();
                retVal = FunctionalCoroutine();
                s_IntervalCoroutine_p.End();
                yield return retVal;
            }
        }
    }

    public virtual IEnumerator FunctionalCoroutine()
    {
        yield return null;
    }
}
