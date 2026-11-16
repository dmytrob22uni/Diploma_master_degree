using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// M0 calibration helper. Moves each axis independently by a raw local
    /// offset so safe travel ranges and directions can be recorded before
    /// print logic exists. Play-mode only.
    /// </summary>
    public class PrinterMotionDebug : MonoBehaviour
    {
        public PrinterMotionBase Motion;

        [Tooltip("Show the calibration sliders while playing")]
        public bool ShowGui = true;

        [Tooltip("Slider half-range in metres. Raise it if the axis can travel further.")]
        [Min(0.01f)]
        public float DebugTravel = 0.25f;

        [Tooltip("Switch the sliders from raw offsets to the calibrated 0..1 range")]
        public bool TestCalibratedRange = false;

        private float[] axisOffsets;
        private string[] axisNames;

        private void Start()
        {
            ResolveMotion();
        }

        private void ResolveMotion()
        {
            if (Motion == null)
            {
                Motion = GetComponentInParent<PrinterMotionBase>();
            }

            if (Motion == null)
            {
                Debug.LogWarning(
                    "[PrinterMotionDebug] No PrinterMotionBase assigned or found.",
                    this);
                return;
            }

            int count = Mathf.Max(0, Motion.AxisCount);
            axisOffsets = new float[count];
            axisNames = new string[count];

            for (int i = 0; i < count; i++)
            {
                axisNames[i] = Motion.GetAxisName(i);
                axisOffsets[i] = Motion.GetAxisLocalOffset(i);
            }
        }

        private void SyncFromMotion()
        {
            if (Motion == null || axisOffsets == null)
            {
                return;
            }

            for (int i = 0; i < axisOffsets.Length; i++)
            {
                axisOffsets[i] = Motion.GetAxisLocalOffset(i);
            }
        }

        private void OnGUI()
        {
            if (!ShowGui || Motion == null || axisOffsets == null)
            {
                return;
            }

            const float width = 380.0f;
            const float rowHeight = 44.0f;
            float height = 150.0f + axisOffsets.Length * rowHeight;
            Rect rect = new Rect(10.0f, 10.0f, width, height);

            GUI.Box(rect, "Printer motion debug (M0)");

            GUI.Label(
                new Rect(rect.x + 10.0f, rect.y + 22.0f, rect.width - 20.0f, 36.0f),
                "Move each axis slowly and note the safe local offsets.\n" +
                "Then switch to range mode and verify 0..1 travel.");

            float travel = Mathf.Max(0.01f, DebugTravel);
            float y = rect.y + 64.0f;

            TestCalibratedRange = GUI.Toggle(
                new Rect(rect.x + 10.0f, y, rect.width - 20.0f, 20.0f),
                TestCalibratedRange,
                "Test calibrated range (0..1)");

            y += 26.0f;

            if (TestCalibratedRange)
            {
                for (int i = 0; i < axisOffsets.Length; i++)
                {
                    float normalized = Motion.GetAxisNormalized(i);

                    GUI.Label(
                        new Rect(rect.x + 10.0f, y, rect.width - 20.0f, 20.0f),
                        $"{axisNames[i]}  t={normalized:0.00}  " +
                        $"offset={Motion.GetAxisLocalOffset(i):+0.000;-0.000;0.000} m");

                    normalized = GUI.HorizontalSlider(
                        new Rect(rect.x + 10.0f, y + 20.0f, rect.width - 20.0f, 20.0f),
                        normalized,
                        0.0f,
                        1.0f);

                    Motion.SetAxisNormalized(i, normalized);
                    y += rowHeight;
                }
            }
            else
            {
                for (int i = 0; i < axisOffsets.Length; i++)
                {
                    GUI.Label(
                        new Rect(rect.x + 10.0f, y, rect.width - 20.0f, 20.0f),
                        $"{axisNames[i]}  offset={axisOffsets[i]:+0.000;-0.000;0.000} m");

                    axisOffsets[i] = GUI.HorizontalSlider(
                        new Rect(rect.x + 10.0f, y + 20.0f, rect.width - 20.0f, 20.0f),
                        axisOffsets[i],
                        -travel,
                        travel);

                    Motion.SetAxisLocalOffset(i, axisOffsets[i]);
                    y += rowHeight;
                }
            }

            if (GUI.Button(
                    new Rect(rect.x + 10.0f, y, 110.0f, 24.0f),
                    "Home axes"))
            {
                Motion.Home();
                SyncFromMotion();
            }

            if (GUI.Button(
                    new Rect(rect.x + 128.0f, y, 150.0f, 24.0f),
                    "Re-read positions"))
            {
                SyncFromMotion();
            }
        }
    }
}
