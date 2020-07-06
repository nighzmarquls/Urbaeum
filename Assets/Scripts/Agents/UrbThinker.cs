using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;


[System.Flags]
public enum UrbUrgeCategory
{
    None = 0,
    Breed = 1,
    Hunger = 2,
    Safety = 4,
    Rest = 8,
}

[RequireComponent(typeof(UrbAgent))]
public class UrbThinker : UrbBase
{
    bool perceptionExists = false;

    UrbPerception _mPerception = null;

    protected UrbPerception mPerception
    {
        get
        {
            return _mPerception;
        }
        set
        {
            _mPerception = value;
            // ReSharper disable once Unity.PerformanceCriticalCodeNullComparison
            perceptionExists = _mPerception != null;
        }
    }

    protected UrbBreeder mBreeder;
    public float BreedUrge { get; protected set; }

    protected UrbEater mEater;
    public float HungerUrge { get; protected set; }

    protected UrbMetabolism mMetabolism;
    public float RestUrge { get; protected set; }

    public float SafetyUrge { get; protected set; } = 0.0f;

    protected UrbPathfinder mPathfinder;
    protected UrbMovement mMovement;
    protected UrbAgent mAgent;
    
    static ProfilerMarker s_Initialize_p = new ProfilerMarker("UrbThinker.Initialize");

    public override void OnEnable()
    {
        s_Initialize_p.Begin();
        
        mAgent = GetComponent<UrbAgent>();
        mPathfinder = GetComponent<UrbPathfinder>();
        
        mMovement = GetComponent<UrbMovement>();

        mPerception = GetComponent<UrbPerception>();

        mBreeder = GetComponent<UrbBreeder>();
        BreedUrge = 0;
        mEater = GetComponent<UrbEater>();
        HungerUrge = 0;
        mMetabolism = GetComponent<UrbMetabolism>();
        RestUrge = 0;
        SafetyUrge = 0;
        
        IsmMetabolismNull = mMetabolism == null;
        IsmEaterNotNull = mEater != null;
        IsmBreederNotNull = mBreeder != null;
        
        base.OnEnable();
        s_Initialize_p.End();
    }
    static ProfilerMarker s_ChooseBehaviour_p = new ProfilerMarker("UrbThinker.ChooseBehaviour");
    bool IsmBreederNotNull;

    float LastTimeSuccessful = 0;
    public void ChooseBehaviour()
    {
        s_ChooseBehaviour_p.Begin(this);
        
        if(!perceptionExists || mPerception.ContactBehaviours == null)
        {
            s_ChooseBehaviour_p.End();
            return;
        }

        bool behaviorChosen = false;
        UrbBehaviour ChosenBehaviour = null;
        float BestEvaluation = 0;
        for(int i = 0; i < mPerception.ContactBehaviours.Length; i++)
        {
            float Evaluation = EvaluateBehaviour(mPerception.ContactBehaviours[i]);
            if(BestEvaluation < Evaluation)
            {
                //we don't even want to give null behaviors a chance to do their dirty work
                //just in case not all of them are null
                if (mPerception.ContactBehaviours[i] == null)
                {
                    continue;
                }
                
                behaviorChosen = true;
                ChosenBehaviour = mPerception.ContactBehaviours[i];
                BestEvaluation = Evaluation;
            }
        }

        if (!behaviorChosen || ChosenBehaviour.WasDestroyed)
        {
            if(mMovement && mMovement.Movement == null)
            {
                mMovement.ExecuteMove();
            }
        }
        else
        {
            LastTimeSuccessful = Time.time;
            ChosenBehaviour.ExecuteTileBehaviour();
        }

        s_ChooseBehaviour_p.End();
    }
    
    static ProfilerMarker s_CheckUrges_p = new ProfilerMarker("UrbThinker.CheckUrges");
    bool IsmEaterNotNull;
    bool IsmMetabolismNull;

