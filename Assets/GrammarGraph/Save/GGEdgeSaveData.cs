using GG.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GG.Data.Save
{
    [Serializable]
    public class GGEdgeSaveData
    {
        [field:SerializeField] public Symbol EdgeSymbol { get; set; }
        [field:SerializeField] public string InputNodeID { get; set; }
        [field:SerializeField] public string OutputNodeID { get; set; }
    }
}
