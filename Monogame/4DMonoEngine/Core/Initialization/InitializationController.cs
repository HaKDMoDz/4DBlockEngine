using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Initialization
{
    public class InitializationController
    {
        public delegate void InitializationHandler();
        private readonly List<IInitializable> m_initializables;

        public InitializationController()
        {
            m_initializables = new List<IInitializable>();
        }

        public InitializationController(IEnumerable<IInitializable> entries)
        {
            m_initializables = new List<IInitializable>(entries);
        }
        public void AddEntry(IInitializable entry)
        {
            m_initializables.Add(entry);
        }

        public async Task<bool> Run()
        {
            var chained = new HashSet<Type>();
            var initializationChain = new Queue<IInitializable>();
            while (m_initializables.Count > 0)
            {
                var removeIndex = -1;
                for (var i = 0; i < m_initializables.Count; i++)
                {
                    var dependencies = m_initializables[i].Dependencies();
                    if (dependencies.All(chained.Contains))
                    {
                        removeIndex = i;
                    }
                }
                Debug.Assert(removeIndex != -1, "Cannot resolve depenendencies!");
                var initializable = m_initializables[removeIndex];
                m_initializables.RemoveAt(removeIndex);
                chained.Add(initializable.GetType());
                initializationChain.Enqueue(initializable);
            }
            if (initializationChain.Count > 0)
            {
                return await Task.Run(() =>
                {
                    var current = new Dictionary<Type, IInitializable>();

                    while (initializationChain.Count > 0)
                    {
                        var dependencies = initializationChain.Peek().Dependencies();
                        if (!dependencies.All(dep => current[dep].IsInitialized()))
                        {
                            Thread.Sleep(10);
                            continue;
                        }
                        var next = initializationChain.Dequeue();
                        next.Initialize();
                        current[next.GetType()] = next;
                    }

                    while (current.Any(pair => !pair.Value.IsInitialized()))
                    {
                        Thread.Sleep(10);
                    }
                    return true;
                });
            }
            return true;
        }
    }
}
