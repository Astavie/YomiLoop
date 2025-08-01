using Godot;
using System;

public partial class Crate : Thing
{
    public override void _Ready()
    {
        base._Ready();

        if (IsPreview)
        {
            CollisionLayer |= 4;
            CollisionMask |= 4;
        }
        else
        {
            CollisionLayer |= 8;
            CollisionMask |= 8;
        }
    }
}
