using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GG.ScriptableObjects
{
    public class GGNodeContainerSO : ScriptableObject
    {
        [field:SerializeField] public string Filename { get; set; }
        [field:SerializeField] public SerializableDictionary<GGGroupListSO, List<GGNodeListSO>> NodeGroups { get; set; }
        [field:SerializeField] public List<GGNodeListSO> UngroupedNodes { get; set; }
        
        public void Initialize(string filename)
        {
            Filename = filename;
            NodeGroups = new SerializableDictionary<GGGroupListSO, List<GGNodeListSO>>();
            UngroupedNodes = new List<GGNodeListSO>(); 
        }
    }
}
