using Impression;
using Impression.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Example07SkinnedModel
{
    public sealed class Camera
    {
        Matrix transform = Matrix.Identity;

        Matrix view = Matrix.Identity;
        Matrix projection = Matrix.Identity;
        float aspectRatio = 1;
        float fieldOfView = 60;
        float nearPlane = 1f;
        float farPlane = 100.0f;

        public Matrix Transform
        {
            get { return transform; }
            set
            {
                transform = value;
                OnUpdateView(transform);
            }
        }

        public Matrix View
        {
            get { return view; }
            private set
            {
                view = value;
                this.OnUpdateFrustum();
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            private set
            {
                projection = value;
                this.OnUpdateFrustum();
            }
        }

        public BoundingFrustum BoundingFrustum { get; private set; }

        public float FieldOfView
        {
            get { return fieldOfView; }
            set { fieldOfView = value; OnUpdateProjection(); }
        }

        public float AspectRatio
        {
            get { return aspectRatio; }
            set { aspectRatio = value; OnUpdateProjection(); }
        }

        public float NearPlane
        {
            get { return nearPlane; }
            set { nearPlane = value; OnUpdateProjection(); }
        }

        public float FarPlane
        {
            get { return farPlane; }
            set { farPlane = value; OnUpdateProjection(); }
        }

        public Camera(Viewport viewport)
        {
            OnInitialize(viewport);
        }

        void OnInitialize(Viewport viewport)
        {
            OnUpdateView(Matrix.Identity);
            this.AspectRatio = (float)viewport.Width / (float)viewport.Height;
        }

        public bool BoundingVolumeIsInView(BoundingBox boundingBox)
        {
            return (this.BoundingFrustum.Contains(boundingBox) != ContainmentType.Disjoint);
        }

        public bool BoundingVolumeIsInView(BoundingSphere boundingSphere)
        {
            return (this.BoundingFrustum.Contains(boundingSphere) != ContainmentType.Disjoint);
        }

        public bool BoundingVolumeIsInView(BoundingFrustum boundingFrustum)
        {
            return (this.BoundingFrustum.Contains(boundingFrustum) != ContainmentType.Disjoint);
        }

        public bool BoundingVolumeIsInView(Vector3 point)
        {
            return (this.BoundingFrustum.Contains(point) != ContainmentType.Disjoint);
        }

        void OnUpdateFrustum()
        {
            Matrix viewproj = view * projection;
            this.BoundingFrustum = new BoundingFrustum(viewproj);
        }

        void OnUpdateProjection()
        {
            this.Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(fieldOfView), aspectRatio, nearPlane, farPlane);
        }

        void OnUpdateView(Matrix world)
        {
            this.View = Matrix.Invert(world);
        }
    }
}
