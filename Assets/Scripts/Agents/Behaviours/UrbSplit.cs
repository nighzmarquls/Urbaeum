using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct UrbSplitProduct
{
    public float ProductRequiredMass;
    public float RequiredSpace;
    public UrbAgent ProductAgent;
}

public class UrbSplit : UrbBehaviour
{
    public UrbSplitProduct[] PossibleProducts;
    public override bool ShouldInterval => false;
    public override bool DeathBehaviour => true;

    public override void ExecuteTileBehaviour()
    {
        if (PossibleProducts == null || PossibleProducts.Length == 0)
        {
            Debug.LogWarning("Attempting Split with no Split Products on " + gameObject.name);
            return;
        }

        UrbTile[] SplitTileTargets = mAgent.OccupiedTiles;

        if(SplitTileTargets.Length == 0)
        {
            SplitTileTargets = mAgent.Tileprint.GetAllPrintTiles(mAgent);
        }

        float AvailableMass = mAgent.Mass;

        for (int p = 0; p < PossibleProducts.Length; p++)
        {
            UrbSplitProduct PossibleProduct = PossibleProducts[p];

            if(AvailableMass < PossibleProduct.ProductRequiredMass)
            {
                continue;
            }


            for (int s = 0; s < SplitTileTargets.Length; s++)
            {
                UrbTile Tile = SplitTileTargets[s];
                if (Tile == null || Tile.Blocked)
                {
                    continue;
                }

                if (Tile.FreeCapacity < PossibleProduct.RequiredSpace)
                {
                    continue;
                }

                if (AvailableMass < PossibleProduct.ProductRequiredMass)
                {
                    break;
                }

                GameObject SplitObject;

                if (UrbAgentSpawner.SpawnAgent(PossibleProduct.ProductAgent, Tile, out SplitObject))
                {
                    AvailableMass -= PossibleProduct.ProductRequiredMass;
                    UrbMetabolism SplitMetabolism = SplitObject.GetComponent<UrbMetabolism>();
                    if(SplitMetabolism != null)
                    {
                        SplitMetabolism.GrowBody(PossibleProduct.ProductRequiredMass);
                    }
                }
            }
        }

        base.ExecuteTileBehaviour();
    }
}
