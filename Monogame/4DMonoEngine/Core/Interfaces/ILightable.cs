using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.Enums;

namespace _4DMonoEngine.Core.Interfaces
{
    public interface ILightable
    {
        byte LightSun { get; set;}
        byte LightRed { get; set; }
        byte LightGreen { get; set; }
        byte LightBlue { get; set; }
        float Opacity { get; }
        HalfVector2[] GetTextureMapping(FaceDirection faceDir);
        bool Exists { get; }
    }
}
