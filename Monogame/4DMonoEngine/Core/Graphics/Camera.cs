using System;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Events;
using _4DMonoEngine.Core.Events.Args;

namespace _4DMonoEngine.Core.Graphics
{
    public class Camera : IEventSink
    {
        public Matrix Projection { get; private set; } // the camera lens.
        public Matrix View { get; private set; } // the camera position.
        public Matrix World { get; private set; } // the world.
        public Ray Ray { get; private set; }

        private const float ViewAngle = MathHelper.PiOver4;
        private const float NearPlaneDistance = 0.01f;
        private const float FarPlaneDistance = 1000f;
        private readonly Action<EventArgs> m_wrappedScreenSizeHandler;
        private readonly Action<EventArgs> m_wrappedViewHandler;
        private readonly Action<EventArgs> m_wrappedPositionHandler; 

        public Camera(float aspectRatio)
        {
            World = Matrix.Identity;
            Projection = Matrix.CreatePerspectiveFieldOfView(ViewAngle, aspectRatio, NearPlaneDistance, FarPlaneDistance);
            m_wrappedScreenSizeHandler = EventHelper.Wrap<Vector2Args>(UpdateScreenSize);
            m_wrappedViewHandler = EventHelper.Wrap<Vector3Args>(UpdateTarget);
            m_wrappedPositionHandler = EventHelper.Wrap<Vector3Args>(UpdatePosition);
        }

        private void Update()
        {
            var target = Ray.Direction + Ray.Position;
            View = Matrix.CreateLookAt(Ray.Position, target, Vector3.Up);
        }

        private void UpdatePosition(Vector3Args args)
        {
            Ray = new Ray(args.Vector, Ray.Direction);
            Update();
        }

        private void UpdateTarget(Vector3Args args)
        {
            Ray = new Ray(Ray.Position, args.Vector);
            Update();
        }

        private void UpdateScreenSize(Vector2Args args)
        {
            Projection = Matrix.CreatePerspectiveFieldOfView(ViewAngle, args.Vector.X / args.Vector.Y, NearPlaneDistance, FarPlaneDistance);
        }
        public bool CanHandleEvent(string eventName)
        {
            switch (eventName)
            {
                case EventConstants.ScreenSizeUpdated:
                    return true;
                case EventConstants.ViewUpdated:
                    return true;
                case EventConstants.PlayerPositionUpdated:
                    return true;
                default :
                    return false;
            }

        }
        public Action<EventArgs> GetHandlerForEvent(string eventName)
        {
            switch (eventName)
            {
                case EventConstants.ScreenSizeUpdated:
                    return m_wrappedScreenSizeHandler;
                case EventConstants.ViewUpdated:
                    return m_wrappedViewHandler;
                case EventConstants.PlayerPositionUpdated:
                    return m_wrappedPositionHandler;
                default :
                    return null;
            }
        }
    }
}