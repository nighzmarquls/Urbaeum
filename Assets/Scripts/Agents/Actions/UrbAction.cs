using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Flags]
public enum UrbTestCategory
{
    None = 0,
    Mobility = 1,
    Defense = 2,
    Attack = 4,
    Bash = 8,
    Cut = 16,
    Pierce = 32,
}

[System.Serializable]
public class UrbAction
{
    
    protected Sprite CachedSprite = null;
    public Sprite ActionIcon    { get{
            if(string.IsNullOrEmpty(IconPath))
            {
                return null;
            }
            if (CachedSprite != null)
            {
                return CachedSprite;
            }
            CachedSprite = Resources.Load<Sprite>(IconPath);
            return CachedSprite;
        }
    }
    protected const string IconDiretory = "Sprites/Icons/";

    public virtual UrbTestCategory Category { get { return UrbTestCategory.None; } }
    public virtual float DisplayTime        { get { return 0.1f; } }
    protected virtual string IconPath       { get { return ""; } }
    public virtual Color IconColor          { get { return Color.white; } }

    public virtual float CostEstimate(UrbAgent Instigator)
    {
        return Test(Instigator)*UrbMetabolism.EnergyConversionRatio;
    }

    protected static float MobilityTest(UrbBody TestBody)
    {
        float Result = 0.0f;

        if (!TestBody.enabled)
        {
            return Result;
        }
            
        float Muscle = TestBody.BodyComposition[UrbSubstanceTag.Muscle];
        float Nerves = TestBody.BodyComposition[UrbSubstanceTag.Nerves];
        Result = Mathf.Min(Muscle, Nerves) * 2;
        float BodyRatio = Muscle / TestBody.BodyComposition.Mass;
        return Result * BodyRatio;
    }
    public virtual float Test(UrbAgent target, float Modifier = 0.0f)
    {
        UrbBody TestBody = target.Body;
        Debug.Log(this.GetType() + " Using Default Test");
        float Result = Modifier;
        
        return Result;
    }

    public virtual float Execute(UrbAgent Instigator, UrbAgent Target, float Modifier = 0.0f)
    {
        if (Instigator.WasDestroyed || Target.WasDestroyed)
        {
            return 0.0f;
        }

        float Result = Modifier;
        Result += Test(Instigator, Modifier);
        Result = Instigator.Body.UtilizeBody(Result);
        if (Result > 0)
        {
            DisplayActionIcon(Instigator, Instigator.Location);
            if(Instigator.Metabolism != null)
            {
                Instigator.Metabolism.SpendEnergy(Result);
            }
        }
        return Result;
    }
    
    public virtual void DisplayActionIcon(UrbAgent Target, Vector3 Location)
    {
        if (Target.Display == null || ActionIcon == null)
        {
            return;
        }
        Target.Display.QueueEffectDisplay(this, Location, false);
    }
}
