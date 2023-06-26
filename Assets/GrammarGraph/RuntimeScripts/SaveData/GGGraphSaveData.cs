using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GG.Data.Save
{
    [Serializable]
    public class GGGraphSaveData
    {
        [field: SerializeField] public string ID;
        [field: SerializeField] public List<GGGroupSaveData> Groups { get; set; }
        [field: SerializeField] public List<GGNodeSaveData> Nodes { get; set; }
        [field: SerializeField] public List<GGEdgeSaveData> Edges { get; set; }
        [field: SerializeField] public List<string> OldGroupNames { get; set; }
        [field: SerializeField] public List<string> OldNodesNames { get; set; }
        [field: SerializeField] public SerializableDictionary<string, List<string>> OldGroupedNodeNames { get; set; }
    }


}
