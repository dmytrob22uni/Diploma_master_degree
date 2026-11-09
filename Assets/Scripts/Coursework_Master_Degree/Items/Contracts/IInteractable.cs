using UnityEngine;
using System.Collections.Generic;

namespace Coursework_Master_Degree.Items.Contracts
{
    public interface IInteractable
    {
        public IReadOnlyList<InteractionOption> GetInteractions();
    }
}
