using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbAgent))]
public class UrbEater : UrbBehaviour
{
    public UrbSubstanceTag[] FoodSubstances;
    public float BiteSize =10.0f;
    public UrbComposition Stomach;

    public UrbScentTag[] FoodScents { get; protected set; }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }
        
        Stomach = new UrbComposition();
        FoodScents = UrbSubstances.Scent(FoodSubstances);

        
        base.Initialize();
        if (mAgent.Body.BodyComposition == null)
        {
            mAgent.Body.Initialize();
        }

        mAgent.Body.BodyComposition.AddComposition(Stomach);
    }

    protected override bool ValidToInterval()
    {
        return base.ValidToInterval() && FoodSubstances.Length > 0 && Stomach != null;
    }

    override public IEnumerator FunctionalCoroutine()
    {
        UrbTile[] Search = GetSearchTiles(true);

        foreach (UrbTile Tile in Search)
        {
            if (Stomach.AvailableCapacity > 0)
            {

                if (Tile == null)
                {
                    continue;
                }

                if (Tile.CurrentContent == null || Tile.CurrentContent == mAgent)
                {
                    continue;
                }

                UrbBody PossibleFood = Tile.CurrentContent.Body;

                if (PossibleFood != null)
                {
                    //Debug.Log(gameObject.name + " Attempting to Eat from " + Tile.CurrentContent.gameObject.name);
                    for (int f = 0; f < FoodSubstances.Length; f++)
                    {
                        if (PossibleFood.BodyComposition == null)
                            yield break;

                        float Eaten = PossibleFood.BodyComposition.TransferTo(Stomach, FoodSubstances[f], BiteSize);
                        //Debug.Log("Eaten " + Eaten + " " + FoodSubstances[f].ToString());
                        if (Eaten >= BiteSize)
                        {
                            
                            yield break;
                        }

                    }
                }
            }
            else
            {
                yield break;
            }
        }

        yield return null;
    }

    override public UrbComponentData GetComponentData()
    {
        UrbComponentData Data = base.GetComponentData();

       
        UrbSubstance[] StomachContents = (Stomach == null)? new UrbSubstance[0] : Stomach.GetCompositionIngredients();

        Data.FieldArrays = new UrbFieldArrayData[]
        {
            UrbEncoder.GetArrayFromSubstances("StomachContents" , StomachContents),
        };

        Data.Fields = new UrbFieldData[]
        {
            new UrbFieldData { Name = "BiteSize", Value = BiteSize}
        };

        Data.StringArrays = new UrbStringArrayData[]
        {
            UrbEncoder.EnumsToArray("FoodSubstances", FoodSubstances),
            UrbEncoder.EnumsToArray("FoodScents",FoodScents)
        };

        return Data;
    }

    override public bool SetComponentData(UrbComponentData Data)
    {
        FoodSubstances = UrbEncoder.GetEnumArray<UrbSubstanceTag>("FoodSubstances", Data);
        FoodScents = UrbEncoder.GetEnumArray<UrbScentTag>("FoodScents", Data);
        Stomach = new UrbComposition(UrbEncoder.GetSubstancesFromArray("StomachContents", Data));
        return true;
    }
}
