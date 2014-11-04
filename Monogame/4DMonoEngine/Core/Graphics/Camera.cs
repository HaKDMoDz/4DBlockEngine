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
        private readonly EventSinkImpl m_eventSinkImpl;

        public Camera(float aspectRatio)
        {
            World = Matrix.Identity;
            Projection = Matrix.CreatePerspectiveFieldOfView(ViewAngle, aspectRatio, NearPlaneDistance, FarPlaneDistance);
            m_eventSinkImpl = new EventSinkImpl();
            m_eventSinkImpl.AddHandler<Vector2Args>(EventConstants.ScreenSizeUpdated, OnUpdateScreenSize);
            m_eventSinkImpl.AddHandler<Vector3Args>(EventConstants.ViewUpdated, OnUpdateTarget);
            m_eventSinkImpl.AddHandler<Vector3Args>(EventConstants.PlayerPositionUpdated, OnUpdatePosition);
         //   m_eventSinkImpl.AddHandler<Vector3Args>(EventConstants.ModalScreenPushed, OnUpdatePosition);
        }

        private void Update()
        {
            var target = Ray.Direction + Ray.Position;
            View = Matrix.CreateLookAt(Ray.Position, target, Vector3.Up);
        }

        private void OnUpdatePosition(Vector3Args args)
        {
            Ray = new Ray(args.Vector, Ray.Direction);
            Update();
        }

        private void OnUpdateTarget(Vector3Args args)
        {
            Ray = new Ray(Ray.Position, args.Vector);
            Update();
        }

        private void OnUpdateScreenSize(Vector2Args args)
        {
            Projection = Matrix.CreatePerspectiveFieldOfView(ViewAngle, args.Vector.X / args.Vector.Y, NearPlaneDistance, FarPlaneDistance);
        }
        
        public bool CanHandleEvent(string eventName)
        {
            return m_eventSinkImpl.CanHandleEvent(eventName);
        }

        public Action<EventArgs> GetHandlerForEvent(string eventName)
        {
            return m_eventSinkImpl.GetHandlerForEvent(eventName);
        }
    }
}