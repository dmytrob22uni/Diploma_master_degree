using System;
using NaughtyAttributes;

namespace Coursework_Master_Degree.ScriptableObjects.Interface.ItemsMarket
{
    [Serializable]
    public class OptionDefinition
    {
        public string Name;
        [Expandable]
        public InterfaceNodeSO InterfaceNodeSO;
    }

}
