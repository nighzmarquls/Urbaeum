using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

[RequireComponent(typeof(UrbSystemIO))]
public class UrbSaveLoadDebug : MonoBehaviour
{
    UrbSystemIO IO;
    public static bool Loading = false;

    // Start is called before the first frame update
    void Start()
    {
        IO = GetComponent<UrbSystemIO>();
    }

    public void StartSave()
    {
        IO.CollectGameData();
        IO.SaveGameDataToFile();
    }

    public void StartLoad()
    {
        IO.LoadGameDataFromFile();
        IO.AssignGameData();
    }

}
