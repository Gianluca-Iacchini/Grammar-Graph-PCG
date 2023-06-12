using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using UnityEditor;
using System.Linq;
using Codice.CM.Common.Serialization;
using Unity.Plastic.Newtonsoft.Json;
using System.Collections.ObjectModel;
using UnityEngine.Rendering.Universal;
using GG.Utils;
using GG.Data.Save;
using GG.ScriptableObjects;

namespace GrammarGraph {

    public class GrammarGraphView : GraphView
    {
        private List<GrammarGraphGroup> m_GroupList;

        private readonly Vector2 defaultNodeSize = new Vector2(150, 200);

        public List<Symbol> Symbols;

        public string ID { get; set; }

        public GrammarGraphView()
        {
            m_GroupList = new List<GrammarGraphGroup>();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());


            AddGridBackground();
            AddStyles();

            serializeGraphElements = SerializeGraphElementsImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;

            this.ID = Guid.NewGuid().ToString();

            graphViewChanged += OnGraphViewChange;
        }

        private GraphViewChange OnGraphViewChange(GraphViewChange gvc)
        {
            if (gvc.elementsToRemove == null) return gvc;

            foreach (var g in gvc.elementsToRemove)
            {
                if (g is GrammarGraphGroup group)
                {
                    m_GroupList.Remove(group);
                }
            }

            return gvc;
        }

        public List<GGNodeSaveData> GetNodeData()
        {
            List<GGNodeSaveData> m_NodeList = new List<GGNodeSaveData>();

            nodes.ForEach(node => 
            { 
                if (node is GrammarGraphNode ggNode)
                {
                    GGNodeSaveData saveDataNode = new GGNodeSaveData
                    {
                        ID = ggNode.ID,
                        Position = ggNode.GetPosition().position,
                        Symbol = ggNode.NodeSymbol
                    };

                    if (ggNode.Group != null)
                    {
                        saveDataNode.GroupID = ggNode.Group.ID;
                    }

                    m_NodeList.Add(saveDataNode);
                }
            });

            return m_NodeList;
        }

        public List<GGGroupSaveData> GetGroupData()
        {
            List<GGGroupSaveData> GroupSaveDataList = new List<GGGroupSaveData>();

            foreach (var group in m_GroupList)
            {
                GGGroupSaveData groupSaveData = new GGGroupSaveData
                {
                    ID = group.ID,
                    Position = group.GetPosition().position,
                    Name = group.title,
                };
                GroupSaveDataList.Add(groupSaveData);
            }

            return GroupSaveDataList;
        }

        public List<GGEdgeSaveData> GetEdgeSaveData()
        {
            List<GGEdgeSaveData> EdgeSaveDataList = new List<GGEdgeSaveData>();

            this.edges.ForEach(e =>
            {
                Symbol edgeSymbol = e.input.userData as Symbol;

                if (edgeSymbol == null)
                {
                    edgeSymbol = Symbol.SymbolAsterisk();
                }
                else 
                {
                    GrammarGraphNode inputNode = e.input.node as GrammarGraphNode;
                    GrammarGraphNode outputNode = e.output.node as GrammarGraphNode;

                    if (inputNode == null || outputNode == null) { return; }

                    GGEdgeSaveData edgeData = new GGEdgeSaveData
                    {
                        EdgeSymbol = edgeSymbol,
                        InputNodeID = inputNode.ID,
                        OutputNodeID = outputNode.ID,
                    };

                    EdgeSaveDataList.Add(edgeData);
                }
            });

            return EdgeSaveDataList;
        }

        public GGGraphSaveData GetGraphSaveData()
        {
            GGGraphSaveData graphSaveData = new GGGraphSaveData
            { 
                ID = this.ID,
                Groups = this.GetGroupData(),
                Nodes = this.GetNodeData(),
                Edges = this.GetEdgeSaveData(),
            };

            return graphSaveData;
        }

        private GrammarGraphGroup CreateGroup(string title, Vector2 localMousePos)
        {
            GrammarGraphGroup group = new GrammarGraphGroup();

            group.title = title;

            group.SetPosition(new Rect(localMousePos, Vector2.zero));

            foreach (GraphElement ge in selection)
            {
                if (ge is not GrammarGraphGroup)
                    group.AddElement(ge);
            }

            group.ID = Guid.NewGuid().ToString();

            m_GroupList.Add(group);

            return group;
        }

