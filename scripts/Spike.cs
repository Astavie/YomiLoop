using Godot;

public partial class Spike : Area2D
{
    private void OnBodyEntered(Node2D body)
    {
        if (body is Robo robo) Robo.Loop.OnFrame(robo, 0);
    }
}
