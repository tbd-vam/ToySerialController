using DebugUtils;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using ToySerialController.UI;
using ToySerialController.Utils;
using UnityEngine;

namespace ToySerialController.MotionSource
{
    public class DildoReference : AbstractReference
    {
        private Atom _dildoAtom;

        private JSONStorableStringChooser DildoChooser;
        private JSONStorableFloat DildoBaseOffset;

        private SuperController Controller => SuperController.singleton;

        public override void CreateUI(IUIBuilder builder)
        {
            DildoChooser = builder.CreatePopup("MotionSource:Dildo", "Select Dildo", null, null, DildoChooserCallback);
            DildoBaseOffset = builder.CreateSlider("MotionSource:DildoBaseOffset", "Dildo base offset", 0, -0.05f, 0.05f, true, true);

            base.CreateUI(builder);

            FindDildos();
        }

        public override void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(DildoChooser);
            builder.Destroy(DildoBaseOffset);

            base.DestroyUI(builder);
        }

        public override void StoreConfig(JSONNode config)
        {
            config.Store(DildoChooser);
            config.Store(DildoBaseOffset);

            base.StoreConfig(config);
        }

        public override void RestoreConfig(JSONNode config)
        {
            config.Restore(DildoChooser);
            config.Restore(DildoBaseOffset);

            base.RestoreConfig(config);

            FindDildos(DildoChooser.val);
        }

        public override bool Update()
        {
            if (_dildoAtom == null || !_dildoAtom.on)
                return false;

            var baseCollider = _dildoAtom.GetComponentByName<Transform>("b1").GetComponentByName<CapsuleCollider>("_Collider1");
            var midCollider = _dildoAtom.GetComponentByName<Transform>("b2").GetComponentByName<CapsuleCollider>("_Collider2");
            var tipCollider = _dildoAtom.GetComponentByName<Transform>("b3").GetComponentByName<CapsuleCollider>("_Collider2");

            if (baseCollider == null || midCollider == null || tipCollider == null)
                return false;

            var basePosition = baseCollider.transform.position - baseCollider.transform.up * (baseCollider.radius / 2 - DildoBaseOffset.val);
            var midPosition = midCollider.transform.position;
            var tipPosition = tipCollider.transform.position + tipCollider.transform.up * tipCollider.height;

            Position = basePosition;
            Length = Vector3.Distance(basePosition, midPosition) + Vector3.Distance(midPosition, tipPosition);
            Radius = midCollider.radius;

            Up = (tipPosition - midPosition).normalized;

            if (FixedNormalPlane.val)
            {
                var rotation = Quaternion.Euler(NormalPlaneOverrideX.val, NormalPlaneOverrideY.val, NormalPlaneOverrideZ.val);
                Right = rotation * _dildoAtom.transform.right;
                Forward = rotation * _dildoAtom.transform.forward;
                PlaneNormal = rotation * _dildoAtom.transform.up;
            }
            else
            {
                Right = -baseCollider.transform.right;
                Forward = Vector3.Cross(Up, Right);
                PlaneNormal = baseCollider.transform.up;
            }

            return true;
        }

        private void FindDildos(string defaultUid = null)
        {
            var dildoUids = Controller.GetAtoms()
                .Where(a => a.type == "Dildo")
                .Select(a => a.uid)
                .ToList();

            if (!dildoUids.Contains(defaultUid))
                defaultUid = dildoUids.FirstOrDefault(uid => uid == _dildoAtom?.uid) ?? dildoUids.FirstOrDefault() ?? "None";

            dildoUids.Insert(0, "None");

            DildoChooser.choices = dildoUids;
            DildoChooserCallback(defaultUid);
        }

        protected void DildoChooserCallback(string s)
        {
            _dildoAtom = Controller.GetAtomByUid(s);
            DildoChooser.valNoCallback = _dildoAtom == null ? "None" : s;
        }

        public override void Refresh() => FindDildos(DildoChooser.val);
    }
}
