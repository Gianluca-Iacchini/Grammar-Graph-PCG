using GG.Data.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GG.ScriptableObjects
{
    public class GGEdgeListSO : ScriptableObject
    {
        [field: SerializeField] public List<GGEdgeSaveData> EdgeSaveData;

        public void Initialize(List<GGEdgeSaveData> edges)
        {
            EdgeSaveData = edges;
        }
    }
}
