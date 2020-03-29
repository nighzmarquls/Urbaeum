using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpriteOnState
{
    public enum ThresholdRule
    {
        Less= 0,
        Equal,
        More
    }
    public Sprite DisplaySprite;
    public ThresholdRule Rule = ThresholdRule.More;
    public UrbSubstance[] StateThreshold;
}

[System.Serializable]
public struct TargetSpriteRenderer
{
    public SpriteRenderer Target;
    public SpriteOnState[] SpriteRules;
}

public class UrbBodyDisplay : UrbBase
{
    public TargetSpriteRenderer[] TargetSpriteRenderers;

    public void UpdateSprites(UrbComposition input)
    {
        if (input == null)
        {
            return;
        }
        for(int r = 0; r < TargetSpriteRenderers.Length; r++)
        {
            if(TargetSpriteRenderers[r].Target == null || !TargetSpriteRenderers[r].Target.isVisible)
            {
                continue;
            }

            for (int s = 0; s < TargetSpriteRenderers[r].SpriteRules.Length; s++)
            {
                bool RuleValid = false;
                switch (TargetSpriteRenderers[r].SpriteRules[s].Rule)
                {
                    case SpriteOnState.ThresholdRule.Equal:
                        RuleValid = input.ContainsEqualTo(TargetSpriteRenderers[r].SpriteRules[s].StateThreshold);
                        break;
                    case SpriteOnState.ThresholdRule.Less:
                        RuleValid = input.ContainsLessThan(TargetSpriteRenderers[r].SpriteRules[s].StateThreshold);
                        break;
                    case SpriteOnState.ThresholdRule.More:
                        RuleValid = input.ContainsMoreThan(TargetSpriteRenderers[r].SpriteRules[s].StateThreshold);
                        break;
                }

                if(RuleValid)
                {
                    TargetSpriteRenderers[r].Target.sprite = TargetSpriteRenderers[r].SpriteRules[s].DisplaySprite;
                    break;
                }
            }

        }
    }

}
