using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Utils.Noise;

namespace _4DMonoEngine.Core.Chunks.Generators.Structures
{

    //TODO : all of this really
    public class PathGenerator
    {
        protected static float RiverEndMaxHeight = 64;
        protected static float RiversStartMinHeight = 92;
        protected const float PathSectionNoiseScale = 1/64f;

        protected readonly List<List<PathData>> ActivePaths;
        protected readonly List<List<PathData>> CompletedPaths;
        protected readonly List<Dictionary<long, PathGraphNode>> PathNodes;
        protected readonly CellNoise3D CellNoise3D;

        public PathGenerator(uint seed)
        {
            CellNoise3D = new CellNoise3D(seed);
            PathNodes = new List<Dictionary<long, PathGraphNode>>();
            ActivePaths = new List<List<PathData>>();
            CompletedPaths = new List<List<PathData>>();
        }

        protected void BuildPathSections(int x, int z, int w)
        {
            var active = ActivePaths[w];
            var completed = CompletedPaths[w];
            var nodeMap = PathNodes[w];
            //determine if any new pathing regions are added
            var segmentation = CellNoise3D.Voroni(x * PathSectionNoiseScale, z * PathSectionNoiseScale, w * PathSectionNoiseScale);
            if (!nodeMap.ContainsKey(segmentation.Id))
            {
                var node = new PathGraphNode();
                node.position = new Vector3(x, z, w) + segmentation.Delta / PathSectionNoiseScale;
                //determine if there are any new sources or sinks in this group

            }

            //create new active paths

            //determine if any active paths can be expanded

            //add cells along each path (A*)

            //move any finished paths to the complete list	

        }


        protected List<PathGraphNode> AddNewPathNodes(Vector3 minPos, Vector3 maxPos)
        {
            var nodes = new List<PathGraphNode>();
            for (var x = (int)minPos.X; x < maxPos.X; ++x)
            {
                for (var z = (int)minPos.Y; z < maxPos.Y; ++z)
                {
                    for (var w = (int)minPos.Z; w < maxPos.Z; ++w)
                    {

                    }
                }
            }
            return nodes;
        }

        protected PathGraphNode BuildPathNode(int x, int z, int w, PathType type)
        {
            var nodeMap = PathNodes[w];
            //determine if any new pathing regions are added
            CellNoise3D.VoroniData segmentation;
            //TODO : do we need to generate different segmentations per path type?
            switch (type)
            {
                case PathType.ROAD:
                    segmentation = CellNoise3D.Voroni(x * PathSectionNoiseScale, z * PathSectionNoiseScale, w * PathSectionNoiseScale);
                    break;
                default:
                    segmentation = CellNoise3D.Voroni(x * PathSectionNoiseScale, z * PathSectionNoiseScale, w * PathSectionNoiseScale);
                    break;
            }
            //TODO : THIS MAY FAIL SINCE THE IDs MAY NOT BE UNIQUE BETWEEN GENERATORS
            if (!nodeMap.ContainsKey(segmentation.Id))
            {
                var node = new PathGraphNode();
                node.position = new Vector3(x, z, w) + segmentation.Delta / PathSectionNoiseScale;
                //determine if there are any new sources or sinks in this group
                return node;
            }
            return null;

        }



        public class PathGraphNode
        {
            public Dictionary<PathGraphNode, float> edges;
            public PathNodeType nodeType;
            public Vector3 position;
            public PathGraphNode()
            {
                edges = new Dictionary<PathGraphNode, float>();
                nodeType = PathNodeType.GENERAL;
            }

        }


        public class PathData
        {
            public PathType type;
            public List<Vector3> cells;
            public bool isBuildingForward;
            public PathGraphNode currentNode;
            public PathData(PathType type, bool isBuildingForward)
            {
                this.type = type;
                this.isBuildingForward = isBuildingForward;
                cells = new List<Vector3>();
            }
        }

        public enum PathNodeType
        {
            SOURCE,
            SINK,
            INVALID,
            GENERAL
        }

        public enum PathType
        {
            RIVER,
            ROAD
        }
    }
}
