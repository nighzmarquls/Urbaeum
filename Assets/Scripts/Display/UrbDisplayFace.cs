using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbDisplayFace : UrbDisplayFeature
{
    public enum Expression
    {
        Default = 0,
        Closed,
        Cry,
        Flinch,
        Joy,
        Dead,
        MaxNum
    }

    const uint maxExpression = (uint)Expression.MaxNum;

    const string FacePath = "Sprites/Modular/Faces/";
    const string HighlightAppend = "_Light";

    protected Dictionary<Expression, Sprite> FaceExpressions;
    protected Dictionary<Expression, Sprite> FaceExpressionsHighlight;
    public virtual string FaceType { get => SpritePath; set => SpritePath = value; }

    public override string SpritePath { get => base.SpritePath;
        set {
            if(value == mSpritePath)
            {
                return;
            }
            SetFaceExpressions(value);

            mSpritePath = value;
        }
    }

    protected Expression mCurrentExpression = Expression.Default;
    public virtual Expression CurrentExpression {
        get {
            return mCurrentExpression;
        }
        set {
            if(Invisible)
            {
                return;
            }

            if(mCurrentExpression == value || value == Expression.MaxNum) 
            {
                return;
            }

            if(FaceExpressions == null)
            {
                SetFaceExpressions(mSpritePath);
            }
            mFillRenderer.sprite = FaceExpressions[value];
            mLineRenderer.sprite = FaceExpressionsHighlight[value];
            mCurrentExpression = value;
        }
    }

    public virtual Color HighlightColor { get => base.LineColor; set => base.LineColor = value; }

    protected void SetFaceExpressions(string NewFaceType){
        if(FaceExpressions == null)
        {
            FaceExpressions = new Dictionary<Expression, Sprite>();
        }
        if(FaceExpressionsHighlight == null)
        {
            FaceExpressionsHighlight = new Dictionary<Expression, Sprite>();
        }

        for(uint i = 0; i < maxExpression; i++)
        {
            
            string Path = FacePath;
            Expression TargetExpression = (Expression)i;
            string ExpressionSprite = TargetExpression.ToString();
            Sprite FaceSprite = Resources.Load<Sprite>(Path + ExpressionSprite);
            Sprite HighlightSprite = Resources.Load<Sprite>(Path + ExpressionSprite + HighlightAppend);
            if (mSpritePath != null && mSpritePath.Length > 0)
            {
                Path += mSpritePath + "/";
                Sprite CustomFace = Resources.Load<Sprite>(Path + ExpressionSprite);
                Sprite CustomHighlight = Resources.Load<Sprite>(Path + ExpressionSprite + HighlightAppend);
                if(CustomFace != null)
                {
                    FaceSprite = CustomFace;
                }
                if(CustomHighlight != null)
                {
                    HighlightSprite = CustomHighlight;
                }
            }

            if(FaceSprite == null)
            {
                //Debug.Log("No Face Sprite at: " + Path);
            }
            else if (FaceExpressions.ContainsKey(TargetExpression))
            {
                FaceExpressions[TargetExpression] = FaceSprite;
            }
            else
            {
                FaceExpressions.Add(TargetExpression, FaceSprite);
            }

            if (FaceSprite == null)
            {
                //Debug.Log("No Face Sprite at: " + Path);
            }
            else if (FaceExpressionsHighlight.ContainsKey(TargetExpression))
            {
                FaceExpressionsHighlight[TargetExpression] = HighlightSprite;
            }
            else
            {
                FaceExpressionsHighlight.Add(TargetExpression, HighlightSprite);
            }
        }
        mFillRenderer.sprite = FaceExpressions[mCurrentExpression];
        mLineRenderer.sprite = FaceExpressionsHighlight[mCurrentExpression];
    }

}
