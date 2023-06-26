using GG.Utils;
using PCG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [NonSerialized]
    public GGNode RoomNode;

    [SerializeField]
    public LineRenderer lineRendererPrefab;

    [NonSerialized]
    public PCGMap.GridCoord CellCoord;

    private int freePathways = 0;

    public List<LineRenderer> nodeLines = new List<LineRenderer>();
    public List<LineRenderer> keyLines = new List<LineRenderer>();

    public void AddConnection(Room room, Color color)
    {

        var lineRenderer = Instantiate(lineRendererPrefab, transform);
        lineRenderer.SetPosition(0, transform.position + Vector3.up * 0.2f);
        lineRenderer.SetPosition(1, room.transform.position + Vector3.up * 0.2f);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        if (color == Color.white)
        { 
            nodeLines.Add(lineRenderer);
        }
        else
        {
            keyLines.Add(lineRenderer);
        }
    }

    public void ToggleNodeLines(bool toggle)
    {
        foreach (var l in nodeLines)
        {
            l.gameObject.SetActive(toggle);
        }
    }

    public void ToggleKeyLines(bool toggle)
    {
        foreach (var l in keyLines)
        {
            l.gameObject.SetActive(toggle);
        }
    }
}
