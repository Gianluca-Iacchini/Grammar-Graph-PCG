using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{

    public string KeyGUID;

    public float floatHeight = 1.0f; // The maximum height the object will float above its initial position.
    public float floatSpeed = 1.0f; // The speed at which the object will float up and down.
    public float rotationSpeed = 30f;

    private Vector3 initialPosition;

    private static HashSet<Color> generatedColors = new HashSet<Color>();

    public Color KeyColor { get; private set; }

    private void Start()
    {
        // Store the initial position of the object.
        initialPosition = transform.position;
    }

    public void SetColor()
    {
        Color color = GenerateRandomColors();
        this.GetComponent<MeshRenderer>().material.color = color;
        KeyColor = color;
    }

    public void SetPosition(Vector3 pos)
    {
        this.transform.position = pos;
        initialPosition = pos;
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

    private Color GenerateRandomColors()
    {
        Color randomColor;
        do
        {
            randomColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }
        while (generatedColors.Contains(randomColor)); // Check if the color has already been generated

        generatedColors.Add(randomColor); // Add the new unique color to the HashSet

        return randomColor;
    }
}
