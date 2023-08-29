using GG.Utils;
using PCG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Room : MonoBehaviour
{
    enum WallFace
    {
        Front,
        Back,
        Top,
        Down,
        Left,
        Right
    }

    private class CorridorLink
    {
        public Vector3 StartPoint;
        public Vector3 EndPoint;

        public CorridorLink PreviousLink;
        public List<CorridorLink> NextLinks;
        public Room ConnectedRoom = null;

        public GameObject CorridorObject = null;
        public Vector3 BottomLeftCorner = Vector3.zero;
        public Vector3 TopRightCorner = Vector3.zero;

        public Vector3 difference { get { return EndPoint - StartPoint; } }
        public float magnitude { get { return difference.magnitude; } }

        public Vector3 direction { get { return difference.normalized; } }

        public float sign { get { return IsHorizontal() ? direction.x : direction.z; } }

        public Vector3 perpendicular { get { return Vector3.Cross(Vector3.up, direction).normalized; } }

        public CorridorLink(Vector3 startPoint, Vector3 endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            
            PreviousLink = null;
            NextLinks = new List<CorridorLink>();
        }

        public bool IsHorizontal()
        {
            if (StartPoint == Vector3.zero && EndPoint == Vector3.zero) return false;

            return StartPoint.z == EndPoint.z;
        }

        public bool IsVertical()
        {
            if (StartPoint == Vector3.zero && EndPoint == Vector3.zero) return false;

            return StartPoint.x == EndPoint.x;
        }

        public void SetCorners(Vector3 BottomLeft, Vector3 TopRight)
        {
            Vector3 middlePoint = (BottomLeft + TopRight) / 2f;
            BottomLeft -= middlePoint;
            TopRight -= middlePoint;

            this.BottomLeftCorner = BottomLeft;
            this.TopRightCorner = TopRight;
        }

        public float GetGameObjectLength(float corridorWidth)
        {
            return difference.magnitude + corridorWidth;
        }
    }

    private GGNode _RoomNode = null;

    public GGNode RoomNode { get { return _RoomNode; } set { _RoomNode = value; this.GetComponent<MeshRenderer>().material.color = SetColor(); } }

    [SerializeField]
    public LineRenderer lineRendererPrefab;

    // Used to compute room positions;
    public Vector2Int Cell;

    // Used to toggle lines for visibility;
    public List<LineRenderer> nodeLines = new List<LineRenderer>();
    public List<LineRenderer> keyLines = new List<LineRenderer>();

    public Room ParentRoom = null;
    public HashSet<Room> ChildRooms = new HashSet<Room>();

    MeshFilter m_MeshFilter = null;
    Mesh m_Mesh = null;

    float corridorHeight = 1.0f;

    //private Vector2 RoomSize = Vector2.zero;
    private Vector3 RoomBottomLeft = Vector3.zero;
    private Vector3 RoomTopRight = Vector3.zero;

    private float RoomWidth  { get { return RoomTopRight.x - RoomBottomLeft.x; } }
    private float RoomHeight { get { return RoomTopRight.z - RoomBottomLeft.z; } }

    private HashSet<CorridorLink> ExitCorridors = new HashSet<CorridorLink>();
    private HashSet<CorridorLink> EntranceCorridors = new HashSet<CorridorLink>();

    private Dictionary<CorridorLink, List<GameObject>> m_dGameObjectsPerCorridor = new Dictionary<CorridorLink, List<GameObject>>();

    [SerializeField]
    private int nRooms = 0;

    

    public void Awake()
    {
        m_MeshFilter = this.GetComponent<MeshFilter>();
    }

    public void AddConnection(Room room, Symbol edgeSymbol)
    {
        if (edgeSymbol.Type == GraphSymbolType.Edge) return;

        ChildRooms.Add(room);

        CorridorLink newLink;

        // If the cells are aligned then we can just connect them by drawing a line
        if (this.Cell.x == room.Cell.x || this.Cell.y == room.Cell.y)
        {
            newLink = new CorridorLink(transform.position, room.transform.position);
            newLink.ConnectedRoom = room;
            room.EntranceCorridors.Add(newLink);
        }
        // Otherwise we instantiate three lines and place them in an "S" shape.
        else
        {

            Vector3 middlePoint = new Vector3((this.transform.position.x + room.transform.position.x) / 2f, 0, this.transform.position.z);
            
            CorridorLink corridorLinkStart = new CorridorLink(transform.position, middlePoint);
            CorridorLink corridorLinkMiddle = new CorridorLink(middlePoint, new Vector3(middlePoint.x, middlePoint.y, room.transform.position.z));
            CorridorLink corridorLinkEnd = new CorridorLink(new Vector3(middlePoint.x, middlePoint.y, room.transform.position.z), room.transform.position);
            corridorLinkEnd.ConnectedRoom = room;
            room.EntranceCorridors.Add(corridorLinkEnd);

            corridorLinkStart.NextLinks.Add(corridorLinkMiddle);
            corridorLinkMiddle.PreviousLink = corridorLinkStart;
            corridorLinkMiddle.NextLinks.Add(corridorLinkEnd);
            corridorLinkEnd.PreviousLink = corridorLinkMiddle;

            newLink = corridorLinkStart;
        }

        AddLink(newLink);
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

    public void ShowLinks()
    {
        foreach (var link in ExitCorridors)
        {
            var lineRenderer = Instantiate(lineRendererPrefab, this.transform);
            lineRenderer.SetPosition(0, link.StartPoint);
            lineRenderer.SetPosition(1, link.EndPoint);
        }
    }

    private void AddLink(CorridorLink link, CorridorLink parent = null)
    {
        var maxLinks = ExitCorridors.ToList().FindAll(l => l.direction == link.direction);
        maxLinks = link.IsHorizontal()? maxLinks.FindAll(l => l.StartPoint.z == link.StartPoint.z) : maxLinks.FindAll(l => l.StartPoint.x == link.StartPoint.x);
        
        if (maxLinks.Count == 0)
        {
            ExitCorridors.Add(link);

            if (parent != null && !parent.NextLinks.Contains(link))
            {
                parent.NextLinks.Add(link);
                link.PreviousLink = parent;
            }

            foreach (var c in link.NextLinks)
            {
                AddLink(c, link);
            }

            return;
        }

        CorridorLink oldMaxLink = maxLinks.First();

        var toAdd = oldMaxLink.magnitude > link.magnitude ? oldMaxLink : link;
        var toRemove = oldMaxLink.magnitude > link.magnitude ? link : oldMaxLink;

        ExitCorridors.Remove(toRemove);
        ExitCorridors.Add(toAdd);

        if (parent != null)
        {
            parent.NextLinks.Remove(toRemove);
            if (!parent.NextLinks.Contains(toAdd)) parent.NextLinks.Add(toAdd);
            toAdd.PreviousLink = parent;
        }

        if (toRemove.ConnectedRoom != null)
        {
            toRemove.ConnectedRoom.EntranceCorridors.Remove(toRemove);
            toAdd.ConnectedRoom = toRemove.ConnectedRoom;
            toRemove.ConnectedRoom.EntranceCorridors.Add(toAdd);
        }

        var children = new List<CorridorLink>();

        toRemove.NextLinks.ForEach(l => children.Add(l));
        toAdd.NextLinks.ForEach(l => { if (!children.Contains(l)) children.Add(l); });
        
        foreach (var c in children)
        {
            AddLink(c, toAdd);
        }
    }

    //public void CreateRoomMesh(float xSize, float ySize)
    //{
    //    m_Mesh = CreateMesh(xSize, ySize);
    //    m_MeshFilter.mesh = m_Mesh;
    //    //this.RoomSize = new Vector2(xSize, ySize);
    //}

    public void CreateMeshFloor(float xSize, float ySize)
    {

        m_MeshFilter.mesh = m_Mesh;
        //this.RoomSize = new Vector2(xSize, ySize);
        
        this.RoomBottomLeft = new Vector3(-xSize / 2f, 0, -ySize / 2f);
        this.RoomTopRight = new Vector3(xSize / 2f, 0, ySize / 2f);

    }

    public void AddRoomWalls()
    {
        var firstCorner = RoomBottomLeft;
        var secondCorner = new Vector3(RoomBottomLeft.x, 0, RoomTopRight.z);
        var direction = (secondCorner - firstCorner).normalized;

        var leftPoints = new List<Vector3>() { firstCorner };
        leftPoints = GetPoints(-this.transform.right, leftPoints);
        leftPoints.Add(secondCorner);
        AddWall(direction, leftPoints, WallFace.Left);


        firstCorner = new Vector3(RoomTopRight.x, 0, RoomBottomLeft.z);
        secondCorner = RoomTopRight;
        direction = (secondCorner - firstCorner).normalized;

        var rightExits = new List<Vector3>() { firstCorner };
        rightExits = GetPoints(this.transform.right, rightExits);
        rightExits.Add(secondCorner);
        AddWall(direction, rightExits, WallFace.Right);

        firstCorner = new Vector3(RoomBottomLeft.x, 0, RoomTopRight.z);
        secondCorner = RoomTopRight;
        direction = (secondCorner - firstCorner).normalized;

        var northPoints = new List<Vector3>() { firstCorner };
        northPoints = GetPoints(this.transform.forward, northPoints);
        northPoints.Add(secondCorner);
        AddWall(direction, northPoints, WallFace.Front);

        firstCorner = RoomBottomLeft;
        secondCorner = new Vector3(RoomTopRight.x, 0, RoomBottomLeft.z);
        direction = (secondCorner - firstCorner).normalized;

        var southPoints = new List<Vector3>() { firstCorner };
        southPoints = GetPoints(-this.transform.forward, southPoints);
        southPoints.Add(secondCorner);
        AddWall(direction, southPoints, WallFace.Back);
    }

    private List<Vector3> GetPoints(Vector3 direction, List<Vector3> initialList)
    {
        List<CorridorLink> allCorridors = new List<CorridorLink>();
        allCorridors.AddRange(EntranceCorridors);
        allCorridors.AddRange(ExitCorridors);

        var rightExits = allCorridors.FindAll(l =>
        {
            if (ExitCorridors.Contains(l))
                return l.direction == direction && l.PreviousLink == null;
            else if (EntranceCorridors.Contains(l))
                return l.direction == -direction && l.ConnectedRoom == this;

            return false;
        });
        

        foreach (var re in rightExits)
        {
            Vector3 leftPoint = Vector3.zero;
            Vector3 rightPoint = Vector3.zero;

            if (ExitCorridors.Contains(re))
            {
                leftPoint = re.BottomLeftCorner;
                rightPoint = re.BottomLeftCorner + re.perpendicular * 0.5f;
            }
            else if (EntranceCorridors.Contains(re))
            {
                leftPoint = re.TopRightCorner - re.perpendicular * 0.5f;
                rightPoint = re.TopRightCorner;
            }



            leftPoint = re.CorridorObject.transform.TransformPoint(leftPoint);
            rightPoint = re.CorridorObject.transform.TransformPoint(rightPoint);

            leftPoint = this.transform.InverseTransformPoint(leftPoint);
            rightPoint = this.transform.InverseTransformPoint(rightPoint);

            initialList.Add(leftPoint);
            initialList.Add(rightPoint);
        }

        return initialList;
    }

    private void AddWall(Vector3 direction, List<Vector3> points, WallFace face)
    {
        var ordererPointList = points.OrderBy(p => Vector3.Distance(p, points.First())).ToList();


        while (ordererPointList.Count >= 2)
        {
            var firstPoint = ordererPointList[0];
            var secondPoint = ordererPointList[1];

            ordererPointList.Remove(firstPoint);
            ordererPointList.Remove(secondPoint);

            firstPoint.y += corridorHeight;


            
            var obj = CreateCorridorGameObject(firstPoint, secondPoint, face);
            Vector3 wPos = obj.transform.localPosition;
            obj.transform.parent = this.transform;
            obj.transform.localPosition = wPos;
        }
    }


    private Mesh CreateMesh(Vector3 bottomLeftAngle, Vector3 topRightAngle, WallFace faceDirection)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[8];
        Vector3[] normals = new Vector3[8];
        Vector2[] uvs = new Vector2[8];

        Vector3 inNormals = this.transform.forward;
        Vector3 outNormals = -this.transform.forward;

        vertices[0] = new Vector3(bottomLeftAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
        vertices[1] = new Vector3(topRightAngle.x, bottomLeftAngle.y, topRightAngle.z);
        vertices[2] = new Vector3(bottomLeftAngle.x, topRightAngle.y, bottomLeftAngle.z);
        vertices[3] = new Vector3(topRightAngle.x, topRightAngle.y, topRightAngle.z);


        if (faceDirection == WallFace.Top || faceDirection == WallFace.Down)
        {
            vertices[0] = new Vector3(bottomLeftAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[1] = new Vector3(topRightAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[2] = new Vector3(bottomLeftAngle.x, topRightAngle.y, topRightAngle.z);
            vertices[3] = new Vector3(topRightAngle.x, topRightAngle.y, topRightAngle.z);
        }    


        vertices[4] = vertices[0];
        vertices[5] = vertices[1];
        vertices[6] = vertices[2];
        vertices[7] = vertices[3];

        switch (faceDirection)
        {
            case WallFace.Front:
            case WallFace.Back:
                outNormals = -this.transform.forward;
                inNormals = this.transform.forward;
                break;
            case WallFace.Right:
            case WallFace.Left:
                outNormals = this.transform.right;
                inNormals = -this.transform.right;
                break;
            case WallFace.Top:
            case WallFace.Down:
                outNormals = -this.transform.up;
                inNormals = this.transform.up;
                break;
        }

        normals[0] = outNormals;
        normals[1] = outNormals;
        normals[2] = outNormals;
        normals[3] = outNormals;

        normals[4] = inNormals;
        normals[5] = inNormals;
        normals[6] = inNormals;
        normals[7] = inNormals;

        uvs[0] = new Vector2(0f, 0f);
        uvs[1] = new Vector2(0f, 1f);
        uvs[2] = new Vector2(1f, 0f);
        uvs[3] = new Vector2(1f, 1f);

        uvs[4] = new Vector2(0f, 0f);
        uvs[5] = new Vector2(0f, 1f);
        uvs[6] = new Vector2(1f, 0f);
        uvs[7] = new Vector2(1f, 1f);

        int[] triangles = new int[12] { 0, 1, 2, 1, 3, 2, 4, 6, 5, 5, 6, 7};

        // Assign the data to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        return mesh;
    }


    // Creates a two sided plane mesh.
    private Mesh CreateMesh(Vector3 bottomLeftAngle, Vector3 topRightAngle, int f)
    {
        Mesh mesh = new Mesh();

        Vector3[] normals = new Vector3[8];
        normals[0] = Vector3.up;
        normals[1] = Vector3.up;
        normals[2] = Vector3.up;
        normals[3] = Vector3.up;

        normals[4] = Vector3.down;
        normals[5] = Vector3.down;
        normals[6] = Vector3.down;
        normals[7] = Vector3.down;

        Vector3[] vertices = new Vector3[8];
        if (!Mathf.Approximately(bottomLeftAngle.x, topRightAngle.x))
        {
            vertices[0] = new Vector3(bottomLeftAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[1] = new Vector3(bottomLeftAngle.x, topRightAngle.y, topRightAngle.z);
            vertices[2] = new Vector3(topRightAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[3] = new Vector3(topRightAngle.x, topRightAngle.y, topRightAngle.z);

            vertices[4] = new Vector3(bottomLeftAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[5] = new Vector3(bottomLeftAngle.x, topRightAngle.y, topRightAngle.z);
            vertices[6] = new Vector3(topRightAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[7] = new Vector3(topRightAngle.x, topRightAngle.y, topRightAngle.z);
        }
        else if (!Mathf.Approximately(bottomLeftAngle.y, topRightAngle.y))
        {

            vertices[0] = new Vector3(topRightAngle.x, bottomLeftAngle.y, topRightAngle.z);
            vertices[1] = new Vector3(bottomLeftAngle.x, topRightAngle.y, topRightAngle.z);
            vertices[2] = new Vector3(bottomLeftAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[3] = new Vector3(bottomLeftAngle.x, topRightAngle.y, bottomLeftAngle.z);


            vertices[4] = new Vector3(topRightAngle.x, bottomLeftAngle.y, topRightAngle.z);
            vertices[5] = new Vector3(bottomLeftAngle.x, topRightAngle.y, topRightAngle.z);
            vertices[6] = new Vector3(bottomLeftAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[7] = new Vector3(bottomLeftAngle.x, topRightAngle.y, bottomLeftAngle.z);
        }

        if (Mathf.Approximately(bottomLeftAngle.x, topRightAngle.x))
        {
            normals[0] = this.transform.right;
            normals[1] = this.transform.right;
            normals[2] = this.transform.right;
            normals[3] = this.transform.right;

            normals[4] = -this.transform.right;
            normals[5] = -this.transform.right;
            normals[6] = -this.transform.right;
            normals[7] = -this.transform.right;
        }
        if (Mathf.Approximately(bottomLeftAngle.z, topRightAngle.z))
        {
            normals[0] = this.transform.forward;
            normals[1] = this.transform.forward;
            normals[2] = this.transform.forward;
            normals[3] = this.transform.forward;

            normals[4] = -this.transform.forward;
            normals[5] = -this.transform.forward;
            normals[6] = -this.transform.forward;
            normals[7] = -this.transform.forward;
        }

        // Define triangles
        int[] triangles = new int[12] { 0, 1, 2, 2, 1, 3, 6, 5, 4, 7, 5, 6};


        // Define UVs
        Vector2[] uvs = new Vector2[8];
        uvs[0] = new Vector2(0f, 0f);
        uvs[1] = new Vector2(0f, 1f);
        uvs[2] = new Vector2(1f, 0f);
        uvs[3] = new Vector2(1f, 1f);

        uvs[4] = new Vector2(0f, 0f);
        uvs[5] = new Vector2(0f, 1f);
        uvs[6] = new Vector2(1f, 0f);
        uvs[7] = new Vector2(1f, 1f);

        // Assign the data to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        return mesh;
    }

    private Color SetColor()
    {
        if (RoomNode.NodeSymbol.Name == "start") return Color.green;
        else if (RoomNode.NodeSymbol.Name == "goal") return Color.red;
        else if (RoomNode.NodeSymbol.Name == "l") return Color.cyan;
        else if (RoomNode.NodeSymbol.Name == "k") return Color.blue;
        else if (RoomNode.NodeSymbol.Name == "t") return Color.gray;
        else if (RoomNode.NodeSymbol.Name == "b") return new Color(139f / 255, 69f / 255, 19f / 255);

        else return Color.white;
    }

    private Vector3[] ComputeCorners(CorridorLink link, float corridorWidth)
    {

        // Corridor corners to compute meshes and intersections, BottomLeft (BL) and TopRight (TR) corners are computed based on the link direction
        //    TR                 TR
        // |  |         ---------
        // |  |         ---------
        // BL          BL
        Vector3 corridorBottomLeft = link.StartPoint - this.transform.position;
        Vector3 corridorTopRight = link.EndPoint - this.transform.position;


        corridorBottomLeft -= link.perpendicular * corridorWidth / 2f;
        corridorTopRight += link.perpendicular * corridorWidth / 2f;

        // Offset the corridor start and/or end by the room size to avoid overlaps. 
        float roomSide = link.IsHorizontal() ? RoomWidth / 2f : RoomHeight / 2f;

        // Link is starting from a room, we offset by room size
        if (link.PreviousLink == null)
            corridorBottomLeft += link.direction * roomSide;
        // Link is starting from a corridor, we offset by half corridor width
        else
            corridorBottomLeft += link.direction * corridorWidth / 2f;

        // Link is ending in a room, we offset by the ending room size
        if (link.ConnectedRoom != null)
            corridorTopRight -= link.direction * (link.IsHorizontal() ? link.ConnectedRoom.RoomWidth / 2f : link.ConnectedRoom.RoomHeight / 2f);
        // Link is ending in a corridor, we offset by half corridor width
        else
            corridorTopRight += link.direction * corridorWidth / 2f;

        return new Vector3[2] { corridorBottomLeft, corridorTopRight };
    }

    public void CreateCorridors(float corridorWidth)
    {
        foreach (var link in ExitCorridors)
        {
            Vector3[] corners = ComputeCorners(link, corridorWidth);
            Vector3 corridorBottomLeft = corners[0];
            Vector3 corridorTopRight = corners[1];

            link.BottomLeftCorner = corridorBottomLeft;
            link.TopRightCorner = corridorTopRight;
        }

        foreach (var link in ExitCorridors)
        {
            if (link.PreviousLink == null && link.ConnectedRoom != null && link.NextLinks.Count == 0)
            {


                Vector3 corridorBottomLeft = link.BottomLeftCorner;
                Vector3 corridorTopRight = link.TopRightCorner;

                var sp = link.StartPoint + link.direction * (link.IsHorizontal() ? this.RoomWidth / 2f : this.RoomHeight /2f);
                var ep = link.EndPoint - link.direction * (link.IsHorizontal() ? link.ConnectedRoom.RoomWidth / 2f : link.ConnectedRoom.RoomHeight / 2f);

                var magnitude = (ep - sp).magnitude;

                if (link.IsVertical() && link.sign == -1)
                    this.RoomBottomLeft += link.direction * magnitude;

                else
                    this.RoomTopRight += link.direction * magnitude;


                corridorBottomLeft += link.direction * magnitude;

                Vector3 middlePoint = (corridorBottomLeft + corridorTopRight) / 2f;
                corridorBottomLeft -= middlePoint;
                corridorTopRight -= middlePoint;

                link.BottomLeftCorner = corridorBottomLeft;
                link.TopRightCorner = corridorTopRight;

                link.CorridorObject = new GameObject("Door space");
                link.CorridorObject.transform.position = this.transform.position + middlePoint;
                link.CorridorObject.transform.parent = this.transform;
                link.CorridorObject.tag = "DoorSpace";
                
            }
        }

        foreach (var link in ExitCorridors)
        {
            Vector3 corridorBottomLeft = link.BottomLeftCorner;
            Vector3 corridorTopRight = link.TopRightCorner;

            if (link.CorridorObject != null) continue;

            // Corridor floor and ceiling game objects
            GameObject corridorFloor = CreateCorridorGameObject(corridorBottomLeft, corridorTopRight, WallFace.Down);
            GameObject corridorCeiling = CreateCorridorGameObject(corridorBottomLeft + Vector3.up * corridorHeight, corridorTopRight + Vector3.up * corridorHeight, WallFace.Top);
            corridorCeiling.transform.parent = corridorFloor.transform;
            corridorCeiling.name = "CorridorCeiling";

            if (link.IsHorizontal())
            {
                Mesh mf = corridorFloor.GetComponent<MeshFilter>().mesh;
                Mesh mc = corridorCeiling.GetComponent<MeshFilter>().mesh;
                
                for (int i = 0; i < mf.normals.Length; i++)
                {
                    mf.normals[i] = Vector3.right; 
                    mc.normals[i] = -mc.normals[i];
                }

                corridorFloor.GetComponent<MeshFilter>().mesh = mf;
                corridorCeiling.GetComponent<MeshFilter>().mesh = mc;
            }

            // Used to compute side and front walls
            Vector3 upperTopLeftCorner = new Vector3(corridorBottomLeft.x, corridorHeight, corridorTopRight.z);
            Vector3 upperBottomRightCorner = new Vector3(corridorTopRight.x, corridorHeight, corridorBottomLeft.z); ;

            // If the corridor has no next links then we can create the side walls immediately, since we don't have to account for any intersections between corridors.
            // We don't care if this corridor ends in a room since it won't overlap due to the room offset we computed earlier and the entrances / exits for the room will be created later.
            if (link.NextLinks.Count == 0)
            {
                GameObject corridorLeftWall;
                GameObject corridorRightWall;

                // Check is used to compute meshes with the right normals and orientations
                if (link.IsHorizontal())
                {
                    corridorLeftWall = CreateCorridorGameObject(upperTopLeftCorner, corridorTopRight, WallFace.Front);
                                        
                    corridorRightWall = CreateCorridorGameObject(upperBottomRightCorner + Vector3.down * corridorHeight, corridorBottomLeft + Vector3.up * corridorHeight, WallFace.Back);
                }
                else
                {
                    
                    corridorLeftWall = CreateCorridorGameObject(corridorBottomLeft, upperTopLeftCorner, link.direction.z > 0 ? WallFace.Left : WallFace.Right);
                    corridorRightWall = CreateCorridorGameObject(upperBottomRightCorner, corridorTopRight, link.direction.z > 0 ? WallFace.Right : WallFace.Left);
                    
                }

                corridorLeftWall.name = "LeftWall";
                corridorRightWall.name = "RightWall";
                corridorLeftWall.transform.parent = corridorFloor.transform;
                corridorRightWall.transform.parent = corridorFloor.transform;
            }

            // If the corridor does NOT end in a room then we compute the front wall mesh position
            //
            //  Front Wall    
            //    |
            //    |
            //   \ /  ______
            //    _ _|     | <- Room
            //   |  _      | 
            //   | | |_____|
            //   | | 
            if (link.ConnectedRoom == null)
            {
                GameObject corridorFrontWall;

                if (link.IsHorizontal())
                {
                    corridorFrontWall = CreateCorridorGameObject(upperBottomRightCorner + Vector3.down * corridorHeight, corridorTopRight + Vector3.up * corridorHeight, WallFace.Right);
                }
                else
                {
                    if (link.sign > 0)
                    {
                        corridorFrontWall = CreateCorridorGameObject(upperTopLeftCorner, corridorTopRight, WallFace.Front);
                    }
                    else
                    {
                        corridorFrontWall = CreateCorridorGameObject(upperTopLeftCorner + Vector3.down * corridorHeight, corridorTopRight + Vector3.up * corridorHeight, WallFace.Back);
                    }
                }
                corridorFrontWall.name = "FrontWall";
                corridorFrontWall.transform.parent = corridorFloor.transform;
            }

            // Assing the corridor Game object and the computed corners to this link for later use
            link.CorridorObject = corridorFloor;
            link.SetCorners(corridorBottomLeft, corridorTopRight);
        }

        // Create holes in the corridors for traversing the level
        //
        //  --------------------------------
        //  ---------   ------------------  |
        //           | | \              / | |
        //                \            / 
        //                 \          /   
        //        Holes needed in the horizontal corridor
        //  
        foreach (CorridorLink corLink in ExitCorridors)
        {
            if (corLink.NextLinks.Count > 0)
            {
                CreateHoles(corLink, corridorWidth);
            }
        }

        // Once all the corridors are created we can create the room walls and entrances
        AddRoomWalls();
        m_Mesh = CreateMesh(this.RoomBottomLeft, this.RoomTopRight, WallFace.Down);
        m_MeshFilter.mesh = m_Mesh;

        CreateCorridorGameObject(this.RoomBottomLeft + Vector3.up * corridorHeight, this.RoomTopRight + Vector3.up * corridorHeight, WallFace.Top);
    }

    private static bool done = false;

    /// <summary>
    /// Compute the holes needed in the corridor by splitting the walls in segments to be placed before and after the next corridor
    /// </summary>
    /// <param name="corridor">Current corridor to create holes for</param>
    /// <param name="corridorWidth">Width of the corridor</param>
    /// <returns></returns>
    private List<GameObject> CreateHoles(CorridorLink corridor, float corridorWidth)
    {
       
        Vector3 leftReferencePoint = corridor.BottomLeftCorner;
        Vector3 rightReferencePoint = corridor.BottomLeftCorner + corridor.perpendicular * corridorWidth;

        // Compute intersection points between corridors. Intersection can only occur on the right side or left side of the corridor
        var LeftHolesCoords = GetCorridorIntersectionPoints(corridor, -corridor.perpendicular, corridorWidth);
        var RightHolesCoords = GetCorridorIntersectionPoints(corridor, corridor.perpendicular, corridorWidth);
        LeftHolesCoords.Insert(0, leftReferencePoint);
        RightHolesCoords.Insert(0, rightReferencePoint);

        Vector3 topLeft = corridor.TopRightCorner - corridor.perpendicular * corridorWidth;
        LeftHolesCoords.Add(topLeft);
        RightHolesCoords.Add(corridor.TopRightCorner);

        List<GameObject> WallPieces = new List<GameObject>();
        WallPieces.AddRange(CreateWallSegments(corridor, LeftHolesCoords, corridorWidth));
        WallPieces.AddRange(CreateWallSegments(corridor, RightHolesCoords, corridorWidth));


        return WallPieces;
    }


    private List<Vector3> GetCorridorIntersectionPoints(CorridorLink corridor, Vector3 direction, float corridorWidth)
    {
        List<Vector3> IntersectionList = new List<Vector3>();

        foreach (var childCorridor in corridor.NextLinks)
        {
            // Safety check but unless some mistakes were made this should never be true
            if (childCorridor.CorridorObject == null) continue;
            if (corridor.CorridorObject == null) continue;

            Vector3[] intersectionPoints = new Vector3[2];

            // For each intersection we compute two intersection points, which corresponds to the two side walls of the intersecting corridor
            //
            //  Corridor we are computing holes for
            //    |      
            //    |     | |__  <- The points where the walls of the next corridor intersect with the current corridor are the ones we want to find out.
            //     ->   |  __  <-  
            //          | |
            //          | |
            //      
            if (childCorridor.direction.normalized == direction.normalized)
            {
                intersectionPoints[0] = childCorridor.CorridorObject.transform.TransformPoint(childCorridor.BottomLeftCorner + childCorridor.perpendicular * corridorWidth);
                intersectionPoints[1] = childCorridor.CorridorObject.transform.TransformPoint(childCorridor.BottomLeftCorner);

                intersectionPoints[0] = corridor.CorridorObject.transform.InverseTransformPoint(intersectionPoints[0]);
                intersectionPoints[1] = corridor.CorridorObject.transform.InverseTransformPoint(intersectionPoints[1]);

                // AKA if the child corridor is starting from the left wall of the current corridor we invert the points.
                // This is so we can reuse the same code for computing the final segment of the corridor when it ends in another corridor.
                if (-corridor.perpendicular == childCorridor.direction)
                {
                    var tmp = intersectionPoints[0];
                    intersectionPoints[0] = intersectionPoints[1];
                    intersectionPoints[1] = tmp;
                }

                IntersectionList.AddRange(intersectionPoints);
            }
        }

        return IntersectionList;
    }


    // We can create a new segment by taking two intersection points from the list, creating the mesh and removing them
    //         -> Top right corner
    //      | |
    //      | |_ -> Intersection point 1
    //      |  _ -> Intersection point 0
    //      | | 
    //      | | -> Reference point is bottom right corner when computing segments for the right wall 
    //         
    //      At each step we remove the two first points from the list and create a segment between them
    //
    //         -> Top right corner
    //      | |
    //      | |_ -> Intersection point 1
    //      |  
    //      | || -> new segment
    //      | || 
    /// <summary>
    /// Create the wall segments for the corridor based on the intersection points found
    /// </summary>
    /// <param name="corridor">The corridor whose segments we want to create</param>
    /// <param name="IntersectionPoints">Intersection points between this corridor and its children</param>
    /// <param name="corridorWidth">Current width of the corridors</param>
    /// <returns></returns>
    private List<GameObject> CreateWallSegments(CorridorLink corridor, List<Vector3> IntersectionPoints, float corridorWidth)
    {


        List<GameObject> WallPieces = new List<GameObject>();

        // Safety check
        if (IntersectionPoints == null || IntersectionPoints.Count == 0) return WallPieces;
        if (corridor.CorridorObject == null) return WallPieces;
        if (corridor.CorridorObject.tag == "DoorSpace") return WallPieces;


        // Reference point which is either the bottom left or the bottom right corner of the corridor depending on which wall we are creating segments for
        var referencePoint = IntersectionPoints.First();
        var lastPoint = referencePoint;

        // There are always at least two points in the list: The reference point and the corresponding top corner
        while (IntersectionPoints.Count >= 2)
        {
            IntersectionPoints.OrderBy(c => Vector3.Distance(referencePoint, c));


            Vector3 firstPoint = IntersectionPoints[0];
            Vector3 secondPoint = IntersectionPoints[1];

            IntersectionPoints.Remove(firstPoint);
            IntersectionPoints.Remove(secondPoint);

            secondPoint += Vector3.up * corridorHeight;


            GameObject wallPiece = CreateCorridorGameObject(firstPoint, secondPoint);
            WallPieces.Add(wallPiece);

            Vector3 oldWLocal = wallPiece.transform.localPosition;
            wallPiece.transform.parent = corridor.CorridorObject.transform;
            wallPiece.transform.localPosition = oldWLocal;
            wallPiece.GetComponent<MeshRenderer>().material.color = Color.blue;

            lastPoint = secondPoint;
        }

        return WallPieces;
    }

    private GameObject CreateCorridorGameObject(Vector3 bottomLeft, Vector3 topRight, WallFace face = WallFace.Front)
    {
        Vector3 middlePoint = (bottomLeft + topRight) / 2f;
        bottomLeft -= middlePoint;
        topRight -= middlePoint;

        GameObject corridor = new GameObject("Corridor");
        corridor.transform.position = this.transform.position + middlePoint;
        corridor.transform.parent = transform;
    
        corridor.AddComponent<MeshFilter>();
        corridor.AddComponent<MeshRenderer>();
        corridor.GetComponent<MeshRenderer>().material.color = Color.gray;



        corridor.GetComponent<MeshFilter>().mesh = CreateMesh(bottomLeft, topRight, face);

        Vector3 colliderSize = Vector3.zero;
        colliderSize.x = Mathf.Max(MathF.Abs(topRight.x - bottomLeft.x), 0.1f);
        colliderSize.y = Mathf.Max(MathF.Abs(topRight.y - bottomLeft.y), 0.1f);
        colliderSize.z = Mathf.Max(MathF.Abs(topRight.z - bottomLeft.z), 0.1f);

        corridor.AddComponent<BoxCollider>();
        corridor.GetComponent<BoxCollider>().size = colliderSize;

        return corridor;
    }
}
