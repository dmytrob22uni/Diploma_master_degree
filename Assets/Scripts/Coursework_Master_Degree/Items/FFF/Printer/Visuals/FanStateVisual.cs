using UnityEngine;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Wear;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Swaps an installed fan between its calm and active child visuals and
    /// slowly rotates the active child. Installation is resolved through the
    /// generic PartLocation contract, so removed, spare, ghost and worn-out
    /// fans stay calm. The printer state is read through Printer.FindFor, so
    /// enclosure fans work when their enclosure is docked to a printer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FanStateVisual : MonoBehaviour
    {
        [SerializeField] private GameObject calmState = null;
        [SerializeField] private GameObject activeState = null;

        [Tooltip("Local axis of the active fan mesh. The authored meshes spin around local Y.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Min(0.0f)]
        [SerializeField] private float rotationDegreesPerSecond = 240.0f;

        private ItemWearState wearState;
        private bool isActive;

        public bool IsActive => isActive;

        private void Awake()
        {
            ResolveWearState();
            SetActiveState(false, true);
        }

        private void OnEnable()
        {
            ResolveWearState();
        }

        private void OnTransformParentChanged()
        {
            ResolveWearState();
            SetActiveState(false, true);
        }

        private void OnDisable()
        {
            SetActiveState(false, true);
        }

        private void Update()
        {
            PartLocation location = GetComponentInParent<PartLocation>();
            bool installed = IsInstalled(location);
            Printer printer = installed
                ? Printer.FindFor(gameObject)
                : null;
            bool broken = wearState != null && wearState.IsWornOut;
            bool active = installed &&
                printer != null &&
                printer.AreFansActive &&
                !broken;

            SetActiveState(active);

            if (active && activeState != null)
            {
                Vector3 axis = rotationAxis.sqrMagnitude > 0.000001f
                    ? rotationAxis.normalized
                    : Vector3.up;

                activeState.transform.Rotate(
                    axis,
                    rotationDegreesPerSecond * Time.deltaTime,
                    Space.Self);
            }
        }

        private bool IsInstalled(PartLocation location)
        {
            if (location == null ||
                !location.IsOccupied ||
                location.InstalledPart == null)
            {
                return false;
            }

            GameObject installed = location.InstalledPart;
            return installed == gameObject ||
                transform.IsChildOf(installed.transform);
        }

        private void ResolveWearState()
        {
            wearState = GetComponentInParent<ItemWearState>();

            if (wearState == null)
            {
                wearState = GetComponentInChildren<ItemWearState>(true);
            }
        }

        private void SetActiveState(bool active, bool force = false)
        {
            if (!force && isActive == active)
            {
                return;
            }

            isActive = active;

            if (calmState != null)
            {
                calmState.SetActive(!active);
            }

            if (activeState != null)
            {
                activeState.SetActive(active);
            }
        }
    }
}
