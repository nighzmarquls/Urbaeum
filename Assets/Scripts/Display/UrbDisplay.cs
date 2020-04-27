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

    public int SortingOrder = 0;
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
            
            if (Invisible || mFlip == value)
            {
                mFlip = value;
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
            if (DisplayBody == null)
            {
                return true;
            }

            return DisplayBody.Invisible;
        }
    }


    public float BodySize { 
        get {
            return mBodySize;
        }

        set {
            if(Invisible || value == mBodySize)
            {
                mBodySize = value;
                return;
            }

            float MazimumFaceSize = value * maxFaceRatio;
            if (mFaceSize > MazimumFaceSize)
            {
                mFaceSize = MazimumFaceSize;
                Face.localScale = new Vector3((mFlip) ? -mFaceSize : mFaceSize, mFaceSize, mFaceSize);
            }

            float MinimumFaceSize = value * minFaceRatio;
            if(mFaceSize < MinimumFaceSize)
            {
                mFaceSize = MinimumFaceSize;
                Face.localScale = new Vector3((mFlip) ? -mFaceSize : mFaceSize, mFaceSize, mFaceSize);
            }
            mBodySize = value;
            
            Body.localScale = new Vector3((mFlip) ? -mBodySize : mBodySize, mBodySize, mBodySize);
       
            SynchronizeBody();
        }
    }

    public float FaceSize {
        get { return mFaceSize; }
        set {
            if(Invisible || mSignificance < FaceSignificance || value == mFaceSize)
            {
                mFaceSize = value;
                return;
            }

            float MazimumBodySize = value / maxFaceRatio;
            if (mBodySize < MazimumBodySize)
            {
                
                mBodySize = MazimumBodySize;
                Body.localScale = new Vector3((mFlip) ? -mBodySize : mBodySize, mBodySize, mBodySize);
            }

            float MinimumBodySize = value / minFaceRatio;
            if (mBodySize > MinimumBodySize)
            {
                
                mBodySize = MinimumBodySize;
                Body.localScale = new Vector3((mFlip) ? -mBodySize : mBodySize, mBodySize, mBodySize);
            }

            mFaceSize = value;

            
            Face.localScale = new Vector3((mFlip) ? -mFaceSize : mFaceSize, mFaceSize, mFaceSize);
            SynchronizeBody();
        }
    }

    const float LineSignificance = 0.09f;
    const float FaceSignificance = 0.06f;
    const float FeatureSignificance = 0.04f;

    protected float mSignificance = 1;
    public float Significance { get { return mSignificance; }
        set {
            if (Invisible || mSignificance == value)
                return;

            
            if(value < LineSignificance)
            {
                DisplayBody.LineRendering = false;
                UpdateFeatureLineRendering(false, PrimaryFeatures);
                UpdateFeatureLineRendering(false, SecondaryFeatures);
                UpdateFeatureLineRendering(false, DetailFeatures);
            }
            else if(value > LineSignificance)
            {
                DisplayBody.LineRendering = true;
                DisplayBody.LineColor = mColors.PrimaryColor.Line;
                UpdateFeatureColors(mColors.PrimaryColor, PrimaryFeatures);
                UpdateFeatureColors(mColors.SecondaryColor, SecondaryFeatures);
                UpdateFeatureColors(mColors.DetailColor, DetailFeatures);
                UpdateFeatureLineRendering(true, PrimaryFeatures);
                UpdateFeatureLineRendering(true, SecondaryFeatures);
                UpdateFeatureLineRendering(true, DetailFeatures);

            }
            
            if(value < FaceSignificance)
            {
                Face.gameObject.SetActive(false);
                if (EffectDisplay != null)
                {
                    EffectDisplay.enabled = false;
                }
            }
            else if(value > FaceSignificance)
            {
                Face.localScale = new Vector3((mFlip) ? -mFaceSize : mFaceSize, mFaceSize, mFaceSize);
                Body.localScale = new Vector3((mFlip) ? -mBodySize : mBodySize, mBodySize, mBodySize);
                if (EffectDisplay != null)
                {
                    EffectDisplay.enabled = true;
                }
                Face.gameObject.SetActive(true);
                DisplayFace.FillColor = mColors.FaceColor;
                DisplayFace.HighlightColor = mColors.HighlightColor;
            }

            if(value < FeatureSignificance)
            {
                ForeFeaturePoint.gameObject.SetActive(false);
                BackFeaturePoint.gameObject.SetActive(false);
                FaceFeaturePoint.gameObject.SetActive(false);
            }
            else if(value > FeatureSignificance)
            {
                Face.localScale = new Vector3((mFlip) ? -mFaceSize : mFaceSize, mFaceSize, mFaceSize);
                Body.localScale = new Vector3((mFlip) ? -mBodySize : mBodySize, mBodySize, mBodySize);

                UpdateFeatureColors(mColors.PrimaryColor, PrimaryFeatures);
                UpdateFeatureColors(mColors.SecondaryColor, SecondaryFeatures);
                UpdateFeatureColors(mColors.DetailColor, DetailFeatures);
                ForeFeaturePoint.gameObject.SetActive(true);
                BackFeaturePoint.gameObject.SetActive(true);
                FaceFeaturePoint.gameObject.SetActive(true);
                SynchronizeBody();
            }

            mSignificance = value;
        }
    }

    protected void UpdateFeatureLineRendering(bool Render, List<UrbDisplayFeature> Features)
    {
        for (int i = 0; i < Features.Count; i++)
        {
            Features[i].LineRendering = Render;
        }
    }


    public SpriteRenderer EffectDisplay = null;
    protected List<UrbTest> EffectQueue;
    protected List<Vector3> EffectPositionQueue;
    float ScheduledDisplayChange = 0;

    public void QueueEffectDisplay(UrbTest Input, Vector3 Position)
    {
        if(EffectDisplay == null)
        {
            return;
        }
        EffectQueue.Add(Input);
        EffectPositionQueue.Add(Position);
    }

    public void UpdateEffectDisplay()
    {
        if (EffectDisplay == null)
        {
            return;
        }

        if(Time.time > ScheduledDisplayChange)
        {
            //EffectDisplay.transform.position = Face.transform.position;
            if (EffectQueue.Count <= 0)
            {
                EffectDisplay.sprite = null;
                EffectDisplay.transform.position = this.transform.position;
                return;
            }

            ScheduledDisplayChange = Time.time + EffectQueue[0].DisplayTime;

            if (Invisible)
            {
                EffectQueue.RemoveAt(0);
                EffectPositionQueue.RemoveAt(0);
                return;
            }
            EffectDisplay.transform.position = EffectPositionQueue[0];
            EffectDisplay.color = EffectQueue[0].SuccessColor;
            EffectDisplay.sprite = EffectQueue[0].SuccessIcon;
            EffectQueue.RemoveAt(0);
            EffectPositionQueue.RemoveAt(0);
        }
    }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }

        if(EffectDisplay != null)
        {
            EffectQueue = new List<UrbTest>();
            EffectPositionQueue = new List<Vector3>();
        }

        mSignificance = 1f;
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
            Face.transform.position += new Vector3(0, 0, offset);

        }
        if (ForeFeaturePoint != null)
        {
            float offset = 0;
            for (int i = 0; i < PrimaryForeFeatureSprites.Length; i++)
            {
                PrimaryFeatures.Add(CreateFeature(PrimaryForeFeatureSprites[i], ForeFeaturePoint, offset));
                offset += featureOffset;
            }
            Face.transform.position += new Vector3(0, 0, offset);
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
        DisplayFace.SortingOrder = SortingOrder;
        DisplayBody.SortingOrder = SortingOrder;
        
       
        base.Initialize();

    }

    public void Express(UrbDisplayFace.Expression Expression)
    {
        DisplayFace.CurrentExpression = Expression;
    }

    public UrbColor PrimaryColor {
        get { return mColors.PrimaryColor; }
        set {
            if(Invisible)
            {
                mColors.PrimaryColor = value;
                return;
            }
            if(value.Line == mColors.PrimaryColor.Line && value.Fill == mColors.PrimaryColor.Fill)
            {
                return;
            }

            DisplayBody.FillColor = value.Fill;
            DisplayBody.LineColor = value.Line;
            if (mSignificance > FeatureSignificance)
            {
                UpdateFeatureColors(value, PrimaryFeatures);
            }

            mColors.PrimaryColor = value;
            
        }
    }

    public UrbColor SecondaryColor {
        get { return mColors.SecondaryColor; }
        set {
            if (Invisible || mSignificance < FeatureSignificance)
            {
                mColors.SecondaryColor = value;
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
            if(Invisible || mSignificance < FeatureSignificance)
            {
                mColors.DetailColor = value;
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

    private void Update()
    {
        UpdateEffectDisplay();
    }

    protected UrbDisplayFeature CreateFeature(string FeatureSprite,Transform AttachmentPoint, float offset = 0.0f)
    {
        GameObject Feature = Instantiate<GameObject>(FeatureTemplate.gameObject, AttachmentPoint);
        UrbDisplayFeature DisplayFeature = Feature.GetComponent<UrbDisplayFeature>();
        DisplayFeature.SpritePath = FeatureSprite;
        Feature.transform.position += (Vector3.forward * -offset);
        DisplayFeature.SortingOrder = SortingOrder;
        return DisplayFeature;
    }

    public void SynchronizeBody()
    {
        if (Invisible || mSignificance < FeatureSignificance)
        {
            return;
        }

        Face.position = FacePivot.position;

        FaceFeaturePoint.position = Face.position + (Vector3.forward * featureOffset);
        FaceFeaturePoint.localScale = Face.localScale;
        FaceFeaturePoint.rotation = Face.rotation;
    }
}
