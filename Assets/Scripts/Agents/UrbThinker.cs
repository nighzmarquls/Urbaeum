using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Flags]
public enum UrbUrgeCategory
{
    None = 0,
    Breed = 1,
    Hunger = 2,
    Rest = 4,
}

[RequireComponent(typeof(UrbAgent))]
public class UrbThinker : UrbBase
{
    protected UrbPerception mPerception;

    protected UrbBreeder mBreeder;
    public float BreedUrge { get; protected set; }

    protected UrbEater mEater;
    public float HungerUrge { get; protected set; }

    protected UrbMetabolism mMetabolism;
    public float RestUrge { get; protected set; }

    protected UrbPathfinder mPathfinder;
    protected UrbMovement mMovement;
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
        mPathfinder = GetComponent<UrbPathfinder>();
        mMovement = GetComponent<UrbMovement>();
        mBody = mAgent.Body;

        mPerception = GetComponent<UrbPerception>();

        mBreeder = GetComponent<UrbBreeder>();
        BreedUrge = 0;
        mEater = GetComponent<UrbEater>();
        HungerUrge = 0;
        mMetabolism = GetComponent<UrbMetabolism>();
        RestUrge = 0;
        
        base.Initialize();
    }

    public void ChooseBehaviour()
    {
        if(mPerception == null || mPerception.Behaviours == null)
        {
            return;
        }

        UrbBehaviour ChosenBehaviour = null;
        float BestEvaluation = 0;
        for(int i = 0; i < mPerception.Behaviours.Length; i++)
        {
            float Evaluation = EvaluateBehaviour(mPerception.Behaviours[i]);
            if(BestEvaluation < Evaluation)
            {
                ChosenBehaviour = mPerception.Behaviours[i];
                BestEvaluation = Evaluation;
            }
        }

        if (ChosenBehaviour == null)
        {
            if(mMovement != null && mMovement.Movement == null)
            {
                mMovement.ExecuteMove();
            }
        }
        else
        { 
            ChosenBehaviour.ExecuteTileBehaviour();
        }
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
                    BreedUrge = 1.0f;
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

    public float EvaluateBehaviour(UrbBehaviour Input)
    {
        float Evaluation = Input.BehaviourEvaluation;

        if (Evaluation <= 0)
            return 0;

        if((Input.UrgeSatisfied & UrbUrgeCategory.Breed) > 0)
        {
            Evaluation *= BreedUrge;
        }
        if((Input.UrgeSatisfied & UrbUrgeCategory.Hunger) > 0)
        {
            Evaluation *= HungerUrge;
        }
        if((Input.UrgeSatisfied & UrbUrgeCategory.Rest) > 0)
        {
            Evaluation *= RestUrge;
        }

        return Evaluation;
    }

    public UrbAction PickAction(UrbTestCategory Test, float Result = 0, UrbTestCategory Exclude = UrbTestCategory.None)
    {
        if (mAgent.AvailableActions == null)
        {
            return null;
        }

        UrbAction ChosenAction = null;
        float BestCost = float.MaxValue;

        for(int i = 0; i < mAgent.AvailableActions.Length; i++)
        {
            bool Valid = (Test & mAgent.AvailableActions[i].Category) == Test &&
               (Exclude == UrbTestCategory.None || (Exclude & mAgent.AvailableActions[i].Category) == 0);
            if (Valid)
            {
                float ActionCost = mAgent.AvailableActions[i].CostEstimate(mAgent);

                if(ActionCost > Result && ActionCost < BestCost)
                {
                    BestCost = ActionCost;
                    ChosenAction = mAgent.AvailableActions[i];
                }
            }
        }

        return ChosenAction;
    }

    public float PerformTest(UrbTestCategory Test, float Modifier = 0, bool Execute = false)
    {
        UrbAction ChosenAction = PickAction(Test);

        if(ChosenAction == null)
        {
            return 0.0f;
        }

        return ChosenAction.Test(mAgent,Modifier);
    }
}
