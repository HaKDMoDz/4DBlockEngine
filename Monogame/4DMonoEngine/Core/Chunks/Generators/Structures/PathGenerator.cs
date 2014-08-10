using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Common.Extensions;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Noise;
using _4DMonoEngine.Core.Utils.Random;

namespace _4DMonoEngine.Core.Chunks.Generators.Structures
{

    //TODO : Split this into a RiverGenerator and an abstract superclass
    public class PathGenerator
    {
        protected static float RiverEndMaxHeight = 64;
        protected static float RiversStartMinHeight = 92;
        protected const float PathCellNoiseScale = 16f;
        private const int SkipCount = 4;

        private readonly CellNoise2D m_cellNoise;
        private readonly GetHeight m_getHeight;

        private readonly Dictionary<uint, PathGraphNode> m_sources;
        private readonly Dictionary<uint, PathGraphNode> m_sinks;
        private readonly Dictionary<uint, PathGraphNode> m_general;

        private readonly List<PathNodeList> m_paths;


        public PathGenerator(uint seed, GetHeight getHeight)
        {
            m_getHeight = getHeight;
            var fastRandom = new FastRandom(seed);
            m_cellNoise = new CellNoise2D(fastRandom.NextUInt());
            m_sources = new Dictionary<uint, PathGraphNode>();
            m_sinks = new Dictionary<uint, PathGraphNode>();
            m_general = new Dictionary<uint, PathGraphNode>();
            m_paths = new List<PathNodeList>();
        }

        public void InitializePathSystem(int originX, int originZ, int originW, int radius)
        {
            var minX = originX - radius;
            var maxX = originX + radius;
            var minZ = originZ - radius;
            var maxZ = originZ + radius;
            var timer = Stopwatch.StartNew();
            for (var x = minX; x < maxX; x += SkipCount)
            {
                for (var z = minZ; z < maxZ; z += SkipCount)
                {
                    var data = m_cellNoise.Voroni(x, z, PathCellNoiseScale);
                    if (m_sources.ContainsKey(data.Id) || m_general.ContainsKey(data.Id) || m_sinks.ContainsKey(data.Id))
                    {
                        continue;
                    }
                    var centroidX= x + (int) Math.Round(data.Delta.X*PathCellNoiseScale, MidpointRounding.ToEven);
                    var centroidZ = z + (int) Math.Round(data.Delta.Y*PathCellNoiseScale, MidpointRounding.ToEven);
                    InsertNodeIntoMaps(data.Id, centroidX, centroidZ, originW);
                }
            }
            var activeList = new Queue<PathGraphNode>(m_sources.Values);
            var nodePool = m_general.Values.ToList();
            nodePool.AddRange(m_sinks.Values);
            var terminalNodes = new Stack<PathGraphNode>();
            while (activeList.Count > 0 && nodePool.Count > 0)
            {
                var activeNode = activeList.Dequeue();
                var workPool = nodePool.FindAll(node => FilterPool(activeNode, node));
                if (workPool.Count == 0 || m_sinks.ContainsValue(activeNode))
                {
                    if (activeNode.Edges.Count == 0)
                    {
                        activeNode.NodeType = PathNodeType.Invalid;
                    }
                    else if (!activeNode.Edges.ContainsValue(1)) 
                    {
                        activeNode.NodeType = PathNodeType.Sink;
                    }
                    terminalNodes.Push(activeNode);
                    nodePool.Remove(activeNode);
                    continue;
                }
                //TODO : replace with a fold
                workPool.Sort((a, b) => SortPool(activeNode, a, b));
                var nextNode = workPool[0];
                activeNode.Edges.Add(nextNode, 1);
                nextNode.Edges.Add(activeNode, -1);
                if (!activeList.Contains(nextNode))
                {
                    activeList.Enqueue(nextNode);
                }
            }
            var map = new Dictionary<uint, PathGraphNode>(m_general);
            foreach (var pathGraphNode in map)
            {
                if (pathGraphNode.Value.NodeType == PathNodeType.Sink)
                {
                    m_general.Remove(pathGraphNode.Key);
                    m_sinks.Add(pathGraphNode.Key, pathGraphNode.Value);
                }
            }
            map = new Dictionary<uint, PathGraphNode>(m_sources);
            foreach (var pathGraphNode in map)
            {
                if (pathGraphNode.Value.NodeType == PathNodeType.Invalid)
                {
                    m_sources.Remove(pathGraphNode.Key);
                    m_general.Add(pathGraphNode.Key, pathGraphNode.Value);
                }
                else
                {
                    m_paths.Add(new PathNodeList(pathGraphNode.Value));
                }
            }
            Console.WriteLine(timer.ElapsedMilliseconds);
        }

        protected bool FilterPool(PathGraphNode activeNode, PathGraphNode candidateNode)
        {
            return activeNode != candidateNode && candidateNode.Position.Y <= activeNode.Position.Y && !activeNode.Edges.ContainsKey(candidateNode) && Vector3.DistanceSquared(candidateNode.Position, activeNode.Position) <= 625;
        }

        protected int SortPool(PathGraphNode activeNode, PathGraphNode candidateNode1, PathGraphNode candidateNode2)
        {
            var result = (int)(candidateNode1.Position.Y - candidateNode2.Position.Y);
            //TODO : test reordering tests
            if (result == 0)
            {
                result = candidateNode1.Edges.Count - candidateNode2.Edges.Count;
            }
            if (result == 0)
            {
                result = (int)(Vector3.DistanceSquared(candidateNode1.Position, activeNode.Position) -
                         Vector3.DistanceSquared(candidateNode2.Position, activeNode.Position));
            }
            return result;
        }

