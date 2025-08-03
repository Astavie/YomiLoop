using Godot;
using System;

public partial class ControlButton : TextureButton {
    private readonly Color BaseColor = Colors.White;
    private readonly Color HoverColor = Colors.CornflowerBlue;
    private readonly Color DisabledColor = Colors.DarkSlateGray;
    private PopupPanel _panel;
    
    [Signal]
    public delegate void UsedEventHandler();

    [Export] public string Label;
    [Export] public Key Key;
    [Export] public Key Key2;

    public Predicate<Robo> IsLegal { get; set; } = o => !o.AboutToDie(); 
    
    private bool _disabledForever = false;
    
    public override void _Ready()
    {
        base.Pressed += EmitSignalUsed;
        
        if (Disabled) Modulate = DisabledColor;
        MouseEntered += () =>
        {
            string text = Label;
            if (_disabledForever) text = "???";
            GetNode<Label>("%Buttons/../../Label").Text = text;
            if (!Disabled) Modulate = HoverColor;
        };
        MouseExited += () => {
            if (!Disabled) Modulate = BaseColor;
        };

        if (GetParent().GetParent() is PopupPanel panel)
            _panel = panel;
    }

    public void Disable(bool forever = false, Texture2D overrideTex = null) {
        Modulate = DisabledColor;
        Disabled = true;
        if (forever) _disabledForever = true;
        if (overrideTex is not null) TextureNormal = overrideTex;
    }
    
    public void Enable() {
        if (_disabledForever) return;
        Modulate = BaseColor;
        Disabled = false;
    }

    public override void _Input(InputEvent @event)
    {
        bool visible = _panel?.Visible ?? true;
        if (Disabled || !visible) return;
        if (@event is InputEventKey { Pressed: true } key && (key.Keycode == Key || key.Keycode == Key2))
        {
            if (Label.StartsWith("Perform"))
                GetNode<Label>("%Buttons/../../Label").Text = "";
            else if (_panel is null)
                GetNode<Label>("%Buttons/../../Label").Text = Label;
            _panel?.Hide();
            EmitSignalUsed();
        }
    }
}
