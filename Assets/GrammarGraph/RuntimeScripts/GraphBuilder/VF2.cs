using GG.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VF2
{
    private static bool m_FoundFirst = false;

    public static List<Dictionary<GGNode, GGNode>> FindSubgraphIsomorphisms(GGGraph patternGraph, GGGraph targetGraph)
    {
        m_FoundFirst = false;
        var result = new List<Dictionary<GGNode, GGNode>>();
        var state = new VF2State(patternGraph, targetGraph);
        Backtrack(state, result);
        return result;
    }

    private static void Backtrack(VF2State state, List<Dictionary<GGNode, GGNode>> result)
    {
        if (state.IsComplete())
        {
            // Found a valid isomorphism, add it to the result
            result.Add(new Dictionary<GGNode, GGNode>(state.MappedNodes));
            m_FoundFirst = true;
            return;
        }

        GGNode currentNodePattern = state.NextNodePattern();

        if (currentNodePattern == null) return;

        // Iterate over all unmatched nodes in the target graph
        foreach (GGNode currentNodeTarget in state.GetUnmatchedNodesTarget())
        {
            if (m_FoundFirst) return;

            if (state.AreCompatible(currentNodePattern, currentNodeTarget))
            {
                // Check if the neighbors of the current pattern and target nodes are compatible
                if (state.AreCompatibleNeighbors(currentNodePattern, currentNodeTarget))
                {
                    // Add the mapping between the current pattern and target nodes
                    state.AddMapping(currentNodePattern, currentNodeTarget);
                    Backtrack(state, result);
                    state.RemoveMapping(currentNodePattern, currentNodeTarget);
                }
            }
        }
    }
}

public class VF2State
{
    public GGGraph PatternGraph { get; private set; }
    public GGGraph TargetGraph { get; private set; }
    public Dictionary<GGNode, GGNode> MappedNodes { get; private set; }
    public HashSet<GGNode> UnmatchedNodesPattern { get; private set; }
    public HashSet<GGNode> UnmatchedNodesTarget { get; private set; }
    public Dictionary<(GGNode, GGNode), bool> PatternCompatibility { get; private set; }
    public Dictionary<(GGNode, GGNode), bool> TargetCompatibility { get; private set; }

    public VF2State(GGGraph patternGraph, GGGraph targetGraph)
    {
        PatternGraph = patternGraph;
        TargetGraph = targetGraph;
        MappedNodes = new Dictionary<GGNode, GGNode>();
        UnmatchedNodesPattern = new HashSet<GGNode>();
        UnmatchedNodesTarget = new HashSet<GGNode>();

        // Initialize the sets of unmatched nodes in the pattern and target graphs
        foreach (var node in PatternGraph.Nodes)
        {
            UnmatchedNodesPattern.Add(node);
        }

        foreach (var node in TargetGraph.Nodes)
        {
            UnmatchedNodesTarget.Add(node);
        }

        // Precompute node compatibility based on labels and adjacency
        PatternCompatibility = new Dictionary<(GGNode, GGNode), bool>();
        TargetCompatibility = new Dictionary<(GGNode, GGNode), bool>();

        foreach (GGNode pNode in PatternGraph.Nodes)
        {
            foreach (GGNode tNode in TargetGraph.Nodes)
            {
                // Check if the nodes are compatible (e.g., same labels)
                bool compatible = AreCompatible(pNode, tNode);
                PatternCompatibility[(pNode, tNode)] = compatible;
                TargetCompatibility[(tNode, pNode)] = compatible;
            }
        }
    }

    public bool IsComplete()
    {
        // Check if all nodes in the pattern graph have been matched
        return UnmatchedNodesPattern.Count == 0;
    }

    public GGNode NextNodePattern()
    {
        var copyList = new List<GGNode>(UnmatchedNodesPattern);
        // Choose the next unmatched node in the pattern graph
        foreach (GGNode node in copyList)
        {
            return node;
        }
        return null; // No unmatched nodes remaining
    }

    public IEnumerable<GGNode> GetUnmatchedNodesTarget()
    {
        var copyList = new List<GGNode>(UnmatchedNodesTarget);

        // Return the unmatched nodes in the target graph
        foreach (GGNode node in copyList)
        {
            yield return node;
        }
    }

