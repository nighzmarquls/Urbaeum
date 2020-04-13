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

    public void PauseBehaviour()
    {
        if (mCoroutine == null)
            return;

        StopCoroutine(mCoroutine);
    }

    public void ResumeBehaviour()
    {
        if (mCoroutine == null)
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
        StartCoroutine(mCoroutine);
        base.Initialize();
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

            yield return new WaitForSeconds(Interval);
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
