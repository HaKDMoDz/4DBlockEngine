using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.Vector;
using _4DMonoEngine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Universe
{
    public class Player : DrawableGameComponent
    {
        public bool FlyingEnabled { get; set; }
        public Vector3 Position { get; private set; }

        public Equipable Equipable { get; set; }
        public Vector3 LookVector { get; set; }
        public PositionedBlock? AimedSolidBlock { get; private set; } // nullable object.        
        public PositionedBlock? AimedEmptyBlock { get; private set; } // nullable object.        
        public Vector3 Velocity;

        private BasicEffect _aimedBlockEffect;
        private Model _aimedBlockModel;
        private Texture2D _aimedBlockTexture;

        private const float MoveSpeed = 5f; // the move speed.
        private const float FlySpeed = 25f; // the fly speed.
        private const float Gravity = -15f;
        private const float JumpVelocity = 6f;

        // required services.
        private ICamera _camera;
        private IAssetManager _assetManager;
        
        public Player(Game game, World world)
            : base(game)
        {
            _world = world;
        }

        public override void Initialize()
        {
            FlyingEnabled = true;
            Equipable = new Shovel(Game);

            // import required services.
            _camera = (ICamera) Game.Services.GetService(typeof (ICamera));

            _assetManager = (IAssetManager)Game.Services.GetService(typeof(IAssetManager));

            LoadContent();

            Equipable.Initialize();
        }

        protected override void LoadContent()
        {
            _aimedBlockModel = _assetManager.AimedBlockModel;
            _aimedBlockEffect = _assetManager.AimedBlockEffect;
            _aimedBlockTexture = _assetManager.AimedBlockTexture;
            _sampleModel = _assetManager.SampleModel;
        }

        public override void Update(GameTime gameTime)
        {
            ProcessPosition(gameTime);
            ProcessView();
        }

        private void ProcessPosition(GameTime gameTime)
        {
            if (FlyingEnabled) return;

            Velocity.Y += Gravity*(float) gameTime.ElapsedGameTime.TotalSeconds;
            var footPosition = Position + new Vector3(0f, -1.5f, 0f);
            Block standingBlock = _blockStorage.BlockAt(footPosition);

            if (standingBlock.Exists) Velocity.Y = 0;
            Position += Velocity*(float) gameTime.ElapsedGameTime.TotalSeconds;
        }

        private void ProcessView()
        {
            if (FlyingEnabled) return;
            var rotationMatrix = Matrix.CreateRotationX(_camera.CurrentElevation)*
                                 Matrix.CreateRotationY(_camera.CurrentRotation);
            LookVector = Vector3.Transform(Vector3.Forward, rotationMatrix);
            LookVector.Normalize();
            FindAimedBlock();
        }

        public void Jump()
        {
            var footPosition = Position + new Vector3(0f, -1.5f, 0f);
            Block standingBlock = _blockStorage.BlockAt(footPosition);

            if (!standingBlock.Exists && Velocity.Y != 0) return;
            float amountBelowSurface = ((ushort) footPosition.Y) + 1 - footPosition.Y;
            Position += new Vector3(0, amountBelowSurface + 0.01f, 0);

            Velocity.Y = JumpVelocity;
        }

        public void Move(GameTime gameTime, MoveDirection direction)
        {
            var moveVector = Vector3.Zero;

            switch (direction)
            {
                case MoveDirection.Forward:
                    moveVector.Z--;
                    break;
                case MoveDirection.Backward:
                    moveVector.Z++;
                    break;
                case MoveDirection.Left:
                    moveVector.X--;
                    break;
                case MoveDirection.Right:
                    moveVector.X++;
                    break;
            }

            if (moveVector == Vector3.Zero) return;

            if (!FlyingEnabled)
            {
                moveVector *= MoveSpeed*(float) gameTime.ElapsedGameTime.TotalSeconds;
                var rotation = Matrix.CreateRotationY(_camera.CurrentRotation);
                var rotatedVector = Vector3.Transform(moveVector, rotation);
                TryMove(rotatedVector);
            }
            else
            {
                moveVector *= FlySpeed*(float) gameTime.ElapsedGameTime.TotalSeconds;
                var rotation = Matrix.CreateRotationX(_camera.CurrentElevation)*
                               Matrix.CreateRotationY(_camera.CurrentRotation);
                var rotatedVector = Vector3.Transform(moveVector, rotation);
                Position += (rotatedVector);
            }
        }

        private void TryMove(Vector3 moveVector)
        {
            // build a test move-vector slightly longer than moveVector.
            Vector3 testVector = moveVector;
            testVector.Normalize();
            testVector *= moveVector.Length() + 0.3f;
            var footPosition = Position + new Vector3(0f, -0.5f, 0f);
            Vector3 testPosition = footPosition + testVector;
            if (_blockStorage.BlockAt(testPosition).Exists) return;

            // There should be some bounding box so his head does not enter a block above ;) /fasbat
            testPosition -= 2*new Vector3(0f, -0.5f, 0f);
            if (_blockStorage.BlockAt(testPosition).Exists) return;


            Position += moveVector;
        }

        public void SpawnPlayer(Vector2Int relativePosition)
        {
            RelativePosition = relativePosition;
            Position = new Vector3(relativePosition.X * Chunk.SizeInBlocks, 150,
                                        relativePosition.Z * Chunk.LengthInBlocks);
            _world.SpawnPlayer(relativePosition);
        }

        private void FindAimedBlock()
        {
            for (float x = 0.5f; x < 8f; x += 0.1f)
            {
                Vector3 target = _camera.Position + (LookVector*x);
                var block = _blockStorage.BlockAt(target);
                if (!block.Exists) AimedEmptyBlock = new PositionedBlock(new Vector3Int(target), block);
                else
                {
                    AimedSolidBlock = new PositionedBlock(new Vector3Int(target), block);
                    return;
                }
            }

            AimedSolidBlock = null;
        }

        public override void Draw(GameTime gameTime)
        {
            if (AimedSolidBlock.HasValue) RenderAimedBlock();
        }

        private void RenderAimedBlock()
        {
            Game.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                // allows any transparent pixels in original PNG to draw transparent
            Game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            var position = AimedSolidBlock.Value.Position.AsVector3() + new Vector3(0.5f, 0.5f, 0.5f);
            Matrix matrix_a, matrix_b;
            Matrix identity = Matrix.Identity; // setup the matrix prior to translation and scaling  
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

            foreach (EffectPass pass in _aimedBlockEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                for (int i = 0; i < _aimedBlockModel.Meshes[0].MeshParts.Count; i++)
                {
                    ModelMeshPart parts = _aimedBlockModel.Meshes[0].MeshParts[i];
                    if (parts.NumVertices == 0) continue;

                    Game.GraphicsDevice.Indices = parts.IndexBuffer;
                    Game.GraphicsDevice.SetVertexBuffer(parts.VertexBuffer);
                    Game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, parts.NumVertices,
                                                              parts.StartIndex, parts.PrimitiveCount);
                }
            }
        }

        public void ToggleFlyForm()
        {
            FlyingEnabled = !FlyingEnabled;
        }
    }

    public enum MoveDirection
    {
        Forward,
        Backward,
        Left,
        Right,
    }
}