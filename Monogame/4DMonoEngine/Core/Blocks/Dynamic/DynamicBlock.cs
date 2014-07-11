using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Common.AbstractClasses;

namespace _4DMonoEngine.Core.Blocks.Dynamic
{
    public class DynamicBlock : Updateable
    {
        private readonly List<DynamicBlockComponent> m_components; 
        
        public DynamicBlock(List<DynamicBlockComponent> components)
        {
            m_components = components;
        }

        public void AddComponent(DynamicBlockComponent component)
        {
            m_components.Add(component);
        }

        public void RemoveComponent(DynamicBlockComponent component)
        {
            m_components.Remove(component);
        }

        public void Dispose()
        {
            foreach (var component in m_components)
            {
                component.Dispose();
            }
        }

        public void Initialize()
        {
            foreach (var component in m_components)
            {
                component.Initialize();
            }
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var component in m_components)
            {
                component.Update(gameTime);
            }
            m_components.Sort();
        }

        public void Interact()
        {
            foreach (var component in m_components)
            {
                if(component.Interact())
                {
                    break;
                }
            }
        }

        //TODO : find way to override the properties of a block 
        //TODO : figure out how to do special rendering for a dynamic block
    }
}
