using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UrbTask
{
    public virtual string Name { get; set; } = "Job";
    public virtual string Description { get; set; } = "Base Job";
    public virtual UrbUrgeCategory SatisfiedOnCompletion { get; set; } = UrbUrgeCategory.Work;

    public abstract bool Matches(System.Type required);
    public abstract float TaskCompletionEstimate(UrbAgent Instigator, UrbTile WorkTile);
    public abstract float TaskSuitableTileCheck(UrbTile Input);
    public abstract bool TaskValid();
}