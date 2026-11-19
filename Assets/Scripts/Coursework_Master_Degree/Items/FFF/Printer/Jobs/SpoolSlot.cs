namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    public enum SpoolSlot
    {
        Left,
        Right
    }

    public static class SpoolSlotSettings
    {
        public static string Label(SpoolSlot slot)
        {
            return slot == SpoolSlot.Right ? "Right" : "Left";
        }
    }
}
