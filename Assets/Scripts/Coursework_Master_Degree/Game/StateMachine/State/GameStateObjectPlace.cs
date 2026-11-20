using System;
using UnityEngine;
using Coursework_Master_Degree.Game.Controller;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Items.Computing;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.FFF.Jobs;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Game.StateMachine.State
{
    /// <summary>
    /// Contextual M2.5 placement state. The object is moved on the plate,
    /// rotated and scaled as a draft; confirming stores the placement back on
    /// the console job and returns to the console.
    /// </summary>
    public class GameStateObjectPlace : GameState
    {
        public Device Device { get; }
        public Printer Printer { get; }
        public ObjectToPrintMetaSO Meta { get; }
        public InfillPattern Infill { get; }
        public SupportsMode Supports { get; }
        public LayerHeightPreset LayerHeight { get; }
        public PrintSpeedPreset Speed { get; }
        public PrintPlacement PreviousPlacement { get; }
        public GameState ReturnState { get; }

        private readonly Action<PrintPlacement> onConfirmed;
        private ObjectPlacementController controller;
        private bool isTransitioning;

        public GameStateObjectPlace(
            Device device,
            Printer printer,
            ObjectToPrintMetaSO meta,
            InfillPattern infill,
            SupportsMode supports,
            LayerHeightPreset layerHeight,
            PrintSpeedPreset speed,
            PrintPlacement previousPlacement,
            GameState returnState,
            Action<PrintPlacement> onConfirmed = null)
        {
            Device = device;
            Printer = printer;
            Meta = meta;
            Infill = infill;
            Supports = supports;
            LayerHeight = layerHeight;
            Speed = speed;
            PreviousPlacement = previousPlacement;
            ReturnState = returnState;
            this.onConfirmed = onConfirmed;
        }

        public override void OnEnter()
        {
            ShowCursor();

            GameStateManager manager = GameStateManager.Instance;

            if (manager == null)
            {
                return;
            }

            manager.InputReader.EnableObjectPlaceStateInput();

            controller = ObjectPlacementController.Instance;

            if (controller == null)
            {
                controller = manager.gameObject.AddComponent<ObjectPlacementController>();
            }

            controller.Begin(this);
            Printer?.EnsureHomePosition();
        }

        public override void OnExit()
        {
            GameStateManager manager = GameStateManager.Instance;

            if (controller != null)
            {
                controller.End();
                controller = null;
            }

            if (manager != null)
            {
                manager.InputReader.DisableAllInput();
            }

            HideCursor();
        }

        public void Confirm(PrintPlacement placement)
        {
            if (isTransitioning)
            {
                return;
            }

            isTransitioning = true;
            onConfirmed?.Invoke(placement);
            ReturnToConsole();
        }

        public void Cancel()
        {
            if (isTransitioning)
            {
                return;
            }

            isTransitioning = true;
            ReturnToConsole();
        }

        private void ReturnToConsole()
        {
            GameState target = ReturnState ?? new GameStateFreeWalk();
            GameStateManager.Instance.ChangeState(target);
        }
    }
}
