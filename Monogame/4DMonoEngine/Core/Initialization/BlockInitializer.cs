using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Initialization
{
    public sealed class BlockInitializer :IInitializable
    {
        private readonly List<Type> m_dependencies;
        public BlockInitializer()
        {
            m_dependencies = new List<Type>();
        }

        public IEnumerable<Type> Dependencies()
        {
            return m_dependencies;
        }

        public bool IsInitialized()
        {
            return BlockDictionary.GetInstance().IsInitialized;
        }

        public void Initialize()
        {
            BlockDictionary.GetInstance();
        }
    }
}
