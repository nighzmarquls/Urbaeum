using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbBehaviour : UrbBase
{
    [SerializeField]
    protected float Interval = 1.0f;
    private IEnumerator mCoroutine;
    protected UrbAgent mAgent;

    public virtual bool ShouldInterval { get { return true; } }
    public virtual UrbUrgeCategory UrgeSatisfied { get { return UrbUrgeCategory.None; } }
    public virtual bool ContactBehaviour { get { return true; } }
    public virtual bool SenseBehaviour { get { return false; } }

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

    override public void Initialize()
    {
        if (bInitialized)
        {
            return;
        }
        mAgent = GetComponent<UrbAgent>();
        mCoroutine = IntervalCoroutine();
        
        base.Initialize();
        if (ShouldInterval && isActiveAndEnabled)
        {
            StartCoroutine(mCoroutine);
        }
    }

    UrbTile LastOriginTile = null;
    UrbTile[] CachedSearchTile = null;

    virtual protected UrbTile[] GetSearchTiles(bool GetLinked)
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

    virtual public float TileEvaluateCheck(UrbTile Target, bool Contact = false)
    {
        return -1f;
    }

    protected void ExpandRegisteredTiles(int Index)
    {
        UrbTile[] ExpandedArray = new UrbTile[Index + 1];
        RegisteredTiles.CopyTo(ExpandedArray, 0);
        RegisteredTiles = ExpandedArray;
    }

    virtual public void RegisterTileForBehaviour(float Evaluation ,UrbTile Target, int Index)
    {
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
    }

    UrbTile LastBehaviourOrigin = null;
    virtual public void ClearBehaviour()
    {
        if(LastBehaviourOrigin == null || LastBehaviourOrigin != mAgent.CurrentTile)
        {
            if (RegisteredTiles != null)
            {
                for (int t = 0; t < RegisteredTiles.Length; t++)
                {
                    if (RegisteredTiles[t] == null)
                    {
                        continue;
                    }
                    RegisteredTiles[t] = null;
                }
            }
            LastBehaviourOrigin = mAgent.CurrentTile;
         
        }
        BehaviourEvaluation = 0;
    }

    public virtual float BehaviourEvaluation { get; protected set; }

    virtual public void ExecuteTileBehaviour()
    {
        ClearBehaviour();
    }

    virtual protected bool ValidToInterval()
    {
        return mAgent != null && mAgent.CurrentMap != null;
    }

    protected UrbUtility.UrbThrottle BehaviourThrottle = new UrbUtility.UrbThrottle();

    public IEnumerator IntervalCoroutine()
    {
        while (true)
        {
            if (ValidToInterval() && mAgent != null)
            {
                yield return BehaviourThrottle.PerformanceThrottle();
                yield return FunctionalCoroutine();
            }

            yield return new WaitForSeconds(Interval * mAgent.TimeMultiplier);
        }
    }

    virtual public IEnumerator FunctionalCoroutine()
    {
        yield return null;
    }
    override public UrbComponentData GetComponentData()
    {
        return base.GetComponentData();
    }
}
