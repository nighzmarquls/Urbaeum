using UnityEngine;
using System.Collections;

public class UrbDamage : UrbAction
{
    public override UrbTestCategory Category => UrbTestCategory.Bash;

    public override float Execute(UrbAgent Instigator, UrbAgent Target, float Modifier = 0)
    {
        float Result = Modifier;

        //Target.Body.BodyComposition;

        DisplayActionIcon(Instigator, Target.Location);
        return Modifier;
    }
}
