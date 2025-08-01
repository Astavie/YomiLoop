using Godot;

public partial class ControlUI : Control
{
    [Export] public bool EnableHover { get; set; } = true;
    [Export] public bool EnableRocket { get; set; } = true;

    private Control Buttons;

    public override void _Ready() {
        GetNode<Physics>("/root/Physics").StateChanged += OnStateChanged;
        
        Buttons = GetNode<Control>("%Buttons");
        if (!EnableHover) GetNode<ControlButton>("%Buttons/Hover").Disable(forever:true);
        if (!EnableRocket) GetNode<ControlButton>("%Buttons/Rocket").Disable(forever:true);
    }

    private void OnStateChanged(PlayState _, PlayState next) {
        if (next is PlayState.Running) {
            for (int i = 0; i < Buttons.GetChildCount(); i++) {
                Buttons.GetChild<ControlButton>(i).Disable();
            }
        } else {
            Robo me = GetNode<Physics>("/root/Physics").Me;
            for (int i = 0; i < Buttons.GetChildCount(); i++) {
                ControlButton controlButton = Buttons.GetChild<ControlButton>(i);
                if (controlButton.Move.IsLegal?.Invoke(me) ?? true) controlButton.Enable();
            }
        }
    }
}
