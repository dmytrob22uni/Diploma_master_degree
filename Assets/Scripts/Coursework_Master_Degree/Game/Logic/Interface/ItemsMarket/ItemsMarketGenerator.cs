using System;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Interface.ItemsMarket;
using Coursework_Master_Degree.ScriptableObjects.Interface.ItemsMarket.Types;
using Coursework_Master_Degree.Game.Logic.Interface.ItemsMarket.ScrollablePick;
using Coursework_Master_Degree.Game.Logic.Interface.ItemsMarket.PrefabData.RegularPick;
using Coursework_Master_Degree.Game.Logic.Interface.ItemsMarket.PrefabData.ScrollablePick;

namespace Coursework_Master_Degree.Game.Logic.Interface.ItemsMarket
{
    public class ItemsMarketGenerator : MonoBehaviour
    {
        public event Action<ItemMetaSO> OnPickItemEvent;

        [Header("Core Interface Node")]
        [Expandable]
        public InterfaceNodeSO CoreInterfaceNodeMetaData;

        [Header("Regular Pick Prefabs")]
        public GameObject RegularPickCanvas;
        public GameObject Option;

        [Header("Scrollable Pick Prefabs")]
        public GameObject ScrollablePickCanvas;
        public GameObject Row;
        public GameObject Item;

        private GameObject corePickCanvas;

        private InterfaceNodeType lastActiveInterfaceNodeType;
        private GameObject lastActivePickCanvas;

        private void OnEnable()
        {
            corePickCanvas.SetActive(true);
        }

        void Awake()
        {
            corePickCanvas = CreatePickCanvas(CoreInterfaceNodeMetaData);

            lastActivePickCanvas = corePickCanvas;
            lastActiveInterfaceNodeType = CoreInterfaceNodeMetaData.InterfaceNodeType;
        }

        private GameObject CreatePickCanvas(InterfaceNodeSO interfaceNodeSO)
        {
            switch (interfaceNodeSO.InterfaceNodeType)
            {
                case InterfaceNodeType.RegularPick:
                    return CreateRegularPickCanvas(interfaceNodeSO);
                case InterfaceNodeType.ScrollablePick:
                    return CreateScrollablePickCanvas(interfaceNodeSO);
                default:
                    Debug.LogError("Unexpected user interface node type");
                    throw new System.Exception("Unexpected user interface node type");
            }
        }

        private GameObject CreateRegularPickCanvas(InterfaceNodeSO interfaceNodeSO)
        {
            GameObject regularPickCanvasGameObject;

            // create regular item picker
            regularPickCanvasGameObject = Instantiate(RegularPickCanvas, gameObject.transform);
            regularPickCanvasGameObject.SetActive(false);
            // retrieve data holder
            RegularPickCanvasDataHolder regularPickCanvas = regularPickCanvasGameObject.GetComponent<RegularPickCanvasDataHolder>();
            // get content
            GameObject content = regularPickCanvas.Content;

            // loop to create options
            int optionsCount = interfaceNodeSO.NamesToInterfaceNodeMetaDatasList.Count;
            for (int optionIndex = 0; optionIndex < optionsCount; optionIndex++)
            {
                // create option
                GameObject optionGameObject = Instantiate(Option, content.transform);
                // retrieve data holder
                OptionDataHolder option = optionGameObject.GetComponent<OptionDataHolder>();

                // get name
                string name = interfaceNodeSO.NamesToInterfaceNodeMetaDatasList[optionIndex].Name;
                // set name
                option.OptionText.text = name;

                // get child level user interface node
                InterfaceNodeSO childInterfaceNodeSO = interfaceNodeSO.NamesToInterfaceNodeMetaDatasList[optionIndex].InterfaceNodeSO;
                // get child level user interface node type
                InterfaceNodeType childInterfaceNodeType = childInterfaceNodeSO.InterfaceNodeType;
                // get item picker for button onclick event
                GameObject childPickCanvas = CreatePickCanvas(childInterfaceNodeSO);

                // get button component
                Button optionButton = optionGameObject.GetComponent<Button>();
                // set button onclick event
                optionButton.onClick.AddListener(
                    () =>
                    {
                        ResetLastActivePickCanvas();

                        regularPickCanvasGameObject.SetActive(false);
                        childPickCanvas.SetActive(true);

                        lastActiveInterfaceNodeType = childInterfaceNodeType;
                        lastActivePickCanvas = childPickCanvas;
                    });
            }

            return regularPickCanvasGameObject;
        }

