using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class UrbAgentManager
{
    protected static List<UrbAgent> Agents;
    protected static bool Uninitialized = true;
    protected static bool Paused = false;
    public static int AgentCount { get
        {
            return Agents?.Count ?? 0;
        }
    }

    public static void Initialize()
    {
        Agents = new List<UrbAgent>();

        Uninitialized = false;
    }
    
    static ProfilerMarker s_UrbAgentMgr_Register_p = new ProfilerMarker("UrbAgentManager.RegisterAgent");
    public static void RegisterAgent(UrbAgent Input)
    {
        s_UrbAgentMgr_Register_p.Begin(Input);
        if(Uninitialized)
        {
            Initialize();
        }

        if(Agents.Contains(Input))
        {
            s_UrbAgentMgr_Register_p.End();
            return;
        }

        Agents.Add(Input);
        s_UrbAgentMgr_Register_p.End();
    }
    
    static ProfilerMarker s_UrbAgentMgr_UnRegister_p = new ProfilerMarker("UrbAgentManager.Unregister");
    public static void UnregisterAgent(UrbAgent Input)
    {
        s_UrbAgentMgr_UnRegister_p.Begin();
        if(Uninitialized)
        {
            return;
        }

        //We can attempt removal without checking if it contains
        //If we don't contain it... is a no-op.
        Agents.Remove(Input);
        s_UrbAgentMgr_UnRegister_p.End();
    }

    public static bool IsPaused { get; protected set; } = false;

    public static void SetPauseOnAllAgents(bool Pause)
    { 
        if (IsPaused == Pause)
        {
            return;
        }
        
        IsPaused = Pause;
        if (Agents == null)
        {
            return;
        }
        
        for (int i = 0; i < Agents.Count; i++)
        {
            Agents[i].Pause = Pause;
        }
    }
}
