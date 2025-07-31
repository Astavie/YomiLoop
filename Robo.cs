using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public partial class Robo : Thing
{

    public List<Move> Moves = [];
    public int MoveIndex = 0;
    public int MoveFrame = 0;
    public Thing Grabbed;

    public override void _Ready()
    {
        base._Ready();
        if (IsPreview)
            Moves = GetParent<Robo>().Moves;
    }

    public override void OnFrame()
    {
        if (Grabbed != null)
        {
            // TODO: make grabbed follow us
            Grabbed.IsPaused = true;
        }

        if (MoveIndex >= Moves.Count) return;
        var move = Moves[MoveIndex];
        move.OnFrame(this, MoveFrame);
        MoveFrame++;
        
        if (MoveFrame < move.Frames) return;
        MoveFrame = 0;
        MoveIndex++;
    }

    public override void Reset([MaybeNull] Thing parent)
    {
        base.Reset(parent);
        var robo = (Robo)parent;
        MoveIndex = robo?.MoveIndex ?? 0;
        MoveFrame = robo?.MoveFrame ?? 0;
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

    public static Move Action(string name, int frames, Action<Robo> action)
    {
        return new Move(
            name,
            frames,
            (o, frame) =>
            {
                if (frame == 0) action(o);
                o.Velocity = new Vector2(0, o.Velocity.Y);
            }
        );
    }

    public static Move Grab(Thing thing)
    {
        return Action("Grab", 30, o =>
        {
            // TODO: check for distance
            o.Grabbed = thing.OrPreview(o);
        });
    }

}
