using GG.Data.Save;
using GG.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GG.ScriptableObjects
{
    public class GGNodeListSO : ScriptableObject
    {
        [field: SerializeField] public List<GGNodeSaveData> NodeSaveData;

        public void Initialize(List<GGNodeSaveData> nodes)
        {
            NodeSaveData = nodes;
        }
    }
}
