namespace AFCT.SfxLab
{
    /// <summary>Which synthesis engine renders a sound. Mirrors the TS <c>SfxEngine</c> union.</summary>
    public enum SfxEngine
    {
        Sfxr = 0,
        Footsteppr = 1,
    }

    /// <summary>Base waveform for the sfxr engine. Mirrors the TS <c>SfxWaveform</c> union.</summary>
    public enum SfxWaveform
    {
        Square = 0,
        Sawtooth = 1,
        Sine = 2,
        Noise = 3,
    }

    /// <summary>Ground material for the footstep engine. Mirrors the TS <c>SfxFootstepTerrain</c> union.</summary>
    public enum SfxFootstepTerrain
    {
        Snow = 0,
        Grass = 1,
        Dirt = 2,
        Gravel = 3,
        Mud = 4,
        Wood = 5,
        Metal = 6,
        Water = 7,
    }

    /// <summary>Preset recipes, in the canonical <c>SFX_PRESET_ORDER</c> order.</summary>
    public enum SfxPresetId
    {
        Random = 0,
        Mutate = 1,
        Click = 2,
        BlipSelect = 3,
        HoverTick = 4,
        NotifyPop = 5,
        SuccessChime = 6,
        ErrorBuzz = 7,
        PickupCoin = 8,
        LaserShoot = 9,
        Explosion = 10,
        Powerup = 11,
        HitHurt = 12,
        Jump = 13,
        Whoosh = 14,
        Riser = 15,
        Impact = 16,
        Glitch = 17,
        Synth = 18,
        Tone = 19,
        FootstepSnow = 20,
        FootstepGrass = 21,
        FootstepDirt = 22,
        FootstepGravel = 23,
        FootstepMud = 24,
        FootstepWood = 25,
        FootstepMetal = 26,
        FootstepWater = 27,
    }
}
