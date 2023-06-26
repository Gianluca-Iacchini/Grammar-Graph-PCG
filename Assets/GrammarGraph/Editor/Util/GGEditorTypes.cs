using GG.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GG.Editor.Utils
{
    /// <summary>
    /// Class using Editor types which cannot be referenced in runtime
    /// </summary>
    public struct GraphRule
    {
        public string ruleName;
        public GGGraphView LGraph;
        public GGGraphView RGraph;

        public GraphRule(string ruleName, GGGraphView lGraph, GGGraphView rGraph)
        {
            this.ruleName = ruleName;
            this.LGraph = lGraph;
            this.RGraph = rGraph;
        }
    };

    public struct RuleData
    {
        public struct GraphViewData
        {
            public List<GGNodeEditor> Nodes;
            public List<GGGroupEditor> Groups;
        }

        public GraphViewData LeftGraphViewData;
        public GraphViewData RightGraphViewData;

    }
}
