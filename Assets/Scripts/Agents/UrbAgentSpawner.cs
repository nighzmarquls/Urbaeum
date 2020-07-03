using UnityEngine;
using Unity.Profiling;
public class UrbAgentSpawner 
{
    static ProfilerMarker s_SpawnAgent_prof = new ProfilerMarker("AgentSpawner.SpawnAgent");
    public static bool SpawnAgent(UrbAgent TestAgent, UrbTile Tile, out GameObject spawned, UrbObjectData Data = null)
    {
        s_SpawnAgent_prof.Begin();

        var Template = TestAgent.gameObject;
        
        if (TestAgent.WasDestroyed)
        {
            spawned = null;
            Debug.Log("Failed to spawn agent because TestAgent was null", TestAgent);
            s_SpawnAgent_prof.End();
            return false;
        }

        UrbTileprint TestPrint = new UrbTileprint(TestAgent.TileprintString);
        
        if(TestPrint.TilePrintCollisionCheck(Tile))
        {
            spawned = null;
            Debug.Log("Failed to spawn agent because of TestPrint check on Tile", Template);
            s_SpawnAgent_prof.End();
            return false;
        }

        Vector3 SpawnLocation = Tile.Location;
        spawned = Object.Instantiate(Template, SpawnLocation, Quaternion.identity);
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
    public UrbAgent AgentTemplate;
    public override void MouseClick(UrbTile currentCursorTile)
    {
        GameObject SpawnedObject;
        if(!UrbAgentSpawner.SpawnAgent(AgentTemplate,currentCursorTile, out SpawnedObject))
        {
            UnityEngine.Debug.LogWarning("Failed to spawn agent from click");
        }
    }

}
