using DebugUtils;
using System.Linq;
using System.Text;
using ToySerialController.MotionSource;
using ToySerialController.Device.OutputTarget;
using UnityEngine;
using ToySerialController.Utils;

namespace ToySerialController
{
    public partial class TCodeDevice : IDevice
    {
        protected readonly float[] XTarget, RTarget, ETarget;
        protected readonly JSONStorableFloat[] XCmd, RCmd, ECmd;
        protected readonly float[] LastXCmd, LastRCmd, LastECmd;
        private readonly JSONStorable _parent;
        private readonly StringBuilder _stringBuilder;
        private readonly StringBuilder _deviceReportBuilder;

        private float? _lastNoCollisionTime;
        private bool _lastNoCollisionSmoothingEnabled;
        private float _lastCollisionSmoothingT;
        private float _lastNoCollisionSmoothingStartTime, _lastNoCollisionSmoothingDuration;
        private bool _isLoading;

        public string GetDeviceReport()
        {
            const string format = "{0,5:0.00},\t";

            _deviceReportBuilder.Length = 0;
            return _deviceReportBuilder.Append("    Target    Cmd    Output\n")
                .Append("L0\t").AppendFormat(format, XTarget[0]).AppendFormat(format, XCmd[0].val).AppendTCode("L0", XCmd[0].val).AppendLine()
                .Append("L1\t").AppendFormat(format, XTarget[1]).AppendFormat(format, XCmd[1].val).AppendTCode("L1", XCmd[1].val).AppendLine()
                .Append("L2\t").AppendFormat(format, XTarget[2]).AppendFormat(format, XCmd[2].val).AppendTCode("L2", XCmd[2].val).AppendLine()
                .Append("R0\t").AppendFormat(format, RTarget[0]).AppendFormat(format, RCmd[0].val).AppendTCode("R0", RCmd[0].val).AppendLine()
                .Append("R1\t").AppendFormat(format, RTarget[1]).AppendFormat(format, RCmd[1].val).AppendTCode("R1", RCmd[1].val).AppendLine()
                .Append("R2\t").AppendFormat(format, RTarget[2]).AppendFormat(format, RCmd[2].val).AppendTCode("R2", RCmd[2].val).AppendLine()
                .Append("V0\t").AppendFormat(format, ETarget[0]).AppendFormat(format, ECmd[0].val).AppendTCode("V0", ECmd[0].val).AppendLine()
                .Append("A0\t").AppendFormat(format, ETarget[1]).AppendFormat(format, ECmd[1].val).AppendTCode("A0", ECmd[1].val).AppendLine()
                .Append("A1\t").AppendFormat(format, ETarget[2]).AppendFormat(format, ECmd[2].val).AppendTCode("A1", ECmd[2].val).AppendLine()
                .Append("A2\t").AppendFormat(format, ETarget[3]).AppendFormat(format, ECmd[3].val).AppendTCode("A2", ECmd[3].val)
                .ToString();
        }

        public TCodeDevice(JSONStorable parent)
        {
            _parent = parent;

            XTarget = new float[3];
            RTarget = new float[3];
            ETarget = new float[9];

            XCmd = new[] { FloatParam("L0"), FloatParam("L1"), FloatParam("L2") };
            RCmd = new[] { FloatParam("R0"), FloatParam("R1"), FloatParam("R2") };
            ECmd = new[] { FloatParam("V0"), FloatParam("A0"), FloatParam("A1"), FloatParam("A2") };

            LastXCmd = new float[] { float.NaN, float.NaN, float.NaN };
            LastRCmd = new float[] { float.NaN, float.NaN, float.NaN };
            LastECmd = Enumerable.Range(0, ECmd.Length).Select(_ => float.NaN).ToArray();

            _lastNoCollisionTime = Time.time;
            _stringBuilder = new StringBuilder();
            _deviceReportBuilder = new StringBuilder();
        }

        private JSONStorableFloat FloatParam(string name, float value = .5f)
        {
            var param = new JSONStorableFloat(name, value, 0f, 1f);
            _parent.RegisterFloat(param);
            return param;
        }

        private static void AppendIfChanged(StringBuilder stringBuilder, string axisName, float cmd, float lastCmd)
        {
            if (float.IsNaN(lastCmd) || Mathf.Abs(lastCmd - cmd) * 9999 >= 1)
                stringBuilder.AppendTCode(axisName, cmd).Append(" ");
        }

