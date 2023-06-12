using GG.Data.Save;
using GG.Editor.Utils;
using GG.ScriptableObjects;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;
using GrammarGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace GG.Utils
{
    enum LeftRightGraph
    {
        Left,
        Right,
    }

    public struct RuleData
    {
        public struct GraphViewData
        {
            public List<GrammarGraphNode> Nodes;
            public List<GrammarGraphGroup> Groups;
        }

        public GraphViewData LeftGraphViewData;
        public GraphViewData RightGraphViewData;

    }

    public class GGSaveGraph
    {
        private string m_GraphFilename;
        private readonly string m_FolderPath = "Assets/GrammarGraph/GrammarGraphs";

        private List<GraphRule> GraphRules;
        private List<Symbol> Symbols;

        public GGSaveGraph(string filename, List<GraphRule> GraphRules, List<Symbol> symbols)
        {
            m_GraphFilename = filename;
            this.GraphRules = GraphRules;
            Symbols = symbols;
        }

        public void Save()
        {
            CreateFolder("Assets/GrammarGraph", "GrammarGraphs");

            CreateFolder(m_FolderPath, m_GraphFilename);

            // Create parent folder for each rule in the graph.

            string graphPath = $"{m_FolderPath}/{m_GraphFilename}";

            var saveDataSO = CreateAsset<GGSaveDataSO>(graphPath, m_GraphFilename);
            saveDataSO.Initialize(m_GraphFilename);

            List<RuleSaveData> ruleData = new List<RuleSaveData>();

            foreach (var r in GraphRules)
            {
                string rulePath = $"{graphPath}/{r.ruleName}";

                CreateFolder(graphPath, r.ruleName);

                CreateFolder(rulePath, "Left");
                CreateFolder(rulePath, "Right");

                var lGraphSO = CreateGraphSO($"{rulePath}/Left", $"{m_GraphFilename}LGraph", r.LGraph.GetGraphSaveData());
                var rGraphSO = CreateGraphSO($"{rulePath}/Right", $"{m_GraphFilename}RGraph", r.RGraph.GetGraphSaveData());

                RuleSaveData ruleSaveData;
                ruleSaveData.Name = r.ruleName;
                ruleSaveData.LeftGraph = lGraphSO;
                ruleSaveData.RightGraph = rGraphSO;

                ruleData.Add(ruleSaveData);
            }

            saveDataSO.RuleList = ruleData;
            saveDataSO.SymbolList = Symbols;

            AssetDatabase.SaveAssets();
        }

        public GGSaveDataSO Load(string filename)
        {
            var saveData = AssetDatabase.LoadAssetAtPath<GGSaveDataSO>($"{m_FolderPath}/{filename}/{filename}.asset");

            return saveData;
        }

        private void CreateFolder(string path, string folderName)
        {
            if (AssetDatabase.IsValidFolder($"{path}/{folderName}"))
            {
                return;
            }

            AssetDatabase.CreateFolder(path, folderName);
        }

        private T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";

            T asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, fullPath);
            }

            return asset;
        }

        private GGGraphSO CreateGraphSO(string path, string graphName, GGGraphSaveData graphData)
        {
            GGGraphSO graphSO = CreateAsset<GGGraphSO>(path, graphName);
            graphSO.Initialize(graphData.ID, m_GraphFilename);
            graphSO.Nodes = CreateAsset<GGNodeListSO>(path, $"{graphName}Nodes");
            graphSO.Groups = CreateAsset<GGGroupListSO>(path, $"{graphName}Groups");
            graphSO.Edges = CreateAsset<GGEdgeListSO>(path, $"{graphName}Edges");

            graphSO.Nodes.NodeSaveData = graphData.Nodes;
            graphSO.Edges.EdgeSaveData = graphData.Edges;
            graphSO.Groups.GroupSaveData = graphData.Groups;

            return graphSO;                                                    
        }

        //private static List<GraphRule> GraphRules;

        //private static string graphFilename;
        //private static string containerFolderPath;

        //public static Dictionary<string, RuleData> RulesData;

        //public static void Initialize(List<GraphRule> Rules, string graphName)
        //{
        //    RulesData = new Dictionary<string, RuleData>();
        //    GraphRules = Rules;
        //    graphFilename = graphName;
        //    containerFolderPath = $"Assets/GrammarGraph/GrammarGraphs/{graphFilename}";
        //}

        //public static void Save()
        //{
        //    CreateStaticFolders();

        //    GetElementsFromRules();

        //    GGSaveDataSO saveData = CreateAsset<GGSaveDataSO>("GrammarGraph/Editor/Graphs", $"{graphFilename}Graph");
        //    saveData.Initialize(graphFilename);

        //    foreach (GraphRule rule in GraphRules)
        //    {
        //        SaveGraphs(rule);
        //    }

        //    SaveAsset(saveData);
        //}


        //private static void SaveGraphs(GraphRule graphRule)
        //{
        //    GGGraphSO lGraphData = CreateAsset<GGGraphSO>($"{containerFolderPath}/{graphRule.ruleName}/Left", $"{graphFilename}Left");
        //    lGraphData.Initialize(graphRule.LGraph.ID, graphFilename);
        //    GGGraphSO rGraphData = CreateAsset<GGGraphSO>($"{containerFolderPath}/{graphRule.ruleName}/Right", $"{graphFilename}Right");
        //    rGraphData.Initialize(graphRule.RGraph.ID, graphFilename);

        //    GGNodeContainerSO lContainer = CreateAsset<GGNodeContainerSO>(containerFolderPath, graphFilename);
        //    lContainer.Initialize(graphFilename);
        //    GGNodeContainerSO rContainer = CreateAsset<GGNodeContainerSO>(containerFolderPath, graphFilename);
        //    rContainer.Initialize(graphFilename);

        //    SaveGroups(graphRule.ruleName, lGraphData, lContainer, rGraphData, rContainer);

        //    SaveAsset(lGraphData);
        //    SaveAsset(rGraphData);

        //    SaveAsset(lContainer);
        //    SaveAsset(rContainer);
        //}

        //private static void SaveNodesToGraphs(GrammarGraphNode node, GGGraphSO graphData)
        //{
        //    GGNodeSaveData nodeData = new GGNodeSaveData()
        //    {
        //        //ID = node.ID,
        //        //Symbol = node.NodeSymbol,
        //        //Inputs = new List<string>(node.InputPorts),
        //        //Outputs = new List<string>(node.OutputPorts),
        //        //Position = node.GetPosition().position,
        //        //GroupID = node.Group?.ID,
        //    };

        //    graphData.Nodes.Add(nodeData);
        //}

        //private static void SaveNodesToScriptableObject(GrammarGraphNode node, GrammarGraphGroup group, GGNodeContainerSO container, string subPath, int index, Dictionary<string, GGGroupSO> groupSODict)
        //{
        //    GGNodeSo nodeSO;

        //    if (node.Group != null)
        //    {
        //        nodeSO = CreateAsset<GGNodeSo>($"{containerFolderPath}/{subPath}/Groups/{node.Group.title}/Nodes", $"Node_{index}");
        //        container.NodeGroups.AddItem(groupSODict[node.Group.ID], nodeSO);
        //    }
        //    else
        //    {
        //        nodeSO = CreateAsset<GGNodeSo>($"{containerFolderPath}/{subPath}/Global/Nodes", $"Node_{index}");
        //        container.UngroupedNodes.Add(nodeSO);
        //    }

        //    //nodeSO.Initialize(node.NodeSymbol, )
        //}

        //private static void SaveGroups(string ruleName, GGGraphSO lGraphData, GGNodeContainerSO lContainer, GGGraphSO rGraphData, GGNodeContainerSO rContainer)
        //{
        //    if (!RulesData.ContainsKey(ruleName))
        //        return;

        //    RuleData ruleData = RulesData[ruleName];

        //    Dictionary<string, GGGroupSO> groupSODict = new Dictionary<string, GGGroupSO>();

        //    foreach (GrammarGraphGroup group in ruleData.LeftGraphViewData.Groups)
        //    {
        //        SaveToGroupGraph(group, lGraphData);
        //        SaveGroupsToScriptableObject(group, lContainer, $"{ruleName}/Left", groupSODict);
        //    }

        //    foreach (var node in ruleData.LeftGraphViewData.Nodes)
        //    {
        //        SaveNodesToGraphs(node, lGraphData);
        //        //SaveGroupsToScriptableObject(group, lContainer, $"{ruleName}/Left");
        //    }

        //    groupSODict.Clear();

        //    foreach (GrammarGraphGroup group in ruleData.RightGraphViewData.Groups)
        //    {
        //        SaveToGroupGraph(group, rGraphData);
        //        SaveGroupsToScriptableObject(group, rContainer, $"{ruleName}/Right", groupSODict);
        //    }

        //    foreach (var node in ruleData.RightGraphViewData.Nodes)
        //    {
        //        SaveNodesToGraphs(node, rGraphData);
        //        //SaveGroupsToScriptableObject(group, rContainer, $"{ruleName}/Right");
        //    }

        //    RulesData[ruleName] = ruleData;
        //}

        //private static void SaveToGroupGraph(GrammarGraphGroup group, GGGraphSO graphData)
        //{
        //    GGGroupSaveData groupData = new GGGroupSaveData()
        //    {
        //        ID = group.ID,
        //        GraphID = graphData.ID,
        //        Name = group.title,
        //        Position = group.GetPosition().position
        //    };

        //    graphData.Groups.Add(groupData);
        //}

        //private static void SaveGroupsToScriptableObject(GrammarGraphGroup group, GGNodeContainerSO container, string subPath, Dictionary<string, GGGroupSO> groupSODict)
        //{
        //    string groupName = group.title;

        //    CreateFolder($"{containerFolderPath}/{subPath}/Groups", groupName);
        //    CreateFolder($"{containerFolderPath}/{subPath}/Groups/{groupName}", "Nodes");

        //    GGGroupSO nodeGroup = CreateAsset<GGGroupSO>($"{containerFolderPath}/{subPath}/Groups/{groupName}", groupName);
        //    nodeGroup.Initialize(groupName);

        //    container.NodeGroups.Add(nodeGroup, new List<GGNodeSo>());
        //    groupSODict.Add(group.ID, nodeGroup);


        //    SaveAsset(nodeGroup);
        //}

        //private static void SaveAsset(UnityEngine.Object asset)
        //{
        //    EditorUtility.SetDirty(asset);

        //    AssetDatabase.SaveAssets();
        //    AssetDatabase.Refresh();
        //}

        //private static void CreateStaticFolders()
        //{

        //    CreateFolder("Assets/GrammarGraph/Editor", "Graphs");

        //    CreateFolder("Assets/GrammarGraph", "GrammarGraphs");

        //    CreateFolder("Assets/GrammarGraph/GrammarGraphs", graphFilename);

        //    // Create parent folder for each rule in the graph.

        //    foreach (var r in GraphRules)
        //    {
        //        CreateFolder(containerFolderPath, r.ruleName);
        //        string rulePath = $"{containerFolderPath}/{r.ruleName}";

        //        CreateFolder(rulePath, "Left");
        //        CreateFolder($"{rulePath}/Left", "Global");
        //        CreateFolder($"{rulePath}/Left", "Groups");
        //        CreateFolder($"{rulePath}/Left/Global", "Nodes");

        //        CreateFolder(rulePath, "Right");
        //        CreateFolder($"{rulePath}/Right", "Global");
        //        CreateFolder($"{rulePath}/Right", "Groups");
        //        CreateFolder($"{rulePath}/Right/Global", "Nodes");
        //    }
        //}


        //private static void CreateFolder(string path, string folderName)
        //{
        //    if (AssetDatabase.IsValidFolder($"{path}/{folderName}"))
        //    {
        //        return;
        //    }

        //    AssetDatabase.CreateFolder(path, folderName);
        //}

        //private static void GetElementsFromGraphView(GrammarGraphView graphView, List<GrammarGraphNode> nodeList, List<GrammarGraphGroup> groupList)
        //{
        //    graphView.graphElements.ForEach(e =>
        //    {
        //        if (e is GrammarGraphNode node)
        //        {
        //            nodeList.Add(node);
        //            return;
        //        }

        //        if (e is GrammarGraphGroup group)
        //        {
        //            groupList.Add(group);

        //            return;
        //        }
        //    });
        //}

        //private static void GetElementsFromRules()
        //{
        //    foreach (var r in GraphRules)
        //    {
        //        RuleData ruleData;
        //        ruleData.LeftGraphViewData = new RuleData.GraphViewData { Nodes = new(), Groups = new() };
        //        ruleData.RightGraphViewData = new RuleData.GraphViewData { Nodes = new(), Groups = new() };

        //        GetElementsFromGraphView(r.LGraph, ruleData.LeftGraphViewData.Nodes, ruleData.LeftGraphViewData.Groups);
        //        GetElementsFromGraphView(r.RGraph, ruleData.RightGraphViewData.Nodes, ruleData.RightGraphViewData.Groups);

        //        RulesData.Add(r.ruleName, ruleData);
        //    }
        //}

        //private static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
        //{
        //    string fullPath = $"{path}/{assetName}.asset";

        //    T asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);

        //    if (asset == null)
        //    {
        //        asset = ScriptableObject.CreateInstance<T>();
        //        AssetDatabase.CreateAsset(asset, fullPath);
        //    }

        //    return asset;
        //}
    }
}
