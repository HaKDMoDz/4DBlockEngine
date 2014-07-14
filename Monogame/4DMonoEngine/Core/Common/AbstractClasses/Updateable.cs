using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Common.AbstractClasses
{
    public abstract class Updateable : IUpdateable
    {
        private bool m_enabled = true;
        private int m_updateOrder;

        public Game Game { get; private set; }

        public bool Enabled
        {
          get
          {
            return m_enabled;
          }
          set
          {
            if (m_enabled == value)
              return;
            m_enabled = value;
            if (EnabledChanged != null)
              EnabledChanged(this, EventArgs.Empty);
            OnEnabledChanged();
          }
        }

        public int UpdateOrder
        {
          get
          {
            return m_updateOrder;
          }
          set
          {
            if (m_updateOrder == value)
              return;
            m_updateOrder = value;
            if (UpdateOrderChanged != null)
              UpdateOrderChanged(this, EventArgs.Empty);
            OnUpdateOrderChanged();
          }
        }

        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> UpdateOrderChanged;

        public virtual void LoadContent() {}

        public abstract void Update(GameTime gameTime);

        protected virtual void OnUpdateOrderChanged() {}
        protected virtual void OnEnabledChanged() {}
    }
}
