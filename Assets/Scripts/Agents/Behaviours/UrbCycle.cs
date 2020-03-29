using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UrbCycleStage
{
    public float CycleDuration = 1.0f;
    public UrbBehaviour[] ActivateOnCycle;
    public UrbBehaviour[] DeactivateOnCycle;
    public bool Repeat = true;
    public bool Skip = false;
}

public class UrbCycle : UrbBehaviour
{
    public UrbCycleStage[] Stages;
    public bool Repeat = true;
    int CurrentCycle = 0;
    float TimeToActivate = -1f;

    public void ProcessCycle()
    {
        UrbCycleStage NewStage = Stages[CurrentCycle];
        if (NewStage.Skip)
        {
            CurrentCycle++;
            return;
        }

        for(int i = 0; i < NewStage.ActivateOnCycle.Length; i++)
        {
            NewStage.ActivateOnCycle[i].enabled = true;
            NewStage.ActivateOnCycle[i].ResumeBehaviour();
        }

        for(int i = 0; i < NewStage.DeactivateOnCycle.Length; i++)
        {
            NewStage.DeactivateOnCycle[i].enabled = false;
            NewStage.DeactivateOnCycle[i].PauseBehaviour();
        }
        TimeToActivate = Time.time + NewStage.CycleDuration;
        CurrentCycle++;

        if(!NewStage.Repeat)
        {
            NewStage.Skip = true;
        }
        if (CurrentCycle >=  Stages.Length)
        {
            if (Repeat)
            {
                CurrentCycle = 0;
            }
            else
            {
                Destroy(this);
            }
        }
    }

    override public void Initialize()
    {
        if (bInitialized)
        {
            return;
        }
       
        if (isActiveAndEnabled)
        {
            for(int i = 0; i < Stages.Length; i ++)
            {
                Stages[i].Skip = false;
            }
            CurrentCycle = 0;
            UrbCycleStage NewStage = Stages[CurrentCycle];
            for (int i = 0; i < NewStage.ActivateOnCycle.Length; i++)
            {
                NewStage.ActivateOnCycle[i].enabled = true;
                NewStage.ActivateOnCycle[i].ResumeBehaviour();
            }

            for (int i = 0; i < NewStage.DeactivateOnCycle.Length; i++)
            {
                NewStage.DeactivateOnCycle[i].enabled = false;
                NewStage.DeactivateOnCycle[i].PauseBehaviour();
            }
        }
        base.Initialize();
    }


    override protected bool ValidToInterval()
    {
        return base.ValidToInterval() && Stages != null && Stages.Length > 0;
    }

    override public IEnumerator FunctionalCoroutine()
    {
        if(Time.time > TimeToActivate)
        {
            ProcessCycle();
        }
        yield return null;
    }
}
