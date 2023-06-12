using GG.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GGRuleSO : ScriptableObject
{
    [field: SerializeField] public string Name { get; set; }

    [field: SerializeField] public GGGraphSO LeftGraph { get; set; }
    [field: SerializeField] public GGGraphSO RightGraph { get; set; }
}
