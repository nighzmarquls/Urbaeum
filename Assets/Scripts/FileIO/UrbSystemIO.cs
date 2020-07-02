using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UrbSystemIO : MonoBehaviour
{
    public static bool HasInstance;
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
            return;
        }
        if(AgentTypes == null || AgentTypes.Count <= 0)
        {
            Debug.LogError("Missing UrbAgent Prefab Key, Cannot collect Game Data");
            return;
        }

        GameData = new UrbSave
        {
            Maps = new UrbMapData[Maps.Count],
            OffsetTime = CurrentTime
        };

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
            return;
        }
        if (AgentTypes == null || AgentTypes.Count <= 0)
        {
            Debug.LogError("Missing UrbAgent Prefab Key, Cannot assign Game Data");
            return;
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
        if(!HasInstance)
        {
            return -1;
        }
        return Instance.Maps.IndexOf(input);
    }

    public static UrbMap GetMapFromID(int ID)
    {
        if (ID < 0 || !HasInstance || ID >= Instance.Maps.Count)
        {
            return null;
        }

        return Instance.Maps[ID];

    }

    public static int GetAgentID(UrbAgent input)
    {
        if (!HasInstance)
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
        if (ID < 0 || !HasInstance || ID >= Instance.AgentTypes.Count)
        {
            return null;
        }

        if (!UrbAgentSpawner.SpawnAgent(Instance.AgentTypes[ID].gameObject, Tile, out var AgentObject, Data))
        {
            return null;
        }
        
        UrbAgent LoadedAgent = AgentObject.GetComponent<UrbAgent>();
        return LoadedAgent;
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
           HasInstance = true;
           Instance = this;
           UrbSubstances.RegisterSubstanceProperties();
           return; 
        }
        
        if (Debug.isDebugBuild || Debug.developerConsoleVisible)
        {
            Debug.LogWarning("OnEnable self-destruct");
        }
        Destroy(this);
    }

    private void OnDisable()
    {
        Instance = null;
        HasInstance = false;
    }
}

public class UrbSaveAction : UrbUserAction
{
    public override string Name => "Save";
    public override string MapDisplayAssetPath => "";

    public override void SelectAction()
    {
        if (UrbUIManager.Instance.CurrentAction != null && UrbUIManager.Instance.CurrentAction == this)
        {
            return;
        }

        UrbUIManager.Instance.SetPause(true);
        UrbSystemIO.Instance.CollectGameData();
        UrbSystemIO.Instance.SaveGameDataToFile();
        UrbUIManager.Instance.SetPause(false);

        base.SelectAction();
    }
}

public class UrbLoadAction : UrbUserAction
{
    public override string Name => "Load";
    public override string MapDisplayAssetPath => "";

    public override void SelectAction()
    {
        if (UrbUIManager.Instance.CurrentAction != null && UrbUIManager.Instance.CurrentAction == this)
        {
            return;
        }

        UrbUIManager.Instance.SetPause(true);
        UrbSystemIO.Instance.LoadGameDataFromFile();
        UrbSystemIO.Instance.AssignGameData();
        UrbUIManager.Instance.SetPause(false);

        base.SelectAction();
    }
}