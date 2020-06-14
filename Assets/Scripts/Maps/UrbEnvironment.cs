using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UrbEnvironmentCondition
{
    Heat = 0,
    MaxEnvironmentCondition
}

public class UrbEnvironment
{
    protected const uint MaxCond = (uint)UrbEnvironmentCondition.MaxEnvironmentCondition;
    float[] Conditions;
    float[] Transfer;
    bool Dirty = false;
    public static float EnvironmentInterval = 0.25f;
    UrbTile OwningTile;

    public void MakeDirty()
    {
        Dirty = true;
    }

    public float this[UrbEnvironmentCondition i] {
        get { return this.Conditions.Length > (uint)i ? this.Conditions[(uint)i] : 0; }
        set {
            if (this.Conditions.Length < (uint)i)
            {
                return;
            }
            this.Conditions[(uint)i] = value;
            Dirty = true;
        }
    }

    public void SetTransfer(UrbEnvironmentCondition i, float value)
    {
        if(this.Conditions.Length > (uint)i)
        {
            Transfer[(uint)i] = value;
        }
    }

    public UrbEnvironment(UrbTile Owner)
    {
        Conditions = new float[MaxCond];
        Transfer = new float[MaxCond];

        for (int i = 0; i< MaxCond; i++)
        {
            Conditions[i] = 0.0f;
            Transfer[i] = 1.0f;
        }

        OwningTile = Owner;
    }

    public UrbEnvironmentData GetEnvironmentData()
    {
        UrbEnvironmentData output = new UrbEnvironmentData();

        output.Conditions = new float[MaxCond];
        output.Transfer = new float[MaxCond];
        
        for (int i = 0; i < MaxCond; i++)
        {
            output.Conditions[i] = Conditions[i];
            output.Transfer[i] = Transfer[i];
        }
        output.Dirty = Dirty;
        return output;
    }

    public void LoadEnvironmentFromData(UrbEnvironmentData input)
    {
        if(input.Conditions.Length != MaxCond)
        {
            Debug.LogError("Condition length loaded of " + input.Conditions.Length + " in Environment Does not match Number of Conditions");
        }
        if (input.Transfer.Length != MaxCond)
        {
            Debug.LogError("Transfer length oaded of " + input.Transfer.Length + " in Environment Does not match Number of Conditions");
        }
        Conditions = new float[MaxCond];
        Transfer = new float[MaxCond];
        for (int i = 0; i < MaxCond; i++)
        {
            Conditions[i] = input.Conditions[i];
            Transfer[i] = input.Transfer[i];
        }
        Dirty = input.Dirty;
    }

    UrbUtility.UrbThrottle EnvironmentThrottle = new UrbUtility.UrbThrottle();

    public IEnumerator EnvironmentCoroutine()
    {
        while(true)
        {
            if (Dirty)
            {

                yield return EnvironmentThrottle.PerformanceThrottle();
               
                Dirty = false;
                PerformEnvironmentFlow();
                
            }
            yield return new WaitForSeconds(EnvironmentInterval*OwningTile.TimeMultiplier);
        }
    }

    float GetPull(UrbEnvironmentCondition targetCond, float Value)
    {
        float Diff = this[targetCond] - Value;

        return Diff * Transfer[(int)targetCond];
    }

    void PerformConditionFlow(int targetCond, UrbTile[] Adjacency)
    {
        PerformConditionFlow( (UrbEnvironmentCondition) targetCond, Adjacency);
    }

    void PerformConditionFlow(UrbEnvironmentCondition targetCond, UrbTile[] Adjacency)
    {
        float startingCondition = this[targetCond];

        int IDBestTile = -1;
        float AbsBestPull = 0;
        float BestPull = 0;

        for (int i = 0; i < Adjacency.Length; i++)
        {
            if (Adjacency[i] == null)
                continue;

            float Pull = Adjacency[i].Environment.GetPull(targetCond, startingCondition);
            float AbsPull = Mathf.Abs(Pull);
            if(AbsBestPull < AbsPull)
            {
                IDBestTile = i;
                AbsBestPull = AbsPull;
                BestPull = Pull;
            }
        }

        if (IDBestTile > -1 && AbsBestPull > float.Epsilon)
        {
            float Push = Transfer[(int)targetCond] * BestPull * 0.5f;
            Adjacency[IDBestTile].Environment[targetCond] -= Push;
            this[targetCond] += Push;
        }
    }

    void PerformEnvironmentFlow()
    {
        UrbTile[] Adjacency = OwningTile.GetAdjacent(true);
 
        for(int i = 0; i < MaxCond; i++)
        {
            PerformConditionFlow(i, Adjacency);
        }

    }
}
