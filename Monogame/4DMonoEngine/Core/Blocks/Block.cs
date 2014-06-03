using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.Blocks.Dynamic;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Blocks
{
    public struct Block : ILightable
    {
        public static ushort None = 0;
        public DynamicBlock DynamicBlockData;
        public ushort MetaObjectId;
        public ushort Type;
        private byte m_lightSun;
        private byte m_lightRed;
        private byte m_lightGreen;
        private byte m_lightBlue;
        public Block(ushort type, ushort metaObjectId)
        {
            DynamicBlockData = null;
            Type = type;
            MetaObjectId = metaObjectId;
            m_lightSun = 255;
            m_lightRed = 0;
            m_lightGreen = 0;
            m_lightBlue = 0;
        }

        public static Block Empty
        {
            get { return new Block(None, 0); }
        }

        public byte LightSun
        {
            get { return m_lightSun; }
            set { m_lightSun = value; }
        }

        public byte LightRed
        {
            get { return m_lightRed; }
            set { m_lightRed = value; }
        }

        public byte LightGreen
        {
            get { return m_lightGreen; }
            set { m_lightGreen = value; }
        }

        public byte LightBlue
        {
            get { return m_lightBlue; }
            set { m_lightBlue = value; }
        }

        public HalfVector2[] GetTextureMapping(FaceDirection faceDir)
        {
            return BlockDictionary.GetInstance().GetTextureMapping(Type, faceDir);
        }

        public bool Exists
        {
            get { return Type != None; }
        }

        public float Opacity
        {
            get { return BlockDictionary.GetInstance().GetStaticData(Type).Opacity; }
        }
    }
}