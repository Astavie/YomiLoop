using Godot;
using System;

public interface IActivatable
{
    bool Active { get; set; }
    IActivatable Preview { get; }
}

public partial class Platform : Crate, IActivatable
{
    [Export] public float Movement = 0;
    [Export] public float Speed = 64;
    
    public bool Active { get; set; } = false;
    public IActivatable Preview => base.Preview as IActivatable;
    
    private float _initialX;

    public override void _Ready()
    {
        base._Ready();
        _initialX = GlobalPosition.X;
    }

    public override void Reset(Thing parent)
    {
        base.Reset(parent);
        Active = ((Platform)parent)?.Active ?? false;
    }

    public override void OnFrame(double delta)
    {
        if (Active)
        {
            var goal = _initialX + Movement;
            var current = GlobalPosition.X;
            if (float.Abs(goal - current) < 1.0)
            {
                GlobalPosition = new Vector2(goal, GlobalPosition.Y);
                Velocity = new Vector2(0, Velocity.Y);
            }
            else if (goal < current)
            {
                Velocity = new Vector2(-Speed, Velocity.Y);
            }
            else
            {
                Velocity = new Vector2(Speed, Velocity.Y);
            }
        }
        else
        {
            Velocity = new Vector2(0, Velocity.Y);
        }
    }
}
