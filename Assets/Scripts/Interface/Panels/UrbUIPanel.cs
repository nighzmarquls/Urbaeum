using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class UrbUIPanel : MonoBehaviour
{
    protected UrbInterfaceInput[] InterfaceInputs;

    private void Start()
    {
        Initialize();
    }
    protected virtual void Initialize()
    {
        InterfaceInputs = GetComponentsInChildren<UrbInterfaceInput>();

        if (InterfaceInputs == null)
        {
            Debug.LogWarning(this.GetType() + " on " + gameObject.name + " has no InterfaceInputs.");
        }
        Debug.Log(this.GetType() + " on " + gameObject.name + " has " + InterfaceInputs.Length + " InterfaceInputs");
    }
}
