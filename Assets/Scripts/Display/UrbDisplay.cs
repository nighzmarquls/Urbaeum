using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct UrbColor
{
    public Color Line;
    public Color Fill;
}

[System.Serializable]
public struct UrbDisplayColors
{
    public UrbColor PrimaryColor;
    public UrbColor SecondaryColor;
    public UrbColor DetailColor;
    public Color FaceColor;
    public Color HighlightColor;
}

public class UrbDisplay : UrbBase
{
    public enum ColorCategory
    {
        Primary =0,
        Secondary,
        Detail,
        Eye,
        Highlight
    }

   

    public float maxFaceRatio = 1.5f;
    public float minFaceRatio = 0.25f;
    public float featureOffset = 0.001f;

    [SerializeField]
    protected UrbDisplayFeature FeatureTemplate;

    [SerializeField]
    protected UrbDisplayColors mColors;

    public UrbDisplayColors Colors { get { return mColors; } }

    public string BodySprite = "BaseSphere";
    public string FaceSprite = "";

    public string[] PrimaryFaceFeatureSprites;
    public string[] PrimaryForeFeatureSprites;
    public string[] PrimaryBackFeatureSprites;

    public string[] SecondaryFaceFeatureSprites;
    public string[] SecondaryForeFeatureSprites;
    public string[] SecondaryBackFeatureSprites;

    protected List<UrbDisplayFeature> PrimaryFeatures;
    protected List<UrbDisplayFeature> SecondaryFeatures;
    protected List<UrbDisplayFeature> DetailFeatures;

    [SerializeField]
    protected UrbDisplayFace DisplayFace;

    [SerializeField]
    protected Transform FaceFeaturePoint;
    [SerializeField]
    protected Transform ForeFeaturePoint;
    [SerializeField]
    protected Transform BackFeaturePoint;

    [SerializeField]
    protected Transform Body;
    [SerializeField]
    protected Transform Face;
    [SerializeField]
    protected Transform BodyPivot;
    [SerializeField]
    protected Transform FacePivot;

    protected float mBodySize = 1;
    protected float mFaceSize = 1;

    bool mFlip = false;
    public bool Flip {
        get {
            return mFlip;
        }
        set {
            if (DisplayBody.Invisible)
            {
                return;
            }
            if (mFlip == value)
            {
                return;
            }
            mFlip = value;
            Body.localScale = new Vector3((mFlip) ? -mBodySize : mBodySize, mBodySize, mBodySize);
            Face.localScale = new Vector3((mFlip) ? -mFaceSize : mFaceSize, mFaceSize, mFaceSize);

            SynchronizeBody();
        }
    }

    protected UrbDisplayFeature DisplayBody;

    public bool Invisible {
        get {
            return DisplayBody.Invisible;
        }
    }


    public float BodySize { 
        get {
            return mBodySize;
        }

        set {
            if(DisplayBody.Invisible)
            {
                return;
            }

            if(value == mBodySize)
            {
                return;
            }

            Body.localScale = new Vector3((mFlip) ? -value : value, value, value);

            float MazimumFaceSize = value * maxFaceRatio;
            if (mFaceSize > MazimumFaceSize)
            {
                Face.localScale = new Vector3((mFlip) ? -MazimumFaceSize : MazimumFaceSize, MazimumFaceSize, MazimumFaceSize);
                mFaceSize = MazimumFaceSize;
            }

            float MinimumFaceSize = value * minFaceRatio;
            if(mFaceSize < MinimumFaceSize)
            {
                Face.localScale = new Vector3((mFlip) ? -MinimumFaceSize : MinimumFaceSize, MinimumFaceSize, MinimumFaceSize);
                mFaceSize = MinimumFaceSize;
            }
            mBodySize = value;
            SynchronizeBody();
        }
    }

