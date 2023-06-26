using GG.Data.Save;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GGGroup
{
    public List<GGNode> Nodes { get; set; } = new();
    public List<GGEdge> Edges { get; set; } = new();
    public float Weight { get; set; } = 1f;
    public string GUID { get; set; } = "";

    public GGGroup()
    {
    }

    public GGGroup(float weight, string guid)
    {
        Weight = weight;
        GUID = guid;
    }

    public GGGroup(List<GGNode> nodes, List<GGEdge> edges, float weight, string guid)
    {
        Nodes = nodes;
        Edges = edges;
        Weight = weight;
        GUID = guid;
    }

    public static List<GGGroup> GetGroupsInGraph(GGGraph graph, List<GGGroupSaveData> groupSaveData)
    {
        Dictionary<string, GGGroup> groups = new Dictionary<string, GGGroup>();

        foreach (GGGroupSaveData group in groupSaveData)
        {
            groups[group.ID] = new GGGroup(group.Weight, group.ID);
        }

        foreach (GGNode n in graph.Nodes)
        {
            if (n.GroupID != null && n.GroupID != string.Empty)
            {
                if (groups.ContainsKey(n.GroupID))
                    groups[n.GroupID].AddNode(n);
            }
        }

        foreach (GGEdge e in graph.Edges)
        {
            if (e.StartNode.GroupID != string.Empty && e.EndNode.GroupID != string.Empty)
            {
                if (e.StartNode.GroupID == e.EndNode.GroupID)
                {
                    if (groups.ContainsKey(e.StartNode.GroupID))
                        groups[e.StartNode.GroupID].AddEdge(e);
                }
            }
        }

        return groups.Values.ToList();
    }

    public void AddNode(GGNode node)
    {
        if (!this.Nodes.Contains(node))
            this.Nodes.Add(node);
    }


    public void AddEdge(GGEdge edge)
    {
        if (this.Nodes.Contains(edge.StartNode) && this.Nodes.Contains(edge.EndNode))
            if (!this.Edges.Contains(edge))
                this.Edges.Add(edge);
    }

    public GGGraph ToGraph()
    {
        GGGraph groupGraph = new GGGraph();

        foreach (GGNode n in this.Nodes)
        {
            groupGraph.AddNode(n);
        }

        foreach (GGEdge e in this.Edges)
        {
            groupGraph.AddEdge(e);
        }

        return groupGraph;
    }
}
