using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Contracts;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Items.FFF
{
    public class FFFInteractions : MonoBehaviour, IInteractable
    {
        public IReadOnlyList<InteractionOption> GetInteractions()
        {
            PartsAssembly partsAssembly = GetComponent<PartsAssembly>();
            ItemDataHolder itemDataHolder = GetComponent<ItemDataHolder>();

            var options = new List<InteractionOption>
            {
                new() {
                    Info = "Parts wear",
                    OnExecute =
                    () =>
                    {
                        GameStateManager.Instance.ChangeState(
                            new GameStatePartsWear(
                                partsAssembly,
                                itemDataHolder));
                    }
                },
                new()
                {
                    Info = "Parts remove",
                    OnExecute =
                    () =>
                    {
                        GameStateManager.Instance.ChangeState(
                            new GameStatePartsRemove(
                                partsAssembly,
                                itemDataHolder));
                    }
                },
                new() {
                    Info = "Parts integrity",
                    OnExecute =
                        () => {
                            GameStateManager.Instance.ChangeState(
                                new GameStatePartsIntegrity(
                                    partsAssembly,
                                    itemDataHolder));
                        }
                }
            };

            return options;
        }
    }
}
