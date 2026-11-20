using System;
using UnityEngine;
using Coursework_Master_Degree.Items.Part;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Typed runtime key for a printer spool slot. The stable string value is
    /// still what drafts and future persistence store; the wrapper keeps
    /// request/event signatures explicit and avoids accidental slot-id swaps.
    /// </summary>
    [Serializable]
    public readonly struct SpoolSlotId : IEquatable<SpoolSlotId>
    {
        public static readonly SpoolSlotId None = new SpoolSlotId(null);

        private readonly string value;

        public SpoolSlotId(string value)
        {
            this.value = value;
        }

        public string Value => value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public bool Equals(SpoolSlotId other)
        {
            return string.Equals(
                Value,
                other.Value,
                StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SpoolSlotId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(SpoolSlotId left, SpoolSlotId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SpoolSlotId left, SpoolSlotId right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// One selectable filament spool socket. Slots are data-driven so a
    /// printer can expose one, two, or up to four material sources without
    /// changing the job flow. Only one slot is active for a job.
    /// </summary>
    [Serializable]
    public sealed class SpoolSlotDefinition
    {
        [Tooltip("Stable id used by job requests and console drafts.")]
        public string Id;

        [Tooltip("Label shown in the console's SPOOL row.")]
        public string Label;

        [Tooltip("Printer socket that holds this spool.")]
        public PartLocation Location;

        public string DisplayLabel =>
            string.IsNullOrWhiteSpace(Label) ? Id : Label;
    }
}
