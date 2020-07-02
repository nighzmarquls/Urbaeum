﻿using UnityEngine;
using System.Collections;
using Unity.Profiling;
public class UrbAgentSpawner 
{
    static ProfilerMarker s_SpawnAgent_prof = new ProfilerMarker("AgentSpawner.SpawnAgent");
    public static bool SpawnAgent(GameObject Template, UrbTile Tile, out GameObject spawned, UrbObjectData Data = null)
    {
        s_SpawnAgent_prof.Begin();
        
        UrbAgent TestAgent = Template.GetComponent<UrbAgent>();

        spawned = null;

        if (TestAgent == null)
        {
            return false;
        }

        UrbTileprint TestPrint = new UrbTileprint(TestAgent.TileprintString);
        
        if(TestPrint.TilePrintCollisionCheck(Tile))
        {
            return false;
        }

        Vector3 SpawnLocation = Tile.Location;
        spawned = GameObject.Instantiate(Template, SpawnLocation, Quaternion.identity);
        UrbAgent Agent = spawned.GetComponent<UrbAgent>();

        if (Data != null)
        {
            UrbEncoder.Write(Data, spawned);
            //Debug.Log(JsonUtility.ToJson(Data, true));
        }

        UrbBase[] BaseComponents = spawned.GetComponents<UrbBase>();
        for (int i = 0; i < BaseComponents.Length; i++)
        {
            BaseComponents[i].Initialize();
        }

        Tile.OnAgentArrive(Agent);
        UrbAgent.TotalAgents++; 
        
        s_SpawnAgent_prof.End();
        return true;
    }
}

public class UrbSpawnAction : UrbUserAction
{
    public GameObject SpawnedTemplate;

    public override void MouseClick(UrbTile currentCursorTile)
    {
   
        GameObject SpawnedObject;
        if(UrbAgentSpawner.SpawnAgent(SpawnedTemplate,currentCursorTile ,out SpawnedObject))
        {
       
        }
        else
        {
          
        }
    }

}