        private bool IsCompatible(Port startPort, Port endPort)
        {
            var userDataStart = startPort.userData as Symbol;
            var userDataEnd = endPort.userData as Symbol;

            if (userDataStart != null && userDataEnd != null)
            {
                // Both null and same edge covered
                if (userDataStart == userDataEnd)
                    return true;



                if (userDataStart.Type != GraphSymbolType.Edge && userDataEnd.Type != GraphSymbolType.Edge)
                    return true;
            }

            return false;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port => {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    if (IsCompatible(startPort, port))
                    {
                        compatiblePorts.Add(port);
                    }
                }
            });

            return compatiblePorts;
        }

        public void ChangePortsName(string oldName, string newName)
        {
            
            ports.ForEach(port =>
            {
                if (port.portName == oldName)
                {
                    port.portName = newName; 
                }
            });
        }

        private GrammarGraphNode CreateGraphGrammarNode(string nodeName, Vector2 pos)
        {
            GrammarGraphNode node = new GrammarGraphNode(nodeName, Guid.NewGuid().ToString(), "*", 0);

            node.AddPort("Node", Direction.Input, Port.Capacity.Single, null, true);
            node.AddPort("Node", Direction.Output, Port.Capacity.Multi, null, true);

            node.SetPosition(new Rect(pos, defaultNodeSize));

            node.InitializeDropdown(Symbols);

            return node;
        }

        public void CreateNode(string nodeName, Vector2 pos = default(Vector2))
        {
            AddElement(CreateGraphGrammarNode(nodeName, pos));
        }

        public void CreateNode(NodeData data, Vector2 pos = default(Vector2))
        {
            var node = CreateGraphGrammarNode(data.title, pos);
            node.Data = data;
            AddElement(node);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var mousePos = evt.localMousePosition;

            evt.menu.AppendAction("Add Node", a => { CreateNode("New node", mousePos); }, a => DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction("Add Group", a => { AddElement(CreateGroup("New group", mousePos)); }, a => DropdownMenuAction.Status.Normal);


            base.BuildContextualMenu(evt);
        }

        private void AddGridBackground()
        {
            GridBackground gridBackground = new GridBackground();
            gridBackground.StretchToParentSize();

            Insert(0, gridBackground);
        }

        private void AddStyles()
        {
            StyleSheet styleSheet = (StyleSheet)EditorGUIUtility.Load("GraphGrammar/GGBackground.uss");
            styleSheets.Add(styleSheet);
        }


        // From https://github.com/Unity-Technologies/ShaderGraph/blob/master/com.unity.shadergraph/Editor/Drawing/Views/MaterialGraphView.cs
        string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            var nodes = elements.OfType<GrammarGraphNode>().Select(x => x.Data as NodeData);
            var edges = elements.OfType<UnityEditor.Experimental.GraphView.Edge>();
            //var properties = selection.OfType<BlackboardField>().Select(x => x.userData as IShaderProperty);
            var s = JsonConvert.SerializeObject(nodes);

            return s;
        }

        void UnserializeAndPasteImplementation(string operationCode, string value)
        {
            var data = JsonConvert.DeserializeObject<List<NodeData>>(value);

            foreach (var node in data)
            {
                CreateNode(node, new Vector2(100, 200));
            }


        }

        public void ClearGraph()
        {
            graphElements.ForEach(x => { x.Clear(); RemoveElement(x); });
        }

        public void CreateFromSO(GGGraphSO saveData)
        {
            this.ID = saveData.ID;

            Dictionary<string, GrammarGraphGroup> groupDict = new Dictionary<string, GrammarGraphGroup>();

            foreach (var groupSaveData in saveData.Groups.GroupSaveData)
            {
                GrammarGraphGroup grammarGraphGroup = CreateGroup(groupSaveData.Name, groupSaveData.Position);
                grammarGraphGroup.ID = groupSaveData.ID;
                groupDict.Add(grammarGraphGroup.ID, grammarGraphGroup);

                AddElement(grammarGraphGroup);
            }

            Dictionary<string, GrammarGraphNode> nodeDict = new Dictionary<string, GrammarGraphNode>();

            foreach (var nodeSaveData in saveData.Nodes.NodeSaveData)
            {
                Symbol s = nodeSaveData.Symbol != null ? nodeSaveData.Symbol : Symbol.SymbolAsterisk();

                GrammarGraphNode grammarGraphNode = CreateGraphGrammarNode(s.Name, nodeSaveData.Position);
                grammarGraphNode.ID = nodeSaveData.ID;
                grammarGraphNode.NodeSymbol = s;

                if (nodeSaveData.GroupID != null && nodeSaveData.GroupID != string.Empty) 
                {
                    GrammarGraphGroup group = groupDict[nodeSaveData.GroupID];
                    group.AddElement(grammarGraphNode);
                    grammarGraphNode.Group = group;
                }

                nodeDict.Add(grammarGraphNode.ID, grammarGraphNode);
                AddElement(grammarGraphNode);
            }


            foreach (var e in saveData.Edges.EdgeSaveData)
            {
                var inputNode = nodeDict[e.InputNodeID];
                var outputNode = nodeDict[e.OutputNodeID];

                if (inputNode != null && outputNode != null) 
                {


                    Symbol symbol = e.EdgeSymbol;
                    if (symbol == null) symbol = Symbol.SymbolAsterisk();

                    var inputPorts = inputNode.GetInputPorts();
                    var outputPorts = outputNode.GetOutputPorts();

                    Port inputPort;
                    Port outputPort;

                    if (inputPorts.First() is Port ip)
                    {
                        if (ip.connected || e.EdgeSymbol.Type == GraphSymbolType.Edge)
                        {
                            inputPort = inputNode.AddPort(symbol.Name, Direction.Input, Port.Capacity.Single, symbol);
                        }

                        else
                        {
                            inputPort = ip;
                        }
                    }
                    else
                    {
                        inputPort = inputNode.AddPort(symbol.Name, Direction.Input, Port.Capacity.Single, symbol);
                    }

                    if (outputPorts.First() is Port op)
                    {


                        if (op.connected || e.EdgeSymbol.Type == GraphSymbolType.Edge)
                        {
                            outputPort = outputNode.AddPort(symbol.Name, Direction.Output, Port.Capacity.Multi, symbol);
                        }

                        else
                        {
                            outputPort = op;
                        }
                    }
                    else
                    {
                        outputPort = outputNode.AddPort(symbol.Name, Direction.Output, Port.Capacity.Multi, symbol);
                    }

                    this.AddElement(inputPort.ConnectTo(outputPort));
                }
            }
        }
    }
}