    public float FaceSize {
        get { return mFaceSize; }
        set {
            if(DisplayBody.Invisible)
            {
                return;
            }

            if (value == mFaceSize)
            {
                return;
            }

            Face.localScale = new Vector3((mFlip) ? -value : value, value, value);

            float MazimumBodySize = value / maxFaceRatio;
            if (mBodySize < MazimumBodySize)
            {
                Body.localScale = new Vector3((mFlip) ? -MazimumBodySize : MazimumBodySize, MazimumBodySize, MazimumBodySize);
                mBodySize = MazimumBodySize;
            }

            float MinimumBodySize = value / minFaceRatio;
            if (mBodySize > MinimumBodySize)
            {
                Body.localScale = new Vector3((mFlip) ? -MinimumBodySize : MinimumBodySize, MinimumBodySize, MinimumBodySize);
                mBodySize = MinimumBodySize;
            }

            mFaceSize = value;
            SynchronizeBody();
        }
    }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }

        DisplayBody = CreateFeature(BodySprite, Body);
        DisplayFace.FaceType = FaceSprite;
        
        PrimaryFeatures = new List<UrbDisplayFeature>();
        SecondaryFeatures = new List<UrbDisplayFeature>();
        DetailFeatures = new List<UrbDisplayFeature>();

        if (FaceFeaturePoint != null)
        {
            float offset = 0;
            for (int i = 0; i < PrimaryFaceFeatureSprites.Length; i++)
            {
                PrimaryFeatures.Add(CreateFeature(PrimaryFaceFeatureSprites[i], FaceFeaturePoint, offset));
                offset += featureOffset;
            }

            for (int i = 0; i < SecondaryFaceFeatureSprites.Length; i++)
            {
                SecondaryFeatures.Add(CreateFeature(SecondaryFaceFeatureSprites[i], FaceFeaturePoint, offset));
                offset += featureOffset;
            }

        }
        if (ForeFeaturePoint != null)
        {
            float offset = 0;
            for (int i = 0; i < PrimaryForeFeatureSprites.Length; i++)
            {
                PrimaryFeatures.Add(CreateFeature(PrimaryForeFeatureSprites[i], ForeFeaturePoint, offset));
                offset += featureOffset;
            }
        }
        if (BackFeaturePoint != null)
        {
            float offset = 0;
            for (int i = 0; i < PrimaryBackFeatureSprites.Length; i++)
            {
                PrimaryFeatures.Add(CreateFeature(PrimaryBackFeatureSprites[i], BackFeaturePoint, offset));
                offset += featureOffset;
            }
        }

        UpdateFeatureColors(mColors.PrimaryColor, PrimaryFeatures);
        UpdateFeatureColors(mColors.SecondaryColor, SecondaryFeatures);
        UpdateFeatureColors(mColors.DetailColor, DetailFeatures);
        DisplayBody.FillColor = mColors.PrimaryColor.Fill;
        DisplayBody.LineColor = mColors.PrimaryColor.Line;
        DisplayFace.FillColor = mColors.FaceColor;
        DisplayFace.HighlightColor = mColors.HighlightColor;

       
        base.Initialize();

    }

    public void Express(UrbDisplayFace.Expression Expression)
    {
        DisplayFace.CurrentExpression = Expression;
    }

    public UrbColor PrimaryColor {
        get { return mColors.PrimaryColor; }
        set {
            if(DisplayBody.Invisible)
            {
                return;
            }
            if(value.Line == mColors.PrimaryColor.Line && value.Fill == mColors.PrimaryColor.Fill)
            {
                return;
            }

            DisplayBody.FillColor = value.Fill;
            DisplayBody.LineColor = value.Line;
            UpdateFeatureColors(value, PrimaryFeatures);
            
            mColors.PrimaryColor = value;
            
        }
    }

    public UrbColor SecondaryColor {
        get { return mColors.SecondaryColor; }
        set {
            if (DisplayBody.Invisible)
            {
                return;
            }
            if (value.Line == mColors.SecondaryColor.Line && value.Fill == mColors.SecondaryColor.Fill)
            {
                return;
            }

            UpdateFeatureColors(value, SecondaryFeatures);

            mColors.SecondaryColor = value;
        }
    }

    public UrbColor DetailColor {
        get { return mColors.DetailColor; }
        set {
            if(DisplayBody.Invisible)
            {
                return;
            }
            if (value.Line == mColors.DetailColor.Line && value.Fill == mColors.DetailColor.Fill)
            {
                return;
            }

            UpdateFeatureColors(value, DetailFeatures);

            mColors.DetailColor = value;
        }
    }

    protected void UpdateFeatureColors(UrbColor Color, List<UrbDisplayFeature> Features)
    {
        for (int i = 0; i < Features.Count; i++)
        {
            Features[i].LineColor = Color.Line;
            Features[i].FillColor = Color.Fill;
        }
    }

    private void Start()
    {
        Initialize();
    }

    protected UrbDisplayFeature CreateFeature(string FeatureSprite,Transform AttachmentPoint, float offset = 0.0f)
    {
        GameObject Feature = Instantiate<GameObject>(FeatureTemplate.gameObject, AttachmentPoint);
        UrbDisplayFeature DisplayFeature = Feature.GetComponent<UrbDisplayFeature>();
        DisplayFeature.SpritePath = FeatureSprite;
        Feature.transform.position += (Vector3.forward * -offset);
        
        return DisplayFeature;
    }

    public void SynchronizeBody()
    {
        if (DisplayBody.Invisible)
        {
            return;
        }
        Face.position = FacePivot.position;

        FaceFeaturePoint.position = Face.position;
        FaceFeaturePoint.localScale = Face.localScale;
        FaceFeaturePoint.rotation = Face.rotation;
    }
}
