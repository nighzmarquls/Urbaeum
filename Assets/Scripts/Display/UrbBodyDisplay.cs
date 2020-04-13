using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DisplayModification
{
    public enum Type
    {
        None =      0b_0000_0000,
        Color =     0b_0000_0001,
        BodySize =  0b_0000_0010,
        FaceSize =  0b_0000_0100,
        Size =      BodySize | FaceSize,
        All =       Size | Color,

    }

    public enum ColorCategory
    {
        None =          0b_0000_0000,
        PrimaryFill =   0b_0000_0001,
        PrimaryLine =   0b_0000_0010,
        Primary =       PrimaryFill | PrimaryLine,
        SecondaryFill = 0b_0000_0100,
        SecondaryLine = 0b_0000_1000,
        Secondary =     SecondaryFill | SecondaryLine,
        DetailFill =    0b_0001_0000,
        DetailLine =    0b_0010_0000,
        Detail =        DetailFill | DetailLine,
        Face =          0b_0100_0000,
        Highlight =     0b_1000_0000,
        AllFill =       PrimaryFill | SecondaryFill | DetailFill,
        AllLine =       PrimaryLine | SecondaryLine | DetailLine,
        All =           Primary | Secondary | Detail | Face | Highlight,
    }


    public UrbSubstanceTag DisplayDrivingSubstance;
    public float SubstanceSizeRatio = 1000;
    public ColorCategory ColorsEffected;
    public Type ModificationType;
    public Color ColorApplied;
    public float SubstanceColorRatio = 1000;

    bool NeedCache = true;
    UrbDisplayColors CachedColors;

    public void ApplyModification(UrbDisplay Display, UrbComposition Composition)
    {
        if (Display.Invisible)
        {
            return;
        }
        float SubstanceAmount = (DisplayDrivingSubstance == UrbSubstanceTag.All) ? Composition.CurrentCapacty : Composition[DisplayDrivingSubstance];

        float SubstanceAlpha = (SubstanceSizeRatio < 0) ? 1.0f - (SubstanceAmount / Mathf.Abs(SubstanceSizeRatio) ) : SubstanceAmount / SubstanceSizeRatio;

        if ((ModificationType & Type.BodySize) == Type.BodySize)
        {
            Display.BodySize = SubstanceAlpha;
        }

        if ((ModificationType & Type.FaceSize) == Type.FaceSize)
        {
            Display.FaceSize = SubstanceAlpha;
        }

        if((ModificationType & Type.Color) == Type.Color)
        {
            ApplyColorModification(Display, SubstanceAmount);
        }

    }

    protected void ApplyColorModification(UrbDisplay Display, float SubstanceAmount)
    {
        if(NeedCache)
        {
            CachedColors = Display.Colors;
            NeedCache = false;
        }

        float Alpha = (SubstanceColorRatio < 0) ? 1.0f - (SubstanceAmount / Mathf.Abs(SubstanceColorRatio)) : SubstanceAmount / SubstanceColorRatio;

        bool LineCheck = (ColorsEffected & ColorCategory.AllLine) > 0;
        bool FillCheck = (ColorsEffected & ColorCategory.AllFill) > 0;

        if((ColorsEffected & ColorCategory.Primary) > 0)
        {
            UrbColor AssignedColor = new UrbColor
            {
                Line = (LineCheck)? Color.Lerp(CachedColors.PrimaryColor.Line, ColorApplied, Alpha) : CachedColors.PrimaryColor.Line,
                Fill = (FillCheck)? Color.Lerp(CachedColors.PrimaryColor.Fill, ColorApplied, Alpha) : CachedColors.PrimaryColor.Fill
            };
            Display.PrimaryColor = AssignedColor;
        }

        if ((ColorsEffected & ColorCategory.Secondary) > 0)
        {
            UrbColor AssignedColor = new UrbColor
            {
                Line = (LineCheck) ? Color.Lerp(CachedColors.SecondaryColor.Line, ColorApplied, Alpha) : CachedColors.SecondaryColor.Line,
                Fill = (FillCheck) ? Color.Lerp(CachedColors.SecondaryColor.Fill, ColorApplied, Alpha) : CachedColors.SecondaryColor.Fill
            };
            Display.SecondaryColor = AssignedColor;
        }

        if ((ColorsEffected & ColorCategory.Detail) > 0)
        {
            UrbColor AssignedColor = new UrbColor
            {
                Line = (LineCheck) ? Color.Lerp(CachedColors.DetailColor.Line, ColorApplied, Alpha) : CachedColors.DetailColor.Line,
                Fill = (FillCheck) ? Color.Lerp(CachedColors.DetailColor.Fill, ColorApplied, Alpha) : CachedColors.DetailColor.Fill
            };
            Display.DetailColor = AssignedColor;
        }
    }
}

[RequireComponent(typeof(UrbAgent))]
public class UrbBodyDisplay : UrbBase
{
    public DisplayModification[] Modifications;

    UrbAgent mAgent;
    UrbDisplay mDisplay;

    public override void Initialize()
    {
        if(bInitialized)
        {
            return;
        }
        mAgent = GetComponent<UrbAgent>();

        base.Initialize();
    }

    public void UpdateDisplay(UrbComposition input)
    {
        if (input == null)
        {
            return;
        }

        if (mDisplay == null)
        {
            mDisplay = mAgent.DisplayObject;
        }
        else
        {
            if (mDisplay.Invisible)
            {
                return;
            }
            for (int s = 0; s < Modifications.Length; s++)
            {
                Modifications[s].ApplyModification(mDisplay, input);
            }
        }
        
    }

    public override UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();
        //TODO: Save the rule data
        return Data;
    }

    public override bool SetComponentData(UrbComponentData Data)
    {
        //TODO: Load the rule data
        return base.SetComponentData(Data);
    }
}
