namespace Coursework_Master_Degree.Items.Contracts
{
    /// <summary>
    /// Simple interactables that provide their own message while the owning
    /// printer is busy. The hover system still resolves them instead of
    /// suppressing the prompt outright.
    /// </summary>
    public interface IPrinterBusyInteraction
    {
        bool AllowsPrinterBusyHover { get; }
    }
}
