using GG.ScriptableObjects;
using GG.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GG.Data.Save
{
    [Serializable]
    public struct RuleSaveData
    {
        [field:SerializeField] public string Name { get; set; }
        [field:SerializeField] public GGGraphSO LeftGraph { get; set; }
        [field:SerializeField] public GGGraphSO RightGraph { get; set; }

    }

    public class GGSaveDataSO : ScriptableObject
    {
        [field:SerializeField] public string Filename { get; set; }
        [field:SerializeField] public List<RuleSaveData> RuleList { get; set; }
        [field: SerializeField] public List<Symbol> SymbolList { get; set; }

        public void Initialize(string filename)
        {
            Filename = filename;
            SymbolList = new List<Symbol>();
            RuleList = new List<RuleSaveData>();
        }
    }
}
