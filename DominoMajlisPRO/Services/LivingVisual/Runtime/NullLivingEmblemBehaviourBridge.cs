namespace DominoMajlisPRO.Services.LivingVisual.Runtime;

public sealed class NullLivingEmblemBehaviourBridge : ILivingEmblemBehaviourBridge
{
    public void Apply(LivingEmblemRuntimeContext context, LivingEmblemBehaviourDecision decision)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (decision is null)
        {
            throw new ArgumentNullException(nameof(decision));
        }
    }
}
