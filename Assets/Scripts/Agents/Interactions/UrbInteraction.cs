using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UrbTestCategory
{
    None = 0,
    Mobility,
    Bite,
    Bash,
    Slash
}

[System.Serializable]
public struct UrbTest
{
    public UrbTestCategory Category;

    public float DisplayTime;
    public Sprite SuccessIcon;
    public Color SuccessColor;
}

[System.Serializable]
public class UrbInteraction 
{
    protected virtual float MobilityTest(UrbBody TestBody)
    {
        float Result = 0.0f;
        float Muscle = TestBody.BodyComposition[UrbSubstanceTag.Muscle];
        float Nerves = TestBody.BodyComposition[UrbSubstanceTag.Nerves];
        Result += Mathf.Min(Muscle, Nerves)*2;

        return Result;
    }

    public virtual float CostEstimate(UrbAgent Instigator)
    {
        return 0;
    }

    protected float Test(UrbAgent target, UrbTestCategory Category , float Modifier = 0.0f)
    {
        UrbBody TestBody = target.Body;

        float Result = Modifier;
        switch(Category)
        {
            case UrbTestCategory.None:
                break;
            case UrbTestCategory.Mobility:
                Result += MobilityTest(TestBody);
                break;
            case UrbTestCategory.Bite:
                float Teeth = TestBody.BodyComposition[UrbSubstanceTag.Teeth];
                float Muscle = TestBody.BodyComposition[UrbSubstanceTag.Muscle];
                Result += Mathf.Min(Teeth, Muscle) * 2;
                break;
            case UrbTestCategory.Bash:
                Result += TestBody.BodyComposition[UrbSubstanceTag.All];
                break;
            case UrbTestCategory.Slash:
                Result += TestBody.BodyComposition[UrbSubstanceTag.Claw];
                break;
        }
        return Result;
    }

    public virtual bool AttemptInteraction(UrbAgent Instigator, UrbAgent Target, out float Result)
    {
        Debug.Log(this.GetType() + " Using Base AttemptInteraction");
        Result = -1;
        return true;
    }
}
