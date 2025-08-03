using Godot;
using System;

public partial class Goal : Thing
{
    private bool _justReset = false;
    
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

    public override void AfterFrame()
    {
        _justReset = false;
    }

    public override void Reset(Thing parent)
    {
        base.Reset(parent);
        _justReset = true;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_justReset || IsGrabbed) return;
        if (body is Robo robo && robo.IsPreview == IsPreview)
            OnRoboEntered(robo);
    }

    private void OnRoboEntered(Robo robo)
    {
        if (robo.IsFrozen && !robo.PastSelf) return;
        if (robo.IsFrozen)
        {
            robo.IsFrozen = false;
            robo.Velocity = Vector2.Zero;
        }
        robo.ForcedMove = Robo.Grab(this);
        robo.MoveFrame = 0;
        robo.MoveIndex = 0;
    }
}
