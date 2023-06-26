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
        //List<RuleSaveData> SaveDataRules;
        //Vector2 GridSize;

        public GGBuilder(/*List<RuleSaveData> saveData, Vector2 gridSize*/)
        {
            //SaveDataRules = saveData;
            //GridSize = gridSize;
        }

        public static void ApplyRule(int ruleIndex, GGGraph intermediateGraph)
        {
            //if (SaveDataRules.Count > ruleIndex) 
            //{
            //    ApplyRule(SaveDataRules[ruleIndex], intermediateGraph);
            //}
        }

        public static GGGraph ApplyAllRules(List<RuleSaveData> ruleSaveDataList, int MaxNodes = 10)
        {


            if (ruleSaveDataList.Count > 0)
            {
                var startingGraphSaveData = ruleSaveDataList[0].LeftGraph.ToSaveData();
                var startingGraph = new GGGraph(startingGraphSaveData);

                int i = 0;

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

                foreach (var node in startingGraph.Nodes)
                {
                    if (startingGraph.GetEdgesToNode(node).Count > 4 || startingGraph.GetEdgesFromNode(node).Count > 4
                        || startingGraph.GetEdgesFromNode(node).Count(e => { return e.EdgeSymbol.Type != Utils.GraphSymbolType.Edge;}) > 3)
                    {
                        return ApplyAllRules(ruleSaveDataList, MaxNodes);
                    }
                }

                return startingGraph;
            }

            return null;
        }

        public static bool ApplyRule(RuleSaveData rule, ref GGGraph intermediateGraph)
        {
            GGGraph ruleGraph = new GGGraph(rule.LeftGraph.ToSaveData());
            GGGraph resultGraph = new GGGraph(rule.RightGraph.ToSaveData());



           
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

                SubstituteGraph(results.First(), intermediateGraph, resultGraph);
                SeparateNodes(intermediateGraph);
                return true;
            }


            return false;
        }

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
            RemovePatternEdges(patternTargetMapping, intermediateGraph);
            SubstitueNodes(patternTargetMapping, intermediateGraph, resultGraph);
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


            Vector2 middlePointTarget = ComputeMiddlePosition(leftNodes.ToList());
            Vector2 middlePointResult = ComputeMiddlePosition(rightNodes.ToList());
                

            for (int i = 0; i < rightNodes.Length; i++)
            {
                GGNode newNode = rightNodes[i];
                newNode.AssingNewGUID();

                Vector2 newPos = ComputeNodePosition(newNode, middlePointTarget, middlePointResult, intermediateGraph);

                newNode.Position = newPos;

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
                else
                {
                    intermediateGraph.AddNode(newNode);
                }

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


        private static void PushNode(GGNode nodeToPush, Vector2 newPos, GGGraph intermediateGraph)
        {
            Vector2 overlapOffset = Vector2.zero;


            foreach (GGNode n in intermediateGraph.Nodes)
            {
                if (n.Position.x >= newPos.x - 150 && n.Position.x <= newPos.x + 150)
                {
                    if (n.Position.y <= newPos.y + 100 && n.Position.y >= newPos.y - 100)
                    {

                        float overlapXOffset = MathF.Abs(n.Position.x - newPos.x);
                        float overlapYOffset = MathF.Abs(n.Position.y - newPos.y);
                        overlapOffset.x = MathF.Max(overlapOffset.x, overlapXOffset);
                        overlapOffset.y = MathF.Max(overlapOffset.y, overlapYOffset);
                    }
                }
            }

            if (overlapOffset != Vector2.zero)
            {
                if (overlapOffset.x > overlapOffset.y)
                {
                    float xOverlapOffset = 200 - overlapOffset.x;
                    var nodesRight = intermediateGraph.Nodes.Where(x => x.Position.x >= newPos.x);
                    var nodesLeft = intermediateGraph.Nodes.Where(x => x.Position.x < newPos.x);

                    if (nodesRight.Count() >= nodesLeft.Count())
                        newPos.x -= xOverlapOffset;
                    else
                        newPos.x += xOverlapOffset;
                }
                else
                {
                    float yOverlapOffset = 250 - overlapOffset.y;

                    var nodesUp = intermediateGraph.Nodes.Where(n => n.Position.y < newPos.y);
                    var nodesDown = intermediateGraph.Nodes.Where(n => n.Position.y >= newPos.y);

                    if (nodesUp.Count() >= nodesDown.Count())
                        newPos.y += yOverlapOffset;
                    else
                        newPos.y -= yOverlapOffset;
                }


                List<GGNode> nodesToPush = new List<GGNode>();
                foreach (GGNode n in intermediateGraph.Nodes)
                {
                    if (n.Position.x >= newPos.x - 150 && n.Position.x <= newPos.x + 150)
                    {
                        if (n.Position.y <= newPos.y + 100 && n.Position.y >= newPos.y - 100)
                        {
                            PushNode(n, n.Position, intermediateGraph);
                        }
                    }
                }

            }
        }

        private static Vector2 ComputeNodePosition(GGNode node, Vector2 targetMiddlePoint, Vector2 resultMiddlePoint, GGGraph intermediateGraph)
        {
            Vector2 newPos = targetMiddlePoint;

            float xOffset = Mathf.Abs(node.Position.x - resultMiddlePoint.x);
            float yOffset = Mathf.Abs(node.Position.y - resultMiddlePoint.y);

            newPos.x = node.Position.x < resultMiddlePoint.x ? newPos.x - xOffset : newPos.x + xOffset;
            newPos.y = node.Position.y < resultMiddlePoint.y ? newPos.y - yOffset : newPos.y + yOffset;

            //Vector2 overlapOffset = Vector2.positiveInfinity;
            //bool changed = false;

            //foreach (GGNode n in intermediateGraph.Nodes)
            //{
            //    if (n.Position.x >= newPos.x - 150 && n.Position.x <= newPos.x + 150)
            //    {
            //        if (n.Position.y <= newPos.y + 200 && n.Position.y >= newPos.y - 200)
            //        {
            //            float overlapXOffset = MathF.Abs(n.Position.x - newPos.x);
            //            float overlapYOffset = MathF.Abs(n.Position.y - newPos.y);

            //            overlapOffset.x = MathF.Min(overlapOffset.x, overlapXOffset);
            //            overlapOffset.y = MathF.Min(overlapOffset.y, overlapYOffset);

            //            changed = true;
            //        }
            //    }
            //}

            //if (changed)
            //{
            //    if (overlapOffset.x > overlapOffset.y)
            //    {
            //        float xOverlapOffset = 200 - overlapOffset.x;
            //        var nodesRight = intermediateGraph.Nodes.Where(x => x.Position.x >= newPos.x);
            //        var nodesLeft = intermediateGraph.Nodes.Where(x => x.Position.x < newPos.x);

            //        if (nodesRight.Count() >= nodesLeft.Count())
            //        {
            //            newPos.x -= xOverlapOffset /2f;
            //            foreach (GGNode n in nodesLeft)
            //            {
            //                n.Position = new Vector2(n.Position.x - xOverlapOffset /2f, n.Position.y);
            //            }
            //        }
            //        else
            //        {
            //            newPos.x += xOverlapOffset /2f;
            //            foreach (GGNode n in nodesRight)
            //            {
            //                n.Position = new Vector2(n.Position.x + xOverlapOffset /2f, n.Position.y);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        float yOverlapOffset = 250 - overlapOffset.y;

            //        var nodesUp = intermediateGraph.Nodes.Where(n => n.Position.y < newPos.y);
            //        var nodesDown = intermediateGraph.Nodes.Where(n => n.Position.y >= newPos.y);

            //        if (nodesUp.Count() >= nodesDown.Count())
            //        {
            //            newPos.y += yOverlapOffset / 2f;
            //            foreach (GGNode n in nodesDown)
            //            {
            //                n.Position = new Vector2(n.Position.x, n.Position.y + yOverlapOffset / 2f);
            //            }
            //        }
            //        else
            //        {
            //            newPos.y -= yOverlapOffset /2f;
            //            foreach (GGNode n in nodesUp)
            //            {
            //                n.Position = new Vector2(n.Position.x, n.Position.y - yOverlapOffset / 2f);
            //            }
            //        }
            //    }

            //}

            return newPos;
        }

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
