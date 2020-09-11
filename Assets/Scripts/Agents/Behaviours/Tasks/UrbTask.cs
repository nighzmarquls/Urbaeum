using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UrbTask
{
    public virtual string Name { get; set; } = "Job";
    public virtual string Description { get; set; } = "Base Job";
    public virtual UrbUrgeCategory SatisfiedOnCompletion { get; set; } = UrbUrgeCategory.Work;

    public bool Matches(System.Type TaskType)
    {
        return this.GetType().IsSubclassOf(TaskType) || this.GetType() == TaskType;
    }

    public abstract float PerformTask(UrbAgent Instigator, UrbTile WorkTile);
    public abstract float TaskCompletionCostEstimate(UrbAgent Instigator, UrbTile WorkTile);
    public abstract float TaskSuitableTileCheck(UrbTile Input);
    public abstract bool TaskValid(UrbAgent TaskedAgent);
    public abstract bool TaskComplete();
}