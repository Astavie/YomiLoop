using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public partial class Robo : Thing
{
    [Export] public float GrabDistance = 48;
    private Line2D _grabLine;

    public AnimationTree BodyTree { get; private set; }
    public AnimationNodeStateMachinePlayback PlayBody { get; private set; }
    public AnimationTree HandTree { get; private set; }
    public AnimationNodeStateMachinePlayback PlayHand { get; private set; }
    private CanvasGroup[] BodyGroups;
    
    public List<Move> Moves = [];
    public int MoveIndex = 0;
    public int MoveFrame = 0;

    private Thing _grabbed;
    public Thing Grabbed {
        get => _grabbed;
        set {
            if (value is null) {
                PlayHand.Travel("idle");
            }

            _grabbed = value;
        }
    }
    public bool PastSelf = false;

    public float Aberration
    {
        get => BodyGroups[0].Material.Get("shader_parameter/aberration").AsSingle();
        set
        {
            foreach (var group in BodyGroups)
            {
                group.Material.Set("shader_parameter/aberration", value);
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
        BodyTree = GetNode<AnimationTree>("BodyTree");
        PlayBody = (AnimationNodeStateMachinePlayback)BodyTree.Get("parameters/playback");
        HandTree = GetNode<AnimationTree>("HandTree");
        PlayHand = (AnimationNodeStateMachinePlayback)HandTree.Get("parameters/playback");
        BodyGroups = [
            GetNode<CanvasGroup>("Sprites/BodyGroup"),
            GetNode<CanvasGroup>("Sprites/WheelGroup"),
            GetNode<CanvasGroup>("Sprites/RightHandGroup"),
            GetNode<CanvasGroup>("Sprites/LeftHandGroup"),
        ];
        
        Travel("idle");

        if (IsPreview)
        {
            Moves = GetParent<Robo>().Moves;
            foreach (var group in BodyGroups)
            {
                group.Material = (Material)group.Material.Duplicate();
            }
        } else {
            _grabLine = GetNode<Line2D>("GrabLine");
        }

        if (IsPreview)
        {
            CollisionMask |= 16;
        }
        else
        {
            CollisionMask |= 32;
        }
    }

    public override void OnFrame(double delta)
    {
        Advance(delta);
        
        // Advance to next frame
        if (MoveIndex >= Moves.Count) return;
        var move = Moves[MoveIndex];
        move.OnFrame(this, MoveFrame);
        MoveFrame++;
        
        // Advance to next move
        if (MoveFrame < move.Frames) return;
        MoveFrame = 0;
        MoveIndex++;
    }

    public override void AfterFrame()
    {
        if (Grabbed != null)
        {
            Grabbed.Velocity = Vector2.Zero;
            Grabbed.GlobalPosition = GlobalPosition + Vector2.Up * 32;
            Grabbed.IsPaused = true;
        }

        // Death logic
        if (MoveIndex >= Moves.Count && PastSelf)
        {
            Velocity = Vector2.Zero;
            Grabbed = null;
            IsFrozen = true;
            Aberration = 1.5f;
            PlayBody.Travel("hurt");
            PlayHand.Travel("RESET");
            Advance(0.001);
        }
    }

    public override void Reset([MaybeNull] Thing parent)
    {
        base.Reset(parent);
        var robo = (Robo)parent;
        MoveIndex = robo?.MoveIndex ?? 0;
        MoveFrame = robo?.MoveFrame ?? 0;
        Grabbed = robo?.Grabbed?.Preview;
        Aberration = robo?.Aberration ?? 0;
        if (robo is not null)
        {
            PlayBody.Stop();
            PlayHand.Stop();
            Advance(0);
            PlayBody.Start(robo.PlayBody.GetCurrentNode(), false);
            PlayHand.Start(robo.PlayHand.GetCurrentNode(), false);
        }
        else
        {
            Travel("RESET");
        }
    }

    public void LineTo(Thing thing) {
        if (thing is null) {
            _grabLine.Points = [];
            return;
        }

        _grabLine.Points = [
            Vector2.Zero, ToLocal(thing.GlobalPosition)
        ];
    }

    public bool CanGrab(Thing thing)
    {
        return thing.GlobalPosition.DistanceSquaredTo(GlobalPosition) < GrabDistance * GrabDistance;
    }

    public static Move Move(string name, int frames, Action<Robo> action = null, string animation = "idle", float? xspeed = 0, float? yspeed = null) {
        return new Move(
            name,
            frames, 
            (o, frame) => {
                if (frame == 0)
                {
                    if (animation != null)
                        o.Travel(animation);
                    action?.Invoke(o);
                }
                o.Velocity = new Vector2(xspeed ?? o.Velocity.X, yspeed ?? o.Velocity.Y);
            }
        );
    }

    public static Move MoveLeft = Move("MoveLeft", 60, animation:"moving_left", xspeed: -64);
    public static Move MoveRight = Move("MoveRight", 60, animation:"moving_right", xspeed: 64);
    public static Move Wait = Move("Wait", 30, animation:"idle");
    public static Move Ungrab = Move("Ungrab", 30, o => o.Grabbed = null);
    public static Move ThrowLeft = Move("ThrowLeft", 30, o =>
    {
        if (o.Grabbed is null) return;
        o.Grabbed.Velocity = new Vector2(o.Grabbed.IsFrozen ? -512 : -256, o.Grabbed.IsFrozen ? 0 : -128);
        o.Grabbed = null;
    });
    public static Move ThrowRight = Move("ThrowRight", 30, o =>
    {
        if (o.Grabbed is null) return;
        o.Grabbed.Velocity = new Vector2(o.Grabbed.IsFrozen ? 512 : 256, o.Grabbed.IsFrozen ? 0 : -128);
        o.Grabbed = null;
    });
    
    public static Move Grab(Thing thing)
    {
        return Move("Grab", 30, action: o =>
        {
            Thing grabbed = thing.OrPreview(o);
            if (o.CanGrab(grabbed)) {
                o.Grabbed = grabbed;
                o.PlayHand.Travel("grab");
            }
        });
    }

    private void Travel(string name) {
        PlayBody.Travel(name);
        if (Grabbed is null) PlayHand.Travel(name);
    }

    private void Advance(double delta) {
        BodyTree.Advance(delta);
        HandTree.Advance(delta);
    }
}
