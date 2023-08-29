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
        private Vector2Int GridSize = new Vector2Int(15, 15);

        [SerializeField]
        private float CellSize;

        [SerializeField]
        private int NumberOfRooms = 10;

        [SerializeField]
        private GGSaveDataSO GrammarGraph;


        private const float RepulsiveForceFactor = 0.5f;
        private const float AttractiveForceFactor = 0.1f;
        private const float DampingFactor = 0.99f;
        private const float ConvergenceThreshold = 0.01f;
        private const float MaxForce = 1000f;
        private const float RepulsionDistance = 1.5f;

        private const float SpringForce = 0.1f;

        private List<Room> RoomList;


        private bool m_ShowKeyLines = false;
        private bool m_ShowNodeLines = true;

        // Start is called before the first frame update

        bool[,] GridOccupancy;

        private bool isGenerating = false;

        GGGraph m_Graph;

        void Start()
        {
            RoomList = new List<Room>();
            InitializeGrids();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (!isGenerating)
                {
                    isGenerating = true;
                    StartCoroutine(roomGeneratorRoutine());
                    m_ShowNodeLines = true;
                    m_ShowKeyLines = false;
                }
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                m_ShowKeyLines = !m_ShowKeyLines;
                foreach (var r in RoomList)
                {
                    r.ToggleKeyLines(m_ShowKeyLines);
                }
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                m_ShowNodeLines = !m_ShowNodeLines;
                foreach (var r in RoomList)
                {
                    r.ToggleNodeLines(m_ShowNodeLines);
                }
            }
        }

        
    


        

        private IEnumerator roomGeneratorRoutine()
        {
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
                    yield break;
                }

                yield return null;
            }

            
            yield return SpawnRooms(m_Graph);


            ConnectRooms(m_Graph);
            isGenerating = false;

            float minX = RoomList.Min(x => x.transform.position.x);
            float maxX = RoomList.Max(x => x.transform.position.x);

            float minZ = RoomList.Min(x => x.transform.position.z); 
            float maxZ = RoomList.Max(x => x.transform.position.z);

            Camera.main.transform.position = new Vector3((minX + maxX) / 2, Camera.main.transform.position.y ,(minZ + maxZ) / 2);
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

                        continue;
                    }

                    var toNode = RoomList.Find(x => x.RoomNode == e.EndNode);
                    r.AddConnection(toNode.GetComponent<Room>(), e.EdgeSymbol);
                }
                r.ToggleKeyLines(false);
            }

            foreach (Room r in RoomList)
                r.CreateCorridors(0.5f);
        }

        private void InitializeGrids()
        {
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

                    // Only consider direct relationships, (i.e. discard key relationships)
                    if (e.EdgeSymbol.Type == GraphSymbolType.Edge) continue;
                    
                    if (visitedNodes.Contains(e.EndNode)) continue;

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
        }

        private Room CreateRoom(GGNode node, Vector2Int CellCoords)
        {
            
            Room r = Instantiate(roomPrefab, new Vector3(CellCoords.x * CellSize, 0, CellCoords.y * CellSize), Quaternion.identity);
            r.RoomNode = node;
            r.Cell = CellCoords;
            ChangeSize(r);
            r.transform.parent = this.transform;

            return r;
        }

        // Avoid cells from overlapping
        private Vector2Int GetEmptyCell(Room parent, int x, int y)
        {
            // This should never happen
            if (x >= GridOccupancy.GetLength(0) || y >= GridOccupancy.GetLength(1)) new Vector2Int(-1,-1);

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
