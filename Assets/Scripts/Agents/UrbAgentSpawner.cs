using UnityEngine;
using Unity.Profiling;
public class UrbAgentSpawner 
{
    static ProfilerMarker s_SpawnAgent_prof = new ProfilerMarker("AgentSpawner.SpawnAgent");
    public static bool SpawnAgent(UrbAgent TestAgent, UrbTile Tile, out GameObject spawned, UrbObjectData Data = null)
    {
        s_SpawnAgent_prof.Begin();

        if (Tile.Occupants.Count >= UrbTile.MaximumOccupants)
        {
            spawned = null;
            Debug.Log("Failed to spawn agent because Tile hit max occupancy!", TestAgent);
            s_SpawnAgent_prof.End();
            return false;
        }
        
        if (TestAgent.WasDestroyed)
        {
            spawned = null;
            Debug.Log("Failed to spawn agent because TestAgent was null!", TestAgent);
            s_SpawnAgent_prof.End();
            return false;
        }

        UrbTileprint TestPrint = new UrbTileprint(TestAgent.TileprintString);
        
        if(TestPrint.TilePrintCollisionCheck(Tile))
        {
            spawned = null;
            Debug.Log("Failed to spawn agent because of TestPrint check on Tile!");
            s_SpawnAgent_prof.End();
            return false;
        }
        
        Vector3 SpawnLocation = Tile.Location;
        if (!TestAgent.enabled)
        {
            Debug.Log("TestAgent not enabled!");
        }
        
        spawned = Object.Instantiate(TestAgent.gameObject, SpawnLocation, Quaternion.identity);
        
        if (Data != null)
        {
            UrbEncoder.Write(Data, spawned);
        }
        
        if (spawned.activeSelf == false)
        {
            Debug.Log("the spawned object was inactive!");
        }
        
        spawned.SetActive(true);
        
        UrbAgent Agent = spawned.GetComponent<UrbAgent>();
        if (!Agent.enabled)
        {
            //This method should automatically be called by Unity.
            //I wonder why it's not always being called all the time
            Agent.gameObject.SetActive(true);
        }

        foreach (var urb in Agent.GetComponents<UrbBase>())
        {
            if (!urb.isActiveAndEnabled)
            {
                urb.gameObject.SetActive(true);
            }
        }

        Tile.OnAgentArrive(Agent);
        UrbAgent.TotalAgents++; 
        
        s_SpawnAgent_prof.End();
        return true;
    } 
}

public class UrbSpawnAction : UrbUserAction
{
    public UrbAgent AgentTemplate;
    public override void MouseClick(UrbTile currentCursorTile)
    {
        if(!UrbAgentSpawner.SpawnAgent(AgentTemplate,currentCursorTile, out _))
        {
            Debug.LogWarning("Failed to spawn agent from click");
        }
    }

}
