using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(UrbAgent))]
public class UrbThinker : UrbBase
{
    // Start is called before the first frame update

    protected UrbBreeder mBreeder;
    public float BreedUrge { get; protected set; }

    protected UrbEater mEater;
    public float HungerUrge { get; protected set; }

    protected UrbMetabolism mMetabolism;
    public float RestUrge { get; protected set; }

    protected UrbPathfinder mPathfinder;
    protected UrbBody mBody;
    protected UrbAgent mAgent;

    private void Start()
    {
        Initialize();   
    }

    public override void Initialize()
    {
        if(bInitialized)
        {
            return;
        }
        mAgent = GetComponent<UrbAgent>();
        mBody = mAgent.Body;
        mBreeder = GetComponent<UrbBreeder>();
        BreedUrge = 0;
        mEater = GetComponent<UrbEater>();
        HungerUrge = 0;
        mMetabolism = GetComponent<UrbMetabolism>();
        RestUrge = 0;
        
        base.Initialize();
    }

    public void CheckUrges()
    {
        if(mBody.BodyComposition == null)
        {
            return;
        }
        bool CanBreed = false;
        bool CanEat = false;
        if(mBreeder != null)
        {
            if (!mBody.BodyComposition.ContainsLessThan(mBreeder.GestationRecipe))
            {
                if (!mBreeder.Gestating)
                {
                    BreedUrge += 0.1f;
                    CanBreed = true;
                }
                else
                {
                    BreedUrge = 0.0f;
                }
            }
            else
            {
                BreedUrge -= 0.1f;
            }
        }

        if(mEater != null)
        {
            float UrgeChange = 0.5f - mEater.Stomach.Fullness;
            HungerUrge = UrgeChange;
            CanEat = UrgeChange > 0.0f;
            RestUrge = mEater.Stomach.Fullness;
        }

        if(mMetabolism != null)
        {
            if(mMetabolism.Starving)
            {
                if (CanBreed)
                {
                    BreedUrge -= 1.0f;
                }
                if (CanEat)
                {
                    HungerUrge += 1.0f;
                }
                RestUrge = 0.0f;
            }
            else
            {
                if(!CanEat)
                {
                    RestUrge = 1.0f;
                }
            }

            if(mMetabolism.Healing)
            {
                if (CanBreed)
                {
                    BreedUrge -= 1.0f;
                }
                if (!CanEat)
                {
                    RestUrge += 1.0f;
                }
            }
        }

    }

    public float EvaluateTile(UrbTile Tile, int TerrainType, int Size)
    {
        float TileValue = 0;

        if (HungerUrge > 0)
        {
            for(int f = 0; f < mEater.FoodScents.Length; f++)
            {
                TileValue += Tile.TerrainFilter[TerrainType][Size][mEater.FoodScents[f]] * HungerUrge;
            }
        }

        if(BreedUrge > 0)
        {
            for(int b = 0; b < mBreeder.MateScents.Length; b++)
            {
                TileValue += Tile.TerrainFilter[TerrainType][Size][mBreeder.MateScents[b]] * BreedUrge;
            }
            for (int b = 0; b < mBreeder.RivalScents.Length; b++)
            {
                TileValue -= Tile.TerrainFilter[TerrainType][Size][mBreeder.RivalScents[b]] * BreedUrge;
            }
        }

        if (RestUrge > 0)
        {
            TileValue -= RestUrge;
        }

        return TileValue;
    }
}
