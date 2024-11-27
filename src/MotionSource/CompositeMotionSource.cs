using DebugUtils;
using SimpleJSON;
using System.Collections.Generic;
using ToySerialController.UI;
using ToySerialController.Utils;
using UnityEngine;

namespace ToySerialController.MotionSource
{
    public class CompositeMotionSource : AbstractRefreshableMotionSource
    {
        private UIDynamicButton TargetTitle, ReferenceTitle;
        private bool _RelativeToNormalPlane;
        protected JSONStorableStringChooser RotationReference;

        protected IMotionSourceReference Reference { get; }
        protected IMotionSourceTarget Target { get; }

        public override Vector3 ReferencePosition => Reference.Position;
        public override Vector3 ReferenceUp => Reference.Up;
        public override Vector3 ReferenceRight => Reference.Right;
        public override Vector3 ReferenceForward => Reference.Forward;
        public override float ReferenceLength => Reference.Length;
        public override float ReferenceRadius => Reference.Radius;
        public override Vector3 ReferencePlaneNormal => Reference.PlaneNormal;
        public override Vector3 TargetPosition => Target.Position;
        public override Vector3 TargetUp => Target.Up;
        public override Vector3 TargetRight => Target.Right;
        public override Vector3 TargetForward => Target.Forward;

        public override bool RelativeToNormalPlane => _RelativeToNormalPlane;

        public CompositeMotionSource(IMotionSourceReference reference, IMotionSourceTarget target)
        {
            Reference = reference;
            Target = target;
        }

        public override void RestoreConfig(JSONNode config)
        {
            config.Restore(RotationReference);
            Reference.RestoreConfig(config);
            Target.RestoreConfig(config);
        }

        public override void StoreConfig(JSONNode config)
        {
            config.Store(RotationReference);
            Reference.StoreConfig(config);
            Target.StoreConfig(config);
        }

        public override bool Update()
        {
            if (Reference.Update() && Target.Update(Reference))
            {
                DebugDraw.DrawSquare(ReferencePosition, ReferencePlaneNormal, ReferenceRight, Color.white, 0.33f);
                DebugDraw.DrawTransform(ReferencePosition, ReferenceUp, ReferenceRight, ReferenceForward, 0.15f);
                DebugDraw.DrawRay(ReferencePosition, ReferenceUp, ReferenceLength, Color.white);
                DebugDraw.DrawLine(ReferencePosition, TargetPosition, Color.yellow);
                DebugDraw.DrawTransform(TargetPosition, TargetUp, TargetRight, TargetForward, 0.15f);

                return true;
            }

            return false;
        }

        public override void CreateUI(IUIBuilder builder)
        {
            var rotationReferences = new List<string> { "Penetrator", "Normal Plane" };
            RotationReference = builder.CreatePopup("Plugin:MotionSourceRotationReference", "Rotation Reference\n(R0, R1, R2)", rotationReferences, "Penetrator", RotationReferenceChooserCallback);

            TargetTitle = builder.CreateDisabledButton("Target", new Color(1.0f, 1.0f, 1.0f, 0.075f), Color.white);
            TargetTitle.buttonText.fontStyle = FontStyle.Bold;

            Target.CreateUI(builder);

            ReferenceTitle = builder.CreateDisabledButton("Reference", new Color(1.0f, 1.0f, 1.0f, 0.075f), Color.white);
            ReferenceTitle.buttonText.fontStyle = FontStyle.Bold;

            Reference.CreateUI(builder);
            base.CreateUI(builder);
        }

        public override void DestroyUI(IUIBuilder builder)
        {
            base.DestroyUI(builder);

            builder.Destroy(RotationReference);

            Reference.DestroyUI(builder);
            builder.Destroy(ReferenceTitle);

            Target.DestroyUI(builder);
            builder.Destroy(TargetTitle);
        }

        private void RotationReferenceChooserCallback(string referenceMode) => _RelativeToNormalPlane = referenceMode == "Normal Plane";

        protected override void RefreshButtonCallback()
        {
            Reference.Refresh();
            Target.Refresh();
        }
    }
}
