using UnityEngine;
using System.Collections;

public class UrbAgentSpawner 
{
    public static bool SpawnAgent(GameObject Template, UrbTile Tile, UrbObjectData Data = null)
    {
        UrbAgent TestAgent = Template.GetComponent<UrbAgent>();

        if(TestAgent == null)
        {
            return false;
        }

        UrbTileprint TestPrint = new UrbTileprint(TestAgent.TileprintString);
        
        if(TestPrint.TilePrintCollisionCheck(Tile))
        {
            return false;
        }

        Vector3 SpawnLocation = Tile.Location;
        GameObject spawned = GameObject.Instantiate(Template, SpawnLocation, Quaternion.identity);
        UrbAgent Agent = spawned.GetComponent<UrbAgent>();

        if(Data != null)
        {

        }

        UrbBase[] BaseComponents = spawned.GetComponents<UrbBase>();
        for (int i = 0; i < BaseComponents.Length; i++)
        {
            BaseComponents[i].Initialize();
        }

        UrbObjectData AgentData = UrbEncoder.Read(spawned);
        string save = JsonUtility.ToJson(AgentData, true);
        Debug.Log(save);
        UrbEncoder.Write(AgentData, spawned);

        Tile.OnAgentArrive(Agent);
        
        return true;
    }
}
