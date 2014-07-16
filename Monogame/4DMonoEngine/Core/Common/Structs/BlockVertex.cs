using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace _4DMonoEngine.Core.Common.Structs
{
    /// Represents block vertex.
    [Serializable]
    public struct BlockVertex : IVertexType
    {
        private Vector3 m_position;
        private HalfVector2 m_blockTextureCoordinate;
        private HalfVector4 m_light;

        public BlockVertex(Vector3 position, HalfVector2 blockTextureCoordinate, Byte4 light)
        {
            m_position = position;
            m_blockTextureCoordinate = blockTextureCoordinate;
            m_light = new HalfVector4(light.ToVector4()/255.0f);
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        private static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new[]
        {
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof (float)*3,VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate,0),
            new VertexElement(sizeof (float)*4, VertexElementFormat.HalfVector4,VertexElementUsage.Color, 0)
        });

        public Vector3 Position
        {
            get { return m_position; }
            set { m_position = value; }
        }

        public HalfVector2 BlockTextureCoordinate
        {
            get { return m_blockTextureCoordinate; }
            set { m_blockTextureCoordinate = value; }
        }

       public HalfVector4 Light
        {
            get { return m_light; }
            set { m_light = value; }
        }

        public static int SizeInBytes
        {
            //Byte4 isn't compatible with sizeof, but should be 4 bytes...
            get { return sizeof (float)*6; }
        }
    }
}