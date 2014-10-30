using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
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
        protected const float PathCellNoiseScale = 32;
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
            var map = new Dictionary<uint, PathGraphNode>(m_sinks);
            foreach (var pathGraphNode in map)
            {
                var pos = pathGraphNode.Value.Position;
                foreach (var edge in pathGraphNode.Value.Edges.Where(edge => edge.Value < 0))
                {
                    if (edge.Key.Position.Y < pos.Y)
                    {
                        pos.Y = edge.Key.Position.Y;
                    }
                }
                pathGraphNode.Value.Position = pos;
            }
            map = new Dictionary<uint, PathGraphNode>(m_general);
            foreach (var pathGraphNode in map)
            {
                if (pathGraphNode.Value.NodeType == PathNodeType.Sink)
                {
                    m_general.Remove(pathGraphNode.Key);
                    m_sinks.Add(pathGraphNode.Key, pathGraphNode.Value);
                }
                var pos = pathGraphNode.Value.Position;
                foreach (var edge in pathGraphNode.Value.Edges.Where(edge => edge.Value < 0))
                {
                    if (edge.Key.Position.Y < pos.Y)
                    {
                        pos.Y = edge.Key.Position.Y;
                    }
                }
                pathGraphNode.Value.Position = pos;
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
                    m_paths.Add(new PathNodeList(pathGraphNode.Value, m_getHeight));
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
            var node = new PathGraphNode(new Vector3(x, height, z), nodeId);
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

        public PathData GetRiverData(int x, int z)
        {
            var testPoint = new Vector3(x, 0, z);
            PathData slice = null;
            foreach (var path in m_paths)
            {
                if (path.BoundingBox.Contains(testPoint) == ContainmentType.Disjoint)
                {
                    continue;
                }
                foreach (var graphNode in path.Head.Edges)
                {
                    if (graphNode.Value <= 0)
                    {
                        continue;
                    }
                    var currentSlice = GetRiverData(x, z, graphNode.Key, path.Head, path);
                    if (currentSlice != null &&(slice == null || currentSlice.Position.Y < slice.Position.Y))
                    {
                        slice = currentSlice;
                    }
                }
            }
            return slice;
        }

        private PathData GetRiverData(int x, int z, PathGraphNode nextNode, PathGraphNode lastNode, PathNodeList path)
        {
            var testPoint = new Vector2(x, z);
            var lastNodePos = new Vector2(lastNode.Position.X, lastNode.Position.Z);
            var nextNodePos = new Vector2(nextNode.Position.X, nextNode.Position.Z);
            var distance = MathUtilities.DistanceFromPointToLineSegment(testPoint, lastNodePos, nextNodePos);
            var pathLeg = path.GetPathLeg(lastNode, nextNode);
            var distSquared = Vector2.DistanceSquared(lastNodePos, testPoint) - distance*distance;
            if (distSquared >= 0)
            {
                var dist = Math.Sqrt(distSquared);
                var ratio = dist/Vector2.Distance(lastNodePos, nextNodePos);
                var sliceIndex = (int) (ratio*pathLeg.Count);
                if (sliceIndex < pathLeg.Count)
                {
                    var pathSlice = pathLeg[sliceIndex];
                    if (distance <= pathSlice.Radius)
                    {
                        return pathSlice;
                    }
                }
            }
            PathData slice = null;
            foreach (var graphNode in nextNode.Edges)
            {
                if (graphNode.Value <= 0)
                {
                    continue;
                }
                var currentSlice = GetRiverData(x, z, graphNode.Key, nextNode, path);
                if (currentSlice != null && (slice == null || currentSlice.Position.Y < slice.Position.Y))
                {
                    slice = currentSlice;
                }
            }
            return slice;
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

        public class PathGraphNode
        {
            public Dictionary<PathGraphNode, float> Edges;
            public readonly uint Id;
            public PathNodeType NodeType;
            public Vector3 Position;
            public PathGraphNode(Vector3 position, uint id, PathNodeType nodeType = PathNodeType.Invalid)
            {
                Edges = new Dictionary<PathGraphNode, float>();
                NodeType = nodeType;
                Id = id;
                Position = position;
            }
        }

        public class PathNodeList
        {
            public readonly PathGraphNode Head;
            public readonly BoundingBox BoundingBox;
            private readonly Dictionary<PathGraphNode, Dictionary<PathGraphNode, Lazy<List<PathData>>>> m_slices;
            public PathNodeList(PathGraphNode head, GetHeight getHeightFunction)
            {
                var noise = new SimplexNoise2D(head.Id);
                Head = head;
                var stack = new Stack<PathGraphNode>();
                var minPosX = float.MaxValue;
                var minPosZ = float.MaxValue;
                var maxPosX = -float.MaxValue;
                var maxPosZ = -float.MaxValue;
                m_slices = new Dictionary<PathGraphNode, Dictionary<PathGraphNode, Lazy<List<PathData>>>>();
                stack.Push(head);
                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    var nodePos = node.Position;

                    if (nodePos.X < minPosX) minPosX = nodePos.X;
                    if (nodePos.Z < minPosZ) minPosZ = nodePos.Z;
                    if (nodePos.X > maxPosX) maxPosX = nodePos.X;
                    if (nodePos.Z > maxPosZ) maxPosZ = nodePos.Z;
                    if (node.Edges.Count(val => val.Value > 0) == 0)
                    {
                        continue;
                    }
                    m_slices[node] = new Dictionary<PathGraphNode, Lazy<List<PathData>>>();
                    foreach (var graphNode in node.Edges)
                    {
                        if (graphNode.Value < 0)
                        {
                            continue;
                        }
                        m_slices[node][graphNode.Key] = new Lazy<List<PathData>>(() => GeneratePath(noise,
                            getHeightFunction, node,
                            graphNode.Key));
                        stack.Push(graphNode.Key);
                    }
                    
                }
                BoundingBox = new BoundingBox(new Vector3(minPosX - 1.5f, 0, minPosZ - 1.5f), new Vector3(maxPosX + 1.5f, 0, maxPosZ + 1.5f));
            }

            public List<PathData> GetPathLeg(PathGraphNode origin, PathGraphNode destination)
            {
                return m_slices[origin][destination].Value;
            }

            private List<PathData> GeneratePath(SimplexNoise2D noise, GetHeight getHeightFunction, PathGraphNode origin, PathGraphNode destination)
            {
                var originProjection = new Vector2(origin.Position.X, origin.Position.Z);
                var destinationProjection = new Vector2(destination.Position.X, destination.Position.Z);
                var gradient = Vector2.Subtract(destinationProjection, originProjection);
                var distance = gradient.LengthSquared();
                gradient.Normalize();
                //just checking if we'll get a NaN so the quality oprtator should be fine
// ReSharper disable CompareOfFloatsByEqualityOperator
                var deltaX = gradient.X == 0 ? 0 : 1 / Math.Abs(gradient.X);
                var deltaZ = gradient.Y == 0 ? 0 : 1 / Math.Abs(gradient.Y);
                if (deltaX == 0 && deltaZ == 0)
                {
                    return new List<PathData>();
                }
                var stepX = Math.Sign(gradient.X);
                var stepZ = Math.Sign(gradient.Y);
                var maxX = 0.0f;
                var maxZ = 0.0f;
                var x = (int)origin.Position.X;
                var z = (int)origin.Position.Z;
                var previousHeight = (int)origin.Position.Y;
                var path = new List<PathData>();
                do
                {
                    var node = BuildPathData(noise, x, z, 0, gradient, getHeightFunction, previousHeight);
                    path.Add(node);
                    previousHeight = (int)node.Position.Y;
                    if (deltaZ == 0 || (deltaX != 0 && maxX < maxZ))
                    {
                        maxX += deltaX;
                        x += stepX;
                    }
                    else
                    {
                        maxZ += deltaZ;
                        z += stepZ;
                    }
                } while (Vector2.DistanceSquared(originProjection, new Vector2(x, z)) <= distance);
// ReSharper restore CompareOfFloatsByEqualityOperator
                return path;
            }

            private PathData BuildPathData(SimplexNoise2D noise, int x, int z, int w, Vector2 gradient, GetHeight getHeightFunction, int previousHeight)
            {
                var radius = noise.Perlin(x, z) * 1 + 2;
                return  new PathData(radius, x, z, w, gradient, getHeightFunction, previousHeight);
            }

        }

        public class PathData
        {
            public readonly float Radius;
            public readonly Vector3 Position;
            public readonly uint Id;

            private static uint s_nextUid = 0;

            public PathData(float radius, int x, int z, int w, Vector2 gradient, GetHeight getHeightFunction, int previousHeight)
            {
                Radius = radius;
                Position = new Vector3(x, CalculateHeight(x, z, w, gradient, getHeightFunction, previousHeight), z);
                lock (this)
                {
                    Id = ++s_nextUid;
                }
            }

            private int CalculateHeight(int x, int z, int w, Vector2 gradient, GetHeight getHeightFunction, int previousHeight)
            {
                var expandedRadius = Radius*1.2f;
                var pos = new Vector2(x, z);
                var samplePoint0 = new Vector2(gradient.Y, - gradient.X);
                samplePoint0.Normalize();
                samplePoint0 = (samplePoint0 * expandedRadius) + pos;
                var samplePoint1 = new Vector2(-gradient.Y, gradient.X);
                samplePoint1.Normalize();
                samplePoint1 = (samplePoint1 * expandedRadius) + pos;

                var height0 = getHeightFunction(samplePoint0.X, samplePoint0.Y, w);
                var height1 = getHeightFunction(samplePoint1.X, samplePoint1.Y, w);
                var heightM = getHeightFunction(x, z, w);

                var nodeHeight = (int)(height0 < height1 ? height0 : height1);
                nodeHeight = (int) (heightM < nodeHeight ? heightM : nodeHeight);
                return nodeHeight < previousHeight ? nodeHeight : previousHeight;
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
