using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Generators;

namespace _4DMonoEngine.Core.Initialization
{
    public sealed class TerrainGeneratorInitializer :IInitializable
    {
        private readonly List<Type> m_dependencies;
        private readonly uint m_seed;
        public TerrainGeneratorInitializer(uint seed)
        {
           m_dependencies = new List<Type>() { typeof(BlockInitializer)};
           m_seed = seed;
        }

        public IEnumerable<Type> Dependencies()
        {
            return m_dependencies;
        }

        public bool IsInitialized()
        {
            return TerrainGenerator.Instance.IsInitialized;
        }

        public void Initialize()
        {
            TerrainGenerator.Instance.Initialize(m_seed);
        }
    }
}
