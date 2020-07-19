using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbDisplayFeature : MonoBehaviour
{
    public string FeaturePath = "Sprites/Modular/Features/";
    public const string LineAppend = "_Lines";
    public const string MaskAppend = "_Mask";
    public const string OverlayAppend = "_Overlay";

    bool HaveLines = false;
    bool HaveFill = false;
    bool HaveOverlay = false;
    bool HaveMask = false;

    [SerializeField]
    protected Color mLineColor;

    protected float HeightOffset = 0.0f;
    public float Height {
        get {
            return 1.0f + HeightOffset;
        }
        set {
            float NewOffset = value - 1.0f;
            if(NewOffset == HeightOffset)
            {
                return;
            }

            HeightOffset = Mathf.Min(3.0f, Mathf.Max(-0.5f, NewOffset));

            if (HaveLines)
            {
                mLineRenderer.transform.localPosition = new Vector3(0, HeightOffset);
            }
            if (HaveFill)
            {
                mFillRenderer.transform.localPosition = new Vector3(0, HeightOffset);
            }

            
        }
    }

    protected float FeatureSize = 1.0f;

    public float Size {
        get {
            return FeatureSize;
        }
        set {
            if(value == FeatureSize)
            {
                return;
            }

            FeatureSize = value;

            Vector3 newScale = new Vector3(FeatureSize, FeatureSize, FeatureSize);

            if (HaveMask)
            {
                mMaskRenderer.transform.localScale = newScale;
            }

            if (HaveLines)
            {
                mLineRenderer.transform.localScale = newScale;
            }

            if (HaveFill)
            {
                mFillRenderer.transform.localScale = newScale;
            }

            if (HaveOverlay)
            {
               mOverlayRenderer.transform.localScale = newScale;
            }
        }
    }

    protected bool _invisible = true;
    public bool Invisible {
        get {
            bool IsInvisible = true;
            if (HaveLines)
            {
                IsInvisible = !mLineRenderer.isVisible;
            }
            if(HaveFill && IsInvisible)
            {
                IsInvisible = !mFillRenderer.isVisible;
            }

            return IsInvisible;
        }
    }

    public int SortingOrder {
        get {
            if (HaveFill)
            {
                return mFillRenderer.sortingOrder;
            }

            if (HaveLines)
            {
                return mLineRenderer.sortingOrder;
            }

            return 0;
        }
        set {
            if (HaveLines)
            {
                mLineRenderer.sortingOrder = value + 2;
            }
            if (HaveOverlay)
            {
                mOverlayRenderer.sortingOrder = value + 1;
            }
            if (HaveFill)
            {
                mFillRenderer.sortingOrder = value;
            }
            if (HaveMask)
            {
                mMaskRenderer.sortingOrder = value;
            }
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

            bool HaveMask = false;
            if (HaveMask)
            {
                Sprite MaskSprite = Resources.Load<Sprite>(FeaturePath + value + MaskAppend);

                if (MaskSprite != null)
                {
                    mMaskRenderer.sprite = MaskSprite;
                    HaveMask = true;
                }
            }

            if (HaveLines)
            {
                Sprite LineSprite = Resources.Load<Sprite>(FeaturePath + value + LineAppend);
                
                mLineRenderer.sprite = LineSprite;
                if(HaveMask)
                {
                    mLineRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                }
                else
                {
                    mLineRenderer.maskInteraction = SpriteMaskInteraction.None;
                }
                
            }
            if(HaveFill)
            {
                Sprite FillSprite = Resources.Load<Sprite>(FeaturePath + value);
                
                mFillRenderer.sprite = FillSprite;
                if (HaveMask)
                {
                    mFillRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                }
                else
                {
                    mFillRenderer.maskInteraction = SpriteMaskInteraction.None;
                }
            }

            if (HaveOverlay)
            {
                Sprite FillSprite = Resources.Load<Sprite>(FeaturePath + value + OverlayAppend);

                mOverlayRenderer.sprite = FillSprite;
                mOverlayRenderer.maskInteraction = SpriteMaskInteraction.None;
            }

            mSpritePath = value; 
        }
    }

    public virtual Color LineColor
    {
        get { return mLineColor; }
        set {
            if (HaveLines && mLineColor != value)
            {
                mLineRenderer.color = value;
                mLineColor = value;
            }
        }
    }

    [SerializeField]
    protected Color mFillColor;

    public virtual Color FillColor
    {
        get { return mFillColor; }
        set {
            if (mFillColor == value)
            {
                return;
            }

            if (HaveFill)
            {
                mFillRenderer.color = value;
                if (HaveOverlay)
                {
                    mOverlayRenderer.color = value;
                }
                mFillColor = value;
            }
        }
    }

    [SerializeField]
    protected SpriteRenderer mLineRenderer;

    [SerializeField]
    protected SpriteRenderer mOverlayRenderer;

    [SerializeField]
    protected SpriteRenderer mFillRenderer;

    [SerializeField]
    protected SpriteMask mMaskRenderer;

    protected void OnEnable()
    {
        HaveLines = mLineRenderer != null;
        HaveFill = mFillRenderer != null;
        HaveOverlay = mOverlayRenderer != null;
        HaveMask = mMaskRenderer != null;
    }
}
