using System;
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

    public List<string> PlayerKeys = new List<string>();

    [SerializeField]
    private Transform KeySpriteContainer;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            Key key = other.gameObject.GetComponent<Key>();
            if (key != null)
            {
                PlayerKeys.Add(key.KeyGUID);
                AddKeySprite(key);
            }
            other.gameObject.SetActive(false);
        }
        else if (other.gameObject.layer == 6)
        {
            Lock _lock = other.gameObject.GetComponent<Lock>();
            if (_lock != null)
            {
                bool unlocked = _lock.Unlock(PlayerKeys);
                other.gameObject.SetActive(!unlocked);
            }

        }
        else if (other.gameObject.layer == 8)
        {
            Task task = other.gameObject.GetComponent<Task>();
            if (task != null)
            {
                task.CompleteTask();
            }
            other.gameObject.SetActive(false);
        }
    }

    private void AddKeySprite(Key key)
    {
        GameObject image = new GameObject("Image");
        image.transform.parent = KeySpriteContainer;
        UnityEngine.UI.Image keyImage = image.AddComponent<UnityEngine.UI.Image>();
        image.transform.localPosition = Vector3.zero;
        image.transform.localScale = Vector3.one;
        image.transform.localRotation = Quaternion.identity;

        keyImage.color = key.KeyColor;
    }
}
