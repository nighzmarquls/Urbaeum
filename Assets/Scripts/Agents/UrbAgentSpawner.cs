using UnityEngine;
using System.Collections;

public class UrbAgentSpawner 
{
    public static bool SpawnAgent(GameObject Template, UrbTile Tile, out GameObject spawned, UrbObjectData Data = null)
    {
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
        
        return true;
    }
}
