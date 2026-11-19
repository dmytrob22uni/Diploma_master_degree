namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Small state holder for the printer. The Printer owns the tick and the
    /// transition rules; this class keeps the current state and exposes the
    /// common state predicates used by validation and interaction guards.
    /// </summary>
    public sealed class PrinterStateMachine
    {
        public PrinterOperationalState State { get; private set; } =
            PrinterOperationalState.Idle;

        public void Enter(PrinterOperationalState state)
        {
            State = state;
        }

        public bool IsActiveJobState =>
            State == PrinterOperationalState.Preheating ||
            State == PrinterOperationalState.Printing ||
            State == PrinterOperationalState.Paused ||
            State == PrinterOperationalState.Cooling ||
            State == PrinterOperationalState.Calibrating;

        public bool CanStartJob =>
            State == PrinterOperationalState.Idle;
    }
}
