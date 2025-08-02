using Godot;
using System;

public partial class ControlButton : TextureButton {
    private readonly Color BaseColor = Colors.White;
    private readonly Color HoverColor = Colors.CornflowerBlue;
    private readonly Color DisabledColor = Colors.DarkSlateGray;

    public Move Move { get; set; }
    
    private bool _disabledForever = false;
    
    public override void _Ready() {
        if (Disabled) Modulate = DisabledColor;
        MouseEntered += () => {
            if (!Disabled) Modulate = HoverColor;
        };
        MouseExited += () => {
            if (!Disabled) Modulate = BaseColor;
        };
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
}
