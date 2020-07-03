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
    public bool Disabled = false;
    protected Image Icon;
    protected Button mButton;
    protected ButtonAnimator mButtonAnimator;

    public Sprite GetIcon()
    {
        if (Icon == null)
        {
            return null;
        }

        return this.Icon.sprite;
    }

    public void SetIcon(Sprite Icon, Color IconColor)
    {
        if(this.Icon == null)
        {
            return;
        }

        
        this.Icon.sprite = Icon;
        this.Icon.color = IconColor;
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
        UserAction?.SelectAction();
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
        if (Disabled)
        {
            return;
        }

        if (Input.GetKeyDown(Shortcut))
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
