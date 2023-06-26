using GG.Builder;
using GG.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

namespace PCG
{
    public class PCGMap : MonoBehaviour
    {
        public struct GridCoord
        {
            public int I;
            public int J;

            public GridCoord(int row, int col)
            {
                I = row;
                J = col;
            }

            public GridCoord(float row, float col)
            {
                I = Mathf.FloorToInt(row);
                J = Mathf.FloorToInt(col);
            }
        }

        [SerializeField]
        private Room roomPrefab;

        [SerializeField]
        private Vector2Int GridSize = new Vector2Int(15, 15);

        [SerializeField]
        private float CellSize;

        [SerializeField]
        private int NumberOfRooms = 10;
        

        private List<Room> RoomList;

        

        // Start is called before the first frame update

        bool[,] GridOccupancy;

        private bool isGenerating = false;

        GGGraph m_Graph;

        int[] row = new int[] { -1, 0, 0, 1 };
        int[] col = new int[] { 0, -1, 1, 0 };

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
                }
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                foreach (var r in RoomList)
                {
                    foreach (var e in m_Graph.GetEdgesFromNode(r.RoomNode))
                    {
                        if (e.EdgeSymbol.Type == GraphSymbolType.Edge)
                        {
                            r.AddConnection(RoomList.Find(x => x.RoomNode == e.EndNode), Color.green);
                            
                        }
                    }
                }

            }
        }

        
    


        

        private IEnumerator roomGeneratorRoutine()
        {
            DeleteAllRooms();
            InitializeGrids();
            m_Graph = GetGraph();
            
            while (!SpawnPrefabs(m_Graph))
            {
                m_Graph = GetGraph();
                yield return null;
            }


            ConnectRooms(m_Graph);
            isGenerating = false;
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
                    if (e.EdgeSymbol.Type == GraphSymbolType.Edge) continue;

                    var toNode = RoomList.Find(x => x.RoomNode == e.EndNode);
                    r.AddConnection(toNode.GetComponent<Room>(), Color.white);
                }

            }
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

        private Vector2 PositionToGrid(Vector2 postion)
        {
            int cellCoordX = (int)(Mathf.Floor(postion.x / CellSize));
            int cellCoordY = (int)(Mathf.Floor(postion.y / CellSize));

            cellCoordX = Math.Clamp(cellCoordX, 0, (int)GridSize.x - 1);
            cellCoordY = Math.Clamp(cellCoordY, 0, (int)GridSize.y - 1);

            if (!GridOccupancy[cellCoordX, cellCoordY])
            {
                GridOccupancy[cellCoordX, cellCoordY] = true;
                return new Vector2(cellCoordX, cellCoordY);
            }
            else if (cellCoordY + 1 < GridSize.y && !GridOccupancy[cellCoordX, cellCoordY + 1])
            {
                GridOccupancy[cellCoordX, cellCoordY + 1] = true;
                return new Vector2(cellCoordX, cellCoordY + 1);
            }
            else if (cellCoordY - 1 >= 0 && !GridOccupancy[cellCoordX, cellCoordY - 1])
            {
                GridOccupancy[cellCoordX, cellCoordY - 1] = true;
                return new Vector2(cellCoordX, cellCoordY - 1);
            }
            else if (cellCoordX + 1 < GridSize.x && !GridOccupancy[cellCoordX + 1, cellCoordY])
            {
                GridOccupancy[cellCoordX + 1, cellCoordY] = true;
                return new Vector2(cellCoordX + 1, cellCoordY);
            }
            else if (cellCoordX - 1 >= 0 && !GridOccupancy[cellCoordX - 1, cellCoordY])
            {
                GridOccupancy[cellCoordX - 1, cellCoordY] = true;
                return new Vector2(cellCoordX - 1, cellCoordY);
            }

            else if (cellCoordX + 1 < GridSize.x && cellCoordY + 1 < GridSize.y && !GridOccupancy[cellCoordX + 1, cellCoordY + 1])
            {
                GridOccupancy[cellCoordX + 1, cellCoordY + 1] = true;
                return new Vector2(cellCoordX + 1, cellCoordY + 1);
            }
            else if (cellCoordX - 1 >= 0 && cellCoordY - 1 >= 0 && !GridOccupancy[cellCoordX - 1, cellCoordY - 1])
            {
                GridOccupancy[cellCoordX - 1, cellCoordY - 1] = true;
                return new Vector2(cellCoordX - 1, cellCoordY - 1);
            }
            else if (cellCoordX + 1 < GridSize.x && cellCoordY - 1 >= 0 && !GridOccupancy[cellCoordX + 1, cellCoordY - 1])
            {
                GridOccupancy[cellCoordX + 1, cellCoordY - 1] = true;
                return new Vector2(cellCoordX + 1, cellCoordY - 1);
            }
            else if (cellCoordX - 1 >= 0 && cellCoordY + 1 < GridSize.y && !GridOccupancy[cellCoordX - 1, cellCoordY + 1])
            {
                GridOccupancy[cellCoordX - 1, cellCoordY + 1] = true;
                return new Vector2(cellCoordX - 1, cellCoordY + 1);
            }

            return new Vector2(-1, -1);
        }

        private GGGraph GetGraph()
        {
            var saveData = GGSaveGraph.Load("FirstGraph");
            var ruleList = saveData.RuleList;
            GGGraph res = GGBuilder.ApplyAllRules(ruleList, NumberOfRooms);

            return res;
        }

        private GameObject CreateRoom(Vector2 bottomLeftCorner, Vector2 topRightCorner)
        {
            Vector3 bottomLeftV = new Vector3(bottomLeftCorner.x, 0, bottomLeftCorner.y);
            Vector3 bottomRightV = new Vector3(topRightCorner.x, 0, bottomLeftCorner.y);
            Vector3 topLeft = new Vector3(bottomLeftCorner.x, 0, topRightCorner.y);
            Vector3 topRight = new Vector3(topRightCorner.x, 0, topRightCorner.y);

            Vector3[] vertices = new Vector3[]
            {
                topLeft,
                topRight,
                bottomLeftV,
                bottomRightV,
            };

            Vector2[] UVs = new Vector2[vertices.Length];
            
            for (int i = 0; i < UVs.Length; i++)
            {
                UVs[i] = new Vector2(vertices[i].x, vertices[i].z);
            }

            int[] triangles = new int[]
            {
                0, 1, 2,
                2, 1, 3
            };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = UVs;
            mesh.triangles = triangles;

            GameObject dngeonFloor = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer), typeof(Room));
            
            dngeonFloor.transform.position = Vector3.zero;
            dngeonFloor.transform.localScale = Vector3.one;
            dngeonFloor.GetComponent<MeshFilter>().mesh = mesh;
            dngeonFloor.GetComponent<MeshRenderer>().material = null;

            return dngeonFloor;
        }

        private Vector2 AddToAdjacent(Vector2 nodePosition)
        {
            int cellCoordX = Mathf.FloorToInt(nodePosition.x / CellSize);
            int cellCoordY = Mathf.FloorToInt(nodePosition.y / CellSize);

            cellCoordX = Math.Clamp(cellCoordX, 0, (int)GridSize.x - 1);
            cellCoordY = Math.Clamp(cellCoordY, 0, (int)GridSize.y - 1);

            Vector2 pos = new Vector2(-10, -10);

            for (int i = 0; i < Math.Max(GridOccupancy.GetLength(0), GridOccupancy.GetLength(1)); i++)
            {
                if (cellCoordX + i < GridOccupancy.GetLength(0))
                {
                    if (!GridOccupancy[i + cellCoordX, cellCoordY])
                    {
                        GridOccupancy[i + cellCoordX, cellCoordY] = true;
                        pos = new Vector2(i + cellCoordX, cellCoordY);
                        break;
                    }
                }
                if (cellCoordX - i >= 0)
                {
                    if (!GridOccupancy[cellCoordX - i, cellCoordY])
                    {
                        GridOccupancy[cellCoordX - i, cellCoordY] = true;
                        pos = new Vector2(cellCoordX - i, cellCoordY);
                        break;
                    }
                }
                if (cellCoordY + i < GridOccupancy.GetLength(1))
                {
                    if (!GridOccupancy[cellCoordX, i + cellCoordY])
                    {
                        GridOccupancy[cellCoordX, i + cellCoordY] = true;
                        pos = new Vector2(cellCoordX, i + cellCoordY);
                        break;
                    }
                }
                if (cellCoordY - i >= 0)
                {
                    if (!GridOccupancy[cellCoordX, cellCoordY - i])
                    {
                        GridOccupancy[cellCoordX, cellCoordY - i] = true;
                        pos = new Vector2(cellCoordX, cellCoordY - i);
                        break;
                    }
                }
                if (cellCoordX + i < GridOccupancy.GetLength(0) && cellCoordY + i < GridOccupancy.GetLength(1))
                {
                    if (!GridOccupancy[i + cellCoordX, i + cellCoordY])
                    {
                        GridOccupancy[i + cellCoordX, i + cellCoordY] = true;
                        pos = new Vector2(i + cellCoordX, i + cellCoordY);
                        break;
                    }
                }
                if (cellCoordX - i >= 0 && cellCoordY - i >= 0)
                {
                    if (!GridOccupancy[cellCoordX - i, cellCoordY - i])
                    {
                        GridOccupancy[cellCoordX - i, cellCoordY - i] = true;
                        pos = new Vector2(cellCoordX - i, cellCoordY - i);
                        break;
                    }
                }
                if (cellCoordX + i < GridOccupancy.GetLength(0) && cellCoordY - i >= 0)
                {
                    if (!GridOccupancy[i + cellCoordX, cellCoordY - i])
                    {
                        GridOccupancy[i + cellCoordX, cellCoordY - i] = true;
                        pos = new Vector2(i + cellCoordX, cellCoordY - i);
                        break;
                    }
                }
                if (cellCoordX - i >= 0 && cellCoordY + i < GridOccupancy.GetLength(1))
                {
                    if (!GridOccupancy[cellCoordX - i, i + cellCoordY])
                    {
                        GridOccupancy[cellCoordX - i, i + cellCoordY] = true;
                        pos = new Vector2(cellCoordX - i, i + cellCoordY);
                        break;
                    }
                }
            }
            return pos;
        }

        private bool SpawnPrefabs(GGGraph graph)
        {
            Vector2 startPos = GetStartPositionOffset(graph);
            Vector2 gridScale = GetScaleGraph(graph);

            Queue<GGNode> nodesToVisit = new Queue<GGNode>();
            GGNode startNode = graph.Nodes.Find(n => n.NodeSymbol.Name == "start");
            nodesToVisit.Enqueue(startNode);

            Vector2 previousPos = startNode.Position;

            List<GGNode> visitedNodes = new List<GGNode>();

            while (nodesToVisit.Count > 0)
            {
                GGNode currentNode = nodesToVisit.Dequeue();

                Vector2 pos = (currentNode.Position - startPos) * gridScale;
                Vector2 gridPos = PositionToGrid(pos);

                if (gridPos.x < 0 && gridPos.y < 0 )
                {
                    gridPos = AddToAdjacent(pos);
                }

                previousPos = currentNode.Position;

                Room r = Instantiate(roomPrefab, new Vector3(gridPos.x * CellSize, 0, gridPos.y * CellSize), Quaternion.identity);
                r.GetComponent<MeshRenderer>().material.color = SetColor(currentNode.NodeSymbol);
                r.RoomNode = currentNode;
                r.CellCoord = new GridCoord(gridPos.x, gridPos.y);
                ChangeSize(r.gameObject);
                RoomList.Add(r);
                r.transform.parent = this.transform;

                foreach (GGEdge e in graph.GetEdgesFromNode(currentNode))
                {
                    if (visitedNodes.Contains(e.EndNode)) continue;

                    if (e.EdgeSymbol.Type != GraphSymbolType.Edge)
                    {
                        e.EndNode.Position = previousPos + new Vector2(200, 0);
                        nodesToVisit.Enqueue(e.EndNode);
                        visitedNodes.Add(e.EndNode);
                    }
                }
            }

            Vector3 startPosition = RoomList.Find(rooms => rooms.RoomNode.NodeSymbol.Name == "start").transform.position;

            Vector3 offset = this.transform.position - startPosition;

            foreach (Room r in RoomList)
            {
                r.transform.position += offset;
            }

            return true;
        }

        private Color SetColor(Symbol nodeSymbol)
        {
            if (nodeSymbol.Name == "start") return Color.green;
            else if (nodeSymbol.Name == "end") return Color.red;
            else if (nodeSymbol.Name == "l") return Color.cyan;
            else if (nodeSymbol.Name == "k") return Color.blue;
            else if (nodeSymbol.Name == "t") return Color.gray;

            else return Color.white;
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

        private void ChangeSize(GameObject go)
        {
            go.transform.localScale = new Vector3(CellSize * 0.75f, 0.1f, CellSize * 0.75f);
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
