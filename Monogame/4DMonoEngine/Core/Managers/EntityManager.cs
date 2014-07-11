using System;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Interfaces;

namespace _4DMonoEngine.Core.Managers
{
    public class EntityManager
    {
        private static EntityManager s_instance;
        
        public static EntityManager GetInstance()
        {
            return s_instance ?? (s_instance = new EntityManager());
        }

        private EntityManager()
        {
            
        }

        public IEntity GetIntersection(Ray raycast, int p)
        {
            throw new NotImplementedException();
        }
    }
}
