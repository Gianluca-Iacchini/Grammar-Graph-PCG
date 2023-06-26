using GG.Data.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GG.ScriptableObjects
{
    public class GGGraphSO : ScriptableObject
    {
        [field: SerializeField] public string ID { get; set; }
        [field: SerializeField] public string Filename { get; set; }
        //[field: SerializeField] public List<GGGroupSaveData> Groups { get; set; }
        [field: SerializeField] public GGGroupListSO Groups { get; set; }
        //[field: SerializeField] public List<GGNodeSaveData> Nodes { get; set; }        
        [field: SerializeField] public GGNodeListSO Nodes { get; set; }
        [field: SerializeField] public GGEdgeListSO Edges { get; set; }
        public void Initialize(string ID, string filename)
        {
            this.Filename = filename;
            this.ID = ID;
        }

        public GGGraphSaveData ToSaveData()
        {
            GGGraphSaveData saveData = new GGGraphSaveData();
            saveData.ID = ID;
            saveData.Groups = Groups.GroupSaveData;
            saveData.Nodes = Nodes.NodeSaveData;
            saveData.Edges = Edges.EdgeSaveData;

            return saveData;
        }
    }
}
