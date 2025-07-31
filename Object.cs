using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class Object : CharacterBody2D
{
    public static float Gravity = 9.8f;

    public Transform2D Initial;
    public Object Preview;
    public bool IsPreview = false;
    public bool IsDead = false;
    public bool IsPaused = false;

    public List<Move> Moves = new List<Move>();
    public int MoveIndex = 0;
    public int MoveFrame = 0;

    public void StepMovement()
    {
        if (IsPaused)
            return;
        
        if (MoveIndex < Moves.Count)
        {
            var move = Moves[MoveIndex];
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

    public virtual void ResetPreview()
    {
        Transform = Initial;
        Visible = false;
        
        var parent = GetParent<Object>();
        Velocity = parent.Velocity;
        MoveIndex = parent.MoveIndex;
        MoveFrame = parent.MoveFrame;
    }

    public virtual void Reset()
    {
        Transform = Initial;
        Velocity = Vector2.Zero;
        MoveIndex = 0;
        MoveFrame = 0;
    }

    public override void _Ready()
    {
        if (!IsPreview)
        {
            Preview = (Object)this.Duplicate();
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

    public static Move Move(int frames, float? xspeed = null, float? yspeed = null)
    {
        return new Move(
            frames, 
            (o, _) =>
                o.Velocity = new Vector2(xspeed ?? o.Velocity.X, yspeed ?? o.Velocity.Y)
            );
    }
}
