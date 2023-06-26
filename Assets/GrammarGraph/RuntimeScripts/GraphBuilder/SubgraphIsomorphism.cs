//using System;
//using System.Collections.Generic;

//class SubgraphIsomorphism
//{
//    public bool IsSubgraphIsomorphic(GGGraph graph, GGGraph subgraph)
//    {
//        Dictionary<GGNode, GGNode> nodeMapping = new Dictionary<GGNode, GGNode>();

//        return Backtrack(graph, subgraph, nodeMapping);
//    }

//    private bool Backtrack(GGGraph graph, GGGraph subgraph, Dictionary<GGNode, GGNode> nodeMapping)
//    {
//        // Check if all nodes in the subgraph have been matched
//        if (nodeMapping.Count == subgraph.Nodes.Count)
//            return true;

//        GGNode graphNode = SelectUnmatchedNode(graph, nodeMapping);
//        if (graphNode == null)
//            return false;

//        foreach (GGNode subgraphNode in subgraph.Nodes)
//        {
//            if (!nodeMapping.ContainsValue(subgraphNode))
//            {
//                if (IsMappingValid(graphNode, subgraphNode, graph, subgraph, nodeMapping))
//                {
//                    nodeMapping[graphNode] = subgraphNode;

//                    if (Backtrack(graph, subgraph, nodeMapping))
//                        return true;

//                    nodeMapping.Remove(graphNode);
//                }
//            }
//        }

//        return false;
//    }

//    private GGNode SelectUnmatchedNode(GGGraph graph, Dictionary<GGNode, GGNode> nodeMapping)
//    {
//        foreach (GGNode node in graph.Nodes)
//        {
//            if (!nodeMapping.ContainsKey(node))
//                return node;
//        }

//        return null;
//    }

//    private bool IsMappingValid(GGNode targetNode, GGNode patternNode, GGGraph targetGraph, GGGraph patternGraph, Dictionary<GGNode, GGNode> nodeMapping)
//    {
//        if (!IsValidDegree(targetNode, patternNode, targetGraph, patternGraph, nodeMapping))
//            return false;

//        foreach (var graphEdge in targetGraph.Edges)
//        {
//            GGNode targetStartNode = graphEdge.StartNode;
//            GGNode targetEndNode = graphEdge.EndNode;

//            if (nodeMapping.ContainsKey(targetStartNode) && nodeMapping.ContainsKey(targetEndNode))
//            {
//                GGNode patternStartNode = nodeMapping[targetStartNode];
//                GGNode patternEndNode = nodeMapping[targetEndNode];

//                if (!patternGraph.HasSimilarEdge(new GGEdge(patternStartNode, patternEndNode, graphEdge.EdgeSymbol)))
//                    return false;
//            }
//            else if (!nodeMapping.ContainsKey(targetStartNode) && !nodeMapping.ContainsKey(targetEndNode))
//            {
//                if (targetGraph.Edges.Contains(new Tuple<GGNode, GGNode>(targetStartNode, targetEndNode)))
//                {
//                    if (!patternGraph.Edges.Contains(new Tuple<GGNode, GGNode>(patternNode, patternGraph.Nodes.Find(n => n.Id == patternNode.Id + 1))))
//                        return false;
//                }
//                else if (targetGraph.Edges.Contains(new Tuple<Node, Node>(targetEndNode, targetStartNode)))
//                {
//                    if (!patternGraph.Edges.Contains(new Tuple<Node, Node>(patternGraph.Nodes.Find(n => n.Id == patternNode.Id + 1), patternNode)))
//                        return false;
//                }
//            }
//        }

//        return true;
//    }

//    private bool IsValidDegree(GGNode graphNode, GGNode subgraphNode, GGGraph graph, GGGraph subgraph, Dictionary<GGNode, GGNode> nodeMapping)
//    {
//        int graphInDegree = GetInDegree(graphNode, graph, nodeMapping);
//        int graphOutDegree = GetOutDegree(graphNode, graph, nodeMapping);
//        int subgraphInDegree = GetInDegree(subgraphNode, subgraph, nodeMapping);
//        int subgraphOutDegree = GetOutDegree(subgraphNode, subgraph, nodeMapping);

//        return graphInDegree >= subgraphInDegree && graphOutDegree >= subgraphOutDegree;
//    }

//    private int GetInDegree(GGNode node, GGGraph graph, Dictionary<GGNode, GGNode> nodeMapping)
//    {
//        int inDegree = 0;
//        foreach (Tuple<GGNode, GGNode> edge in graph.Edges)
//        {
//            Node target = edge.Item2;

//            if (node == target && nodeMapping.ContainsKey(edge.Item1))
//                inDegree++;
//        }

//        return inDegree;
//    }

//    private int GetOutDegree(GGNode node, GGGraph graph, Dictionary<GGNode, GGNode> nodeMapping)
//    {
//        int outDegree = 0;
//        foreach (Tuple<Node, Node> edge in graph.Edges)
//        {
//            Node source = edge.Item1;

//            if (node == source && nodeMapping.ContainsKey(edge.Item2))
//                outDegree++;
//        }

//        return outDegree;
//    }
//}
