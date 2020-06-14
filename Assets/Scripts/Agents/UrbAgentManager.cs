using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbAgentManager
{
    protected static List<UrbAgent> Agents;
    protected static bool Uninitialized = true;

    public static int AgentCount { get {
            if(Agents == null)
            {
                return 0;
            }
            return Agents.Count;
        }
    }

    public static void Initialize()
    {
        Agents = new List<UrbAgent>();

        Uninitialized = false;
    }

    public static void RegisterAgent(UrbAgent Input)
    {
        if(Uninitialized)
        {
            Initialize();
        }

        if(Agents.Contains(Input))
        {
            return;
        }

        Agents.Add(Input);
    }

    public static void UnregisterAgent(UrbAgent Input)
    {
        if(Uninitialized)
        {
            return;
        }

        if (Agents.Contains(Input))
        {
            Agents.Remove(Input);
        }
    }

    public static bool IsPaused { get; protected set; } = false;

    public static void SetPauseOnAllAgents(bool Pause)
    {
        if(IsPaused != Pause)
        {
            IsPaused = Pause;
            if (Agents != null)
            {
                for (int i = 0; i < Agents.Count; i++)
                {
                    Agents[i].Pause = Pause;
                }
            }
        }
    }
}
