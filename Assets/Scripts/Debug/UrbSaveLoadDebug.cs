using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UrbSystemIO))]
public class UrbSaveLoadDebug : MonoBehaviour
{
    UrbSystemIO IO;


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
    // Update is called once per frame
    void Update()
    {
       
    }
}