        protected void InsertNodeIntoMaps(uint nodeId, int x, int z, int w)
        {
            var height = m_getHeight(x, z, w);
            var node = new PathGraphNode(new Vector3(x, height, z));
            if (height >= RiversStartMinHeight)
            {
                node.NodeType = PathNodeType.Source;
                m_sources.Add(nodeId, node);
            }
            else if (height <= RiverEndMaxHeight)
            {
                node.NodeType = PathNodeType.Sink;
                m_sinks.Add(nodeId, node);
            }
            else
            {
                node.NodeType = PathNodeType.General;
                m_general.Add(nodeId, node);
            }
        }

        public bool IsOnRiver(int x, int z)
        {
            var stack = new Stack<PathGraphNode>();
            var testPoint = new Vector3(x, 0, z);
            foreach (var path in m_paths)
            {
                if (path.BoundingBox.Contains(testPoint) != ContainmentType.Disjoint)
                {
                    var pathGraphNode = path.Head;
                    var lastNodePos = new Vector3(pathGraphNode.Position.X, 0, pathGraphNode.Position.Z);
                    stack.Push(pathGraphNode);
                    while (stack.Count > 0)
                    {
                        var node = stack.Pop();
                        var nodePos = new Vector3(node.Position.X, 0, node.Position.Z);
                        var distance = DistanceFromPointToLineSegment(testPoint, lastNodePos, nodePos);
                        if (distance <= 1.5)
                        {
                            return true;
                        }
                        lastNodePos = nodePos;
                        foreach (var graphNode in node.Edges)
                        {
                            if (graphNode.Value > 0)
                            {
                                stack.Push(graphNode.Key);
                            }
                        }
                    }
                }
            }
            return false;
        }

        public float GetHeightForRiver(int x, int z, float groundHeight)
        {
            var stack = new Stack<PathGraphNode>();
            var testPoint = new Vector3(x, 0, z);
            foreach (var path in m_paths)
            {
                if (path.BoundingBox.Contains(testPoint) != ContainmentType.Disjoint)
                {
                    var pathGraphNode = path.Head;
                    var lastNode = pathGraphNode;
                    stack.Push(pathGraphNode);
                    while (stack.Count > 0)
                    {
                        var lastNodePos = new Vector3(lastNode.Position.X, 0, lastNode.Position.Z);
                        var node = stack.Pop();
                        var nodePos = new Vector3(node.Position.X, 0, node.Position.Z);
                        var distance = DistanceFromPointToLineSegment(testPoint, lastNodePos, nodePos);
                        if (distance <= 1.5)
                        {
                            /*var distance = 
                            MathHelper.Lerp()  */
                            return Math.Min(groundHeight, lastNode.Position.Y);
                        }
                        lastNode = node;
                        foreach (var graphNode in node.Edges)
                        {
                            if (graphNode.Value > 0)
                            {
                                stack.Push(graphNode.Key);
                            }
                        }
                    }
                }
            }
            return groundHeight;
        }

        float DistanceFromPointToLineSegment(Vector3 point, Vector3 anchor, Vector3 end)
        {
            var d = end - anchor;
            var length = d.Length();
            if (Math.Abs(length) < 0.0001)
            {
                return (point - anchor).Length();
            }
            d.Normalize();
            var intersect = Vector3.Dot((point - anchor), d);
            if (intersect < 0)
            {
                return (point - anchor).Length();
            }
            return intersect > length ? (point - end).Length() : (point - (anchor + d * intersect)).Length();
        }

        protected void BuildPathSections(int x, int z, int w)
        {
            //determine if any new pathing regions are added

            //determine if there are any new sources or sinks in this group

            //create new active paths

            //determine if any active paths can be expanded

            //add cells along each path (A*)

            //move any finished paths to the complete list	

        }

        public class PathNodeList
        {
            public PathGraphNode Head;
            public BoundingBox BoundingBox;

            public PathNodeList(PathGraphNode head)
            {
                Head = head;
                var stack = new Stack<PathGraphNode>();
                var minPosX = float.MaxValue;
                var minPosZ = float.MaxValue;
                var maxPosX = -float.MaxValue;
                var maxPosZ = -float.MaxValue;
                stack.Push(head);
                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    var nodePos = node.Position;

                    if (nodePos.X < minPosX) minPosX = nodePos.X;
                    if (nodePos.Z < minPosZ) minPosZ = nodePos.Z;
                    if (nodePos.X > maxPosX) maxPosX = nodePos.X;
                    if (nodePos.Z > maxPosZ) maxPosZ = nodePos.Z;
                    
                    foreach (var graphNode in node.Edges)
                    {
                        if (graphNode.Value > 0)
                        {
                            stack.Push(graphNode.Key);
                        }
                    }
                }
                BoundingBox = new BoundingBox(new Vector3(minPosX - 1.5f, 0, minPosZ - 1.5f), new Vector3(maxPosX + 1.5f, 0, maxPosZ + 1.5f));
            }
        }

        public class PathGraphNode
        {
            public Dictionary<PathGraphNode, float> Edges;
            public PathNodeType NodeType;
            public Vector3 Position;
            public PathGraphNode(Vector3 position, PathNodeType nodeType = PathNodeType.Invalid)
            {
                Edges = new Dictionary<PathGraphNode, float>();
                NodeType = nodeType;
                Position = position;
            }
        }

        public class PathData
        {
            public List<Vector3> Cells;
            public bool IsBuildingForward;
            public PathGraphNode CurrentNode;
            public PathData(bool isBuildingForward)
            {
                 IsBuildingForward = isBuildingForward;
                Cells = new List<Vector3>();
            }
        }

        public enum PathNodeType
        {
            Source,
            Sink,
            Invalid,
            General
        }
    }
}
