using Unity.Assertions;
using UnityEngine;
using Unity.Profiling;
public class UrbAgentSpawner
{
    static ProfilerMarker s_SpawnAgent_prof = new ProfilerMarker("AgentSpawner.SpawnAgent");
    public static bool SpawnAgent(UrbAgent TestAgent, UrbTile Tile, out GameObject spawned, UrbObjectData Data = null)
    {
        Assert.IsTrue(Tile != null, "Tile != null");
        Assert.IsTrue(Tile.Occupants != null, "Tile.Occupants != null");
        
        s_SpawnAgent_prof.Begin();

        if (Tile.Occupants.Count >= UrbTile.MaximumOccupants)
        {
            spawned = null;
            //Debug.Log("Failed to spawn agent because Tile hit max occupancy!", TestAgent);
            s_SpawnAgent_prof.End();
            return false;
        }
        
        if (TestAgent.WasDestroyed)
        {
            spawned = null;
            //Debug.Log("Failed to spawn agent because TestAgent was null!", TestAgent);
            s_SpawnAgent_prof.End();
            return false;
        }

        UrbTileprint TestPrint = new UrbTileprint(TestAgent.TileprintString);
        
        if(TestPrint.TilePrintCollisionCheck(Tile))
        {
            spawned = null;
            //Debug.Log("Failed to spawn agent because of TestPrint check on Tile!");
            s_SpawnAgent_prof.End();
            return false;
        }
        
        Vector3 SpawnLocation = Tile.Location;

        spawned = Object.Instantiate(TestAgent.gameObject, SpawnLocation, Quaternion.identity);
        
        if (Data != null)
        {
            UrbEncoder.Write(Data, spawned);
        }

        // Tweaked the Critter prefabs so they *should* spawn as inactive
        // if (spawned.activeSelf == false)
        // {
        //     Debug.Log("the spawned object was inactive!");
        // }

        UrbAgent Agent = spawned.GetComponent<UrbAgent>();
        //Doing this should mean not required to manually activate children
        spawned.SetActive(true);
        
        if (!Agent.HasAwakeBeenCalled)
        {
            Agent.gameObject.SetActive(true);
        }
        
        //This loop is kept around mostly out of concern that Agent SetActive
        //may fail to trigger children. Decent chance this is no longer required.
        //light testing shows removing may be acceptable.
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
