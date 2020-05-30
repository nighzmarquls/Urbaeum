using UnityEngine;
using System.Collections;

public class UrbDamage : UrbAction
{
    protected virtual UrbSubstanceTag DamageSubstance { get{ return UrbSubstanceTag.All; } }
    public override UrbTestCategory Category => UrbTestCategory.Bash;

    public override float Execute(UrbAgent Instigator, UrbAgent Target, float Modifier = 0)
    {
        float Result = Modifier;

        Result = Target.Body.BodyComposition.Membrane.Impact(Instigator.Body.BodyComposition, DamageSubstance, Result);

        DisplayActionIcon(Instigator, Target.Location);
        return Modifier;
    }
}
