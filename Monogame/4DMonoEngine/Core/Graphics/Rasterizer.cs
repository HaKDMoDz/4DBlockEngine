using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Graphics
{
    public class Rasterizer
    {
        public bool Wireframe
        {
            get { return State == WireframedRaster; }
            set { State = value ? WireframedRaster : NormalRaster; }
        }

        public RasterizerState State { get; private set; }

        public Rasterizer()
        {
            Wireframe = false;
        }

        private static readonly RasterizerState WireframedRaster = new RasterizerState()
        {
            CullMode = CullMode.CullCounterClockwiseFace,
            FillMode = FillMode.WireFrame
        };

        private static readonly RasterizerState NormalRaster = new RasterizerState()
        {
            CullMode = CullMode.CullCounterClockwiseFace,
            FillMode = FillMode.Solid
        };
    }
}