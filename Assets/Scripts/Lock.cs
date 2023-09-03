using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Lock : MonoBehaviour
{
    public List<string> Keys = new List<string>();

    [NonSerialized]
    public Room lockRoom;

    public float floatHeight = 1.0f; // The maximum height the object will float above its initial position.
    public float floatSpeed = 1.0f; // The speed at which the object will float up and down.
    public float rotationSpeed = 30f;

    private Vector3 initialPosition;

    public Transform keyImageContainer;
    private void Start()
    {
        // Store the initial position of the object.
        initialPosition = transform.position;
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

    public bool Unlock(List<string> PlayerKeys)
    {
        foreach (string key in Keys)
        {
            if (!PlayerKeys.Contains(key))
                return false;
        }

        lockRoom.OpenDoors();
        return true;
    }

    public void AddKey(Key key)
    {
        Keys.Add(key.KeyGUID);

        GameObject image = new GameObject("Image" + Keys.Count);
        image.transform.parent = keyImageContainer;
        UnityEngine.UI.Image keyImage = image.AddComponent<UnityEngine.UI.Image>();
        image.transform.localPosition = Vector3.zero;

        keyImage.color = key.KeyColor;
    }
}
