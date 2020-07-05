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
                IsInvisible = !mLineRenderer.isVisible;
            }
            if(mFillRenderer != null && IsInvisible)
            {
                IsInvisible = !mFillRenderer.isVisible;
            }

            return IsInvisible;
        }
    }

    public int SortingOrder {
        get {
            if (mLineRenderer != null)
            {
                return mLineRenderer.sortingOrder;
            }
            if(mFillRenderer != null)
            {
                return mFillRenderer.sortingOrder;
            }
            return 0;
        }
        set {
            mLineRenderer.sortingOrder = value;
            mFillRenderer.sortingOrder = value;
        }
    }

    public bool LineRendering {
        get {
            return mLineRenderer.gameObject.activeInHierarchy;
        }
        set {
            mLineRenderer.gameObject.SetActive(value);
        }
    }

    protected string mSpritePath;

    public virtual string SpritePath
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

    public virtual Color LineColor
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

    public virtual Color FillColor
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
