using GG.Data.Save;
using GG.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents a graph
/// </summary>
public class GGGraph
{
    public List<GGNode> Nodes { get; private set; } = new();
    public List<GGEdge> Edges { get; private set; } = new();

    public Dictionary<GGNode, List<GGNode>> AdjacencyList { get; private set; } = new();

    public Dictionary<GGNode, List<GGEdge>> EdgesOutList { get; private set; } = new();
    public Dictionary<GGNode, List<GGEdge>> EdgesInList { get; private set; } = new();

    public GGGraph()
    {
    }

    public GGGraph(GGGraphSaveData graphSaveData)
    {

        Dictionary<string, GGNode> saveNodeDict = new Dictionary<string, GGNode>();

        foreach (var node in graphSaveData.Nodes) 
        {
            GGNode ggNode = new GGNode(node);
            this.AddNode(ggNode);

            saveNodeDict.Add(node.ID, ggNode);
        }

        foreach (var edge in graphSaveData.Edges)
        {
            GGNode startNode;
            GGNode endNode;

            if (!saveNodeDict.TryGetValue(edge.StartNodeID, out startNode) || !saveNodeDict.TryGetValue(edge.EndNodeID, out endNode))
                continue;


            GGEdge ggEdge = new GGEdge(startNode, endNode, edge.EdgeSymbol);

            this.AddEdge(ggEdge);
        }

    }

    #region Edges

    public void AddEdge(GGNode startNode, GGNode endNode, Symbol edgeSymbol)
    {
        if (!this.Nodes.Contains(startNode) || !this.Nodes.Contains(endNode))
            return;

        GGEdge edge = new GGEdge(startNode, endNode, edgeSymbol);

        this.AddEdge(edge);
    }

    public void AddEdge(GGEdge edge)
    {
        if (!this.AdjacencyList.ContainsKey(edge.StartNode))
            this.AdjacencyList[edge.StartNode] = new List<GGNode>();

        if (!this.EdgesOutList.ContainsKey(edge.StartNode))
            this.EdgesOutList[edge.StartNode] = new List<GGEdge>();

        if (!this.EdgesInList.ContainsKey(edge.EndNode))
            this.EdgesInList[edge.EndNode] = new List<GGEdge>();

        this.AdjacencyList[edge.StartNode].Add(edge.EndNode);
        this.EdgesOutList[edge.StartNode].Add(edge);
        this.EdgesInList[edge.EndNode].Add(edge);

        this.Edges.Add(edge);
    }

    public List<GGEdge> GetEdgesFromNode(GGNode node)
    {
        if (!EdgesOutList.ContainsKey(node))
        {
            return new List<GGEdge>();
        }

        return new List<GGEdge>(EdgesOutList[node]);

    }

    public List<GGEdge> GetEdgesToNode(GGNode node)
    {
        //List<GGEdge> edges = new List<GGEdge>(Edges.Where(e=>e.EndNode == node));

        //return edges;

        if (!EdgesInList.ContainsKey(node))
        {
            return new List<GGEdge>();
        }

        return new List<GGEdge>(EdgesInList[node]);
    }

    public List<GGEdge> GetEdgesWithNode(GGNode node)
    {
        List<GGEdge> edges = new List<GGEdge>();

        //foreach (GGEdge e in Edges)
        //{
        //    if (e.StartNode == node || e.EndNode == node)
        //    {
        //        edges.Add(e);
        //    }
        //}

        edges.AddRange(EdgesOutList[node]);
        edges.AddRange(EdgesInList[node]);


        return edges;
    }

    public void RemoveEdge(GGEdge e)
    {
        this.Edges.Remove(e);
        this.EdgesOutList[e.StartNode].Remove(e);
        this.EdgesInList[e.EndNode].Remove(e);

        this.AdjacencyList[e.StartNode].Remove(e.EndNode);

    }

    public void RemoveEdgesFrom(GGNode node)
    {
        foreach (GGEdge e in this.Edges)
        {
            if (e.StartNode == node)
            {
                this.RemoveEdge(e);
            }
        }
    }

    public void RemoveEdgesTo(GGNode node)
    {
        foreach (GGEdge e in this.Edges)
        {
            if (e.EndNode == node)
            {
                this.RemoveEdge(e);
            }
        }
    }

    public void RemoveEdgesWith(GGNode node)
    {
        var copyList = new List<GGEdge>(this.Edges);

        foreach (GGEdge e in copyList)
        {
            if (e.StartNode == node || e.EndNode == node)
            {
                this.RemoveEdge(e);
            }
        }
    }

    public bool IsEdge(GGNode startNode, GGNode endNode)
    {
        if (this.AdjacencyList.ContainsKey(startNode))
        {
            return this.AdjacencyList[startNode].Contains(endNode);
        }

        return false;
    }

    public bool HasSimilarEdge(GGEdge edge)
    {
        if (this.Edges.Contains(edge))
            return true;

        else
        {
            foreach (var e in this.Edges)
            {
                if (e.StartNode == edge.StartNode && e.EndNode == edge.EndNode && e.EdgeSymbol == edge.EdgeSymbol)
                    return true;
            }
            
            return false;
        }
    }

    #endregion Edges

    #region Nodes

    public void AddNode(GGNode node)
    {
        if (!EdgesOutList.ContainsKey(node))
            EdgesOutList[node] = new List<GGEdge>();
        if (!EdgesInList.ContainsKey(node))
            EdgesInList[node] = new List<GGEdge>();

        this.Nodes.Add(node);
    }

    public void RemoveNode(GGNode node)
    {
        this.RemoveEdgesWith(node);
        this.Nodes.Remove(node);
        this.AdjacencyList.Remove(node);
        this.EdgesOutList.Remove(node);
        this.EdgesInList.Remove(node);
    }

    public List<GGNode> GetNeighbours(GGNode node)
    {
        List<GGNode> nodes = new List<GGNode>();

        foreach (GGEdge e in this.EdgesOutList[node])
        {
            nodes.Add(e.EndNode);
        }
        foreach (GGEdge e in this.EdgesInList[node])
        {
            nodes.Add(e.StartNode);
        }

        return nodes;
    }

    public List<GGNode> GetNeighboursIn(GGNode node)
    {
        List<GGNode> neighboursIn = new List<GGNode>();

        var items = AdjacencyList.Where(kvp => kvp.Value.Contains(node));

        neighboursIn = items.Select(kvp => kvp.Key).ToList();
        return neighboursIn;
    }

    public List<GGNode> GetNeighboursOut(GGNode node)
    {
        if (this.AdjacencyList.ContainsKey(node))
        {
            return this.AdjacencyList[node];
        }

        return new List<GGNode>();
    }

    #endregion Nodes

    public GGGraphSaveData ToSaveData()
    {
        var nodeList = new List<GGNodeSaveData>();
        var edgeList = new List<GGEdgeSaveData>();

        foreach (var node in Nodes)
        {
            nodeList.Add(node.ToSaveData());
        }

        foreach (var edge in Edges)
        {
            edgeList.Add(edge.ToSaveData());
        }

        GGGraphSaveData saveData = new GGGraphSaveData()
        {
            Groups = new List<GGGroupSaveData>(),
            Nodes = nodeList,
            Edges = edgeList,
            ID = ""
        };

        return saveData;
    }
}
