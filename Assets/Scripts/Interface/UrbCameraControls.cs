using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbCameraControls : MonoBehaviour
{
    protected static UrbAgent CameraFocus;
    protected static bool CameraIsFocused;
    public static UrbAgent Focus {
        get
        {
            return CameraFocus;
        }
        set {
            CameraIsFocused = value != null;
            CameraFocus = value;
        }
    }

    public void ClearFocus()
    {
        CameraIsFocused = false;
        CameraFocus = null;
    }
    
    public float CameraSpeed = 1.0f;
    public float CameraZoomSpeed = 1.0f;
    public float MinZoomSize = 1.25f;

    public float ScreenPushPixelBuffer = 60;

    public Vector3 CameraStartLocation;
    Camera mCamera;

    float FarViewportPadding = 0.9f;
    float NearViewportPadding = 0.1f;

    public Vector3 CursorWorldPosition {
        get {
            return WorldPosition;
        }
    }

    public float AdjustedCameraSpeed {  get { return ( CameraSpeed * (mCamera.orthographicSize / StartingSize) );  } }

    Vector3 ViewportPosition;
    protected Vector3 WorldPosition;
    float StartingSize;
    // Start is called before the first frame update
    void Start()
    {
        mCamera = GetComponent<Camera>();
        mCamera.orthographic = true;
        StartingSize = mCamera.orthographicSize;
        CameraStartLocation = mCamera.transform.position;

        float bufferSize = ScreenPushPixelBuffer / Screen.height;
        FarViewportPadding = 1 - bufferSize;
        NearViewportPadding = bufferSize;
    }

    void SynchronizeMousePosition()
    {
        ViewportPosition = mCamera.ScreenToViewportPoint(Input.mousePosition);
        WorldPosition = mCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    void RecenterCamera()
    {
        mCamera.transform.position = CameraStartLocation;
    }

    Vector3 GetScreenPushInput()
    {
        Vector3 PushInput = Vector2.zero;

        if (UrbUIManager.MouseOver)
        {
            PushInput.x = ViewportPosition.x > FarViewportPadding ? 1 : ViewportPosition.x < NearViewportPadding ? -1 : 0;
            PushInput.y = ViewportPosition.y > FarViewportPadding ? 1 : ViewportPosition.y < NearViewportPadding ? -1 : 0;
        }

        return PushInput ;
    }

    Vector3 GetKeyboardInput()
    {
        Vector3 KeyboardInput = Vector3.zero;

        KeyboardInput.x = Input.GetAxisRaw("Horizontal");
        KeyboardInput.y = Input.GetAxisRaw("Vertical");

        return KeyboardInput ;
    }

    float GetZoomTarget()
    {
        float size = mCamera.orthographicSize;

        if(Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus))
        {
            size -= (CameraZoomSpeed*Time.unscaledDeltaTime);
        }
        else if(Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            size += (CameraZoomSpeed * Time.unscaledDeltaTime);
        }

        size -= (Input.mouseScrollDelta.y * CameraZoomSpeed * Time.unscaledDeltaTime) ;


        return Mathf.Max(size, MinZoomSize);
    }

    // Update is called once per frame
    void Update()
    {
        SynchronizeMousePosition();
        Vector3 CameraMoveInput = GetScreenPushInput() + GetKeyboardInput();
        mCamera.orthographicSize = GetZoomTarget();
        if (CameraMoveInput.magnitude > 0)
        {
            ClearFocus();
            this.transform.position += (CameraMoveInput * (Time.unscaledDeltaTime * AdjustedCameraSpeed));
        }
        //Temporary until we have non-ui shortcuts.
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (CameraIsFocused)
            {
                ClearFocus();
            }
            RecenterCamera();
        }

        if(CameraIsFocused)
        {
            Vector3 FocusLocation = Focus.transform.position;
            FocusLocation.z = this.transform.position.z;
            transform.position = FocusLocation;
        }
    }
}
    