using Godot;
using System;

public partial class StartButton : Godot.Button
{    [Export]
    public PackedScene FirstLevel { get; set; }

    public override void _Ready()
    {
        base.Pressed += OnPressed;
    }

    private void OnPressed()
    {
        GetNode<Wipe>("/root/Wipe").DoWipe(Start, playSound:true);
    }

    private void Start()
    {
        GetNode<Physics>("/root/Physics").OnLevelEnd();
        GetTree().ChangeSceneToPacked(FirstLevel);
    }
}
