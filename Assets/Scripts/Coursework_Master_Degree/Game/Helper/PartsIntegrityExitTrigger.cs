using System;
using UnityEngine;

namespace Coursework_Master_Degree.Game.Helper
{
    public class PartsIntegrityExitTrigger : MonoBehaviour
    {
        public event Action<Collider> OnTriggerExited = delegate { };

        private void OnTriggerExit(Collider other)
        {
            OnTriggerExited?.Invoke(other);
        }
    }
}
