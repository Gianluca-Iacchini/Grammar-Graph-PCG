using GG.ScriptableObjects;
using GG.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GGEdgeData
{
    [field:SerializeField] public Symbol EdgeSymbol {  get; set; }
    [field:SerializeField] public GGNodeListSO ConnectedNode { get; set; }
    [field:SerializeField] public Direction EdgeDirection { get; set; }
}
