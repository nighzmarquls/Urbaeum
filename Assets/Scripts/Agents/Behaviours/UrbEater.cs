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

    }

    protected override bool ValidToInterval()
    {
        return base.ValidToInterval() && FoodSubstances.Length > 0 && Stomach != null;
    }

    override public IEnumerator FunctionalCoroutine()
    {
        UrbTile[] Adjacent = mAgent.Tileprint.GetBorderingTiles(mAgent, true);

        foreach (UrbTile Tile in Adjacent)
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
}
