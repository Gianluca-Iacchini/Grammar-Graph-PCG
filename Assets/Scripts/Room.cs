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

        public float GetGameObjectLength(float CorridorWidth)
        {
            return difference.magnitude + CorridorWidth;
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

    [SerializeField]
    private Key keyPrefab;

    [SerializeField]
    private Task taskPrefab;

    [SerializeField]
    private Lock lockPrefab;    
    
    [SerializeField]
    private Treasure treasurePrefab;

    public Room ParentRoom = null;
    public HashSet<Room> LockRooms = new HashSet<Room>();

    MeshFilter m_MeshFilter = null;
    Mesh m_Mesh = null;

    public float CorridorWidth = 0.5f;
    public float CorridorHeight = 1.0f;
    public float Thickness = 0.1f;


    public Lock lockInstance { get; private set; }

    //private Vector2 RoomSize = Vector2.zero;
    private Vector3 RoomBottomLeft = Vector3.zero;
    private Vector3 RoomTopRight = Vector3.zero;

    private float RoomWidth  { get { return RoomTopRight.x - RoomBottomLeft.x; } }
    private float RoomHeight { get { return RoomTopRight.z - RoomBottomLeft.z; } }

    private HashSet<CorridorLink> ExitCorridors = new HashSet<CorridorLink>();
    private HashSet<CorridorLink> EntranceCorridors = new HashSet<CorridorLink>();

    private Dictionary<CorridorLink, List<GameObject>> m_dGameObjectsPerCorridor = new Dictionary<CorridorLink, List<GameObject>>();


    [NonSerialized]
    public List<Door> ExitDoors = new();
    

    public void Awake()
    {
        m_MeshFilter = this.GetComponent<MeshFilter>();
        lockInstance = Instantiate(lockPrefab, this.transform);
    }

    public void AddConnection(Room room, Symbol edgeSymbol)
    {
        if (edgeSymbol.Type == GraphSymbolType.Edge)
        {
            LockRooms.Add(room);
            return;
        }

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
                rightPoint = re.BottomLeftCorner + re.perpendicular * CorridorWidth;
            }
            else if (EntranceCorridors.Contains(re))
            {
                leftPoint = re.TopRightCorner - re.perpendicular * CorridorWidth;
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

            secondPoint.y += CorridorHeight;



            var obj = CreateCorridorGameObject(firstPoint, secondPoint, face);
            Vector3 wPos = obj.transform.localPosition;
            obj.transform.parent = this.transform;

            switch(face)
            {
                case WallFace.Left:
                    wPos.x += Thickness / 2f;
                    break;
                case WallFace.Right:
                    wPos.x -= Thickness / 2f;
                    break;
                case WallFace.Front:
                    wPos.z -= Thickness / 2f;
                    break;
                case WallFace.Back:
                    wPos.z += Thickness / 2f;
                    break;
            }

            obj.transform.localPosition = wPos;


        }
    }


    private Mesh CreateMesh(Vector3 bottomLeftAngle, Vector3 topRightAngle, WallFace faceDirection, float thickness = 0.1f)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[24];
        Vector3[] normals = new Vector3[24] { 
            transform.forward, transform.forward, transform.forward, transform.forward,
            -transform.forward, -transform.forward, -transform.forward, -transform.forward,
            
            -transform.right, -transform.right, -transform.right, -transform.right,
            transform.right, transform.right, transform.right, transform.right,

            -transform.up, -transform.up, -transform.up, -transform.up,
            transform.up, transform.up, transform.up, transform.up
        };
        Vector2[] uvs = new Vector2[24];

        if (faceDirection == WallFace.Back || faceDirection == WallFace.Front)
        {
            vertices[0] = new Vector3(bottomLeftAngle.x, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[1] = new Vector3(topRightAngle.x, bottomLeftAngle.y, topRightAngle.z);
            vertices[2] = new Vector3(bottomLeftAngle.x, topRightAngle.y, bottomLeftAngle.z);
            vertices[3] = new Vector3(topRightAngle.x, topRightAngle.y, topRightAngle.z);
            
            for (int i = 4; i < 8; i++)
            {
                vertices[i] = vertices[i - 4];
                
                vertices[i-4].z += thickness / 2f;
                vertices[i].z -= thickness / 2f;
                
            }
        }
        else if (faceDirection == WallFace.Right || faceDirection == WallFace.Left)
        {
            vertices[4] = new Vector3(bottomLeftAngle.x - thickness / 2f, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[5] = new Vector3(bottomLeftAngle.x + thickness / 2f, bottomLeftAngle.y, bottomLeftAngle.z);
            vertices[6] = new Vector3(bottomLeftAngle.x - thickness / 2f, topRightAngle.y, bottomLeftAngle.z);
            vertices[7] = new Vector3(bottomLeftAngle.x + thickness / 2f, topRightAngle.y, bottomLeftAngle.z);

            for (int i = 0; i < 4; i++)
            {
                vertices[i] = vertices[i + 4];
                vertices[i].z = topRightAngle.z;
            }
        }
        else if (faceDirection == WallFace.Top || faceDirection == WallFace.Down)
        {

            vertices[4] = new Vector3(bottomLeftAngle.x, bottomLeftAngle.y - thickness/2f, bottomLeftAngle.z );
            vertices[5] = new Vector3(topRightAngle.x, bottomLeftAngle.y - thickness/2f, bottomLeftAngle.z);
            vertices[6] = new Vector3(bottomLeftAngle.x, bottomLeftAngle.y + thickness/2f, bottomLeftAngle.z);
            vertices[7] = new Vector3(topRightAngle.x, bottomLeftAngle.y + thickness/2f, bottomLeftAngle.z);

            for (int i = 0; i < 4; i++)
            {
                vertices[i] = vertices[i + 4];
                vertices[i].z = topRightAngle.z;
            }
        }

        vertices[8] = vertices[4];
        vertices[9] = vertices[0];
        vertices[10] = vertices[6];
        vertices[11] = vertices[2];

        vertices[12] = vertices[1];
        vertices[13] = vertices[5];
        vertices[14] = vertices[3];
        vertices[15] = vertices[7];

        vertices[16] = vertices[0];
        vertices[17] = vertices[4];
        vertices[18] = vertices[1];
        vertices[19] = vertices[5];

        vertices[20] = vertices[6];
        vertices[21] = vertices[2];
        vertices[22] = vertices[7];
        vertices[23] = vertices[3];



        int[] triangles = new int[36] {
            0, 1, 2,
            1, 3, 2,
            
            5, 4, 7,
            4, 6, 7,

            8, 9, 10,
            9, 11, 10,

            12, 13, 14,
            13, 15, 14,

            16, 17, 18,
            17, 19, 18,

            20, 21, 22,
            21, 23, 22
        };

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

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

    private Vector3[] ComputeCorners(CorridorLink link)
    {

        // Corridor corners to compute meshes and intersections, BottomLeft (BL) and TopRight (TR) corners are computed based on the link direction
        //    TR                 TR
        // |  |         ---------
        // |  |         ---------
        // BL          BL
        Vector3 corridorBottomLeft = link.StartPoint - this.transform.position;
        Vector3 corridorTopRight = link.EndPoint - this.transform.position;


        corridorBottomLeft -= link.perpendicular * CorridorWidth / 2f;
        corridorTopRight += link.perpendicular * CorridorWidth / 2f;

        // Offset the corridor start and/or end by the room size to avoid overlaps. 
        float roomSide = link.IsHorizontal() ? RoomWidth / 2f : RoomHeight / 2f;

        // Link is starting from a room, we offset by room size
        if (link.PreviousLink == null)
            corridorBottomLeft += link.direction * roomSide;
        // Link is starting from a corridor, we offset by half corridor width
        else
            corridorBottomLeft += link.direction * CorridorWidth / 2f;

        // Link is ending in a room, we offset by the ending room size
        if (link.ConnectedRoom != null)
            corridorTopRight -= link.direction * (link.IsHorizontal() ? link.ConnectedRoom.RoomWidth / 2f : link.ConnectedRoom.RoomHeight / 2f);
        // Link is ending in a corridor, we offset by half corridor width
        else
            corridorTopRight += link.direction * CorridorWidth / 2f;

        return new Vector3[2] { corridorBottomLeft, corridorTopRight };
    }

    public void CreateCorridors()
    {
        foreach (var link in ExitCorridors)
        {
            Vector3[] corners = ComputeCorners(link);
            Vector3 corridorBottomLeft = corners[0];
            Vector3 corridorTopRight = corners[1];

            link.BottomLeftCorner = corridorBottomLeft;
            link.TopRightCorner = corridorTopRight;
        }

        foreach (var link in ExitCorridors)
        {

            if (link.PreviousLink == null && link.NextLinks.Count == 0)
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

                link.CorridorObject = new GameObject("Corridor");
                link.CorridorObject.transform.position = this.transform.position + middlePoint;
                link.CorridorObject.transform.parent = this.transform;

                GameObject doorGO;
                if (link.sign < 0 || link.IsHorizontal())
                {
                    doorGO = CreateCorridorGameObject(corridorTopRight, corridorBottomLeft + Vector3.up * CorridorHeight, link.IsHorizontal() ? WallFace.Right : WallFace.Front);
                }
                else
                {
                    doorGO = CreateCorridorGameObject(corridorTopRight + Vector3.up * CorridorHeight, corridorBottomLeft, link.IsHorizontal() ? WallFace.Right : WallFace.Front);

                }
                doorGO.transform.parent = this.transform;
                doorGO.transform.position = this.transform.position + middlePoint + Vector3.up * CorridorHeight / 2f;
                doorGO.name = "ExitDoor";
                Door door = doorGO.AddComponent<Door>();
                
                ExitDoors.Add(door);
            }
        }

        foreach (var link in ExitCorridors)
        {
            Vector3 corridorBottomLeft = link.BottomLeftCorner;
            Vector3 corridorTopRight = link.TopRightCorner;

            if (link.CorridorObject != null) continue;

            GameObject corridorFloor;
            GameObject corridorCeiling;


            // Corridor floor and ceiling game objects
            if (link.IsHorizontal())
            {
                corridorFloor = CreateCorridorGameObject(corridorBottomLeft - transform.forward * CorridorWidth, corridorTopRight + transform.forward * CorridorWidth, WallFace.Down);
                corridorCeiling = CreateCorridorGameObject(corridorBottomLeft + new Vector3(0, CorridorHeight, -CorridorWidth), corridorTopRight + new Vector3(0, CorridorHeight, CorridorWidth), WallFace.Top);

            }
            else
            {
                corridorFloor = CreateCorridorGameObject(corridorBottomLeft, corridorTopRight, WallFace.Down);
                corridorCeiling = CreateCorridorGameObject(corridorBottomLeft + Vector3.up * CorridorHeight, corridorTopRight + Vector3.up * CorridorHeight, WallFace.Top);
            }
            
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
            Vector3 upperTopLeftCorner = new Vector3(corridorBottomLeft.x, CorridorHeight, corridorTopRight.z);
            Vector3 upperBottomRightCorner = new Vector3(corridorTopRight.x, CorridorHeight, corridorBottomLeft.z); ;

            // If the corridor has no next links then we can create the side walls immediately, since we don't have to account for any intersections between corridors.
            // We don't care if this corridor ends in a room since it won't overlap due to the room offset we computed earlier and the entrances / exits for the room will be created later.
            if (link.NextLinks.Count == 0)
            {
                GameObject corridorLeftWall;
                GameObject corridorRightWall;

                // Check is used to compute meshes with the right normals and orientations
                if (link.IsHorizontal())
                {
                    corridorLeftWall = CreateCorridorGameObject(upperTopLeftCorner + Vector3.down * CorridorHeight, corridorTopRight + Vector3.up * CorridorHeight, WallFace.Front);
                                        
                    corridorRightWall = CreateCorridorGameObject(corridorBottomLeft, upperBottomRightCorner, WallFace.Back);
                }
                else
                {
                    
                    corridorLeftWall = CreateCorridorGameObject(corridorBottomLeft, upperTopLeftCorner, link.direction.z > 0 ? WallFace.Left : WallFace.Right);
                    corridorRightWall = CreateCorridorGameObject(corridorTopRight, upperBottomRightCorner, link.direction.z > 0 ? WallFace.Right : WallFace.Left);
                    
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
                    corridorFrontWall = CreateCorridorGameObject(corridorTopRight, upperBottomRightCorner, WallFace.Right);
                }
                else
                {
                    if (link.sign > 0)
                    {
                        corridorFrontWall = CreateCorridorGameObject(upperTopLeftCorner + Vector3.down * CorridorHeight, corridorTopRight + Vector3.up * CorridorHeight, WallFace.Front);
                    }
                    else
                    {
                        corridorFrontWall = CreateCorridorGameObject(corridorTopRight, upperTopLeftCorner, WallFace.Back);
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
                CreateHoles(corLink);
            }
            if (corLink.PreviousLink == null && corLink.NextLinks.Count > 0)
            {
                if (corLink.IsHorizontal())
                {
                    GameObject doorGO = CreateCorridorGameObject(new Vector3(corLink.TopRightCorner.x, CorridorHeight, corLink.BottomLeftCorner.z), corLink.TopRightCorner, WallFace.Right);
                    doorGO.name = "ExitDoor";
                    Vector3 pos = corLink.CorridorObject.transform.position;
                    pos.x += corLink.BottomLeftCorner.x;
                    pos.y += CorridorHeight / 2f;
                    doorGO.transform.position = pos;
                    doorGO.transform.parent = this.transform;
                    Door door = doorGO.AddComponent<Door>();
                    ExitDoors.Add(door);
                }
            }
        }

        // Once all the corridors are created we can create the room walls and entrances
        AddRoomWalls();
        m_Mesh = CreateMesh(this.RoomBottomLeft, this.RoomTopRight, WallFace.Down, Thickness);
        m_MeshFilter.mesh = m_Mesh;
        var boxColl = this.AddComponent<BoxCollider>();
        boxColl.size = new Vector3(RoomWidth, Thickness, RoomHeight);

        CreateCorridorGameObject(this.RoomBottomLeft + Vector3.up * CorridorHeight, this.RoomTopRight + Vector3.up * CorridorHeight, WallFace.Top);

        Vector3[] lightCorners = new Vector3[4];
        lightCorners[0] = new Vector3(RoomWidth / 2f - Thickness, CorridorHeight - Thickness, 0f);
        lightCorners[1] = new Vector3(-RoomWidth / 2f + Thickness, CorridorHeight - Thickness, 0f);
        lightCorners[2] = new Vector3(0, CorridorHeight - Thickness, RoomHeight / 2f);
        lightCorners[3] = new Vector3(0, CorridorHeight - Thickness, -RoomHeight / 2f);
        
        for (int i = 0; i < 4; i++)
        {
            GameObject light = new GameObject("Light" + i);
            Light l = light.AddComponent<Light>();
            l.type = LightType.Point;
            l.intensity = 3f;
            l.range = 20f;
            light.transform.parent = this.transform;
            light.transform.position = this.transform.position + lightCorners[i];
        }
    }


    /// <summary>
    /// Compute the holes needed in the corridor by splitting the walls in segments to be placed before and after the next corridor
    /// </summary>
    /// <param name="corridor">Current corridor to create holes for</param>
    /// <param name="CorridorWidth">Width of the corridor</param>
    /// <returns></returns>
    private List<GameObject> CreateHoles(CorridorLink corridor)
    {
       
        Vector3 leftReferencePoint = corridor.BottomLeftCorner;
        Vector3 rightReferencePoint = corridor.BottomLeftCorner + corridor.perpendicular * CorridorWidth;

        // Compute intersection points between corridors. Intersection can only occur on the right side or left side of the corridor
        var LeftHolesCoords = GetCorridorIntersectionPoints(corridor, -corridor.perpendicular);
        var RightHolesCoords = GetCorridorIntersectionPoints(corridor, corridor.perpendicular);
        LeftHolesCoords.Insert(0, leftReferencePoint);
        RightHolesCoords.Insert(0, rightReferencePoint);

        Vector3 topLeft = corridor.TopRightCorner - corridor.perpendicular * CorridorWidth;
        LeftHolesCoords.Add(topLeft);
        RightHolesCoords.Add(corridor.TopRightCorner);

        List<GameObject> WallPieces = new List<GameObject>();

        WallFace leftFaces;
        WallFace rightFaces;

        if (corridor.IsHorizontal())
        {
            leftFaces = WallFace.Front;
            rightFaces = WallFace.Back;
        }
        else
        {
            leftFaces = WallFace.Left;
            rightFaces = WallFace.Right;
        }

        WallPieces.AddRange(CreateWallSegments(corridor, LeftHolesCoords, leftFaces));
        WallPieces.AddRange(CreateWallSegments(corridor, RightHolesCoords, rightFaces));


        return WallPieces;
    }


    private List<Vector3> GetCorridorIntersectionPoints(CorridorLink corridor, Vector3 direction)
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
                intersectionPoints[0] = childCorridor.CorridorObject.transform.TransformPoint(childCorridor.BottomLeftCorner + childCorridor.perpendicular * CorridorWidth);
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
    private List<GameObject> CreateWallSegments(CorridorLink corridor, List<Vector3> IntersectionPoints, WallFace face)
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
            IntersectionPoints.OrderBy(c => -Vector3.Distance(referencePoint, c));


            Vector3 firstPoint = IntersectionPoints[1];
            Vector3 secondPoint = IntersectionPoints[0];

            IntersectionPoints.Remove(firstPoint);
            IntersectionPoints.Remove(secondPoint);

            if (corridor.sign > 0)
            {
                (firstPoint, secondPoint) = (secondPoint, firstPoint);
            }

            secondPoint += Vector3.up * CorridorHeight;



            GameObject wallPiece = CreateCorridorGameObject(firstPoint, secondPoint, face);
            WallPieces.Add(wallPiece);

            Vector3 oldWLocal = wallPiece.transform.localPosition;
            wallPiece.transform.parent = corridor.CorridorObject.transform;
            wallPiece.transform.localPosition = oldWLocal;

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

        corridor.GetComponent<MeshFilter>().mesh = CreateMesh(bottomLeft, topRight, face, Thickness);

        Vector3 colliderSize = Vector3.zero;
        colliderSize.x = Mathf.Max(MathF.Abs(topRight.x - bottomLeft.x), 0.1f);
        colliderSize.y = Mathf.Max(MathF.Abs(topRight.y - bottomLeft.y), 0.1f);
        colliderSize.z = Mathf.Max(MathF.Abs(topRight.z - bottomLeft.z), 0.1f);

        corridor.AddComponent<BoxCollider>();
        corridor.GetComponent<BoxCollider>().size = colliderSize;

        return corridor;
    }



    public void SetupRoomState()
    {
        if (this.RoomNode.NodeSymbol.Name == "l")
        {
            lockInstance.SetPosition(this.transform.position + Vector3.up * CorridorHeight / 3f);
            lockInstance.lockRoom = this;
            return;
        }

        if (this.RoomNode.NodeSymbol.Name == "start")
            OpenDoors();

        else if (this.RoomNode.NodeSymbol.Name == "k")
        {
            Key key = Instantiate(keyPrefab, this.transform);
            key.SetPosition(this.transform.position + Vector3.up * CorridorHeight / 3f);
            key.KeyGUID = this.RoomNode.GUID;
            key.SetColor();

            foreach (var room in this.LockRooms)
            {
                room.lockInstance.AddKey(key);
            }
        }
        else if (this.RoomNode.NodeSymbol.Name == "t")
        {
            Task task = Instantiate(taskPrefab, this.transform);
            task.SetPosition(this.transform.position + Vector3.up * CorridorHeight / 3f);
            task.room = this;
        }
        else if (this.RoomNode.NodeSymbol.Name == "b")
        {
            Treasure treasure = Instantiate(treasurePrefab, this.transform);
            treasure.SetPosition(this.transform.position + Vector3.up * CorridorHeight / 3f);
        }
        Destroy(lockInstance.gameObject);
    }

    public void OpenDoors()
    {
        foreach (var door in ExitDoors)
        {
            door.Open();
        }
    }


}