    /// <summary>
    /// Check if the nodes are compatible (same symbol)
    /// </summary>
    /// <param name="nodePattern">Node in the pattern graph</param>
    /// <param name="nodeTarget">Node in the target graph</param>
    /// <returns>true if compatible false otherwise</returns>
    public bool AreCompatible(GGNode nodePattern, GGNode nodeTarget)
    {
        if (nodePattern.NodeSymbol.Type != GraphSymbolType.Asterisk)
            if (nodePattern.NodeSymbol.Name != nodeTarget.NodeSymbol.Name) return false;

        if (!CheckEdgeCount(nodePattern, nodeTarget)) return false;

        var patternEdges = PatternGraph.GetEdgesWithNode(nodePattern);
        var targetEdges = TargetGraph.GetEdgesWithNode(nodeTarget);
        var targetEdgesBag = new List<GGEdge>(targetEdges);

        foreach (var pe in patternEdges)
        {
            if (targetEdgesBag.Count == 0) return false;

            bool foundSimilarEdge = false;

            foreach (var te in targetEdges)
            {
                if (CheckEdge(nodePattern, nodeTarget, pe, te))
                {
                    targetEdgesBag.Remove(te);
                    foundSimilarEdge = true;
                    break;
                }
            }

            if (!foundSimilarEdge) return false;
        }

        var patternEdgesBag = new List<GGEdge>(patternEdges);
        foreach (var te in targetEdges)
        {
            var otherNode = te.StartNode == nodeTarget ? te.EndNode : te.StartNode;
            if (MappedNodes.Values.Contains(otherNode))
            {
                bool foundSimilarEdge = false;
                if (patternEdgesBag.Count == 0) return false;

                foreach (var pe in patternEdges)
                {
                    if (CheckEdge(nodeTarget, nodePattern, te, pe))
                    {
                        patternEdgesBag.Remove(pe);
                        foundSimilarEdge = true;
                        break;
                    }
                }
                
                if (!foundSimilarEdge) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if the neighbors of the nodes are compatible
    /// </summary>
    /// <param name="nodePattern">Node in the pattern graph</param>
    /// <param name="nodeTarget">Node in the target graph</param>
    /// <returns>true if compatible false otherwise</returns>
    public bool AreCompatibleNeighbors(GGNode nodePattern, GGNode nodeTarget)
    {
        return CheckNeighbor(nodePattern, nodeTarget);
    }

    private bool CheckNeighbor(GGNode nodePattern, GGNode nodeTarget, List<GGNode> visitedPatternNodesIn = null, List<GGNode> visitedPatternNodesOut = null)
    {
        if (visitedPatternNodesIn == null) visitedPatternNodesIn = new List<GGNode>();
        if (visitedPatternNodesOut == null) visitedPatternNodesOut = new List<GGNode>();

        foreach (var patternNeighbor in PatternGraph.GetNeighboursIn(nodePattern))
        {
            if (visitedPatternNodesIn.Contains(patternNeighbor)) continue;
            visitedPatternNodesIn.Add(patternNeighbor);

            bool foundCompatibleNeighbor = false;

            foreach (var targetNeighbor in TargetGraph.GetNeighboursIn(nodeTarget))
            {
                if (AreCompatible(patternNeighbor, targetNeighbor))
                {
                    if (CheckNeighbor(patternNeighbor, targetNeighbor, new List<GGNode>(visitedPatternNodesIn), new List<GGNode>(visitedPatternNodesOut)))
                    {
                        foundCompatibleNeighbor = true;
                        break;
                    }
                }
            }

            if (!foundCompatibleNeighbor) return false;
        }

        foreach (var patternNeighbor in PatternGraph.GetNeighboursOut(nodePattern))
        {
            if (visitedPatternNodesOut.Contains(patternNeighbor)) continue;
            visitedPatternNodesOut.Add(patternNeighbor);

            bool foundCompatibleNeighbor = false;

            foreach (var targetNeighbor in TargetGraph.GetNeighboursOut(nodeTarget))
            {
                if (AreCompatible(patternNeighbor, targetNeighbor))
                {
                    if (CheckNeighbor(patternNeighbor, targetNeighbor, new List<GGNode>(visitedPatternNodesIn), new List<GGNode>(visitedPatternNodesOut)))
                    {
                        foundCompatibleNeighbor = true;
                        break;
                    }
                }
            }

            if (!foundCompatibleNeighbor) return false;
        }

        return true;
    }

    /// <summary>
    /// A target node as at least the same number of total connections as the pattern node (unless pattern is marked as exact)
    /// For each edge type, the target node has at least the same number of connections as the pattern node (unless pattern is marked as exact)
    /// </summary>
    /// <param name="nodePattern"></param>
    /// <param name="nodeTarget"></param>
    /// <returns></returns>
    private bool CheckEdgeCount(GGNode nodePattern, GGNode nodeTarget)
    {
        if (nodePattern.IsExactInput || nodePattern.IsExactOutput)
            return ExactEdgeCount(nodePattern, nodeTarget);

        var pEdgesIn = PatternGraph.GetEdgesToNode(nodePattern);
        var pEdgesOut = PatternGraph.GetEdgesFromNode(nodePattern);

        var tEdgesIn = TargetGraph.GetEdgesToNode(nodeTarget);
        var tEdgesOut = TargetGraph.GetEdgesFromNode(nodeTarget);

        int nPatternEdgesOut = pEdgesOut.Count;
        int nTargetEdgesOut = tEdgesOut.Count;

        int nPatternEdgesIn = pEdgesIn.Count;
        int nTargetEdgesIn = tEdgesIn.Count;

        if (nPatternEdgesOut > nTargetEdgesOut || nPatternEdgesIn > nTargetEdgesIn) return false;

        List<Symbol> symbolsIn = new List<Symbol>();
        List<Symbol> symbolsOut = new List<Symbol>();

        foreach (var e in pEdgesIn)
        {
            if (!symbolsIn.Contains(e.EdgeSymbol) && e.EdgeSymbol.Type != GraphSymbolType.Asterisk)
                symbolsIn.Add(e.EdgeSymbol);
        }

        foreach (var e in pEdgesOut)
        {
            if (!symbolsOut.Contains(e.EdgeSymbol) && e.EdgeSymbol.Type != GraphSymbolType.Asterisk)
                symbolsOut.Add(e.EdgeSymbol);
        }

        foreach (var si in symbolsIn)
        {
            if (tEdgesIn.Count(e => e.EdgeSymbol == si) < pEdgesIn.Count(e => e.EdgeSymbol == si))
                return false;
        }

        foreach (var so in symbolsOut)
        {
            if (tEdgesOut.Count(e => e.EdgeSymbol == so) < pEdgesOut.Count(e => e.EdgeSymbol == so))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the target node has the same number of connections as the pattern node
    /// </summary>
    /// <returns></returns>
    private bool ExactEdgeCount(GGNode nodePattern, GGNode nodeTarget)
    {
        var pEdgesIn = PatternGraph.GetEdgesToNode(nodePattern);
        var pEdgesOut = PatternGraph.GetEdgesFromNode(nodePattern);

        var tEdgesIn = TargetGraph.GetEdgesToNode(nodeTarget);
        var tEdgesOut = TargetGraph.GetEdgesFromNode(nodeTarget);

        int nPatternEdgesOut = pEdgesOut.Count;
        int nTargetEdgesOut = tEdgesOut.Count;



        int nPatternEdgesIn = pEdgesIn.Count;
        int nTargetEdgesIn = tEdgesIn.Count;

        if (nodePattern.IsExactInput)
        {
            if (nPatternEdgesIn != nTargetEdgesIn) return false;
        }
        if (nodePattern.IsExactOutput)
        {
            if (nPatternEdgesOut != nTargetEdgesOut) return false;
        }

        List<Symbol> symbolsIn = new List<Symbol>();
        List<Symbol> symbolsOut = new List<Symbol>();

        foreach (var e in pEdgesIn)
        {
            if (!symbolsIn.Contains(e.EdgeSymbol) && e.EdgeSymbol.Type != GraphSymbolType.Asterisk)
                symbolsIn.Add(e.EdgeSymbol);
        }

        foreach (var e in pEdgesOut)
        {
            if (!symbolsOut.Contains(e.EdgeSymbol) && e.EdgeSymbol.Type != GraphSymbolType.Asterisk)
                symbolsOut.Add(e.EdgeSymbol);
        }

        int nAsteriskIn = pEdgesIn.Count(e => e.EdgeSymbol.Type == GraphSymbolType.Asterisk);
        int nAsteriskOut = pEdgesOut.Count(e => e.EdgeSymbol.Type == GraphSymbolType.Asterisk);

        foreach (var si in symbolsIn)
        {
            int nPatternEdgesInWithSymbol = pEdgesIn.Count(e => e.EdgeSymbol == si);
            int nTargetEdgesInWithSymbol = tEdgesIn.Count(e => e.EdgeSymbol == si);

            if (nTargetEdgesInWithSymbol < nPatternEdgesInWithSymbol) return false;
            
            if (nodePattern.IsExactInput)
                if (nPatternEdgesInWithSymbol + nAsteriskIn < nTargetEdgesInWithSymbol) 
                    return false;
        }

        foreach (var so in symbolsOut)
        {
            int nPatternEdgesOutWithSymbol = pEdgesOut.Count(e => e.EdgeSymbol == so);
            int nTargetEdgesOutWithSymbol = tEdgesOut.Count(e => e.EdgeSymbol == so);

            if (nTargetEdgesOutWithSymbol < nPatternEdgesOutWithSymbol) return false;

            if (nodePattern.IsExactOutput)
                if (nPatternEdgesOutWithSymbol + nAsteriskOut < nTargetEdgesOutWithSymbol) 
                    return false;
        }

        return true;
    }

    private bool IsNodeInTarget(GGNode node)
    {
        return !UnmatchedNodesTarget.Contains(node);
    }

    private bool IsNodeInPattern(GGNode node)
    { 
        return !UnmatchedNodesPattern.Contains(node);
    }

    /// <summary>
    /// Checks if the edge are the equivalent (same direction, same symbol, same nodes)
    /// </summary>
    /// <param name="pNode"></param>
    /// <param name="tNode"></param>
    /// <param name="pEdge"></param>
    /// <param name="tEdge"></param>
    /// <returns></returns>
    private bool CheckEdge(GGNode pNode, GGNode tNode, GGEdge pEdge, GGEdge tEdge)
    {
        bool sameDirection = false;
        if (pEdge.StartNode == pNode)
            sameDirection = tEdge.StartNode == tNode;
        else if (pEdge.EndNode == pNode)
            sameDirection = tEdge.EndNode == tNode;

        if (!sameDirection) return false;

        return Symbol.AreEquivalent(pEdge.EdgeSymbol, tEdge.EdgeSymbol) && 
            Symbol.AreEquivalent(pEdge.StartNode.NodeSymbol, tEdge.StartNode.NodeSymbol) && 
            Symbol.AreEquivalent(pEdge.EndNode.NodeSymbol, tEdge.EndNode.NodeSymbol);
    }

    public void AddMapping(GGNode nodePattern, GGNode nodeTarget)
    {
        // Add the mapping between a pattern node and target node
        MappedNodes[nodePattern] = nodeTarget;
        UnmatchedNodesPattern.Remove(nodePattern);
        UnmatchedNodesTarget.Remove(nodeTarget);
        PatternCompatibility[(nodePattern, nodeTarget)] = true;
        TargetCompatibility[(nodeTarget, nodePattern)] = true;
    }

    public void RemoveMapping(GGNode nodePattern, GGNode nodeTarget)
    {
        // Remove the mapping between a pattern node and target node
        MappedNodes.Remove(nodePattern);
        UnmatchedNodesPattern.Add(nodePattern);
        UnmatchedNodesTarget.Add(nodeTarget);
        PatternCompatibility[(nodePattern, nodeTarget)] = false;
        TargetCompatibility[(nodeTarget, nodePattern)] = false;
    }
}

