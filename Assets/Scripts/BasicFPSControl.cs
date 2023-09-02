using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BasicFPSControl : MonoBehaviour
{
    // Start is called before the first frame update

    public float moveSpeed = 5.0f;
    public float sensitivity = 2.0f;
    public float jumpForce = 50.0f;

    private Camera playerCamera;
    private CharacterController characterController;
    private float rotationX = 0;

    private bool isActive = false;

    public void Activate()
    {
        isActive = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Deactivate()
    {
        isActive = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        Activate();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isActive) return;

        // Player movement
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 movement = transform.TransformDirection(new Vector3(horizontalInput, 0, verticalInput) * moveSpeed);

        // Jumping
        if (characterController.isGrounded)
        {
            if (Input.GetButtonDown("Jump"))
            {
                movement.y = jumpForce;
            }
        }

        // Apply gravity
        movement.y -= 9.8f * Time.deltaTime;

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90, 90);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, mouseX, 0);

        // Apply movement
        characterController.Move(movement * Time.deltaTime);
    }
}
