using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Pages;

namespace _4DMonoEngine.Core.Initialization
{
    public sealed class SaveSystemInitializer :IInitializable
    {
        private readonly List<Type> m_dependencies;
        public SaveSystemInitializer()
        {
            m_dependencies = new List<Type>();
        }

        public IEnumerable<Type> Dependencies()
        {
            return m_dependencies;
        }

        public bool IsInitialized()
        {
            return PageManager.Instance.IsInitialized;
        }

        public void Initialize()
        {
            PageManager.Instance.Initialize();
        }
    }
}
