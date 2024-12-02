using SimpleJSON;
using System.Linq;
using ToySerialController.UI;
using ToySerialController.Utils;
using UnityEngine;

namespace ToySerialController.MotionSource
{
    public class MaleReference : IMotionSourceReference
    {
        private Atom _maleAtom;

        private JSONStorableStringChooser MaleChooser;
        private JSONStorableFloat PenisBaseOffset;
        private JSONStorableBool FixedNormalPlane;
        private JSONStorableFloat NormalPlaneOverrideX, NormalPlaneOverrideY, NormalPlaneOverrideZ;
        private JSONStorableAction AlignNormalPlaneAction;
        private UIDynamicButton AlignNormalPlaneButton;

        private SuperController Controller => SuperController.singleton;

        public Vector3 Position { get; private set; }
        public Vector3 Up { get; private set; }
        public Vector3 Right { get; private set; }
        public Vector3 Forward { get; private set; }
        public float Length { get; private set; }
        public float Radius { get; private set; }
        public Vector3 PlaneNormal { get; private set; }

        public void CreateUI(IUIBuilder builder)
        {
            MaleChooser = builder.CreatePopup("MotionSource:Male", "Select Male", null, null, MaleChooserCallback);
            PenisBaseOffset = builder.CreateSlider("MotionSource:PenisBaseOffset", "Penis base offset", 0, -0.05f, 0.05f, true, true);

            var normalPlaneGroup = new UIGroup(builder);
            FixedNormalPlane = builder.CreateToggle("MotionSource:FixedNormalPlane", "Fixed normal plane", false, v => normalPlaneGroup.SetVisible(v), false);
            NormalPlaneOverrideX = normalPlaneGroup.CreateSlider("MotionSource:FixedNormalPlaneRotationX", "X-Rotation", 0, -180, 180, true, true);
            NormalPlaneOverrideY = normalPlaneGroup.CreateSlider("MotionSource:FixedNormalPlaneRotationY", "Y-Rotation", 0, -180, 180, true, true);
            NormalPlaneOverrideZ = normalPlaneGroup.CreateSlider("MotionSource:FixedNormalPlaneRotationZ", "Z-Rotation", 0, -180, 180, true, true);
            AlignNormalPlaneButton = normalPlaneGroup.CreateButton("Align normal plane", AlignNormalPlane);
            normalPlaneGroup.SetVisible(FixedNormalPlane.val);

            AlignNormalPlaneAction = UIManager.CreateAction("Align normal plane", AlignNormalPlane);

            FindMales();
        }

        public void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(MaleChooser);
            builder.Destroy(PenisBaseOffset);
            builder.Destroy(FixedNormalPlane);
            builder.Destroy(NormalPlaneOverrideX);
            builder.Destroy(NormalPlaneOverrideY);
            builder.Destroy(NormalPlaneOverrideZ);
            builder.Destroy(AlignNormalPlaneButton);

            UIManager.RemoveAction(AlignNormalPlaneAction);
        }

        public void StoreConfig(JSONNode config)
        {
            config.Store(MaleChooser);
            config.Store(PenisBaseOffset);
            config.Store(FixedNormalPlane);
            config.Store(NormalPlaneOverrideX);
            config.Store(NormalPlaneOverrideY);
            config.Store(NormalPlaneOverrideZ);
        }

        public void RestoreConfig(JSONNode config)
        {
            config.Restore(MaleChooser);
            config.Restore(PenisBaseOffset);
            config.Restore(FixedNormalPlane);
            config.Restore(NormalPlaneOverrideX);
            config.Restore(NormalPlaneOverrideY);
            config.Restore(NormalPlaneOverrideZ);

            FindMales(MaleChooser.val);
        }

