using System;

namespace AFCT.SfxLab
{
    /// <summary>
    /// Display metadata for one preset: the enum value together with the label and the
    /// group shown in the preset browser.
    /// </summary>
    public readonly struct SfxPresetInfo
    {
        public SfxPresetInfo(SfxPresetId preset, string displayName, string category)
        {
            Preset = preset;
            DisplayName = displayName;
            Category = category;
        }

        /// <summary>Preset this entry describes.</summary>
        public readonly SfxPresetId Preset;

        /// <summary>Human readable preset name, e.g. "Pickup Coin".</summary>
        public readonly string DisplayName;

        /// <summary>UI, Action, Cinematic, Footsteps or Utility.</summary>
        public readonly string Category;
    }
}
