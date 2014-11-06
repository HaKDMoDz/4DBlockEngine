using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.Common.Enums;

namespace _4DMonoEngine.Core.Common.Interfaces
{
    public interface ILightable
    {
        byte LightSun { get; set;}
        byte LightRed { get; set; }
        byte LightGreen { get; set; }
        byte LightBlue { get; set; }
        Color GetTint();
        float Opacity { get; }
        HalfVector2[] GetTextureMapping(FaceDirection faceDir);
        bool Exists { get; }
        byte EmissivitySun { get; }
        byte EmissivityRed { get; }
        byte EmissivityGreen { get; }
        byte EmissivityBlue { get; }
    }
}
