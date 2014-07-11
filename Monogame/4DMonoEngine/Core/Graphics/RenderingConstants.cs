using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Graphics
{
    public static class RenderingConstants
    {
        public static readonly Vector4 NightColor = Color.Black.ToVector4();
        public static readonly Vector4 SunColor = Color.White.ToVector4();
        public static readonly Vector4 HorizonColor = Color.DarkGray.ToVector4();
        public static readonly Vector4 EveningTint = Color.Red.ToVector4();
        public static readonly Vector4 MorningTint = Color.Gold.ToVector4();
    }
}
