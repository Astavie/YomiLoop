using Godot;
using System;

public partial class Crate : Thing
{
    [Export] public bool CollidesPlayer = false;
    
    public override void _Ready()
    {
        base._Ready();

        CollisionObject2D oneWay = GetNodeOrNull<CollisionObject2D>("OneWay");
        if (IsPreview)
        {
            CollisionLayer |= 4;
            CollisionMask |= 4;
            if (CollidesPlayer)
                CollisionLayer |= 16;
            if (oneWay is not null)
                oneWay.CollisionLayer |= 16;
        }
        else
        {
            CollisionLayer |= 8;
            CollisionMask |= 8;
            if (CollidesPlayer)
                CollisionLayer |= 32;
            if (oneWay is not null)
                oneWay.CollisionLayer |= 32;
        }
    }

    public override void AfterFrame()
    {
        // Round position on standstill
        if (Velocity.X < Single.Epsilon && Velocity.Y < Single.Epsilon)
        {
            Position = Position.Round();
        }
    }
}
