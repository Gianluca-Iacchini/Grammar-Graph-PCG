using GG.Builder;
using GG.Data.Save;
using GG.ScriptableObjects;
using GG.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

namespace PCG
{
    public class PCGMap : MonoBehaviour
    {
        

        [SerializeField]
        private Room roomPrefab;        

        [SerializeField]
        private float CellSize;

        [SerializeField]
        private int NumberOfRooms = 10;

        [SerializeField]
        private GGSaveDataSO GrammarGraph;

        [SerializeField]
        private GameObject PlayerPrefab;

        private GameObject PlayerClone;

        private List<Room> RoomList;


        public float CorridorWidth = 0.5f;
        public float RoomHeight = 3f;
        public float WallThickness = 0.1f;

        private Vector2Int GridSize = new Vector2Int(15, 15);

        // Start is called before the first frame update

        bool[,] GridOccupancy;

        private bool isGenerating = false;

        private bool isPlaying = false;

        GGGraph m_Graph;

        Camera mainCamera = null;

        void Start()
        {
            mainCamera = Camera.main;
            RoomList = new List<Room>();
            InitializeGrids();
        }

        public Transform GeneratingText;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (!isGenerating)
                {
                    GeneratingText.gameObject.SetActive(true);
                    isGenerating = true;
                    StartCoroutine(roomGeneratorRoutine());
                    if (PlayerClone)
                        PlayerClone.GetComponent<BasicFPSControl>().Reset();
                }
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (RoomList.Count > 0)
                {
                    RoomList.ForEach(r => r.ToggleRoomColor());
                }
            }

