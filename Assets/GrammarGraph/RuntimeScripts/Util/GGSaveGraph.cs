using GG.Data.Save;
using GG.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace GG.Utils
{
    public struct RuleGraphSaveData
    {
        public string Name { get; set; }
        public GGGraphSaveData LeftGraph { get; set; }
        public GGGraphSaveData RightGraph { get; set; }
    }

    public class GGSaveGraph
    {
        private string m_GraphFilename;
        private static readonly string m_FolderPath = "Assets/GrammarGraph/GrammarGraphs";

        private List<RuleGraphSaveData> GraphRules;
        private List<Symbol> Symbols;

        public GGSaveGraph(List<RuleGraphSaveData> GraphRules, List<Symbol> symbols)
        {
            this.GraphRules = GraphRules;
            Symbols = symbols;
        }

        public void Save(string filename)
        {
            m_GraphFilename = filename;

            CreateFolder("Assets/GrammarGraph", "GrammarGraphs");

            CreateFolder(m_FolderPath, m_GraphFilename);

            // Create parent folder for each rule in the graph.

            string graphPath = $"{m_FolderPath}/{m_GraphFilename}";

            var saveDataSO = CreateAsset<GGSaveDataSO>(graphPath, m_GraphFilename);
            saveDataSO.Initialize(m_GraphFilename);

            List<RuleSaveData> ruleData = new List<RuleSaveData>();

            foreach (var r in GraphRules)
            {
                string rulePath = $"{graphPath}/{r.Name}";

                CreateFolder(graphPath, r.Name);

                CreateFolder(rulePath, "Left");
                CreateFolder(rulePath, "Right");

                var lGraphSO = CreateGraphSO($"{rulePath}/Left", $"{m_GraphFilename}LGraph", r.LeftGraph);
                var rGraphSO = CreateGraphSO($"{rulePath}/Right", $"{m_GraphFilename}RGraph", r.RightGraph);

                RuleSaveData ruleSaveData = new RuleSaveData();
                ruleSaveData.Name = r.Name;
                ruleSaveData.LeftGraph = lGraphSO;
                ruleSaveData.RightGraph = rGraphSO;

                ruleData.Add(ruleSaveData);
            }

            saveDataSO.RuleList = ruleData;
            saveDataSO.SymbolList = Symbols;

            AssetDatabase.SaveAssets();
        }

        public static GGSaveDataSO Load(string filename)
        {
            if (AssetDatabase.FindAssets(filename, new[] { $"{m_FolderPath}/{filename}" }).Length == 0)
            {
                return null;
            }

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

            EditorUtility.SetDirty(asset);

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
    }
}
