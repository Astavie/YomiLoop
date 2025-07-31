using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public partial class Thing : CharacterBody2D
{
    public static float Gravity = 9.8f;

    public Transform2D Initial;
    public Thing Preview;
    public bool IsPreview = false;
    public bool IsDead = false;
    public bool IsPaused = false;

    public List<Move> Moves = [];
    public int MoveIndex = 0;
    public int MoveFrame = 0;

    public void StepMovement()
    {
        if (IsPaused)
            return;
        
        if (MoveIndex < Moves.Count)
        {
            Move move = Moves[MoveIndex];
            move.OnFrame(this, MoveFrame);
            MoveFrame++;
            if (MoveFrame >= move.Frames)
            {
                MoveFrame = 0;
                MoveIndex++;
            }
        }
        
        if (IsDead)
        {
            Visible = false;
            return;
        }
        
        Visible = true;
        Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity);
        MoveAndSlide();
    }

    public virtual void Reset([MaybeNull] Thing parent)
    {
        Transform = Initial;
        Velocity = parent?.Velocity ?? Vector2.Zero;
        MoveIndex = parent?.MoveIndex ?? 0;
        MoveFrame = parent?.MoveFrame ?? 0;
    }

    public override void _Ready()
    {
        if (!IsPreview)
        {
            Preview = (Thing)this.Duplicate();
            Preview.IsPreview = true;
            Preview.Moves = this.Moves;
            this.AddChild(Preview);
        }
        else
        {
            Preview = this;
            Visible = false;
            Transform = Transform2D.Identity;
            Modulate = new Color(0.8f, 0.8f, 1, 0.5f);
        }
        Initial = Transform;

        var physics = GetNode<Physics>("/root/Physics");
        physics.RegisterObject(this);
    }

    public static Move Move(string name, int frames, float? xspeed = null, float? yspeed = null)
    {
        return new Move(
            name,
            frames, 
            (o, _) =>
                o.Velocity = new Vector2(xspeed ?? o.Velocity.X, yspeed ?? o.Velocity.Y)
            );
    }
}
