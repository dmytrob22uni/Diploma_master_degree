using TMPro;
using System;
using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;
using System.Numerics;

namespace Coursework_Master_Degree.Game.Logic.Hover
{
    public class ItemHoverInteract : MonoBehaviour
    {
        [Header("Hover Settings")]
        [Tooltip(
            "Use player eyes instead of camera, because current setup " +
            "uses Cinemachine, which catch up the player camera in LateUpdate, " +
            "(not in Update, which is default / expectable place to raycast)")]
        public Transform Eyes;
        public float HoverDistance;
        [Tooltip(
            "Layers against which to check for item, e.g.:\n" +
            "Surface\n" +
            "Item\n" +
            "Part\n" +
            "Printer\n" +
            "Enclosure\n" +
            "ObjectToPrint\n" +
            "etc.")]
        public LayerMask ItemLayer;

        [Header("Interface Units")]
        public TextMeshProUGUI InteractText;
        public GameObject InteractRoot;
        private readonly string defaultInteractText = "Interact";

        private Collider cachedCollider;

        private IInteractable cachedInteractable;
        public IInteractable CachedInteractable => cachedInteractable;
        private IReadOnlyList<InteractionOption> cachedInteractions;
        private Action cachedAction;

        private bool isHoverClear;

        private void Awake()
        {
            ClearHover();
        }

        private void Update()
        {
            PerformHoverCheck();
        }

        private bool IsCameraRaycastHit(out RaycastHit hit)
        {
            Ray ray = new Ray(
                Eyes.position,
                Eyes.forward);

            return Physics.Raycast(
                ray,
                out hit,
                HoverDistance,
                ItemLayer,
                QueryTriggerInteraction.Ignore);
        }

        private void PerformHoverCheck()
        {
            if (IsCameraRaycastHit(out RaycastHit hit))
            {
                if (hit.collider == cachedCollider)
                {
                    return;
                }

                // simple interactable
                if (hit.collider.TryGetComponent<IInteractable>(out var simple))
                {
                    if (simple == cachedInteractable)
                    {
                        return;
                    }

                    cachedInteractable = simple;

                    var interactions = simple.GetInteractions();
                    if (interactions.Count > 1)
                    {
                        return;
                    }

                    cachedAction = interactions[0].OnExecute;

                    string info = interactions[0].Info;
                    SetupInteract(info);

                    return;
                }

                cachedCollider = hit.collider;

                // prepare interfaces
                ClearCached();
                ClearHover();

                if (hit.collider.attachedRigidbody == null)
                {
                    return;
                }

                // complex interactable
                if (hit.collider.attachedRigidbody.TryGetComponent<PartsAssembly>(out var _) &&
                    hit.collider.attachedRigidbody.TryGetComponent<IInteractable>(out var complex))
                {
                    if (complex == cachedInteractable)
                    {
                        return;
                    }

                    cachedInteractable = complex;
                    var interactions = complex.GetInteractions();
                    if (interactions.Count < 2)
                    {
                        return;
                    }

                    cachedInteractions = interactions;
                    cachedAction = ShowInteractions;

                    string info = defaultInteractText;
                    SetupInteract(info);

                    return;
                }

                return;
            }

            if (!isHoverClear)
            {
                ClearCachedCollider();
                ClearCached();
                ClearHover();
            }
        }

        private void SetupInteract(string info)
        {
            InteractText.text = info;
            InteractRoot.SetActive(true);

            isHoverClear = false;
        }

        private void ShowInteractions()
        {
            GameStateManager.Instance.ChangeState(
                new GameStateInteractionsList(cachedInteractions));
        }

        private void ClearCachedCollider()
        {
            cachedCollider = default;
        }

        private void ClearCached()
        {
            cachedInteractable = default;
            cachedInteractions = default;
            cachedAction = default;
        }

        private void ClearHover()
        {
            InteractText.text = defaultInteractText;
            InteractRoot.SetActive(false);

            isHoverClear = true;
        }

        private void OnDisable()
        {
            ClearCachedCollider();
            ClearCached();
            ClearHover();
        }

        // === Player Controller invoked ===

        public void ApplyInteractWithItem()
        {
            cachedAction?.Invoke();
        }
    }
}
