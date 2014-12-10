using System;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.Blocks.Dynamic;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Blocks
{
    [Serializable]
    public struct Block : ILightable, ISerializable
    {
        public bool Equals(Block other)
        {
            return m_type == other.m_type && m_color == other.m_color && m_lightSun == other.m_lightSun && m_lightRed == other.m_lightRed && m_lightGreen == other.m_lightGreen && m_lightBlue == other.m_lightBlue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Block && Equals((Block) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_type.GetHashCode();
                hashCode = (hashCode*397) ^ m_color.GetHashCode();
                hashCode = (hashCode*397) ^ m_lightSun.GetHashCode();
                hashCode = (hashCode*397) ^ m_lightRed.GetHashCode();
                hashCode = (hashCode*397) ^ m_lightGreen.GetHashCode();
                hashCode = (hashCode*397) ^ m_lightBlue.GetHashCode();
                return hashCode;
            }
        }

        public const ushort None = 0;
        //private DynamicBlock m_dynamicBlockData;
        private readonly ushort m_type;
        private ushort m_color;
        private byte m_lightSun;
        private byte m_lightRed;
        private byte m_lightGreen;
        private byte m_lightBlue;
        public Block(ushort type)
        {
          //  m_dynamicBlockData = null;
            m_type = type;
            m_color = (15 << 8) | (15 << 4) | 15;
            m_lightSun = 0;
            m_lightRed = 0;
            m_lightGreen = 0;
            m_lightBlue = 0;
        }

        public Block(SerializationInfo info, StreamingContext context)
        {
            m_type = info.GetUInt16("t");
            m_color = (15 << 8) | (15 << 4) | 15;
            m_lightSun = info.GetByte("ls");
            m_lightRed = info.GetByte("lr");
            m_lightGreen = info.GetByte("lg");
            m_lightBlue = info.GetByte("lb");
        }

        public static Block Empty
        {
            get { return new Block(None); }
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

        public Color GetTint()
        {
            var red = ((m_color >> 8) & 15) * 16;
            var green = ((m_color >> 4) & 15) * 16;
            var blue = (m_color & 15) * 16;
            return new Color(red, green, blue);
        }

        public ushort Color
        {
            get { return m_color; }
            set { m_color = value; }
        }

        public ushort Type
        {
            get { return m_type; }
        }

        public HalfVector2[] GetTextureMapping(FaceDirection faceDir)
        {
            return BlockDictionary.Instance.GetTextureMapping(Type, faceDir);
        }

        public bool Exists
        {
            get { return Type != None; }
        }

        public float Opacity
        {
            get { return BlockDictionary.Instance.GetStaticData(Type).Opacity; }
        }

        public byte EmissivitySun
        {
            get { return BlockDictionary.Instance.GetStaticData(Type).EmissivitySun; }
        }

        public byte EmissivityRed
        {
            get { return BlockDictionary.Instance.GetStaticData(Type).EmissivityRed; }
        }

        public byte EmissivityGreen
        {
            get { return BlockDictionary.Instance.GetStaticData(Type).EmissivityGreen; }
        }

        public byte EmissivityBlue
        {
            get { return BlockDictionary.Instance.GetStaticData(Type).EmissivityBlue; }
        }


        public static bool operator ==(Block b1, Block b2)
        {
            return b1.Type == b2.Type &&
                   b1.LightSun == b2.LightSun &&
                   b1.LightRed == b2.LightRed &&
                   b1.LightGreen == b2.LightGreen &&
                   b1.LightBlue == b2.LightBlue &&
                   b1.Color == b2.Color;
        }

        public static bool operator !=(Block b1, Block b2)
        {
            return b1.Type != b2.Type ||
                   b1.LightSun != b2.LightSun ||
                   b1.LightRed != b2.LightRed ||
                   b1.LightGreen != b2.LightGreen ||
                   b1.LightBlue != b2.LightBlue ||
                   b1.Color != b2.Color;
        }


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("t", Type);
            info.AddValue("ls", LightSun);
            info.AddValue("lr", LightRed);
            info.AddValue("lg", LightGreen);
            info.AddValue("lb", LightBlue);
            info.AddValue("c", Color);
        }
    }
}