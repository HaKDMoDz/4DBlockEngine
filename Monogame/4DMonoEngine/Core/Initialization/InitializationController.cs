using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Initialization
{
    class InitializationController
    {
        private readonly List<IInitializable> m_initializables;
        private Task m_initializationTask;
        private bool m_isComplete;

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

        public bool IsComplete()
        {
            return m_isComplete;
        }

        public void Run()
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
                m_initializationTask = Task.Run(() =>
                {
                    IInitializable current = null;
                    while (initializationChain.Count > 0)
                    {
                        if (current != null && !current.IsInitialized())
                        {
                            Thread.Sleep(10);
                            continue;
                        }
                        current = initializationChain.Dequeue();
                        current.Initialize();
                    }
                    Debug.Assert(current != null, "current unexpectedly null");
                    while (!current.IsInitialized())
                    {
                        Thread.Sleep(10);
                    }
                    m_isComplete = true;
                });
            }
            else
            {
                m_isComplete = true;
            }
        }
    }
}
