using System.Linq;
using Godot;

public partial class ControlUI : Control
{
    [Export] public bool EnableHover { get; set; } = true;
    [Export] public bool EnableRocket { get; set; } = true;

    private Control Buttons;

    public override void _Ready() {
        GetNode<Physics>("/root/Physics").StateChanged += OnStateChanged;
        
        Buttons = GetNode<Control>("%Buttons");
        Buttons.GetChildren().OfType<ControlButton>().ToList().ForEach(b => b.Disable());
        if (!EnableHover) GetNode<ControlButton>("%Buttons/Hover").Disable(forever:true);
        if (!EnableRocket) GetNode<ControlButton>("%Buttons/Rocket").Disable(forever:true);
    }

    public override void _ExitTree()
    {
        GetNode<Physics>("/root/Physics").StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(PlayState _, PlayState next) {
        if (next is PlayState.Running) {
            Buttons.GetChildren().OfType<ControlButton>().ToList().ForEach(b => b.Disable());
        } else {
            Robo me = GetNode<Physics>("/root/Physics").Me;
            Buttons.GetChildren().OfType<ControlButton>().ToList().ForEach(b => {
                if (b.IsLegal?.Invoke(me) ?? true) b.Enable();
            });
        }
    }
}
