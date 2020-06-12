using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class UrbInterfaceInput : MonoBehaviour
{
    public KeyCode Shortcut = KeyCode.None;
    public UrbUserAction UserAction;
    protected Image Icon;
    protected Button mButton;
    protected ButtonAnimator mButtonAnimator;

    public void SetIcon(Sprite Input)
    {
        if(Icon == null)
        {
            return;
        }

        
        if(Input == null)
        {
            Icon.color = Color.clear;
        }
        else
        {
            Icon.sprite = Input;
            Icon.color = Color.white;
        }
    }

    public void Start()
    {
        mButton = GetComponent<Button>();
        mButtonAnimator = GetComponent<ButtonAnimator>();

        mButton.onClick.AddListener(ButtonActivation);

        Transform Child = transform.GetChild(0);
        if(Child != null)
        {
            Icon = Child.GetComponent<Image>();
        }
     
    }

    public void ButtonActivation()
    {
        if (UserAction == null)
        {
            return;
        }

        UserAction.SelectAction();
    }

    public void ShortcutActivation()
    {
        PointerEventData Data = new PointerEventData(EventSystem.current);
        Data.position = mButton.transform.position;

        mButton.OnPointerClick(Data);
        mButton.OnPointerUp(Data);
        mButtonAnimator.OnPointerUp(Data);
    }

    private void Update()
    {
        if(Input.GetKeyDown(Shortcut))
        {
            PointerEventData Data = new PointerEventData(EventSystem.current);
            Data.position = mButton.transform.position;
            mButton.OnPointerDown(Data);
            mButtonAnimator.OnPointerDown(Data);
        }
        if(Input.GetKeyUp(Shortcut))
        {
            ShortcutActivation();
        }
    }
}
