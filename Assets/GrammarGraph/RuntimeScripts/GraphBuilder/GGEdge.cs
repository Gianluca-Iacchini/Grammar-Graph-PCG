using GG.Data.Save;
using GG.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data class used to represent edges in the graph
/// </summary>
public class GGEdge
{
    public GGNode StartNode { get; private set; }
    public GGNode EndNode { get; private set; }
    public Symbol EdgeSymbol { get; set; }
    public GGEdge(GGNode startNode, GGNode endNode, Symbol symbol = null)
    {
        StartNode = startNode;
        EndNode = endNode;
        EdgeSymbol = symbol != null ? symbol : Symbol.SymbolAsterisk();
    }

    public GGEdge(GGNodeSaveData startNode, GGNodeSaveData endNode, Symbol symbol = null)
    {
        StartNode = new GGNode(startNode);
        EndNode = new GGNode(endNode);
        EdgeSymbol = symbol != null ? symbol : Symbol.SymbolAsterisk();
    }

    public override bool Equals(object other)
    {
        return Equals(other as GGEdge);
    }

    public bool Equals(GGEdge other)
    {
        return other != null &&
            this.StartNode == other.StartNode &&
            this.EndNode == other.EndNode &&
            this.EdgeSymbol == other.EdgeSymbol;
    }

    public static bool operator ==(GGEdge lhs, GGEdge rhs)
    {
        if (ReferenceEquals(lhs, rhs))
            return true;

        if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
            return false;

        return lhs.Equals(rhs);
    }

    public static bool operator !=(GGEdge l, GGEdge r)
    {
        return !(l == r);
    }

    public override int GetHashCode()
    {
        return StartNode.GetHashCode() ^ EndNode.GetHashCode() ^ EdgeSymbol.GetHashCode();
    }

    public GGEdgeSaveData ToSaveData()
    {
        GGEdgeSaveData saveData = new GGEdgeSaveData();
        saveData.StartNodeID = StartNode.GUID;
        saveData.EndNodeID = EndNode.GUID;
        saveData.EdgeSymbol = EdgeSymbol;

        return saveData;
    }
}
