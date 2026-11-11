using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Game.Logic.Interface.ItemsMarket;
using Coursework_Master_Degree.Game.Logic.Interface.InteractionsList;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Controller
{
    public class ItemController : MonoBehaviour
    {
        [Header("Logic Components")]
        [HorizontalLine(1.0f)]
        [Header("ItemsMarket Logics")]
        public ItemsMarketGenerator ItemsMarketGenerator;
        [Header("InteractionsList Logics")]
        public InteractionsListGenerator InteractionsListGenerator;

        public void SetItemsMarketState(bool enable)
        {
            if (enable)
            {
                // ItemsMarket Logics
                ItemsMarketGenerator.OnPickItemEvent += HandlePickItem;
            }
            else
            {
                // ItemsMarket Logics
                ItemsMarketGenerator.OnPickItemEvent -= HandlePickItem;
            }
        }

        public void SetInteractionsListState(bool enable, IReadOnlyList<InteractionOption> interactions)
        {
            if (enable)
            {
                // InteractionsList Logics
                InteractionsListGenerator.OnPickInteractionEvent += HandlePickInteraction;
                InteractionsListGenerator.SetInteractions(interactions);
            }
            else
            {
                // InteractionsList Logics
                InteractionsListGenerator.OnPickInteractionEvent -= HandlePickInteraction;
            }
        }

        // === Handler methods ===

        // ItemsMarket

        private void HandlePickItem(ItemMetaSO itemMeta)
        {
            var itemGameObject = GameStateManager.Instance.ItemInstancer.InstantiateItem(itemMeta);
            itemGameObject.SetActive(false);
            GameStateManager.Instance.ChangeState(new GameStateItemPlace(itemGameObject));
        }

        // InteractionsList

        private void HandlePickInteraction(InteractionOption interaction)
        {
            interaction.OnExecute?.Invoke();
        }
    }
}
