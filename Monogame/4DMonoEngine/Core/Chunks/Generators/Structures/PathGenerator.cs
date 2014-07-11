using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Common.Noise;

namespace _4DMonoEngine.Core.Chunks.Generators.Structures
{

    //TODO : all of this really
    public class PathGenerator
    {
        protected static float s_riverEndMaxHeight = 64;
        protected static float s_riversStartMinHeight = 92;
        protected static float s_pathSectionNoiseScale = 1 / 64f;

        protected List<List<PathData>> activePaths;
        protected List<List<PathData>> completedPaths;
        protected List<Dictionary<long, PathGraphNode>> pathNodes;
        protected CellNoise cellNoise;

        public PathGenerator(uint seed)
        {
            cellNoise = new CellNoise(seed);
            pathNodes = new List<Dictionary<long, PathGraphNode>>();
            activePaths = new List<List<PathData>>();
            completedPaths = new List<List<PathData>>();
        }

        protected void BuildPathSections(int x, int z, int w)
        {
            List<PathData> active = activePaths[w];
            List<PathData> completed = completedPaths[w];
            Dictionary<long, PathGraphNode> nodeMap = pathNodes[w];
            //determine if any new pathing regions are added
            CellNoise.VoroniData segmentation = cellNoise.Voroni(x * s_pathSectionNoiseScale, z * s_pathSectionNoiseScale, w * s_pathSectionNoiseScale);
            if (!nodeMap.ContainsKey(segmentation.id))
            {
                PathGraphNode node = new PathGraphNode();
                node.position = new Vector3(x, z, w) + segmentation.delta / s_pathSectionNoiseScale;
                //determine if there are any new sources or sinks in this group

            }

            //create new active paths

            //determine if any active paths can be expanded

            //add cells along each path (A*)

            //move any finished paths to the complete list	

        }


        protected List<PathGraphNode> AddNewPathNodes(Vector3 minPos, Vector3 maxPos)
        {
            List<PathGraphNode> nodes = new List<PathGraphNode>();
            for (int x = (int)minPos.X; x < maxPos.X; ++x)
            {
                for (int z = (int)minPos.Y; z < maxPos.Y; ++z)
                {
                    for (int w = (int)minPos.Z; w < maxPos.Z; ++w)
                    {

                    }
                }
            }
            return nodes;
        }

        protected PathGraphNode BuildPathNode(int x, int z, int w, PathType type)
        {
            Dictionary<long, PathGraphNode> nodeMap = pathNodes[w];
            //determine if any new pathing regions are added
            CellNoise.VoroniData segmentation;
            //TODO : do we need to generate different segmentations per path type?
            switch (type)
            {
                case PathType.ROAD:
                    segmentation = cellNoise.Voroni(x * s_pathSectionNoiseScale, z * s_pathSectionNoiseScale, w * s_pathSectionNoiseScale);
                    break;
                default:
                    segmentation = cellNoise.Voroni(x * s_pathSectionNoiseScale, z * s_pathSectionNoiseScale, w * s_pathSectionNoiseScale);
                    break;
            }
            //TODO : THIS MAY FAIL SINCE THE IDs MAY NOT BE UNIQUE BETWEEN GENERATORS
            if (!nodeMap.ContainsKey(segmentation.id))
            {
                PathGraphNode node = new PathGraphNode();
                node.position = new Vector3(x, z, w) + segmentation.delta / s_pathSectionNoiseScale;
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
