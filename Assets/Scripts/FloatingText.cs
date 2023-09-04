using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float floatHeight = 1.0f; // The maximum height the object will float above its initial position.
    public float floatSpeed = 1.0f; // The speed at which the object will float up and down.
    public float rotationSpeed = 30f;

    private Vector3 initialPosition;

    public void SetPosition(Vector3 pos)
    {
        this.transform.position = pos;
        initialPosition = pos;
    }

    private void Start()
    {
        // Store the initial position of the object.
        initialPosition = transform.position;
    }

    private void Update()
    {
        // Calculate the new Y position of the object using a sine wave.
        float newY = initialPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        // Update the object's position.
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Rotate the object around its local Y axis at 1 degree per second
        transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed);
    }

    public void SetText(string text)
    {
        this.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }
}
