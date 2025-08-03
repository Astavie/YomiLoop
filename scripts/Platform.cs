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
    [Export] public bool Active { get; set; } = false;
    
    public IActivatable Preview => base.Preview as IActivatable;

    private bool _initialActive;
    private float _initialX;
    private AnimatedSprite2D sprite;

    public override void _Ready()
    {
        base._Ready();
        sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _initialX = GlobalPosition.X;
        _initialActive = Active;
        if (Active && !IsPreview)
        {
            Position = new Vector2(Position.X + Movement, Position.Y);
        }
    }

    public override void Reset(Thing parent)
    {
        base.Reset(parent);
        Active = ((Platform)parent)?.Active ?? _initialActive;
        if (Active && !IsPreview)
        {
            Position = new Vector2(Position.X + Movement, Position.Y);
        }
    }

    public override void OnFrame(double delta)
    {
        var goal = _initialX;
        if (Active) goal += Movement;
        
        var current = GlobalPosition.X;
        if (float.Abs(goal - current) < 1.0)
        {
            GlobalPosition = new Vector2(goal, GlobalPosition.Y);
            Velocity = new Vector2(0, Velocity.Y);
            sprite.Stop();
        }
        else if (goal < current)
        {
            Velocity = new Vector2(-Speed, Velocity.Y);
            sprite.PlayBackwards();
        }
        else
        {
            sprite.Play();
            Velocity = new Vector2(Speed, Velocity.Y);
        }
    }
}
