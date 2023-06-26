using GG.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GG.Data.Save
{
    [Serializable]
    public class GGNodeSaveData
    {
        [field: SerializeField] public string ID { get; set; }
        [field: SerializeField] public Symbol Symbol { get; set; }
        [field: SerializeField] public int Identifier { get; set; }
        [field: SerializeField] public string GroupID { get; set; }
        [field: SerializeField] public Vector2 Position { get; set; }
        [field: SerializeField] public bool IsExactInput { get; set; }
        [field: SerializeField] public bool IsExactOutput { get; set; }

    }
}
