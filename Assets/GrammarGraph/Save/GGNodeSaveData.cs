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

        //[field: SerializeField] public List<string> Inputs { get; set; }
        //[field: SerializeField] public List<string> Outputs { get; set; }

        [field: SerializeField] public string GroupID { get; set; }

        [field: SerializeField] public Vector2 Position { get; set; }
    }
}
