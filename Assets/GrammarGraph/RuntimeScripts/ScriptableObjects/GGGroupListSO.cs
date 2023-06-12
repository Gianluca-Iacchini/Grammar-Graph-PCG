using GG.Data.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GGGroupListSO : ScriptableObject
{ 
    [field: SerializeField] public List<GGGroupSaveData> GroupSaveData { get; set; }

    public void Initialize(List<GGGroupSaveData> list)
    {
        GroupSaveData = list;
    }
}
