using System.Runtime.Serialization;
using _4DMonoEngine.Core.Enums;

namespace _4DMonoEngine.Core.Config
{
    [DataContract]
    public class BlockTexture
    {
        [DataMember]
        public int[] FaceTextureIds { get; set; }
        public int GetTextureForFace(FaceDirection facing)
        {
            return FaceTextureIds[(int) facing];
        }
    }
}