    public void CheckUrges()
    {
        using (s_CheckUrges_p.Auto())
        {
            if (HasBody && mBody.BodyComposition == null)
            {
                return;
            }

            bool CanBreed = false;
            bool CanEat = false;
            if (IsmBreederNotNull)
            {
                if (mBreeder.CanBreed)
                {
                    if (!mBreeder.Gestating)
                    {
                        BreedUrge = 1.0f;
                        SafetyUrge = 0.25f;
                        CanBreed = true;
                    }
                    else
                    {
                        SafetyUrge = 1.0f;
                        BreedUrge = 0.0f;
                    }
                }
                else
                {
                    SafetyUrge = 0.5f;
                    BreedUrge -= 0.1f;
                }
            }

            if (IsmEaterNotNull)
            {
                float UrgeChange = 0.5f - mEater.Stomach.Fullness;
                HungerUrge = UrgeChange;
                CanEat = UrgeChange > 0.0f;
                RestUrge = mEater.Stomach.Fullness;
            }

           
            if (IsmMetabolismNull)
            {
                return;
            }
            if (mMetabolism.Starving)
            {
                if (CanBreed)
                {
                    BreedUrge -= 1.0f;
                }

                if (CanEat)
                {
                    HungerUrge += 1.0f;
                }

                SafetyUrge -= 0.5f;
                RestUrge = 0.0f;
            }
            else
            {
                if (!CanEat && !CanBreed)
                {
                    RestUrge = 1.0f;
                }
            }

            if (!mMetabolism.Healing)
            {
                SafetyUrge = Mathf.Max(0.0f,SafetyUrge);
                return;
            }
            
            SafetyUrge = 1.0f;

            if (CanBreed)
            {
                BreedUrge -= 0.5f;
            }

            if (!CanEat)
            {
                RestUrge += 1.0f;
            }
        }
    }

    public float EvaluateTile(UrbTile Tile, int TerrainType, int Size)
    {
        float TileValue = 0;

        if (HungerUrge > 0)
        {
            HungerUrge = Mathf.Min(1, HungerUrge);
            for(int f = 0; f < mEater.FoodScents.Length; f++)
            {
                TileValue += Tile.TerrainFilter[TerrainType][Size][mEater.FoodScents[f]] * HungerUrge;
            }

        }

        if(BreedUrge > 0)
        {
            BreedUrge = Mathf.Min(1, BreedUrge);
            for (int b = 0; b < mBreeder.MateScents.Length; b++)
            {
                TileValue += Tile.TerrainFilter[TerrainType][Size][mBreeder.MateScents[b]] * BreedUrge;
            }
            for (int b = 0; b < mBreeder.RivalScents.Length; b++)
            {
                TileValue -= Tile.TerrainFilter[TerrainType][Size][mBreeder.RivalScents[b]] * BreedUrge;
            }
        }

        if (SafetyUrge > 0)
        {
            SafetyUrge = Mathf.Min(1, SafetyUrge);
            TileValue -= Tile.TerrainFilter[TerrainType][Size][UrbScentTag.Violence] * SafetyUrge;
        }

        if (RestUrge > 0)
        {
            RestUrge = Mathf.Min(1, RestUrge);
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

        if ((Input.UrgeSatisfied & UrbUrgeCategory.Safety) > 0)
        {
            Evaluation *= SafetyUrge;
        }
        else
        {
            Evaluation *=  1-SafetyUrge;
        }
        return Evaluation;
    }

    static ProfilerMarker s_PickAction_p = new ProfilerMarker("UrbThinker.PickAction");

    public UrbAction PickAction(UrbTestCategory Test, float Result = 0, UrbTestCategory Exclude = UrbTestCategory.None)
    {
        if (mAgent.AvailableActions == null || mAgent.AvailableActions.Length == 0)
        {
            return null;
        }

        s_PickAction_p.Begin();
        
        UrbAction ChosenAction = null;
        float BestCost = float.MaxValue;

        for(int i = 0; i < mAgent.AvailableActions.Length; i++)
        {
            bool Valid = (Test & mAgent.AvailableActions[i].Category) == Test &&
               (Exclude == UrbTestCategory.None || (Exclude & mAgent.AvailableActions[i].Category) == 0);
            if (!Valid)
            {
                continue;
            }
            
            float ActionCost = mAgent.AvailableActions[i].CostEstimate(mAgent);

            if(ActionCost > Result && ActionCost < BestCost)
            {
                BestCost = ActionCost;
                ChosenAction = mAgent.AvailableActions[i];
            }
        }

        s_PickAction_p.End();
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
