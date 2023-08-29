using GG.Data.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GG.Builder
{
    public class GGBuilder
    {
        public GGBuilder()
        {

        }

        /// <summary>
        /// Graph generation main function
        /// </summary>
        /// <param name="ruleSaveDataList"></param>
        /// <param name="MaxNodes"></param>
        /// <returns></returns>
        public static GGGraph ApplyAllRules(List<RuleSaveData> ruleSaveDataList, int MaxNodes = 10)
        {

            
            if (ruleSaveDataList.Count > 0)
            {
                var startingGraphSaveData = ruleSaveDataList[0].LeftGraph.ToSaveData();
                var startingGraph = new GGGraph(startingGraphSaveData);

                int i = 0;


                /// Max iterations is used to prevent infinite loops, the variable is increased at each rule to prevent Non Terminal symbols from remaining in the graph
                int maxIterations = MaxNodes;

                do
                {
                    if (ApplyRule(ruleSaveDataList[i], ref startingGraph) && maxIterations > 0)
                    {
                        maxIterations -= 1;
                    }
                    else
                    {
                        i += 1;
                        maxIterations = MaxNodes + i;
                    }
                    
                }

                while (i < ruleSaveDataList.Count);

                // Just to keep the graph pretty, we only discard results where any node as more than 4 edges incoming or outgoing, we also discard nodes which have more than 3 main Edges.
                //foreach (var node in startingGraph.Nodes)
                //{
                //    //if (startingGraph.GetEdgesToNode(node).Count > 4 || startingGraph.GetEdgesFromNode(node).Count > 4
                //    //    || startingGraph.GetEdgesFromNode(node).Count(e => { return e.EdgeSymbol.Type != Utils.GraphSymbolType.Edge;}) > 3)
                //    //{
                        
                //        return ApplyAllRules(ruleSaveDataList, MaxNodes);
                //    //}
                //}

                //m_MaxTries = 20;
                return startingGraph;
            }

            //m_MaxTries = 20;
            return null;
        }

        public static bool ApplyRule(RuleSaveData rule, ref GGGraph intermediateGraph)
        {
            GGGraph ruleGraph = new GGGraph(rule.LeftGraph.ToSaveData());
            GGGraph resultGraph = new GGGraph(rule.RightGraph.ToSaveData());



           // Find Subgraph: Step 2
            var results = VF2.FindSubgraphIsomorphisms(ruleGraph, intermediateGraph);

            if (results.Count > 0)
            {
                if (rule.RightGraph.Groups != null && rule.RightGraph.Groups.GroupSaveData != null && rule.RightGraph.Groups.GroupSaveData.Count > 0)
                {
                    List<GGGroup> groups = GGGroup.GetGroupsInGraph(resultGraph, rule.RightGraph.Groups.GroupSaveData);
                    if (groups.Count > 0)
                    {
                        resultGraph = GetRandomGroupGraph(groups);
                    }
                }

                // Steps 3,4 and 5
                SubstituteGraph(results.First(), intermediateGraph, resultGraph);
                
                // Distance nodes to make a prettier graph
                SeparateNodes(intermediateGraph);
                return true;
            }


            return false;
        }

        // Used to get random graphs from a rule with a random element to it.
        private static GGGraph GetRandomGroupGraph(List<GGGroup> groups)
        {
            float totalWeight = groups.Sum(x => x.Weight);
            float randomNumber = UnityEngine.Random.Range(0f, totalWeight);

            float weightSum = 0f;

            foreach (var g in groups)
            {
                weightSum += g.Weight;
                if (weightSum >= randomNumber)
                {
                    return g.ToGraph();
                }
            }

            // Should never happen
            return groups.Last().ToGraph();
        }

        private static void SubstituteGraph(Dictionary<GGNode, GGNode> patternTargetMapping, GGGraph intermediateGraph, GGGraph resultGraph)
        {
            // Step 3
            RemovePatternEdges(patternTargetMapping, intermediateGraph);
            //Step 4
            SubstitueNodes(patternTargetMapping, intermediateGraph, resultGraph);
            //Step 5 and 6
            ConnectNodesAndRemoveIDs(intermediateGraph, resultGraph);
        }



        private static void RemovePatternEdges(Dictionary<GGNode, GGNode> patternTargetMapping, GGGraph intermediateGraph)
        {
            foreach (var node in patternTargetMapping)
            {
                var keyNode = node.Key;
                var valueNode = node.Value;

                valueNode.Identifier = keyNode.Identifier;
            }

            List<GGEdge> edgesToRemove = new List<GGEdge>(intermediateGraph.Edges);

            foreach (GGEdge e in edgesToRemove)
            {
                bool isInputIn = patternTargetMapping.ContainsValue(e.StartNode);
                bool isOutputIn = patternTargetMapping.ContainsValue(e.EndNode);

                if (isInputIn && isOutputIn)
                {
                    intermediateGraph.RemoveEdge(e);
                }
            }
        }

        private static void SubstitueNodes(Dictionary<GGNode, GGNode> patternTargetMapping, GGGraph intermediateGraph, GGGraph resultGraph)
        {
            GGNode[] leftNodes = new GGNode[patternTargetMapping.Values.Select(x => x.Identifier >= 0).Count()];
            GGNode[] rightNodes = new GGNode[resultGraph.Nodes.Select(x => x.Identifier >= 0).Count()];

            // Initialize arrays
            foreach (var valueNode in patternTargetMapping.Values)
            {
                if (valueNode.Identifier < 0) continue;

                leftNodes[valueNode.Identifier] = valueNode;
            }

            foreach (var resNode in resultGraph.Nodes)
            {
                if (resNode.Identifier < 0) continue;

                rightNodes[resNode.Identifier] = resNode;
            }

            if (leftNodes.Length > rightNodes.Length)
            {
                for (int i = rightNodes.Length; i < leftNodes.Length; i++)
                {
                    intermediateGraph.RemoveNode(leftNodes[i]);
                }
            }

            // Computes position in order to put the new nodes roughly in the same place as the old ones
            Vector2 middlePointTarget = ComputeMiddlePosition(leftNodes.ToList());
            Vector2 middlePointResult = ComputeMiddlePosition(rightNodes.ToList());
                

            for (int i = 0; i < rightNodes.Length; i++)
            {
                GGNode newNode = rightNodes[i];
                newNode.AssingNewGUID();

                Vector2 newPos = ComputeNodePosition(newNode, middlePointTarget, middlePointResult, intermediateGraph);

                newNode.Position = newPos;

                // If the node as same identifier as a node in the left graph, we replace it
                if (i < leftNodes.Length)
                {


                    foreach (GGEdge e in intermediateGraph.GetEdgesWithNode(leftNodes[i]))
                    {
                        if (e.EdgeSymbol == leftNodes[i].NodeSymbol)
                        {
                            e.EdgeSymbol = rightNodes[i].NodeSymbol;
                        }
                    }

                    leftNodes[i].Identifier = newNode.Identifier;
                    leftNodes[i].NodeSymbol = newNode.NodeSymbol.Type != Utils.GraphSymbolType.Asterisk? newNode.NodeSymbol : leftNodes[i].NodeSymbol;
                    leftNodes[i].Position = newPos;
                }
                // Otherwise we add a new node
                else
                {
                    intermediateGraph.AddNode(newNode);
                }

                // We separate at each iteration to keep things pretty
                SeparateNodes(intermediateGraph);
                
            }
        }

        private static Vector2 ComputeMiddlePosition(List<GGNode> nodes)
        {
            

            Vector2 middlePoint = Vector2.zero;

            float minX = Mathf.Infinity;
            float minY = Mathf.Infinity;
            float maxX = Mathf.NegativeInfinity;
            float maxY = Mathf.NegativeInfinity;

            foreach (GGNode n in nodes)
            {
                minX = Mathf.Min(minX, n.Position.x);
                maxX = Mathf.Max(maxX, n.Position.x);

                minY = Mathf.Min(minY, n.Position.y);
                maxY = Mathf.Max(maxY, n.Position.y);
            }

            middlePoint.x = (minX + maxX) / 2;
            middlePoint.y = (minY + maxY) / 2;
        

            return new Vector2(middlePoint.x, middlePoint.y);
        }

        private static Vector2 ComputeNodePosition(GGNode node, Vector2 targetMiddlePoint, Vector2 resultMiddlePoint, GGGraph intermediateGraph)
        {
            Vector2 newPos = targetMiddlePoint;

            float xOffset = Mathf.Abs(node.Position.x - resultMiddlePoint.x);
            float yOffset = Mathf.Abs(node.Position.y - resultMiddlePoint.y);

            newPos.x = node.Position.x < resultMiddlePoint.x ? newPos.x - xOffset : newPos.x + xOffset;
            newPos.y = node.Position.y < resultMiddlePoint.y ? newPos.y - yOffset : newPos.y + yOffset;


            return newPos;
        }

        // Pushes nodes away from each other
        private static void SeparateNodes(GGGraph finalGraph)
        {
            int maxIterations = 100;

            while (HasOverlaps(finalGraph) && maxIterations > 0)
            {
                List<Vector2> overlapVectors = new List<Vector2>();

                foreach (GGNode n in finalGraph.Nodes)
                {
                    Vector2 overlapVector = Vector2.zero;
                    foreach (GGNode n2 in finalGraph.Nodes)
                    {
                        if (n == n2) continue;

                        if (Mathf.Abs(n.Position.x - n2.Position.x) < 150)
                        {
                            if (Mathf.Abs(n.Position.y - n2.Position.y) < 200)
                            {
                                overlapVector += (n.Position - n2.Position);
                            }
                        }
                    }
                    overlapVector.Normalize();
                    overlapVectors.Add(overlapVector);
                }

                for (int i = 0; i < finalGraph.Nodes.Count; i++)
                {
                    GGNode node = finalGraph.Nodes[i];
                    node.Position += overlapVectors[i] * 1.3f;
                }

                maxIterations -= 1;
            }
        }

        //Checks wheter two nodes overlap
        private static bool HasOverlaps(GGGraph finalGraph)
        {
            foreach (GGNode n in finalGraph.Nodes)
            {
                foreach (GGNode n2 in finalGraph.Nodes)
                {
                    if (n == n2) continue;

                    if (Mathf.Abs(n.Position.x - n2.Position.x) < 150)
                    {
                        if (Mathf.Abs(n.Position.y - n2.Position.y) < 200)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void ConnectNodesAndRemoveIDs(GGGraph intermediateGraph, GGGraph resultGraph)
        {
            Dictionary<int, GGNode> newNodes = new Dictionary<int, GGNode>();

            foreach (GGNode n in intermediateGraph.Nodes)
            {
                if (n.Identifier > -1)
                {
                    newNodes[n.Identifier] = n;
                }
            }

            foreach (GGEdge e in resultGraph.Edges)
            {
                GGNode startNode = newNodes[e.StartNode.Identifier];
                GGNode endNode = newNodes[e.EndNode.Identifier];

                intermediateGraph.AddEdge(startNode, endNode, e.EdgeSymbol);
            }

            foreach (GGNode n in newNodes.Values)
            {
                n.Identifier = -1;
            }
        }

    }

    


    
}
