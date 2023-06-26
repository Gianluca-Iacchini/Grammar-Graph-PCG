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

namespace GG.Editor {

    public class GGGraphView : GraphView
    {
        private List<GGGroupEditor> m_GroupList;

        private readonly Vector2 defaultNodeSize = new Vector2(150, 200);

        public List<Symbol> Symbols;

        public string ID { get; set; }

        public GGGraphView()
        {
            m_GroupList = new List<GGGroupEditor>();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());


            AddGridBackground();
            AddStyles();

            this.ID = Guid.NewGuid().ToString();

            graphViewChanged += OnGraphViewChange;
        }

        private GraphViewChange OnGraphViewChange(GraphViewChange gvc)
        {
            if (gvc.elementsToRemove != null)
            {

                foreach (var g in gvc.elementsToRemove)
                {
                    if (g is GGGroupEditor group)
                    {
                        m_GroupList.Remove(group);
                    }
                }

                var nNodesRemoved = gvc.elementsToRemove.Where(e=> e is GGNodeEditor).ToList().Count;

                nodes.ForEach(n => { if (n is GGNodeEditor gn) { gn.RemoveIdentifiers(nodes.Count() - nNodesRemoved); } });
            }

            return gvc;
        }

        public List<GGNodeSaveData> GetNodeData()
        {
            List<GGNodeSaveData> m_NodeList = new List<GGNodeSaveData>();

            nodes.ForEach(node => 
            { 
                if (node is GGNodeEditor ggNode)
                {
                    GGNodeSaveData saveDataNode = new GGNodeSaveData
                    {
                        ID = ggNode.ID,
                        Position = ggNode.GetPosition().position,
                        Symbol = ggNode.NodeSymbol,
                        Identifier = ggNode.NodeIdentifier,
                        IsExactInput = ggNode.IsExactInput,
                        IsExactOutput = ggNode.IsExactOutput,
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
                    Weight = group.Weight,
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
                    GGNodeEditor inputNode = e.input.node as GGNodeEditor;
                    GGNodeEditor outputNode = e.output.node as GGNodeEditor;

                    if (inputNode == null || outputNode == null) { return; }

                    GGEdgeSaveData edgeData = new GGEdgeSaveData
                    {
                        EdgeSymbol = edgeSymbol,
                        StartNodeID = outputNode.ID,
                        EndNodeID = inputNode.ID,
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

        private GGGroupEditor CreateGroup(string title, Vector2 localMousePos)
        {
            GGGroupEditor group = new GGGroupEditor();

            group.title = title;

            group.SetPosition(new Rect(localMousePos, Vector2.zero));

            foreach (GraphElement ge in selection)
            {
                if (ge is not GGGroupEditor)
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

        private GGNodeEditor CreateGraphGrammarNode(string nodeName, Vector2 pos)
        {
            GGNodeEditor node = new GGNodeEditor(Guid.NewGuid().ToString(), "*", 0);

            node.AddPort("Node", Direction.Input, null, true);
            node.AddPort("Node", Direction.Output, null, true);

            node.SetPosition(new Rect(pos, defaultNodeSize));

            node.InitializeDropdown(Symbols);

            node.graphView = this;

            return node;
        }



        public void CreateNode(string nodeName, Vector2 pos = default(Vector2))
        {
            var node = CreateGraphGrammarNode(nodeName, pos);
            node.NodeIdentifier = nodes.Count();
            AddElement(node);
            nodes.ForEach((n) => { if (n is GGNodeEditor gn) { gn.AddIdentifiers(nodes.Count()); } });
        }

        public void CreateNode(GGNodeSaveData data, Vector2 pos = default(Vector2))
        {
            var node = CreateGraphGrammarNode(data.Symbol.Name, pos);
            node.NodeIdentifier = nodes.Count();
            AddElement(node);
            nodes.ForEach((n) => { if (n is GGNodeEditor gn) { gn.AddIdentifiers(nodes.Count()); } });
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var mousePos = evt.localMousePosition;

            if (evt.target is not GGNodeEditor)
            {
                evt.menu.AppendAction("Add Node", a => { CreateNode("New node", mousePos); }, a => DropdownMenuAction.Status.Normal);
                evt.menu.AppendAction("Add Group", a => { AddElement(CreateGroup("New group", mousePos)); }, a => DropdownMenuAction.Status.Normal);
            }

            if (evt.target is GGNodeEditor)
            {
                var node = evt.target as GGNodeEditor;

                if (node.Group != null)
                {
                    evt.menu.AppendAction("Remove from group", a => {
                        foreach (var s in selection)
                        {
                            if (s is GGNodeEditor sNode && sNode.Group != null)
                            {
                                sNode.Group.RemoveElement(sNode);
                                sNode.Group = null;
                            }
                        }

                    }, a => DropdownMenuAction.Status.Normal);
                }
            }

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

        public void ClearGraph()
        {
            graphElements.ForEach(x => { x.Clear(); RemoveElement(x); });
        }

        public void CreateFromSaveData(GGGraphSaveData saveData)
        {
            this.ID = saveData.ID;

            Dictionary<string, GGGroupEditor> groupDict = new Dictionary<string, GGGroupEditor>();

            foreach (var groupSaveData in saveData.Groups)
            {
                GGGroupEditor grammarGraphGroup = CreateGroup(groupSaveData.Name, groupSaveData.Position);
                grammarGraphGroup.ID = groupSaveData.ID;
                grammarGraphGroup.Weight = groupSaveData.Weight;
                groupDict.Add(grammarGraphGroup.ID, grammarGraphGroup);

                AddElement(grammarGraphGroup);
            }

            Dictionary<string, GGNodeEditor> nodeDict = new Dictionary<string, GGNodeEditor>();

            foreach (var nodeSaveData in saveData.Nodes)
            {
                Symbol s = nodeSaveData.Symbol != null ? nodeSaveData.Symbol : Symbol.SymbolAsterisk();

                GGNodeEditor grammarGraphNode = CreateGraphGrammarNode(s.Name, nodeSaveData.Position);
                grammarGraphNode.ID = nodeSaveData.ID;
                grammarGraphNode.NodeSymbol = s;
                grammarGraphNode.NodeIdentifier = nodeSaveData.Identifier;
                grammarGraphNode.IsExactInput = nodeSaveData.IsExactInput;
                grammarGraphNode.IsExactOutput = nodeSaveData.IsExactOutput;

                if (nodeSaveData.GroupID != null && nodeSaveData.GroupID != string.Empty)
                {
                    GGGroupEditor group = groupDict[nodeSaveData.GroupID];
                    group.AddElement(grammarGraphNode);
                    grammarGraphNode.Group = group;
                }

                nodeDict.Add(grammarGraphNode.ID, grammarGraphNode);
                AddElement(grammarGraphNode);
                nodes.ForEach((n) => { if (n is GGNodeEditor gn) { gn.AddIdentifiers(nodes.Count()); } });
            }

            foreach (var e in saveData.Edges)
            {
                var startNode = nodeDict[e.StartNodeID];
                var endNode = nodeDict[e.EndNodeID];



                if (startNode == null || endNode == null) continue;
                


                Symbol symbol = e.EdgeSymbol;
                if (symbol == null) symbol = Symbol.SymbolAsterisk();

                var startNodePorts = startNode.GetOutputPorts();
                var endNodePorts = endNode.GetInputPorts();

                Port inputPort = null;
                Port outputPort = null;

                foreach (var op in startNodePorts)
                {


                    if (op.userData is not Symbol s) continue;



                    if (symbol.Type == GraphSymbolType.Edge)
                    {
                        if (symbol == s)
                        {
                            outputPort = op;
                            break;
                        }
                    }
                    else if (Symbol.AsteriskEquality(symbol.Type, s.Type))
                    {
                        outputPort = startNodePorts.First();
                        break;
                    }
                }

                if (outputPort == null)
                {
                    outputPort = startNode.AddPort(symbol.Name, Direction.Output, symbol);
                }


                foreach (var ip in endNodePorts)
                {
                    if (ip.userData is not Symbol s) continue;

                    if (symbol.Type == GraphSymbolType.Edge)
                    {
                        if (symbol == s)
                        {
                            inputPort = ip;
                            break;
                        }
                    }
                    else if (Symbol.AsteriskEquality(symbol.Type, s.Type))
                    {
                        inputPort = endNodePorts.First();
                        break;
                    }
                }

                if (inputPort == null)
                {
                    inputPort = endNode.AddPort(symbol.Name, Direction.Input, symbol);
                }

                this.AddElement(outputPort.ConnectTo(inputPort));
            }
            
        }
    
        public void CreateFromSO(GGGraphSO graphSO)
        {
            CreateFromSaveData(graphSO.ToSaveData());
        }

        public void HideAllToggles()
        {
            nodes.ForEach(nodes => { if (nodes is GGNodeEditor node) { node.HideToggle(); } });
        }
    }
}