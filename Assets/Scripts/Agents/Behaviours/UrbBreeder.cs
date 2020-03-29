using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UrbBreedTag
{
    Plant,
    Bun,
    MaxBreedTag
}

[RequireComponent(typeof(UrbAgent))]
[RequireComponent(typeof(UrbBody))]
public class UrbBreeder : UrbBehaviour
{
    public int MateRequirement = 0;
    public int MateCrowding = 8;

    public float Gestation = 1.0f;
    public UrbSubstance[] GestationRecipe;
    public UrbBreedTag BreedType = UrbBreedTag.Plant;

    public int OffspringCount = 1;
    public GameObject[] OffspringObjects;

    public UrbScentTag[] MateScents;
    public UrbScentTag[] RivalScents;

    public bool Gestating { get; protected set; }

    public override void Initialize()
    {
        if (bInitialized)
        {
            return;
        }
        Gestating = false;
        base.Initialize();
       
    }

    protected override bool ValidToInterval()
    {
        return base.ValidToInterval() && OffspringObjects.Length > 0;
    }

    override public IEnumerator FunctionalCoroutine()
    {
        if (!ValidToInterval())
            yield return null;
        UrbTile[] Adjacent = mAgent.CurrentMap.GetNearestAdjacent(transform.position, true);

        int MateCount = 0;

        foreach(UrbTile Tile in Adjacent)
        {
            yield return BehaviourThrottle.PerformanceThrottle();

            if (Tile == null)
            {
                continue;
            }

            if(Tile.CurrentContent == null)
            {
                continue;
            }

            UrbBreeder MateCandidate = Tile.CurrentContent.GetComponent<UrbBreeder>();

            if(MateCandidate != null && MateCandidate != this && MateCandidate.BreedType == BreedType)
            {
                MateCount++;
            }
        }

        yield return null;

        if (MateCount >= MateRequirement && MateCount < MateCrowding && OffspringCount > 0 && mAgent.Body.BodyComposition.ContainsMoreOrEqualThan(GestationRecipe))
        {
            Gestating = true;
            yield return new WaitForSeconds(Gestation);

            if (!ValidToInterval())
                yield return null;

            Adjacent = mAgent.CurrentMap.GetNearestAdjacent(transform.position);
            int NumberOffspring = 0;
            int Delay = Random.Range((int)0, (int)3);

            foreach (UrbTile Tile in Adjacent)
            {
                yield return BehaviourThrottle.PerformanceThrottle();

                if (Tile == null || Tile.Blocked)
                {
                    continue;
                }

                if (Tile.CurrentContent != null)
                    continue;

                if (Delay > 0)
                {
                    Delay--;
                    continue;
                }

                int OffspringChoice = Random.Range(0, OffspringObjects.Length);

                GameObject OffspringObject = OffspringObjects[OffspringChoice];

                if (UrbAgentSpawner.SpawnAgent(OffspringObject, Tile))
                {
                    Delay = Random.Range((int)0, (int)2);
                    NumberOffspring++;
                    mAgent.Body.BodyComposition.RemoveRecipe(GestationRecipe);
                }

                if (OffspringCount <= NumberOffspring || mAgent.Body.BodyComposition.ContainsLessThan(GestationRecipe))
                {
                    Gestating = false;
                    yield break;
                }
            }
            Gestating = false;
        }
    }
}
