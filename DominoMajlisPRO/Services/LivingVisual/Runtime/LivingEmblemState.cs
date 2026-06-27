namespace DominoMajlisPRO.Services.LivingVisual.Runtime;

/// <summary>
/// Runtime behaviour state for a living emblem.
/// This describes behavioural intent only; rendering remains delegated to the existing Filament pipeline.
/// </summary>
public enum LivingEmblemState
{
    None = 0,
    Idle = 1,
    Breathing = 2,
    Blink = 3,
    LookAround = 4,
    MouthMotion = 5,
    Attention = 6,
    Rest = 7
}
