using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class UrbUIPanel : MonoBehaviour
{
    protected UrbInterfaceInput[] InterfaceInputs;

    protected virtual void Awake()
    {
        InterfaceInputs = GetComponentsInChildren<UrbInterfaceInput>();

        if (InterfaceInputs == null)
        {
            Debug.LogWarning(this.GetType() + " on " + gameObject.name + " has no InterfaceInputs.");
        }

    }
}