        public bool Update(IMotionSource motionSource, IOutputTarget outputTarget)
        {
            if (_isLoading)
            {
                for (var i = 0; i < 9; i++)
                    ETarget[i] = Mathf.Lerp(ETarget[i], 0f, 0.05f);

                for (var i = 0; i < 3; i++)
                {
                    XTarget[i] = Mathf.Lerp(XTarget[i], 0.5f, 0.05f);
                    RTarget[i] = Mathf.Lerp(RTarget[i], 0f, 0.05f);
                }
            }
            else if(motionSource != null)
            {
                UpdateMotion(motionSource);

                DebugDraw.DrawCircle(motionSource.TargetPosition + motionSource.TargetUp * RangeMinL0Slider.val * motionSource.ReferenceLength, motionSource.TargetUp, motionSource.TargetRight, Color.white, 0.05f);
                DebugDraw.DrawCircle(motionSource.TargetPosition + motionSource.TargetUp * RangeMaxL0Slider.val * motionSource.ReferenceLength, motionSource.TargetUp, motionSource.TargetRight, Color.white, 0.05f);
            }

            UpdateValues(outputTarget);

            return true;
        }

        public void UpdateValues(IOutputTarget outputTarget)
        {
            if (_lastNoCollisionSmoothingEnabled)
            {
                _lastCollisionSmoothingT = Mathf.Pow(2, 10 * ((Time.time - _lastNoCollisionSmoothingStartTime) / _lastNoCollisionSmoothingDuration - 1));
                if (_lastCollisionSmoothingT > 1.0f)
                {
                    _lastNoCollisionSmoothingEnabled = false;
                    _lastCollisionSmoothingT = 0;
                }
            }

            UpdateL0(); UpdateL1(); UpdateL2();
            UpdateR0(); UpdateR1(); UpdateR2();
            UpdateV0();
            UpdateA0(); UpdateA1(); UpdateA2();

            _stringBuilder.Length = 0;
            AppendIfChanged(_stringBuilder, "L0", XCmd[0].val, LastXCmd[0]);
            AppendIfChanged(_stringBuilder, "L1", XCmd[1].val, LastXCmd[1]);
            AppendIfChanged(_stringBuilder, "L2", XCmd[2].val, LastXCmd[2]);
            AppendIfChanged(_stringBuilder, "R0", RCmd[0].val, LastRCmd[0]);
            AppendIfChanged(_stringBuilder, "R1", RCmd[1].val, LastRCmd[1]);
            AppendIfChanged(_stringBuilder, "R2", RCmd[2].val, LastRCmd[2]);
            AppendIfChanged(_stringBuilder, "V0", ECmd[0].val, LastECmd[0]);
            AppendIfChanged(_stringBuilder, "A0", ECmd[1].val, LastECmd[1]);
            AppendIfChanged(_stringBuilder, "A1", ECmd[2].val, LastECmd[2]);
            AppendIfChanged(_stringBuilder, "A2", ECmd[3].val, LastECmd[3]);

            for (var i = 0; i < XCmd.Length; i++)
                LastXCmd[i] = XCmd[i].val;
            for (var i = 0; i < RCmd.Length; i++)
                LastRCmd[i] = RCmd[i].val;
            for (var i = 0; i < ECmd.Length; i++)
                LastECmd[i] = ECmd[i].val;
            
            if (_stringBuilder.Length > 0)
                outputTarget?.Write(_stringBuilder.AppendLine().ToString());
        }

        public void UpdateL0()
        {
            var t = Mathf.Clamp01((XTarget[0] - RangeMinL0Slider.val) / (RangeMaxL0Slider.val - RangeMinL0Slider.val));
            var output = Mathf.Clamp01(Mathf.Lerp(OutputMinL0Slider.val, OutputMaxL0Slider.val, t));

            if (InvertL0Toggle.val) output = 1f - output;
            if (EnableOverrideL0Toggle.val) output = OverrideL0Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(XCmd[0].val, output, _lastCollisionSmoothingT);

            XCmd[0].val = Mathf.Lerp(XCmd[0].val, output, 1 - SmoothingSlider.val);
        }

        public void UpdateL1()
        {
            var t = Mathf.Clamp01((XTarget[1] + RangeMaxL1Slider.val) / (2 * RangeMaxL1Slider.val));
            var output = Mathf.Clamp01(OffsetL1Slider.val + 0.5f + Mathf.Lerp(-OutputMaxL1Slider.val, OutputMaxL1Slider.val, t));

            if (InvertL1Toggle.val) output = 1f - output;
            if (EnableOverrideL1Toggle.val) output = OverrideL1Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(XCmd[1].val, output, _lastCollisionSmoothingT);

            XCmd[1].val = Mathf.Lerp(XCmd[1].val, output, 1 - SmoothingSlider.val);
        }

