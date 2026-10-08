using System;

namespace CatsAndArrows.Core.Localization
{
    /// <summary>Issue #4: presentation of generation outcomes, independent of technical diagnostics.</summary>
    public sealed class GenerationLocalizer
    {
        private readonly LocalizationService text;

        public GenerationLocalizer(LocalizationService text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public string Format(GenerationStatus status)
        {
            switch (status)
            {
                case GenerationStatus.Success: return text.GetText("generation.success");
                case GenerationStatus.InvalidProfile: return text.GetText("generation.invalid-profile");
                case GenerationStatus.UnsupportedProfile: return text.GetText("generation.unsupported-profile");
                case GenerationStatus.Exhausted: return text.GetText("generation.exhausted");
                case GenerationStatus.Cancelled: return text.GetText("generation.cancelled");
                default: throw new ArgumentOutOfRangeException(nameof(status));
            }
        }
    }
}
