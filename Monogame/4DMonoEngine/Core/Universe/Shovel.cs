using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Universe
{
    public class Shovel : IEquipable
    {
        public Shovel()
        {
        }

        public IAction Use(int actionIndex)
        {
            return null;
        }

        public int InteractionDistance { get; private set; }
    }
}