        public void UpdateL2()
        {
            var t = Mathf.Clamp01((XTarget[2] + RangeMaxL2Slider.val) / (2 * RangeMaxL2Slider.val));
            var output = Mathf.Clamp01(OffsetL2Slider.val + 0.5f + Mathf.Lerp(-OutputMaxL2Slider.val, OutputMaxL2Slider.val, t));

            if (InvertL2Toggle.val) output = 1f - output;
            if (EnableOverrideL2Toggle.val) output = OverrideL2Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(XCmd[2].val, output, _lastCollisionSmoothingT);

            XCmd[2].val = Mathf.Lerp(XCmd[2].val, output, 1 - SmoothingSlider.val);
        }

        public void UpdateR0()
        {
            var t = Mathf.Clamp01(0.5f + (RTarget[0] / 2) / (RangeMaxR0Slider.val / 180));
            var output = Mathf.Clamp01(OffsetR0Slider.val + 0.5f + Mathf.Lerp(-OutputMaxR0Slider.val, OutputMaxR0Slider.val, t));

            if (InvertR0Toggle.val) output = 1f - output;
            if (EnableOverrideR0Toggle.val) output = OverrideR0Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(RCmd[0].val, output, _lastCollisionSmoothingT);

            RCmd[0].val = Mathf.Lerp(RCmd[0].val, output, 1 - SmoothingSlider.val);
        }

        public void UpdateR1()
        {
            var t = Mathf.Clamp01(0.5f + (RTarget[1] / 2) / (RangeMaxR1Slider.val / 90));
            var output = Mathf.Clamp01(OffsetR1Slider.val + 0.5f + Mathf.Lerp(-OutputMaxR1Slider.val, OutputMaxR1Slider.val, t));

            if (InvertR1Toggle.val) output = 1f - output;
            if (EnableOverrideR1Toggle.val) output = OverrideR1Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(RCmd[1].val, output, _lastCollisionSmoothingT);

            RCmd[1].val = Mathf.Lerp(RCmd[1].val, output, 1 - SmoothingSlider.val);
        }

        public void UpdateR2()
        {
            var t = Mathf.Clamp01(0.5f + (RTarget[2] / 2) / (RangeMaxR2Slider.val / 90));
            var output = Mathf.Clamp01(OffsetR2Slider.val + 0.5f + Mathf.Lerp(-OutputMaxR2Slider.val, OutputMaxR2Slider.val, t));

            if (InvertR2Toggle.val) output = 1f - output;
            if (EnableOverrideR2Toggle.val) output = OverrideR2Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(RCmd[2].val, output, _lastCollisionSmoothingT);

            RCmd[2].val = Mathf.Lerp(RCmd[2].val, output, 1 - SmoothingSlider.val);
        }

        public void UpdateV0()
        {
            var output = Mathf.Clamp01(ETarget[0]);

            if (EnableOverrideV0Toggle.val) output = OverrideV0Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(ECmd[0].val, output, _lastCollisionSmoothingT);

            ECmd[0].val = Mathf.Lerp(ECmd[0].val, output, 1 - SmoothingSlider.val);
        }

        public void UpdateA0()
        {
            var output = Mathf.Clamp01(ETarget[1]);

            if (EnableOverrideA0Toggle.val) output = OverrideA0Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(ECmd[1].val, output, _lastCollisionSmoothingT);

            ECmd[1].val = Mathf.Lerp(ECmd[1].val, output, 1 - SmoothingSlider.val);
        }

        public void UpdateA1()
        {
            var output = Mathf.Clamp01(ETarget[2]);

            if (EnableOverrideA1Toggle.val) output = OverrideA1Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(ECmd[2].val, output, _lastCollisionSmoothingT);

            ECmd[2].val = Mathf.Lerp(ECmd[2].val, output, 1 - SmoothingSlider.val);
        }

        public void UpdateA2()
        {
            var output = Mathf.Clamp01(ETarget[3]);

            if (EnableOverrideA2Toggle.val) output = OverrideA2Slider.val;
            if (_lastNoCollisionSmoothingEnabled)
                output = Mathf.Lerp(ECmd[3].val, output, _lastCollisionSmoothingT);

            ECmd[3].val = Mathf.Lerp(ECmd[3].val, output, 1 - SmoothingSlider.val);
        }

