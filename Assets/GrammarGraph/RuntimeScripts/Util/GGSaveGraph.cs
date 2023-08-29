using GG.Data.Save;
using GG.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace GG.Utils
{
    [Serializable]
    public struct RuleGraphSaveData
    {
        public string Name { get; set; }
        public GGGraphSaveData LeftGraph { get; set; }
        public GGGraphSaveData RightGraph { get; set; }
    }

    public class GGSaveGraph
    {

        public static readonly string FolderPath = "Assets/GrammarGraph/GrammarGraphs";

        public static void Save(string filepath, List<RuleGraphSaveData> GraphRules, List<Symbol> symbols, List<string> RemovedRules = null, Dictionary<string, string> RenamedRules = null)
        {
            string filename = Path.GetFileNameWithoutExtension(filepath);
            string directoryPath = Path.GetDirectoryName(filepath);

            CreateFolder("Assets/GrammarGraph", "GrammarGraphs");

            CreateFolder(FolderPath, filename);

            // Create parent folder for each rule in the graph.

            string graphPath = $"{FolderPath}/{filename}";

            var saveDataSO = CreateAsset<GGSaveDataSO>(directoryPath, filename);
            saveDataSO.Initialize(filename);

            List<RuleSaveData> ruleData = new List<RuleSaveData>();

            if (RemovedRules != null)
            {
                foreach (var r in RemovedRules)
                {
                    DeleteFolder(graphPath, r);
                }
            }


            foreach (var r in GraphRules)
            {

                string rulePath = $"{graphPath}/{r.Name}";
                string oldName;

                if (RenamedRules != null && RenamedRules.TryGetValue(r.Name, out oldName) && GraphExists($"{graphPath}/{oldName}"))
                {
                    Debug.Log(oldName);
                    RenameFolder(graphPath, oldName, r.Name);
                }

                else
                {
                    CreateFolder(graphPath, r.Name);

                    CreateFolder(rulePath, "Left");
                    CreateFolder(rulePath, "Right");
                }

                var lGraphSO = CreateGraphSO(filename, $"{rulePath}/Left", $"{filename}LGraph", r.LeftGraph);
                var rGraphSO = CreateGraphSO(filename, $"{rulePath}/Right", $"{filename}RGraph", r.RightGraph);

                RuleSaveData ruleSaveData = new RuleSaveData();
                ruleSaveData.Name = r.Name;
                ruleSaveData.LeftGraph = lGraphSO;
                ruleSaveData.RightGraph = rGraphSO;

                ruleData.Add(ruleSaveData);
            }

            saveDataSO.RuleList = ruleData;
            saveDataSO.SymbolList = symbols;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static GGSaveDataSO GetSaveDataSO(List<RuleGraphSaveData> GraphRules, List<Symbol> symbols)
        {

            var saveDataSO = ScriptableObject.CreateInstance<GGSaveDataSO>();
            saveDataSO.Initialize("temp");

            List<RuleSaveData> ruleData = new List<RuleSaveData>();



            foreach (var r in GraphRules)
            {

                var lGraphSO = CreateGraphSOAsInstance(r.LeftGraph);
                var rGraphSO = CreateGraphSOAsInstance(r.RightGraph);

                RuleSaveData ruleSaveData = new RuleSaveData();
                ruleSaveData.Name = r.Name;
                ruleSaveData.LeftGraph = lGraphSO;
                ruleSaveData.RightGraph = rGraphSO;

                ruleData.Add(ruleSaveData);
            }

            saveDataSO.RuleList = ruleData;
            saveDataSO.SymbolList = symbols;

            return saveDataSO;
        }

        public static bool GraphExists(string path)
        {
            path = path.Substring(path.IndexOf("Assets"));
            string filename = Path.GetFileNameWithoutExtension(path);
            string directoryPath = Path.GetDirectoryName(path);

            if (AssetDatabase.FindAssets(filename, new[] { $"{directoryPath}" }).Length != 0)
            {
                return true;
            }

            return false;
        }

        public static GGSaveDataSO Load(string filename)
        {
            if (AssetDatabase.FindAssets(filename, new[] { $"{FolderPath}/{filename}" }).Length == 0)
            {
                return null;
            }
            
            var saveData = AssetDatabase.LoadAssetAtPath<GGSaveDataSO>($"{FolderPath}/{filename}/{filename}.asset");

            return saveData;
        }

        public static GGSaveDataSO LoadAtPath(string path)
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
            string filename = Path.GetFileNameWithoutExtension(path);
            string directoryPath = Path.GetDirectoryName(path);


            if (AssetDatabase.FindAssets(filename, new[] { $"{directoryPath}" }).Length == 0)
            {
                return null;
            }

            var saveData = AssetDatabase.LoadAssetAtPath<GGSaveDataSO>(path);

            return saveData;
        }

        public static void DeleteFolder(string path, string folderName)
        {
            DeleteFolder($"{path}/{folderName}");
        }

        public static void DeleteFolder(string path)
        {
            
            path = path.Substring(path.IndexOf("Assets"));
            if (!AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }

        public static void RenameFolder(string path, string oldName, string newName)
        {
            if (!AssetDatabase.IsValidFolder($"{path}/{oldName}"))
            {
                return;
            }
            
            AssetDatabase.MoveAsset($"{path}/{oldName}", $"{path}/{newName}");
            AssetDatabase.Refresh();
        }

        private static void CreateFolder(string path, string folderName)
        {
            if (AssetDatabase.IsValidFolder($"{path}/{folderName}"))
            {
                return;
            }

            AssetDatabase.CreateFolder(path, folderName);
        }

        private static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            path = path.Substring(path.IndexOf("Assets"));
            string relativeFullPath = $"{path}/{assetName}.asset";

            T asset = AssetDatabase.LoadAssetAtPath<T>(relativeFullPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, relativeFullPath);
            }

            EditorUtility.SetDirty(asset);

            return asset;
        }

        private static GGGraphSO CreateGraphSO(string filename, string path, string graphName, GGGraphSaveData graphData)
        {
            GGGraphSO graphSO = CreateAsset<GGGraphSO>(path, graphName);
            graphSO.Initialize(graphData.ID, filename);
            graphSO.Nodes = CreateAsset<GGNodeListSO>(path, $"{graphName}Nodes");
            graphSO.Groups = CreateAsset<GGGroupListSO>(path, $"{graphName}Groups");
            graphSO.Edges = CreateAsset<GGEdgeListSO>(path, $"{graphName}Edges");

            graphSO.Nodes.NodeSaveData = graphData.Nodes;
            graphSO.Edges.EdgeSaveData = graphData.Edges;
            graphSO.Groups.GroupSaveData = graphData.Groups;

            return graphSO;                                                    
        }

        private static GGGraphSO CreateGraphSOAsInstance(GGGraphSaveData graphData)
        {
            GGGraphSO graphSO = ScriptableObject.CreateInstance<GGGraphSO>();
            graphSO.Initialize(graphData.ID, "temp");
            graphSO.Nodes = ScriptableObject.CreateInstance<GGNodeListSO>();
            graphSO.Groups = ScriptableObject.CreateInstance<GGGroupListSO>();
            graphSO.Edges = ScriptableObject.CreateInstance<GGEdgeListSO>();

            graphSO.Nodes.NodeSaveData = graphData.Nodes;
            graphSO.Edges.EdgeSaveData = graphData.Edges;
            graphSO.Groups.GroupSaveData = graphData.Groups;

            return graphSO;
        }
    }
}
