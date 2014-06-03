using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Common.Enums
{
    public abstract class Renderable : Updateable, IDrawable
    {
        private bool m_visible = true;
        private int m_drawOrder;

        public int DrawOrder
        {
            get
            {
                return m_drawOrder;
            }
            set
            {
                if (m_drawOrder != value)
                {
                    m_drawOrder = value;
                    if (DrawOrderChanged != null)
                        DrawOrderChanged(this, null);
                    OnDrawOrderChanged(this, null);
                }
            }
        }

        public bool Visible
        {
            get
            {
                return m_visible;
            }
            set
            {
                if (m_visible != value)
                {
                    m_visible = value;
                    if (VisibleChanged != null)
                    {
                        VisibleChanged(this, EventArgs.Empty);
                    }
                    OnVisibleChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler<EventArgs> DrawOrderChanged;
        public event EventHandler<EventArgs> VisibleChanged;
        public abstract void Draw(GameTime gameTime);

        protected virtual void OnVisibleChanged(object sender, EventArgs args) {}
        protected virtual void OnDrawOrderChanged(object sender, EventArgs args) {}
    }
}
