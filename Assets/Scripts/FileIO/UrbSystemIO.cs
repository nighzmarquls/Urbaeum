using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UrbSystemIO : MonoBehaviour
{
    public static UrbSystemIO Instance { get; protected set; }

    public List<UrbAgent> AgentTypes;

    [SerializeField]
    protected List<UrbMap> Maps;

    public static void RegisterMap(UrbMap Map)
    {
        if(Instance.Maps == null)
        {
            Instance.Maps = new List<UrbMap>();
        }

        if(Instance.Maps.Contains(Map))
        {
            return;
        }

        Instance.Maps.Add(Map);
    }

    public static void UnregisterMap(UrbMap Map)
    {
        if(Instance.Maps == null || !Instance.Maps.Contains(Map))
        {
            return;
        }
        Instance.Maps.Remove(Map);
    }

    protected static float OffsetTime = 0;
    public static float CurrentTime {get {
            return OffsetTime + Time.time;
        }
    }

    public bool Loading { get; protected set; }
    UrbSave GameData;

    public void CollectGameData()
    {
        if(Maps == null || Maps.Count <= 0)
        {
            Debug.LogError("Missing Reference to UrbMap, Cannot collect Game Data");
        }
        if(AgentTypes == null || AgentTypes.Count <= 0)
        {
            Debug.LogError("Missing UrbAgent Prefab Key, Cannot collect Game Data");
        }

        GameData = new UrbSave();
        GameData.Maps = new UrbMapData[Maps.Count];
        GameData.OffsetTime = CurrentTime;

        for (int i = 0; i < Maps.Count; i ++)
        {
            GameData.Maps[i] = Maps[i].GetMapData();
        }
    }

    public void AssignGameData()
    {
        Loading = true;


        if (Maps == null || Maps.Count <= 0)
        {
            Debug.LogError("Missing Reference to UrbMap, Cannot assign Game Data");
        }
        if (AgentTypes == null || AgentTypes.Count <= 0)
        {
            Debug.LogError("Missing UrbAgent Prefab Key, Cannot assign Game Data");
        }

        for (int i = 0; i < Maps.Count; i++)
        {
            Maps[i].LoadMapFromData(GameData.Maps[i]);
        }
        OffsetTime = GameData.OffsetTime - Time.time;

        Loading = false;
    }

    public void SaveGameDataToFile(string filename = "/gamesave.txt")
    {
        string save = JsonUtility.ToJson(GameData, true);
        Debug.Log("Saving to:" + Application.dataPath + filename);
        StreamWriter file = new StreamWriter(Application.dataPath + filename, false);

        file.Write(save);
        file.Close();
    }

    public void LoadGameDataFromFile(string filename = "/gamesave.txt")
    {
        StreamReader file = new StreamReader(Application.dataPath + filename);
        string load = file.ReadToEnd();
        file.Close();
        GameData = JsonUtility.FromJson<UrbSave>(load);
    }

    public static int GetMapID(UrbMap input)
    {
        if(Instance == null)
        {
            return -1;
        }
        return Instance.Maps.IndexOf(input);
    }

    public static UrbMap GetMapFromID(int ID)
    {
        if (ID < 0 || Instance == null || ID >= Instance.Maps.Count)
        {
            return null;
        }

        return Instance.Maps[ID];

    }

    public static int GetAgentID(UrbAgent input)
    {
        if (Instance == null)
        {
            return -1;
        }
        for(int i = 0; i < Instance.AgentTypes.Count; i++)
        {
            UrbAgent candidate = Instance.AgentTypes[i];
            if(candidate.TemplatesMatch(input))
            {
                return i;
            }
        }

        return -1;
    }

    public static UrbAgent LoadAgentFromID(int ID, UrbTile Tile, UrbObjectData Data)
    {
        if (ID < 0 || Instance == null || ID >= Instance.AgentTypes.Count)
        {
            return null;
        }
        GameObject AgentObject;
        if (UrbAgentSpawner.SpawnAgent(Instance.AgentTypes[ID].gameObject, Tile, out AgentObject, Data))
        {
            UrbAgent LoadedAgent = AgentObject.GetComponent<UrbAgent>();
            return LoadedAgent;
        }
        return null;
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDisable()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}
