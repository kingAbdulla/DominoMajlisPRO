namespace DominoMajlisPRO.Services.LivingVisual.Runtime;

public sealed class LivingEmblemBehaviourRuntime
{
    private readonly LivingEmblemBehaviorController controller;
    private readonly ILivingEmblemBehaviourBridge bridge;
    private readonly LivingEmblemRuntimeContext context;

    public LivingEmblemBehaviourRuntime(
        LivingEmblemRuntimeContext context,
        LivingEmblemBehaviourProfile profile,
        ILivingEmblemBehaviourBridge bridge)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        controller = new LivingEmblemBehaviorController(profile ?? throw new ArgumentNullException(nameof(profile)));
        this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    public LivingEmblemState CurrentState => controller.CurrentState;

    public LivingEmblemBehaviourDecision Tick(double deltaSeconds)
    {
        var decision = controller.Tick(context, deltaSeconds);
        bridge.Apply(context, decision);
        return decision;
    }

    public void SetVisible(bool isVisible)
    {
        context.IsVisible = isVisible;
    }

    public void SetFocused(bool isFocused)
    {
        context.IsFocused = isFocused;
    }
}
