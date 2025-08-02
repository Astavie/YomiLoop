using Godot;
using System;

public partial class Goal : Thing
{
    
    public override void _Ready()
    {
        base._Ready();
        if (IsPreview)
        {
            CollisionMask |= 16;
        }
        else
        {
            CollisionMask |= 32;
        }
        
        GetNode<Area2D>("Area2D").BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Robo robo && robo.IsPreview == IsPreview)
            OnRoboEntered(robo);
    }

    private void OnRoboEntered(Robo robo)
    {
        if (!IsPreview)
        {
            physics.GoalAction?.Invoke(this, robo);
        }
        else
        {
            // TODO: what do we do on preview?
        }
    }
}
