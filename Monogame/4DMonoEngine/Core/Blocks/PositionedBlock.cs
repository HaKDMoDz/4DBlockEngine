

using _4DMonoEngine.Core.Common.Vector;

namespace _4DMonoEngine.Core.Common.Enums
{
    public struct PositionedBlock
    {
        public readonly Vector3Int Position;
        public readonly Block Block;

        public PositionedBlock(Vector3Int position, Block block)
        {
            Position = position;
            Block = block;
        }
    }
}