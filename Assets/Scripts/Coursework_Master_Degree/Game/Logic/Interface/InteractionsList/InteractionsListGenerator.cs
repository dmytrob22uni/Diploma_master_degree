using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Game.Logic.Interface.ItemsMarket.PrefabData.RegularPick;

namespace Coursework_Master_Degree.Game.Logic.Interface.InteractionsList
{
    public class InteractionsListGenerator : MonoBehaviour
    {
        public event Action<InteractionOption> OnPickInteractionEvent;

        [Header("Interactions List Prefabs")]
        public GameObject InteractionsListCanvas;
        public GameObject Option;

        private IReadOnlyList<InteractionOption> interactions;
        private GameObject interactionsListCanvas;

        private void OnEnable()
        {
            CreateInteractionsListCanvas();
        }

        private void OnDisable()
        {
            DestroyInteractionsListCanvas();

            interactions = default;
        }

        public void SetInteractions(IReadOnlyList<InteractionOption> interactions)
        {
            this.interactions = interactions;

            if (isActiveAndEnabled)
            {
                CreateInteractionsListCanvas();
            }
        }

        private void CreateInteractionsListCanvas()
        {
            if (interactions == null)
            {
                return;
            }

            DestroyInteractionsListCanvas();

            // create interactions list
            interactionsListCanvas = Instantiate(InteractionsListCanvas, gameObject.transform);
            // retrieve data holder
            RegularPickCanvasDataHolder interactionsList = interactionsListCanvas.GetComponent<RegularPickCanvasDataHolder>();
            // get content
            GameObject content = interactionsList.Content;

            // loop to create options
            int optionsCount = interactions.Count;
            for (int optionIndex = 0; optionIndex < optionsCount; optionIndex++)
            {
                // capture interaction to avoid closure-capture-in-a-loop issue for option index used inside lambda
                InteractionOption interaction = interactions[optionIndex];

                // create option
                GameObject optionGameObject = Instantiate(Option, content.transform);
                // retrieve data holder
                OptionDataHolder option = optionGameObject.GetComponent<OptionDataHolder>();

                // set option text
                option.OptionText.text = interaction.Info;

                // get button component
                Button optionButton = optionGameObject.GetComponent<Button>();
                // set button onclick event
                optionButton.onClick.AddListener(
                    () => HandlePickInteraction(interaction));
            }
        }

        private void DestroyInteractionsListCanvas()
        {
            if (interactionsListCanvas == null)
            {
                return;
            }

            interactionsListCanvas.SetActive(false);
            Destroy(interactionsListCanvas);
            interactionsListCanvas = default;
        }

        private void HandlePickInteraction(InteractionOption interaction)
        {
            DestroyInteractionsListCanvas();

            OnPickInteractionEvent?.Invoke(interaction);
        }

        public void ApplyQuitInteractionsList()
        {
            DestroyInteractionsListCanvas();
        }
    }
}
