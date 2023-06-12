using GrammarGraph;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GG.Editor.Utils
{
    public struct GraphRule
    {
        public string ruleName;
        public GrammarGraphView LGraph;
        public GrammarGraphView RGraph;

        public GraphRule(string ruleName, GrammarGraphView lGraph, GrammarGraphView rGraph)
        {
            this.ruleName = ruleName;
            this.LGraph = lGraph;
            this.RGraph = rGraph;
        }
    };
}
