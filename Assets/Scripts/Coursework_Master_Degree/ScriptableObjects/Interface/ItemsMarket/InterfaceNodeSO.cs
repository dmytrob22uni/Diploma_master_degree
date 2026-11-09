using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Interface.ItemsMarket.Types;

namespace Coursework_Master_Degree.ScriptableObjects.Interface.ItemsMarket
{
    [CreateAssetMenu(fileName = "User_interface_node_SO", menuName = "User Interface Network/User Interface Node", order = 0)]
    public class InterfaceNodeSO : ScriptableObject
    {
        public InterfaceNodeType InterfaceNodeType;

        public bool IsRegularPickType() { return InterfaceNodeType == InterfaceNodeType.RegularPick; }
        [ShowIf("IsRegularPickType")]
        public List<OptionDefinition> NamesToInterfaceNodeMetaDatasList;

        public bool IsScrollablePickType() { return InterfaceNodeType == InterfaceNodeType.ScrollablePick; }
        [ShowIf("IsScrollablePickType"), Expandable]
        public List<ItemMetasListSO> ItemMetasList;
    }
}
