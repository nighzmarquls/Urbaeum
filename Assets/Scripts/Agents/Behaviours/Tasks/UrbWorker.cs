using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class UrbWorker : UrbBehaviour
{
    public override UrbUrgeCategory UrgeSatisfied => JobsSatisfy();
    public override bool ShouldInterval => false;
    public override bool LivingBehaviour => true;
    public override bool ContactBehaviour => true;

    public uint MaxTasks = 1;
    public List<UrbPlanStep> WorkerTasks { get; protected set; }
    bool TasksChanged = true;

    UrbUrgeCategory CachedUrges;
    protected UrbUrgeCategory JobsSatisfy ()
    {
        if (TasksChanged)
        {
            UrbUrgeCategory ValidUrges = base.UrgeSatisfied;
            for (int j = 0; j < WorkerTasks.Count; j++)
            {
                ValidUrges = (WorkerTasks[j].Task.TaskValid(mAgent)) ? ValidUrges | WorkerTasks[j].Task.SatisfiedOnCompletion : ValidUrges;
            }
            CachedUrges = ValidUrges;
            TasksChanged = false;
        }
        return CachedUrges;
    }

    public void GetTaskFromPlan(UrbPlan Plan)
    {
        if(WorkerTasks.Count >= MaxTasks)
        {
            return;
        }

       if(Plan.GetCurrentStep(out UrbPlanStep PlanStep))
        {
            if(PlanStep.Task.TaskValid(mAgent))
            {
                if(WorkerTasks.Count == 0)
                {
                    WorkerTasks.Add(PlanStep);
                }
                else if(! WorkerTasks.Contains(PlanStep))
                {
                    WorkerTasks.Add(PlanStep);
                }
            }
        }
    }

    static ProfilerMarker s_TileEvaluateCheck_p = new ProfilerMarker("UrbWorker.TileEvaluateCheck");

    public override float TileEvaluateCheck(UrbTile Target, bool Contact = false)
    {
       float value = 0;
       for(int j = 0; j < WorkerTasks.Count; j++)
       {
            value += WorkerTasks[j].Task.TaskSuitableTileCheck(Target);
       }
       return value;
    }
}
