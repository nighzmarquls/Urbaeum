using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbDisplayFeature : MonoBehaviour
{
    public string FeaturePath = "Sprites/Modular/Features/";
    public const string LineAppend = "_Lines";
    [SerializeField]
    protected Color mLineColor;

    public bool Invisible {
        get {
            bool IsInvisible = true;
            if (mLineRenderer != null)
            {
                return !mLineRenderer.isVisible;
            }
            if(mFillRenderer != null)
            {
                return !mFillRenderer.isVisible;
            }

            return IsInvisible;
        }
    }

    protected string mSpritePath;

    virtual public string SpritePath
    {
        get { return mSpritePath; }
        set {
            if(mLineRenderer != null)
            {
                Sprite LineSprite = Resources.Load<Sprite>(FeaturePath + value + LineAppend);
                
                 mLineRenderer.sprite = LineSprite;
                
            }
            if(mFillRenderer != null)
            {
                Sprite FillSprite = Resources.Load<Sprite>(FeaturePath + value);
                
                mFillRenderer.sprite = FillSprite;
            }
            mSpritePath = value;
        }
    }

    virtual public Color LineColor
    {
        get { return mLineColor; }
        set {
            if (mLineRenderer != null && mLineColor != value)
            {
                mLineRenderer.color = value;
                mLineColor = value;
            }
        }
    }

    [SerializeField]
    protected SpriteRenderer mLineRenderer;

    [SerializeField]
    protected Color mFillColor;

    virtual public Color FillColor
    {
        get { return mFillColor; }
        set {
            if (mFillRenderer != null && mFillColor != value)
            {
                mFillRenderer.color = value;
                mFillColor = value;
            }
        }
    }

    [SerializeField]
    protected SpriteRenderer mFillRenderer;
}
