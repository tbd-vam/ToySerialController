using SimpleJSON;
using ToySerialController.UI;
using ToySerialController.Utils;
using UnityEngine;

namespace ToySerialController.MotionSource
{
    public abstract class AbstractReference : IMotionSourceReference
    {
        protected JSONStorableBool FixedNormalPlane;
        protected JSONStorableFloat NormalPlaneOverrideX, NormalPlaneOverrideY, NormalPlaneOverrideZ;

        public Vector3 Position { get; protected set; }
        public Vector3 Up { get; protected set; }
        public Vector3 Right { get; protected set; }
        public Vector3 Forward { get; protected set; }
        public float Length { get; protected set; }
        public float Radius { get; protected set; }
        public Vector3 PlaneNormal { get; protected set; }

        public virtual void CreateUI(IUIBuilder builder)
        {
            var normalPlaneGroup = new UIGroup(builder);
            FixedNormalPlane = builder.CreateToggle("MotionSource:FixedNormalPlane", "Fixed normal plane", false, v => normalPlaneGroup.SetVisible(v), false);
            NormalPlaneOverrideX = normalPlaneGroup.CreateSlider("MotionSource:FixedNormalPlaneRotationX", "X-Rotation", 0, -180, 180, true, true);
            NormalPlaneOverrideY = normalPlaneGroup.CreateSlider("MotionSource:FixedNormalPlaneRotationY", "Y-Rotation", 0, -180, 180, true, true);
            NormalPlaneOverrideZ = normalPlaneGroup.CreateSlider("MotionSource:FixedNormalPlaneRotationZ", "Z-Rotation", 0, -180, 180, true, true);

            normalPlaneGroup.SetVisible(FixedNormalPlane.val);
        }

        public virtual void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(FixedNormalPlane);
            builder.Destroy(NormalPlaneOverrideX);
            builder.Destroy(NormalPlaneOverrideY);
            builder.Destroy(NormalPlaneOverrideZ);
        }

        public virtual void StoreConfig(JSONNode config)
        {
            config.Store(FixedNormalPlane);
            config.Store(NormalPlaneOverrideX);
            config.Store(NormalPlaneOverrideY);
            config.Store(NormalPlaneOverrideZ);
        }

        public virtual void RestoreConfig(JSONNode config)
        {
            config.Restore(FixedNormalPlane);
            config.Restore(NormalPlaneOverrideX);
            config.Restore(NormalPlaneOverrideY);
            config.Restore(NormalPlaneOverrideZ);
        }

        public abstract void Refresh();
        public abstract bool Update();
    }
}
