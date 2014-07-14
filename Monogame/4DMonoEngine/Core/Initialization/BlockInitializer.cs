using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Initialization
{
    class BlockInitializer :IInitializable
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
            return BlockDictionary.GetInstance().IsValidBlockId(0);
        }

        public void Initialize()
        {
            BlockDictionary.GetInstance();
        }
    }
}