        public bool UpdateMotion(IMotionSource motionSource)
        {
            var length = motionSource.ReferenceLength * ReferenceLengthScaleSlider.val;
            var radius = motionSource.ReferenceRadius * ReferenceRadiusScaleSlider.val;
            var referenceEnding = motionSource.ReferencePosition + motionSource.ReferenceUp * length;
            var diffPosition = motionSource.TargetPosition - motionSource.ReferencePosition;
            var diffEnding = motionSource.TargetPosition - referenceEnding;
            var aboveTarget = (Vector3.Dot(diffPosition, motionSource.TargetUp) < 0 && Vector3.Dot(diffEnding, motionSource.TargetUp) < 0)
                                || Vector3.Dot(diffPosition, motionSource.ReferenceUp) < 0;

            for (var i = 0; i < 5; i++)
                DebugDraw.DrawCircle(Vector3.Lerp(motionSource.ReferencePosition, referenceEnding, i / 4.0f), motionSource.ReferenceUp, motionSource.ReferenceRight, Color.grey, radius);

            var t = Mathf.Clamp(Vector3.Dot(motionSource.TargetPosition - motionSource.ReferencePosition, motionSource.ReferenceUp), 0f, length);
            var closestPoint = motionSource.ReferencePosition + motionSource.ReferenceUp * t;

            if (Vector3.Magnitude(closestPoint - motionSource.TargetPosition) <= radius)
            {
                if (diffPosition.magnitude > 0.0001f)
                {
                    XTarget[0] = 1 - Mathf.Clamp01((closestPoint - motionSource.ReferencePosition).magnitude / length);
                    if (aboveTarget)
                        XTarget[0] = XTarget[0] > 0 ? 1 : 0;

                    var diffOnPlane = Vector3.ProjectOnPlane(diffPosition, motionSource.ReferencePlaneNormal);
                    var rightOffset = Vector3.Project(diffOnPlane, motionSource.ReferenceRight);
                    var forwardOffset = Vector3.Project(diffOnPlane, motionSource.ReferenceForward);
                    XTarget[1] = forwardOffset.magnitude * Mathf.Sign(Vector3.Dot(forwardOffset, motionSource.ReferenceForward));
                    XTarget[2] = rightOffset.magnitude * Mathf.Sign(Vector3.Dot(rightOffset, motionSource.ReferenceRight));
                }
                else
                {
                    XTarget[0] = 1;
                    XTarget[1] = 0;
                    XTarget[2] = 0;
                }

                var correctedRight = Vector3.ProjectOnPlane(motionSource.TargetRight, motionSource.ReferenceUp);
                if (Vector3.Dot(correctedRight, motionSource.ReferenceRight) < 0)
                    correctedRight -= 2 * Vector3.Project(correctedRight, motionSource.ReferenceRight);

                RTarget[0] = Vector3.SignedAngle(motionSource.ReferenceRight, correctedRight, motionSource.ReferenceUp) / 180;
                RTarget[1] = -Vector3.SignedAngle(motionSource.ReferenceUp, Vector3.ProjectOnPlane(motionSource.TargetUp, motionSource.ReferenceForward), motionSource.ReferenceForward) / 90;
                RTarget[2] = Vector3.SignedAngle(motionSource.ReferenceUp, Vector3.ProjectOnPlane(motionSource.TargetUp, motionSource.ReferenceRight), motionSource.ReferenceRight) / 90;

                ETarget[0] = OutputV0CurveEditorSettings.Evaluate(XTarget, RTarget);
                ETarget[1] = OutputA0CurveEditorSettings.Evaluate(XTarget, RTarget);
                ETarget[2] = OutputA1CurveEditorSettings.Evaluate(XTarget, RTarget);
                ETarget[3] = OutputA2CurveEditorSettings.Evaluate(XTarget, RTarget);

                if (_lastNoCollisionTime != null)
                {
                    _lastNoCollisionSmoothingEnabled = true;
                    _lastNoCollisionSmoothingStartTime = Time.time;
                    _lastNoCollisionSmoothingDuration = Mathf.Clamp(Time.time - _lastNoCollisionTime.Value, 0.5f, 2);
                    _lastNoCollisionTime = null;
                }

                return true;
            }
            else
            {
                if (_lastNoCollisionTime == null)
                    _lastNoCollisionTime = Time.time;

                return false;
            }
        }

        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool disposing)
        {
            foreach (var cmd in XCmd.Concat(RCmd).Concat(ECmd))
                _parent.DeregisterFloat(cmd);
        }

        public virtual void OnSceneChanging() => _isLoading = true;
        public virtual void OnSceneChanged()
        {
            _lastNoCollisionSmoothingEnabled = true;
            _lastNoCollisionSmoothingStartTime = Time.time;
            _lastNoCollisionSmoothingDuration = 2;
            _lastNoCollisionTime = null;

            _isLoading = false;
        }
    }
}
