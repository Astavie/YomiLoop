using Godot;
using System;

public partial class TimeLeft : TextureRect {
    [Export] public Texture2D Full { get; set; }
    [Export] public Texture2D ThreeQuarters { get; set; }
    [Export] public Texture2D Half { get; set; }
    [Export] public Texture2D Quarter { get; set; }
    [Export] public Texture2D Empty { get; set; }
    
    private Physics physics;

    private float TimeLeftFraction(Robo robo) {
        int extraLifeTime = robo.LifeTime - physics.LifeTime;
        int adjustedAge = robo.Age - extraLifeTime;
        return (float)(physics.LifeTime - adjustedAge) / physics.LifeTime;
    }

    public override void _Ready() {
        physics = GetNode<Physics>("/root/Physics");
        physics.StateChanged += OnStateChanged;
        Texture = Full;
    }

    public override void _ExitTree()
    {
        physics.StateChanged -= OnStateChanged;
    }

    public override void _Process(double delta) {
        if (physics.State is PlayState.Running) {
            Texture = TimeLeftFraction(physics.Me) switch {
                > 0.75f => Full,
                > 0.5f => ThreeQuarters,
                > 0.25f => Half,
                > 0 => Quarter,
                _ => Empty
            };
        }
    }

    private void OnStateChanged(PlayState current, PlayState next) {
        if (current == PlayState.Running && next == PlayState.Preview) {
            Texture = TimeLeftFraction(physics.Me) switch {
                > 0.75f => Full,
                > 0.5f => ThreeQuarters,
                > 0.25f => Half,
                > 0 => Quarter,
                _ => Empty
            };
        }
    }
}