            if (Input.GetKeyDown(KeyCode.P) && RoomList.Count > 0)
            {
                if (!isPlaying)
                {
                    mainCamera.gameObject.SetActive(false);
                    PlayerClone.SetActive(true);
                    PlayerClone.GetComponent<BasicFPSControl>().Activate();
                    isPlaying = true;
                }
                else
                {
                    mainCamera.gameObject.SetActive(true);
                    PlayerClone.GetComponent<BasicFPSControl>().Deactivate();
                    PlayerClone.SetActive(false);
                    isPlaying = false;
                }
            }
        }

        
    


        

        private IEnumerator roomGeneratorRoutine()
        {
            yield return new WaitForSeconds(0.1f);

            DeleteAllRooms();
            InitializeGrids();
            m_Graph = GetGraph();

            int maxTries = 20;

            if (m_Graph == null)
            {
                m_Graph = GetGraph();

                maxTries--;

                if (maxTries <= 0)
                {
                    Debug.LogError("Could not generate valid graph");
                    isGenerating = false;
                    GeneratingText.gameObject.SetActive(false); 
                    yield break;
                }
                GeneratingText.gameObject.SetActive(false);
                yield return null;
            }

            
            yield return SpawnRooms(m_Graph);


            ConnectRooms(m_Graph);
            isGenerating = false;

            Room startRoom = RoomList.First(x => x.RoomNode.NodeSymbol.Name == "start");

            if (startRoom != null)
            {
                Camera.main.transform.position = new Vector3(startRoom.transform.position.x, Camera.main.transform.position.y, startRoom.transform.position.z);
            }

            GeneratingText.gameObject.SetActive(false);
        }



        private void DeleteAllRooms()
        {
            foreach (Room r in RoomList)
            {
                Destroy(r.gameObject);
            }

            RoomList.Clear();   
        }

        private void ConnectRooms(GGGraph graph)
        {
            foreach (Room r in RoomList)
            {
                if (r.RoomNode == null) continue;

                foreach (GGEdge e in graph.GetEdgesFromNode(r.RoomNode))
                {
                    if (e.EdgeSymbol.Type == GraphSymbolType.Edge)
                    {
                        var keyNode = RoomList.Find(x => x.RoomNode == e.EndNode);

                        r.AddConnection(keyNode.GetComponent<Room>(), e.EdgeSymbol);

                    }
                    else
                    {
                        var toNode = RoomList.Find(x => x.RoomNode == e.EndNode);
                        r.AddConnection(toNode, e.EdgeSymbol);
                    }
                }
            }

            foreach (Room r in RoomList)
                r.CreateCorridors();

            foreach (Room r in RoomList)
                r.SetupRoomState();
        }

        private void InitializeGrids()
        {
            GridSize = new Vector2Int(NumberOfRooms * 3, NumberOfRooms * 3);
            bool[,] roomGrid = new bool[GridSize.x, GridSize.y];
            
            for (int i = 0; i < GridSize.x; i++)
            {
                for (int j = 0; j < GridSize.y; j++)
                {
                    roomGrid[i, j] = false;
                }
            }

            GridOccupancy = roomGrid;
        }

       
        private GGGraph GetGraph()
        {
            if (GrammarGraph != null)
            {
                var ruleList = GrammarGraph.RuleList;
                GGGraph res = GGBuilder.ApplyAllRules(ruleList, NumberOfRooms);

                if (res != null)
                {
                    return res;
                }
            }

            Debug.LogError("Error loading Grammar Graph");

            return null;
        }

        private IEnumerator SpawnRooms(GGGraph graph)
        {

            Vector2 startPos = GetStartPositionOffset(graph);
            Vector2 gridScale = GetScaleGraph(graph);

            // Set starting room position
            Queue<Room> nodesToVisit = new Queue<Room>();
            GGNode startNode = graph.Nodes.Find(n => n.NodeSymbol.Name == "start");
            Room startRoom = CreateRoom(startNode, new Vector2Int(0, GridSize.y / 2));

            GridOccupancy[0, GridSize.y / 2] = true;

            nodesToVisit.Enqueue(startRoom);

            List<GGNode> visitedNodes = new List<GGNode>();

            while (nodesToVisit.Count > 0)
            {
                Room r = nodesToVisit.Dequeue();
                GGNode currentNode = r.RoomNode;
                RoomList.Add(r);

                int zeroOne = UnityEngine.Random.Range(0, 2);
                int minusOrPlus = zeroOne * 2 - 1;

                // Randomly sets the y coordinate of the next room as being one higher or one lower than the current room
                int cellY = r.Cell.y - minusOrPlus;

                // If the room has only one child, then randomly choose it to place at the same height as the current room
                // This is done to reduce zig-zaging between rooms
                if (graph.GetEdgesFromNode(currentNode).Count(e => e.EdgeSymbol.Type != GraphSymbolType.Edge) == 1)
                    if (UnityEngine.Random.Range(0, 10) < 7)
                        cellY = r.Cell.y;

                // Order by increasing number of children to avoid overlaps.
                var edges = graph.GetEdgesFromNode(currentNode);
                edges = edges.OrderBy((x) => graph.GetEdgesFromNode(x.EndNode).Count(e => e.EdgeSymbol.Type != GraphSymbolType.Edge)).ToList();

                // Used to avoid stacking rooms vertically, which will cause corridors to run inside rooms
                bool alreadyPlacedVertical = false;
                // Used to keep the map compact (reduces spaces between rooms)
                int placedChildren = 0;

                for (int i = 0; i < edges.Count; i++)
                {
                    GGEdge e = edges[i];
                    GGEdge nextChild = i + 1 < edges.Count ? edges[i + 1] : null;
                    
                    if (visitedNodes.Contains(e.EndNode)) continue;

                    // Only consider direct relationships, (i.e. discard key relationships)
                    if (e.EdgeSymbol.Type == GraphSymbolType.Edge) continue;

                    // Get number of rooms connected to the current child and the next child
                    int nChildren = graph.GetEdgesFromNode(e.EndNode).Count(e => e.EdgeSymbol.Type != GraphSymbolType.Edge);
                    int nChildrenNext = i + 1 < edges.Count ? graph.GetEdgesFromNode(nextChild.EndNode).Count(e => e.EdgeSymbol.Type != GraphSymbolType.Edge) : 0;

                    // Place the room one cell to the right of the current room
                    int cellX = r.Cell.x + 1;

                    // If the current room has only one child, then place the next room on the same horizontal coordinate
                    // But on a different vertical coordinate, we need to change the y coordinate becayse we dont know if the cellY coordinate was left unchanged.
                    if (nChildren < 1 && cellX > 0 && !alreadyPlacedVertical)
                    {
                        if (!GridOccupancy[cellX - 1, cellY])
                        {
                            cellX -= 1;
                            alreadyPlacedVertical = true;
                        }
                        else if (cellY - 1 >=0 && !GridOccupancy[cellX - 1, cellY -1])
                        {
                            cellX -= 1;
                            cellY -= 1;
                            alreadyPlacedVertical = true;
                        }
                        else if (cellY + 1 < GridOccupancy.GetLength(1) && !GridOccupancy[cellX - 1, cellY + 1])
                        {
                            cellX -= 1;
                            cellY += 1;
                            alreadyPlacedVertical = true;
                        }
                    }

                    // Returns the closest empty cell starting from (cellX, cellY) 
                    Vector2Int emptyCell = GetEmptyCell(r, cellX, cellY);

                    Room newRoom = CreateRoom(e.EndNode, emptyCell);
                    newRoom.ParentRoom = r;
                        

                    nodesToVisit.Enqueue(newRoom);
                    visitedNodes.Add(e.EndNode);

                    cellY += minusOrPlus * (Math.Max(1,nChildren - placedChildren + nChildrenNext));
                }

                yield return null;
            }

            Vector3 startPosition = RoomList.Find(rooms => rooms.RoomNode.NodeSymbol.Name == "start").transform.position;

            Vector3 offset = this.transform.position - startPosition;

            foreach (Room r in RoomList)
            {
                r.transform.position += offset;
            }

            if (!PlayerClone) PlayerClone = Instantiate(PlayerPrefab);
            PlayerClone.transform.position = startRoom.transform.position + new Vector3(0, 0.6f, 0);
        }

        private Room CreateRoom(GGNode node, Vector2Int CellCoords)
        {
            
            Room r = Instantiate(roomPrefab, new Vector3(CellCoords.x * CellSize, 0, CellCoords.y * CellSize), Quaternion.identity);
            r.RoomNode = node;
            r.Cell = CellCoords;
            ChangeSize(r);
            r.transform.parent = this.transform;

            r.CorridorWidth = CorridorWidth;
            r.CorridorHeight = RoomHeight;
            r.Thickness = WallThickness;

            return r;
        }

        // Avoid cells from overlapping
        private Vector2Int GetEmptyCell(Room parent, int x, int y)
        {
            // This should never happen
            if (x >= GridOccupancy.GetLength(0) || y >= GridOccupancy.GetLength(1)) return new Vector2Int(-1,-1);
            if (x < 0 || y < 0) return new Vector2Int(-1, -1);

            if (!GridOccupancy[x, y])
            {
                GridOccupancy[x, y] = true;
                return new Vector2Int(x, y);    
            }
            else
            {
                int i = 0;
                y = parent.Cell.y;
                while (y + i < GridOccupancy.GetLength(1) && y - i >= 0)
                {
                    if (!GridOccupancy[x, y + i])
                    {
                        GridOccupancy[x, y + i] = true;
                        return new Vector2Int(x, y + i);
                    }
                    else if (!GridOccupancy[x, y - i])
                    {
                        GridOccupancy[x, y - i] = true;
                        return new Vector2Int(x, y - i);
                    }

                    i++;
                }

                return new Vector2Int(-1, -1);
            }
        }

        private Vector2 GetStartPositionOffset(GGGraph graph)
        {
            foreach (GGNode n in graph.Nodes)
            {
                if (n.NodeSymbol.Name == "Start")
                {
                    return n.Position;
                }
            }

            return Vector2.zero;
        }

        private void ChangeSize(Room r)
        {
            float randomX = UnityEngine.Random.Range(0.25f, 0.85f) * CellSize;
            float randomZ = UnityEngine.Random.Range(0.25f, 0.85f) * CellSize;

            r.CreateMeshFloor(randomX, randomZ);
        }


        private Vector2 ComputeMin(GGGraph graph)
        {
            Vector2 min = Vector2.positiveInfinity;

            foreach (GGNode node in graph.Nodes)
            {
                min.x = Mathf.Min(min.x, node.Position.x);
                min.y = Mathf.Min(min.y, node.Position.y);
            }

            return min;
        }


        private Vector2 ComputeMax(GGGraph graph)
        {
            Vector2 max = Vector2.negativeInfinity;

            foreach (GGNode node in graph.Nodes)
            {
                max.x = Mathf.Max(max.x, node.Position.x);
                max.y = Mathf.Max(max.y, node.Position.y);
            }

            return max;
        }

        private Vector2 GetScaleGraph(GGGraph graph)
        {
            Vector2 max = ComputeMax(graph);
            Vector2 min = ComputeMin(graph);

            float xDistance = Mathf.Abs(max.x - min.x);
            float yDistance = Mathf.Abs(max.y - min.y);

            float gridScaleX = (GridSize.x) * CellSize / xDistance;
            float gridScaleY = (GridSize.y) * CellSize / yDistance;

            return new Vector2(gridScaleX, gridScaleY);
        }
    }
}
