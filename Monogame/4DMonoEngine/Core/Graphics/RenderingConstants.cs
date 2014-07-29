using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Graphics
{
    public static class RenderingConstants
    {
        public static readonly Vector4 NightColor = Color.Navy.ToVector4();
        public static readonly Vector4 DayColor = Color.White.ToVector4();
        public static readonly Vector4 EveningTint = Color.Red.ToVector4();
        public static readonly Vector4 MorningTint = Color.Gold.ToVector4();

        public static readonly Vector4 NightHorizonColor = Color.Black.ToVector4();
        public static readonly Vector4 DayHorizonColor = Color.DarkGray.ToVector4();
       // public static readonly Vector4 EveningHorizonTint = Color.DarkGray.ToVector4();
        //public static readonly Vector4 MorningHorizonTint = Color.DarkGray.ToVector4();
    }
}
