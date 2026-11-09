using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Contracts;

namespace Coursework_Master_Degree.Items.Computing
{
    public class Device : MonoBehaviour, IInteractable
    {
        public IReadOnlyList<InteractionOption> GetInteractions()
        {
            throw new System.NotImplementedException();
        }
    }
}
