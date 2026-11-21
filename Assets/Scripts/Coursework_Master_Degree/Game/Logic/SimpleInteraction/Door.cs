using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Items.FFF;

namespace Coursework_Master_Degree.Game.Logic.SimpleInteraction
{
    public class Door : MonoBehaviour, IInteractable, IPrinterBusyInteraction
    {
        public DoorSide DoorSide;
        public Transform Hinge;
        [Tooltip("Rotation in degrees of opened unit")]
        public float Rotation;

        private Vector3 closedRotation = Vector3.zero;
        private Vector3 openedRotation;
        private Quaternion closedRotationQuaternion;
        private Quaternion openedRotationQuaternion;

        private readonly List<InteractionOption> interactions =
            new List<InteractionOption>(1);
        private readonly InteractionOption lockedOption =
            new InteractionOption { Info = "Locked door" };
        private readonly InteractionOption blockedOption =
            new InteractionOption();
        private readonly InteractionOption doorOption =
            new InteractionOption { Info = "Door" };

        [Header("Animation")]
        [Min(0.01f)]
        public float transitionDuration = 0.5f;
        public AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Lock Settings")]
        public bool isLocked;

        // State Tracking
        private bool isAnimating;
        private bool isOpened;
        private float progress;

        private Quaternion fromRotation;
        private Quaternion toRotation;

        public bool IsAnimating => isAnimating;
        public bool IsOpened => isOpened;
        public bool IsLocked => isLocked;

        public bool AllowsPrinterBusyHover => true;

        private void Awake()
        {
            if (DoorSide == DoorSide.Left)
            {
                openedRotation = new Vector3(0.0f, 0.0f, Rotation);
            }
            else if (DoorSide == DoorSide.Right)
            {
                openedRotation = new Vector3(0.0f, 0.0f, -Rotation);
            }

            closedRotationQuaternion = Quaternion.Euler(closedRotation);
            openedRotationQuaternion = Quaternion.Euler(openedRotation);
            doorOption.OnExecute = ToggleDoor;
        }

        private void Update()
        {
            AnimateDoor();
        }

        private void AnimateDoor()
        {
            if (isAnimating)
            {
                progress += Time.deltaTime / transitionDuration;
                float clampedProgress = Mathf.Clamp01(progress);

                float curveEvaluated = smoothCurve.Evaluate(clampedProgress);
                Hinge.localRotation = Quaternion.Slerp(
                    fromRotation,
                    toRotation,
                    curveEvaluated);

                if (Mathf.Approximately(clampedProgress, 1.0f))
                {
                    Hinge.localRotation = toRotation;
                    isOpened = !isOpened;
                    isAnimating = false;
                }
            }
        }

        public bool TrySetLockState(bool lockState)
        {
            if (!isAnimating && !isOpened)
            {
                this.isLocked = lockState;
                return true;
            }

            return false;
        }

        public void ToggleDoor()
        {
            if (isAnimating)
            {
                return;
            }

            if (isLocked)
            {
                return;
            }

            fromRotation = Hinge.localRotation;
            toRotation =
                isOpened ?
                closedRotationQuaternion :
                openedRotationQuaternion;

            isAnimating = true;
            progress = default;
        }

        public IReadOnlyList<InteractionOption> GetInteractions()
        {
            interactions.Clear();

            if (isLocked)
            {
                interactions.Add(lockedOption);
                return interactions;
            }

            Printer printer = Printer.FindFor(gameObject);

            // Only opening is interlocked. An already-open door may always be
            // closed, including while the printer is hot or paused.
            if (printer != null &&
                !isOpened &&
                !printer.CanOpenEnclosureDoors)
            {
                blockedOption.Info = printer.EnclosureDoorBlockReason;
                interactions.Add(blockedOption);
                return interactions;
            }

            interactions.Add(doorOption);
            return interactions;
        }
    }
}