        public bool Update()
        {
            if (_maleAtom == null || !_maleAtom.on)
                return false;

            var gen1Collider = _maleAtom.GetComponentByName<CapsuleCollider>("AutoColliderGen1Hard");
            var gen2Collider = _maleAtom.GetComponentByName<CapsuleCollider>("AutoColliderGen2Hard");
            var gen3aCollider = _maleAtom.GetComponentByName<CapsuleCollider>("AutoColliderGen3aHard");
            var gen3bCollider = _maleAtom.GetComponentByName<CapsuleCollider>("AutoColliderGen3bHard");

            if (gen1Collider == null || gen2Collider == null || gen3aCollider == null || gen3bCollider == null)
                return false;

            var gen1Transform = gen1Collider.transform;
            var gen1Position = gen1Transform.position - gen1Transform.up * (gen1Collider.height / 2 - gen1Collider.radius - PenisBaseOffset.val);
            var gen2Position = gen2Collider.transform.position;
            var gen3aPosition = gen3aCollider.transform.position;
            var gen3bPosition = gen3bCollider.transform.position + gen3bCollider.transform.right * gen3bCollider.radius;

            Up = gen1Transform.up;
            Position = gen1Position;
            Radius = gen2Collider.radius;
            Length = Vector3.Distance(gen1Position, gen2Position) + Vector3.Distance(gen2Position, gen3aPosition) + Vector3.Distance(gen3aPosition, gen3bPosition);

            if (FixedNormalPlane.val)
            {
                var rotation = Quaternion.Euler(NormalPlaneOverrideX.val, NormalPlaneOverrideY.val, NormalPlaneOverrideZ.val);
                Right = rotation * _maleAtom.transform.right;
                Forward = rotation * _maleAtom.transform.forward;
                PlaneNormal = rotation * _maleAtom.transform.up;
            }
            else
            {
                Right = -gen1Transform.forward;
                Forward = gen1Transform.right;

                var pelvisRight = _maleAtom.GetComponentByName<Collider>("AutoColliderpelvisFR3Joint")?.transform;
                var pelvidLeft = _maleAtom.GetComponentByName<Collider>("AutoColliderpelvisFL3Joint")?.transform;
                var pelvisMid = _maleAtom.GetComponentByName<Transform>("AutoColliderpelvisF1")?.GetComponentByName<Collider>("AutoColliderpelvisF4Joint")?.transform;

                if (pelvisRight == null || pelvidLeft == null || pelvisMid == null)
                    PlaneNormal = Up;
                else
                    PlaneNormal = -Vector3.Cross(pelvisMid.position - pelvidLeft.position, pelvisMid.position - pelvisRight.position).normalized;
            }

            return true;
        }

        private void AlignNormalPlane()
        {
            var pelvisRight = _maleAtom.GetComponentByName<Collider>("AutoColliderpelvisFR3Joint")?.transform;
            var pelvidLeft = _maleAtom.GetComponentByName<Collider>("AutoColliderpelvisFL3Joint")?.transform;
            var pelvisMid = _maleAtom.GetComponentByName<Transform>("AutoColliderpelvisF1")?.GetComponentByName<Collider>("AutoColliderpelvisF4Joint")?.transform;

            if (pelvisRight == null || pelvidLeft == null || pelvisMid == null)
                return;

            var planeNormal = -Vector3.Cross(pelvisMid.position - pelvidLeft.position, pelvisMid.position - pelvisRight.position).normalized;
            var angles = Quaternion.FromToRotation(_maleAtom.transform.up, planeNormal).eulerAngles;

            NormalPlaneOverrideX.val = angles.x > 180 ? angles.x - 360 : angles.x;
            NormalPlaneOverrideY.val = angles.y > 180 ? angles.y - 360 : angles.y;
            NormalPlaneOverrideZ.val = angles.z > 180 ? angles.z - 360 : angles.z;
        }

        private void FindMales(string defaultUid = null)
        {
            var people = Controller.GetAtoms().Where(a => a.type == "Person");
            var maleUids = people
                .Where(a => a.GetComponentInChildren<DAZCharacterSelector>()?.gender == DAZCharacterSelector.Gender.Male)
                .Select(a => a.uid)
                .ToList();

            if (!maleUids.Contains(defaultUid))
                defaultUid = maleUids.FirstOrDefault(uid => uid == _maleAtom?.uid) ?? maleUids.FirstOrDefault() ?? "None";

            maleUids.Insert(0, "None");

            MaleChooser.choices = maleUids;
            MaleChooserCallback(defaultUid);
        }

        protected void MaleChooserCallback(string s)
        {
            _maleAtom = Controller.GetAtomByUid(s);
            MaleChooser.valNoCallback = _maleAtom == null ? "None" : s;
        }

        public void Refresh() => FindMales(MaleChooser.val);
    }
}
