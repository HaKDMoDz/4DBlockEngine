using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.AbstractClasses;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Events;
using _4DMonoEngine.Core.Events.Args;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Universe
{
    public class Player : Renderable, IEventSink, IEventSource
    {
        private const float MouseVelocity = 0.05f;
        public bool FlyingEnabled { get; set; }
        public IEquipable Equipable { get; set; }
        private Vector2 m_currentMousePosition;
        private Vector2 m_currentLookPolarVector;
        private uint m_controlBitVector;
        private const uint Jump = 1;
        private const Keys JumpKey = Keys.Space;
        private const uint Forward = 2;
        private const Keys ForwardKey = Keys.W;
        private const uint Right = 4;
        private const Keys RightKey = Keys.D;
        private const uint Backward = 8;
        private const Keys BackwardKey = Keys.S;
        private const uint Left = 16;
        private const Keys LeftKey = Keys.A;
        private readonly Block[] m_blocks;
        private readonly HashSet<string> m_eventsHandled;
        private readonly EventSource m_eventSourceImpl;
        private Vector3 m_velocity;
        private Vector3 m_lookVector;
        private Vector3 m_position;

        private const float MoveSpeed = 5f; // the move speed.
        private const float FlySpeed = 25f; // the fly speed.
        private const float Gravity = -15f;
        private const float JumpVelocity = 6f;

        private readonly Action<EventArgs> m_mousePositionUpdated;
        private readonly Action<EventArgs> m_keyDown;
        private readonly Action<EventArgs> m_keyUp;
        private readonly MappingFunctionVector3 m_mappingFunction;

        public Player(Block[] blocks, MappingFunctionVector3 mappingFunction)
        {
            FlyingEnabled = true;
            m_blocks = blocks;
            m_mappingFunction = mappingFunction;
            Equipable = new Shovel();
            m_eventsHandled = new HashSet<string>
            {
                EventConstants.MousePositionUpdated, 
                EventConstants.LeftMouseDown, 
                EventConstants.LeftMouseUp,
                EventConstants.RightMouseDown, 
                EventConstants.RightMouseUp,
                EventConstants.KeyDown,
                EventConstants.KeyUp
            };
            EventsFired = new[]
            {
                EventConstants.PlayerPositionUpdated,
                EventConstants.ViewUpdated
            };
            m_mousePositionUpdated = EventHelper.Wrap<Vector2Args>(OnMouseUpdated);
            m_keyDown = EventHelper.Wrap<KeyArgs>(OnKeyDown);
            m_keyUp = EventHelper.Wrap<KeyArgs>(OnKeyUp);
            m_eventSourceImpl = new EventSource(EventsFired, true);
        }

        public override void LoadContent()
        {
        }

        public override void Update(GameTime gameTime)
        {
            var deltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
            ProcessPosition(deltaTime);
            ProcessView(deltaTime);
        }

        private void ProcessPosition(float deltaTime)
        {
            var footPosition = m_position + new Vector3(0f, -1.5f, 0f);
            if (!FlyingEnabled)
            {
                m_velocity.Y += Gravity * deltaTime;
                var standingBlock = m_blocks[m_mappingFunction(ref footPosition)];
                if (standingBlock.Exists)
                {
                    if ((m_controlBitVector & Jump) != 0)
                    {
                        var amountBelowSurface = ((ushort) footPosition.Y) + 1 - footPosition.Y;
                        m_position += new Vector3(0, amountBelowSurface + 0.01f, 0);
                        m_velocity.Y = JumpVelocity;
                    }
                    else
                    {
                        m_velocity.Y = 0;
                    }
                } 
            }
            var moveVector = Vector3.Zero;
            if ((m_controlBitVector & Forward) != 0)
            {
                 moveVector.Z--;
            }
            if ((m_controlBitVector & Right) != 0)
            {
                moveVector.X++;
            }
            if ((m_controlBitVector & Backward) != 0)
            {
                moveVector.Z++;
            }
            if ((m_controlBitVector & Left) != 0)
            {
                moveVector.X--;
            }
            if (moveVector != Vector3.Zero)
            {
                if (FlyingEnabled)
                {
                    var rotation = Matrix.CreateRotationX(m_currentLookPolarVector.Y) * Matrix.CreateRotationY(m_currentLookPolarVector.X);
                    Vector3.Transform(ref moveVector, ref rotation, out moveVector);
                    m_velocity = moveVector * FlySpeed;
                }
                else
                {
                    m_velocity.X = moveVector.X * MoveSpeed;
                    m_velocity.Z = moveVector.Z * MoveSpeed;
                }
            }
            else
            {
                if (FlyingEnabled)
                {
                    m_velocity = Vector3.Zero;
                }
                else
                {
                    m_velocity.X = 0;
                    m_velocity.Z = 0;
                }
            }
            if (m_velocity != Vector3.Zero)
            {
                var nextPosition = m_position + m_velocity * deltaTime;
                if (!FlyingEnabled && CheckCollision(ref nextPosition))
                {
                    ResolveCollision(ref nextPosition, out nextPosition);
                }
                if ((m_position - nextPosition).LengthSquared() > 0)
                {
                    m_position = nextPosition;
                    m_eventSourceImpl.FireEvent(EventConstants.PlayerPositionUpdated, new Vector3Args(m_position));
                }
            }
        }

        private bool CheckCollision(ref Vector3 nextPosition)
        {
            var index = m_mappingFunction(ref nextPosition);
            return m_blocks[index].Exists || m_blocks[index + 1].Exists;
        }

        private void ResolveCollision(ref Vector3 positionIn, out Vector3 positionOut)
        {
            var index = m_mappingFunction(ref positionIn);
            positionOut = positionIn;
            if ((m_velocity.X > 0 && m_blocks[index + ChunkCache.BlockStepX].Exists) || (m_velocity.X < 0 && m_blocks[index - ChunkCache.BlockStepX].Exists))
            {
                positionOut.X = m_position.X;
            }
            if ((m_velocity.Y > 0 && m_blocks[index + 1].Exists) || (m_velocity.Y < 0 && m_blocks[index - 1].Exists))
            {
                positionOut.Y = m_position.Y;
            }
            if ((m_velocity.Z > 0 && m_blocks[index + ChunkCache.BlockStepZ].Exists) || (m_velocity.Z < 0 && m_blocks[index - ChunkCache.BlockStepZ].Exists))
            {
                positionOut.Z = m_position.Z;
            }
        }

        private void ProcessView(float deltaTime)
        {
            if (m_currentMousePosition != Vector2.Zero)
            {
                m_currentLookPolarVector.X -= deltaTime * m_currentMousePosition.X * MouseVelocity;
                m_currentLookPolarVector.Y -= deltaTime * m_currentMousePosition.Y * MouseVelocity;
                m_currentLookPolarVector.Y = MathHelper.Clamp(m_currentLookPolarVector.Y, -3.1415f/2, 3.1415f/2);
                var rotationMatrix = Matrix.CreateRotationX(m_currentLookPolarVector.Y) * Matrix.CreateRotationY(m_currentLookPolarVector.X);
                m_lookVector = Vector3.Transform(Vector3.Forward, rotationMatrix);
                m_lookVector.Normalize();
                m_eventSourceImpl.FireEvent(EventConstants.ViewUpdated, new Vector3Args(m_lookVector));
            }
        }

        public void SpawnPlayer(Vector2Int relativePosition)
        {
            m_position = new Vector3(relativePosition.X * Chunk.SizeInBlocks, 100, relativePosition.Z * Chunk.SizeInBlocks);
            m_eventSourceImpl.FireEvent(EventConstants.PlayerPositionUpdated, new Vector3Args(m_position));
            m_eventSourceImpl.FireEvent(EventConstants.ViewUpdated, new Vector3Args(m_lookVector));
        }
       

        public bool CanHandleEvent(string eventName)
        {
            return m_eventsHandled.Contains(eventName);
        }

        public Action<EventArgs> GetHandlerForEvent(string eventName)
        {
            switch (eventName)
            {
                case EventConstants.KeyDown:
                    return m_keyDown;
                case EventConstants.KeyUp:
                    return m_keyUp;
                case EventConstants.MousePositionUpdated:
                    return m_mousePositionUpdated;
                default:
                    return null;
            }
        }

        private void OnMouseUpdated(Vector2Args obj)
        {
            m_currentMousePosition = obj.Vector;
        }

        private void OnKeyUp(KeyArgs key)
        {
            switch (key.KeyCode)
            {
                case JumpKey:
                    m_controlBitVector &= ~Jump;
                    break;
                case ForwardKey:
                    m_controlBitVector &= ~Forward;
                    break;
                case RightKey:
                    m_controlBitVector &= ~Right;
                    break;
                case BackwardKey:
                    m_controlBitVector &= ~Backward;
                    break;
                case LeftKey:
                    m_controlBitVector &= ~Left;
                    break;
            }
        }

        private void OnKeyDown(KeyArgs key)
        {
            switch (key.KeyCode)
            {
                case JumpKey:
                    m_controlBitVector |= Jump;
                    break;
                case ForwardKey:
                    m_controlBitVector |= Forward;
                    break;
                case RightKey:
                    m_controlBitVector |= Right;
                    break;
                case BackwardKey:
                    m_controlBitVector |= Backward;
                    break;
                case LeftKey:
                    m_controlBitVector |= Left;
                    break;
            }
        }

        public IEnumerable<string> EventsFired { get; private set; }
        public bool EventsEnabled
        {
            get { return m_eventSourceImpl.EventsEnabled; }
            set { m_eventSourceImpl.EventsEnabled = value; }
        }

        public void Register(string eventName, Action<EventArgs> handler)
        {
            m_eventSourceImpl.Register(eventName, handler);
        }

        public void Unregister(string eventName, Action<EventArgs> handler)
        {
            m_eventSourceImpl.Unregister(eventName, handler);
        }

        public override void Draw(GameTime gameTime)
        {
            //TODO : render held tool
            //if (AimedSolidBlock.HasValue) RenderAimedBlock();
        }

        /* private void FindAimedBlock()
        {
            for (var x = 0.5f; x < 8f; x += 0.1f)
            {
                var target = _camera.Position + (LookVector*x);
                var block = _blockStorage.BlockAt(target);
                if (!block.Exists) AimedEmptyBlock = new PositionedBlock(new Vector3Int(target), block);
                else
                {
                    AimedSolidBlock = new PositionedBlock(new Vector3Int(target), block);
                    return;
                }
            }

            AimedSolidBlock = null;
        }*/


        /*private void RenderAimedBlock()
        {
            Game.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                // allows any transparent pixels in original PNG to draw transparent
            Game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            var position = AimedSolidBlock.Value.Position.AsVector3() + new Vector3(0.5f, 0.5f, 0.5f);
            Matrix matrix_a, matrix_b;
            var identity = Matrix.Identity; // setup the matrix prior to translation and scaling  
            Matrix.CreateTranslation(ref position, out matrix_a);
                // translate the position a half block in each direction
            Matrix.CreateScale(0.505f, out matrix_b);
                // scales the selection box slightly larger than the targetted block
            identity = Matrix.Multiply(matrix_b, matrix_a); // the final position of the block

            _aimedBlockEffect.World = identity;
            _aimedBlockEffect.View = _camera.View;
            _aimedBlockEffect.Projection = _camera.Projection;
            _aimedBlockEffect.Texture = _aimedBlockTexture;
            _aimedBlockEffect.TextureEnabled = true;

            foreach (var pass in _aimedBlockEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                for (var i = 0; i < _aimedBlockModel.Meshes[0].MeshParts.Count; i++)
                {
                    var parts = _aimedBlockModel.Meshes[0].MeshParts[i];
                    if (parts.NumVertices == 0) continue;

                    Game.GraphicsDevice.Indices = parts.IndexBuffer;
                    Game.GraphicsDevice.SetVertexBuffer(parts.VertexBuffer);
                    Game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, parts.NumVertices,
                                                              parts.StartIndex, parts.PrimitiveCount);
                }
            }
        }*/
    }
}