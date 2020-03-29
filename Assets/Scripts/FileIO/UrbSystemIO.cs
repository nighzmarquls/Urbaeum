using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UrbSystemIO : MonoBehaviour
{
    protected static UrbSystemIO instance;

    [SerializeField]
    protected List<UrbAgent> AgentTypes;

    [SerializeField]
    protected List<UrbMap> Maps;

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

        for (int i = 0; i < Maps.Count; i ++)
        {
            GameData.Maps[i] = Maps[i].GetMapData();
        }
    }

    public void AssignGameData()
    {
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
        if(instance == null)
        {
            return -1;
        }
        return instance.Maps.IndexOf(input);
    }

    public static UrbMap GetMapFromID(int ID)
    {
        if (ID < 0 || instance == null || ID >= instance.Maps.Count)
        {
            return null;
        }

        return instance.Maps[ID];

    }

    public static int GetAgentID(UrbAgent input)
    {
        if (instance == null)
        {
            return -1;
        }
        for(int i = 0; i < instance.AgentTypes.Count; i++)
        {
            UrbAgent candidate = instance.AgentTypes[i];
            if(candidate.TemplatesMatch(input))
            {
                return i;
            }
        }

        return -1;
    }

    public static UrbAgent LoadAgentFromID(int ID)
    {
        if (ID < 0 || instance == null || ID >= instance.AgentTypes.Count)
        {
            return null;
        }
        GameObject AgentObjject = Instantiate(instance.AgentTypes[ID].gameObject);
        UrbAgent LoadedAgent = AgentObjject.GetComponent<UrbAgent>();
        return LoadedAgent;
    }

    private void OnEnable()
    {
        if (instance == null)
        {

            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDisable()
    {
        if(instance == this)
        {
            instance = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
