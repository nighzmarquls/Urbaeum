using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbWorker : UrbBehaviour
{
    public override UrbUrgeCategory UrgeSatisfied => JobsSatisfy();
    public override bool ShouldInterval => false;
    public override bool LivingBehaviour => true;
    public override bool ContactBehaviour => true;

    public UrbTask[] WorkerTasks;
    bool TasksChanged = true;

    UrbUrgeCategory CachedUrges;
    protected UrbUrgeCategory JobsSatisfy ()
    {
        if (TasksChanged)
        {
            UrbUrgeCategory ValidUrges = base.UrgeSatisfied;
            for (int j = 0; j < WorkerTasks.Length; j++)
            {
                ValidUrges = (WorkerTasks[j].TaskValid()) ? ValidUrges | WorkerTasks[j].SatisfiedOnCompletion : ValidUrges;
            }
            CachedUrges = ValidUrges;
            TasksChanged = false;
        }
        return CachedUrges;
    }
}
