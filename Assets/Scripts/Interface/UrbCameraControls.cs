using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbCameraControls : MonoBehaviour
{
    public float CameraSpeed = 1.0f;
    public float CameraZoomSpeed = 1.0f;
    public float MinZoomSize = 1.25f;
    Camera mCamera;

    public Vector3 CursorWorldPosition {
        get {
            return WorldPosition;
        }
    }

    public float AdjustedCameraSpeed {  get { return CameraSpeed * (mCamera.orthographicSize / StartingSize);  } }

    Vector3 ViewportPosition;
    protected Vector3 WorldPosition;
    float StartingSize;
    // Start is called before the first frame update
    void Start()
    {
        mCamera = GetComponent<Camera>();
        mCamera.orthographic = true;
        StartingSize = mCamera.orthographicSize;
    }

    void SynchronizeMousePosition()
    {
        ViewportPosition = mCamera.ScreenToViewportPoint(Input.mousePosition);
        WorldPosition = mCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    Vector3 GetScreenPushInput()
    {
        Vector3 PushInput = Vector2.zero;

        PushInput.x = ViewportPosition.x > 0.9f ? 1 : ViewportPosition.x < 0.1f ? -1 : 0;

        PushInput.y = ViewportPosition.y > 0.9f ? 1 : ViewportPosition.y < 0.1f ? -1 : 0;

        return PushInput ;
    }

    Vector3 GetKeyboardInput()
    {
        Vector3 KeyboardInput = Vector3.zero;

        KeyboardInput.x = Input.GetAxis("Horizontal");
        KeyboardInput.y = Input.GetAxis("Vertical");

        return KeyboardInput ;
    }

    float GetZoomTarget()
    {
        float size = mCamera.orthographicSize;

        if(Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus))
        {
            size -= CameraZoomSpeed*Time.deltaTime;
        }
        else if(Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            size += CameraZoomSpeed * Time.deltaTime;
        }

        size -= Input.mouseScrollDelta.y * CameraZoomSpeed * Time.deltaTime;


        return Mathf.Max(size, MinZoomSize);
    }

    // Update is called once per frame
    void Update()
    {
        SynchronizeMousePosition();
        Vector3 CameraMoveInput = GetKeyboardInput();//  GetScreenPushInput() + GetKeyboardInput();
        mCamera.orthographicSize = GetZoomTarget();
        this.transform.position += (CameraMoveInput*Time.deltaTime* AdjustedCameraSpeed);
    }
}
    