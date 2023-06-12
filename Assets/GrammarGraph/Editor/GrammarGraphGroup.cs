using GrammarGraph;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GrammarGraphGroup : Group
{
    public string ID { get; set; }

    protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
    {
        foreach (GraphElement element in elements) 
        { 
            if (element is GrammarGraphNode node)
            {
                node.Group = this;
            }


        }
        base.OnElementsAdded(elements);
    }

    protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
    {
        foreach (GraphElement element in elements)
        {
            if (element is GrammarGraphNode node)
            {
                node.Group = null;
            }


        }

        base.OnElementsRemoved(elements);
    }
}