        private GameObject CreateScrollablePickCanvas(InterfaceNodeSO interfaceNodeSO)
        {
            GameObject scrollablePickCanvasGameObject;

            // create scrollable item picker
            scrollablePickCanvasGameObject = Instantiate(ScrollablePickCanvas, gameObject.transform);
            scrollablePickCanvasGameObject.SetActive(false);
            // retrieve data holder
            ScrollablePickCanvasDataHolder scrollablePickCanvas = scrollablePickCanvasGameObject.GetComponent<ScrollablePickCanvasDataHolder>();
            // get content
            GameObject content = scrollablePickCanvas.Content;

            // loop to create rows
            int rowsCount = interfaceNodeSO.ItemMetasList.Count;
            for (int rowIndex = 0; rowIndex < rowsCount; rowIndex++)
            {
                // create row
                GameObject rowGameObject = Instantiate(Row, content.transform);
                // retrieve data holder
                RowDataHolder row = rowGameObject.GetComponent<RowDataHolder>();

                // get horizontal scroll rect
                RowScrollRect rowScrollRect = row.RowScrollRect;
                // set parent vertical scroll rect
                rowScrollRect.ParentScrollRect = scrollablePickCanvas.VerticalScrollRect;

                // set header text
                row.HeaderText.text = interfaceNodeSO.ItemMetasList[rowIndex].ItemsListName;

                // get content
                GameObject rowContent = row.Content;

                // loop to create items
                int itemsCount = interfaceNodeSO.ItemMetasList[rowIndex].ItemMetasList.Length;
                for (int itemIndex = 0; itemIndex < itemsCount; itemIndex++)
                {
                    // make item variable to easily access its data
                    // and
                    // capture item to avoid closure-capture-in-a-loop issue for row/item indexes used inside lambda
                    ItemMetaSO itemMeta = interfaceNodeSO.ItemMetasList[rowIndex].ItemMetasList[itemIndex];

                    // create item
                    GameObject itemGameObject = Instantiate(Item, rowContent.transform);
                    // retrieve data holder
                    ItemDataHolder item = itemGameObject.GetComponent<ItemDataHolder>();

                    // set item so
                    item.ItemMetaData = itemMeta;

                    // set description header text field
                    item.HeaderText = scrollablePickCanvas.HeaderText;
                    // set description body text field
                    item.DescriptionText = scrollablePickCanvas.DescriptionText;

                    // set description text data of the first item of the first row
                    if (rowIndex == 0 && itemIndex == 0)
                    {
                        string header = itemMeta.Name;
                        string description = itemMeta.Description;

                        item.HeaderText.text = header;
                        item.DescriptionText.text = description;
                    }

                    // get item image and create sprite out of it
                    Texture2D image = itemMeta.Icon;
                    Sprite imageSprite = Sprite.Create(
                        image,
                        new Rect(0, 0, image.width, image.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    // set item image
                    item.IconImage.sprite = imageSprite;
                    // get item name text
                    string name = itemMeta.Name;
                    // set item name text
                    item.NameText.text = name;
                    // get item price text
                    float price = itemMeta.Price;
                    // set item price text
                    item.PriceText.text = price + " $";

                    // subscribe to pointer enter action
                    HoverManager itemHoverManager = itemGameObject.GetComponent<HoverManager>();
                    itemHoverManager.OnPointerEnterAction +=
                        () =>
                        {
                            item.HeaderText.text = itemMeta.Name;
                            item.DescriptionText.text = itemMeta.Description;
                        };

                    Button itemButton = itemGameObject.GetComponent<Button>();
                    itemButton.onClick.AddListener(
                    () =>
                    {
                        ResetLastActivePickCanvas();

                        scrollablePickCanvasGameObject.SetActive(false);

                        lastActiveInterfaceNodeType = InterfaceNodeType.ScrollablePick;
                        lastActivePickCanvas = scrollablePickCanvasGameObject;

                        OnPickItemEvent?.Invoke(item.ItemMetaData);
                    });
                }
            }

            return scrollablePickCanvasGameObject;
        }

        private void ResetLastActivePickCanvas()
        {
            switch (lastActiveInterfaceNodeType)
            {
                case InterfaceNodeType.RegularPick:
                    ResetRegularPickCanvas();
                    break;
                case InterfaceNodeType.ScrollablePick:
                    ResetScrollablePickCanvas();
                    break;
                default:
                    Debug.LogError("Unexpected user interface node type");
                    throw new System.Exception("Unexpected user interface node type");
            }
        }

        private void ResetRegularPickCanvas()
        {
            // reset vertical scroll of item regular picker
            RegularPickCanvasDataHolder regularPickCanvas = lastActivePickCanvas.GetComponent<RegularPickCanvasDataHolder>();
            // reset position
            regularPickCanvas.VerticalScrollRect.verticalNormalizedPosition = 1.0f;
        }

        private void ResetScrollablePickCanvas()
        {
            // reset vertical scroll of item scrollable picker
            ScrollablePickCanvasDataHolder scrollablePickCanvas = lastActivePickCanvas.GetComponent<ScrollablePickCanvasDataHolder>();
            // reset position
            scrollablePickCanvas.VerticalScrollRect.verticalNormalizedPosition = 1.0f;

            // reset horizontal scroll of each row
            int rowsCount = scrollablePickCanvas.Content.transform.childCount;
            for (int rowIndex = 0; rowIndex < rowsCount; rowIndex++)
            {
                GameObject rowGameObject = scrollablePickCanvas.Content.transform.GetChild(rowIndex).gameObject;
                RowDataHolder row = rowGameObject.GetComponent<RowDataHolder>();
                // reset position
                row.RowScrollRect.horizontalNormalizedPosition = 0.0f;
            }
        }

        public void ApplyQuitItemsMarket()
        {
            ResetLastActivePickCanvas();
            lastActivePickCanvas.SetActive(false);
        }
    }
}
