using UnityEngine;
using UnityEngine.EventSystems;
using Action = System.Action;

namespace Coursework_Master_Degree.Game.Logic.Interface.ItemsMarket.PrefabData.ScrollablePick
{
    public class HoverManager : MonoBehaviour, IPointerEnterHandler
    {
        public Action OnPointerEnterAction;

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnterAction?.Invoke();
        }

        public void ResetSelection()
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
