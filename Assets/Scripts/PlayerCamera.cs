using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform _inPlayerCameraPosition;
    [SerializeField] private Transform _cameraParent;
    [SerializeField] private Transform _orientation;
    
    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity;
    [SerializeField] private float lerpParameter;
    [SerializeField] private float minPitch;
    [SerializeField] private float maxPitch;
    [SerializeField] private bool invertPitch;
    
    [Header("Cursor Settings")]
    [SerializeField] private CursorLockMode cursorLockMode;
    [SerializeField] private bool cursorVisible;
    
    [Header("Input")]
    [SerializeField] private Vector2 cachedMouseDeltaPosition;
    
    public void OnLook(InputValue value)
    {
        cachedMouseDeltaPosition = value.Get<Vector2>();
    }

    private void Start()
    {
        SetCursor();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if(hasFocus) SetCursor();
    }

    private void Update()
    {
        UpdatePositionAndRotation();
    }

    private void UpdatePositionAndRotation()
    {
        _cameraParent.position = _inPlayerCameraPosition.position;
        float pitch = _cameraParent.eulerAngles.x;
        if(pitch > 180f) pitch -= 360f;
        float yaw = _cameraParent.eulerAngles.y; 
        
        float deltaPitch = cachedMouseDeltaPosition.y * mouseSensitivity * Time.deltaTime;
        if(invertPitch) deltaPitch *= -1f;
        float deltaYaw = cachedMouseDeltaPosition.x * mouseSensitivity * Time.deltaTime;
        
        pitch = Mathf.Clamp(pitch + deltaPitch, minPitch, maxPitch);
        yaw += deltaYaw;
        
        Quaternion startRotation = Quaternion.Euler(_cameraParent.eulerAngles.x, _cameraParent.eulerAngles.y, 0f);
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        Quaternion newRotation = Quaternion.Slerp(startRotation, targetRotation, lerpParameter);

        _cameraParent.rotation = newRotation;
        _orientation.rotation = Quaternion.Euler(0f, newRotation.eulerAngles.y, 0f);
    }

    private void SetCursor()
    {
        Cursor.lockState = cursorLockMode;
        Cursor.visible = cursorVisible;
    }
}
