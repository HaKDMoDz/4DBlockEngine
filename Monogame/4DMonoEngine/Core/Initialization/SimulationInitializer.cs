using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Universe;

namespace _4DMonoEngine.Core.Initialization
{
    public sealed class SimulationInitializer :IInitializable
    {
        private readonly List<Type> m_dependencies;
        private Simulation m_sim;
        private readonly Game m_game;
        private readonly uint m_seed;
        public SimulationInitializer(Game game, uint seed)
        {
            m_game = game;
            m_seed = seed;
            m_dependencies = new List<Type>() {typeof(BlockInitializer)};
        }


        public IEnumerable<Type> Dependencies()
        {
            return m_dependencies;
        }

        public bool IsInitialized()
        {
            return m_sim != null;
        }

        public void Initialize()
        {
            m_sim = new Simulation(m_game, m_seed);
        }

        public Simulation GetSimulation()
        {
            return m_sim;
        }

    }
}
