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

    public void AddConnection(Room room, Color color)
    {


        var lineRenderer = Instantiate(lineRendererPrefab, transform);
        lineRenderer.SetPosition(0, transform.position + Vector3.up * 0.2f);
        lineRenderer.SetPosition(1, room.transform.position + Vector3.up * 0.2f);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    public Vector2Int GetEntrance(int CorridorThickness)
    {
        int i = (this.CellCoord.I + 1) * CorridorThickness; 
        int j = (this.CellCoord.J + 1) * CorridorThickness;

        int offset = Mathf.FloorToInt(CorridorThickness / 2f);

        return new Vector2Int(i, j);
    }

    public Vector2Int GetExit(int CorridorThickness)
    {
        int i = (this.CellCoord.I + 1) * CorridorThickness;
        int j = (this.CellCoord.J + 1) * CorridorThickness;

        return new Vector2Int(i, j);
    }
}
