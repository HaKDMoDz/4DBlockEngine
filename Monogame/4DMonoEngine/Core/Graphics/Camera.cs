﻿

using _4DMonoEngine.Core.Common.Logging;
using _4DMonoEngine.Core.Universe;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Graphics
{
    /// <summary>
    /// Interface that provides camera information.
    /// </summary>
    public interface ICamera
    {
        /// <summary>
        /// Camera's projection matrix (lens).
        /// </summary>
        Matrix Projection { get; }

        /// <summary>
        /// Cameras view matrix (position).
        /// </summary>
        Matrix View { get; }

        /// <summary>
        /// The world matrix.
        /// </summary>
        Matrix World { get; }

        /// <summary>
        /// Camera's position.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Current camera elevation.
        /// </summary>
        float CurrentElevation { get; }

        /// <summary>
        /// Current camera rotation.
        /// </summary>
        float CurrentRotation { get; }
    }

    /// <summary>
    /// Interface that allows control of the camera.
    /// </summary>
    public interface ICameraControlService
    {
        /// <summary>
        /// Rotates camera in x-axis.
        /// </summary>
        /// <param name="step">The amount to rotate.</param>
        void RotateCamera(float step);

        /// <summary>
        /// Elevates camera in y-axis.
        /// </summary>
        /// <param name="step">The amount to elavate</param>
        void ElevateCamera(float step);

        void LookAt(Vector3 target);
    }

    public class Camera : GameComponent, ICamera, ICameraControlService
    {
        public Matrix Projection { get; private set; } // the camera lens.
        public Matrix View { get; private set; } // the camera position.
        public Matrix World { get; private set; } // the world.
        public Vector3 Position { get; private set; }
        public float CurrentElevation { get; private set; }
        public float CurrentRotation { get; private set; }

        private const float ViewAngle = MathHelper.PiOver4;
                            // the field of view of the lens -- states how wide or narrow it is.

        private const float NearPlaneDistance = 0.01f;
                            // the near plane distance. objects between near and far plane distance will be get rendered.

        private const float FarPlaneDistance = 1000f;
                            // the far plance distance, objects behinds this will not get rendered.

        private const float RotationSpeed = 0.025f; // the rotation speed.
        private float _aspectRatio; //aspect ratio of the field of view (width/height). 

        /// <summary>
        /// Logging facility.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetOrCreateLogger();

        public Camera(Game game)
            : base(game)
        {
            // export the services.
            game.Services.AddService(typeof (ICamera), this);
            game.Services.AddService(typeof (ICameraControlService), this);
        }

        public override void Initialize()
        {
            Logger.Trace("init()");
            _aspectRatio = Game.GraphicsDevice.Viewport.AspectRatio;
            World = Matrix.Identity;
            Projection = Matrix.CreatePerspectiveFieldOfView(ViewAngle, _aspectRatio, NearPlaneDistance,
                                                                  FarPlaneDistance); // set field of view of the camera.
            Position = _player.Position;
        }

        public override void Update(GameTime gameTime)
        {
            Position = _player.Position;
            
            var rotation = Matrix.CreateRotationX(CurrentElevation)*Matrix.CreateRotationY(CurrentRotation);

            // transform camera position based on rotation and elevation.
            var target = Vector3.Transform(Vector3.Forward, rotation) + Position;
            var upVector = Vector3.Transform(Vector3.Up, rotation);
            View = Matrix.CreateLookAt(Position, target, upVector);
        }

        /// <summary>
        /// Makes camera to look at given Vector3 target.
        /// </summary>
        /// <param name="target"></param>
        public void LookAt(Vector3 target)
        {
            View = Matrix.CreateLookAt(Position, target, Vector3.Up);
        }

        public void RotateCamera(float step)
        {
            CurrentRotation -= RotationSpeed*(step/25);
        }

        public void ElevateCamera(float step)
        {
            CurrentElevation -= RotationSpeed*(step/25);
        }
    }
}