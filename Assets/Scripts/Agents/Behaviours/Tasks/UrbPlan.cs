﻿using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEngine;

public struct UrbPlanStep
{
    public UrbTask Task;
    public uint Index;
    public UrbPlan RequiredPlan;
}

[RequireComponent(typeof(UrbSmellSource))]
public abstract class UrbPlan : UrbBase
{
    public bool Complete { get; protected set; } = false;
    public virtual UrbUrgeCategory SatisfiedOnCompletion { get; set; } = UrbUrgeCategory.Work;
    public abstract float EffortCheck();
    public abstract float TimeCheck();

    protected static UrbPlanStep Finished = new UrbPlanStep { };

    public bool GetCurrentStep (out UrbPlanStep Output)
    {
        if(Complete)
        {
            Output = Finished;
            return false;
        }
        Assert.IsFalse(FullPlan == null);
        Assert.IsTrue(Index < FullPlan.Length);
        Output = FullPlan[Index];
        return true;
    }

    public bool CompleteStep(uint StepIndex)
    {
        if (FullPlan[StepIndex].Task.TaskComplete())
        {
            if (Index == StepIndex)
            {
                ProgressPlan();
            }

            return true;
        }
        
        return false;
    }

    protected void ProgressPlan()
    {
        if(Index < FullPlan.Length)
        {
            Index++;
        }
        if(Index >= FullPlan.Length)
        {
            Complete = true;
        }
    }

    protected uint Index;
    protected UrbPlanStep[] FullPlan;
}