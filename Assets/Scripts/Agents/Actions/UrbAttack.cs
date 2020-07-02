using UnityEngine;
using System.Collections;
using Unity.Profiling;

public class UrbAttack : UrbAction
{
    protected virtual UrbTestCategory DefenseCategory => UrbTestCategory.Mobility | UrbTestCategory.Defense;
    protected virtual UrbDamage DamageAction { get; set; } = new UrbDamage();

    public override UrbTestCategory Category => UrbTestCategory.Attack | DamageAction.Category;

    static ProfilerMarker s_AttackAction_p = new ProfilerMarker("UrbAttack.Execute");

    public override float Execute(UrbAgent Instigator, UrbAgent Target, float Modifier = 0)
    {
        if (Instigator.WasDestroyed || Target.WasDestroyed)
        {
            return 0;
        }

        using (s_AttackAction_p.Auto())
        {
            float Result = base.Execute(Instigator, Target, Modifier);

            if (!(Result > 0))
            {
                return Result;
            }

            UrbAction DefenseAction = Target.PickAction(DefenseCategory, Result);

            if (DefenseAction != null)
            {
                Result -= DefenseAction.Execute(Target, Instigator);
            }

            if (Result > 0)
            {
                Result = DamageAction.Execute(Instigator, Target, Result);
            }

            return Result;
        }
    }

    public override void DisplayActionIcon(UrbAgent Target, Vector3 Location)
    {
        if (Target.Display == null || ActionIcon == null)
        {
            return;
        }
        Target.Display.QueueEffectDisplay(this, Location, true);
    }
}

