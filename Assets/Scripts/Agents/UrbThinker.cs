using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
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
    Work = 16
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
    
    public float BreedUrge { get; protected set; }

    public float HungerUrge { get; protected set; }

    public float RestUrge { get; protected set; }

    public float SafetyUrge { get; protected set; } = 0.0f;

    protected UrbPathfinder mPathfinder;
    protected UrbMovement mMovement;
    
    static ProfilerMarker s_Initialize_p = new ProfilerMarker("UrbThinker.Initialize");

    public override void OnEnable()
    {
        s_Initialize_p.Begin();
        
        mAgent = GetComponent<UrbAgent>();
        mPathfinder = GetComponent<UrbPathfinder>();
        
        mMovement = GetComponent<UrbMovement>();
        mPerception = GetComponent<UrbPerception>();

        BreedUrge = 0;
        HungerUrge = 0;
        RestUrge = 0;
        SafetyUrge = 0;
        
        
        base.OnEnable();
        s_Initialize_p.End();
    }
    static ProfilerMarker s_ChooseBehaviour_p = new ProfilerMarker("UrbThinker.ChooseBehaviour");

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

    public void ClampUrges()
    {
        var toAssert = float.IsNaN(BreedUrge) || float.IsInfinity(BreedUrge);
        Assert.IsFalse(toAssert, "BreedUrge Must Be Valid float");
        
        if (BreedUrge > 1f)
        {
            BreedUrge = 1.0f;
        }
        else if (BreedUrge < 0f)
        {
            BreedUrge = 0f;
        }

        toAssert = float.IsNaN(SafetyUrge) || float.IsInfinity(SafetyUrge);
        Assert.IsFalse(toAssert, "SafetyUrge Must Be Valid float");

        if (SafetyUrge > 1f)
        {
            SafetyUrge = 1f;
        } else if (SafetyUrge < 0f)
        {
            SafetyUrge = 0f;
        }


        if (RestUrge > 1f)
        {
            RestUrge = 1f;
        } 
        else if (RestUrge < 0f)
        {
            RestUrge = 0f;
        }
    }

    //Addjusts breeding urges and returns whether or not agent can breed.
    protected bool CheckBreedingUrge()
    {
        if (!IsBreeder)
        {
            return false;
        }

        if (!Breeder.CanBreed)
        {
            if (SafetyUrge < 0.75f)
            {
                SafetyUrge += 0.015f;
            }
            
            BreedUrge = 0.0f;
            return false;
        }
        
        if (Breeder.Gestating)
        {
            BreedUrge = 0.0f;
            SafetyUrge += 0.025f;
            return false;
        }
        
        //Once we reach this point, we know we can breed
        //Let's scale up the urges to breed over time.
        if (BreedUrge < 1)
        {
            BreedUrge += 0.015f;
        }

        //... And scale down the Safety urge until the
        if (SafetyUrge > 0.25f)
        {
            SafetyUrge -= 0.015f;
        }
        
        return true;
    }



    public void CheckUrges()
    {
        ClampUrges();

        if (!HasBody || !mBody.HasComposition)
        {
            return;
        }

        bool CanBreed = CheckBreedingUrge();
        bool CanEat = false;

        if (IsEater)
        {
            Assert.IsTrue(Eater.Stomach.MaxCapacity > 0, "To Calculate Fullness, MaxCapacity must be > 0");
            var fullness = Eater.Stomach.Fullness;

            float UrgeChange = 0.5f - fullness;
            HungerUrge = UrgeChange;
            CanEat = UrgeChange > 0.0f;
            RestUrge = fullness;
        }

        if (!HasMetabolism)
        {
            if (!CanEat && !CanBreed)
            {
                RestUrge += .05f;
            }
            return;
        }

        if (Metabolism.Starving)
        {
            if (CanBreed)
            {
                BreedUrge = 0f;
            }

            if (CanEat)
            {
                HungerUrge = 1.0f;
            }

            SafetyUrge -= 0.5f;
            RestUrge = 0.0f;
            return;
        }

        if (!CanEat && !CanBreed)
        {
            RestUrge += .05f;
        }

        if (!Metabolism.Healing)
        {
            SafetyUrge = Mathf.Max(0.0f, SafetyUrge);
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

    public float EvaluateTile(UrbTile Tile, int TerrainType, int Size)
    {
        float TileValue = 0;

        if (HungerUrge > 0)
        {
            HungerUrge = Mathf.Min(1, HungerUrge);
            for(int f = 0; f < Eater.FoodScents.Length; f++)
            {
                TileValue += Tile.TerrainFilter[TerrainType][Size][Eater.FoodScents[f]] * HungerUrge;
            }

        }

        if(BreedUrge > 0)
        {
            BreedUrge = Mathf.Min(1, BreedUrge);
            for (int b = 0; b < Breeder.MateScents.Length; b++)
            {
                TileValue += Tile.TerrainFilter[TerrainType][Size][Breeder.MateScents[b]] * BreedUrge;
            }
            for (int b = 0; b < Breeder.RivalScents.Length; b++)
            {
                TileValue -= Tile.TerrainFilter[TerrainType][Size][Breeder.RivalScents[b]] * BreedUrge;
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

        Assert.IsFalse(float.IsInfinity(TileValue) || float.IsNaN(TileValue));
        
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
