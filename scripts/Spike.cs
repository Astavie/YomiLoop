using Godot;

public partial class Spike : Area2D
{
    private void OnBodyEntered(Node2D body)
    {
        if (body is Robo robo) robo.IsDead = true;
    }
}
