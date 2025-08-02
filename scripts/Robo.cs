using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

public enum Direction {
    Right,
    Left,
    Up,
    Down,
    UpRight,
    DownRight,
    UpLeft,
    DownLeft
}

public partial class Robo : Thing
{
    [Export] public float GrabDistance = 48;
    private Line2D _grabLine;

    public AnimationTree BodyTree { get; private set; }
    public AnimationNodeStateMachinePlayback PlayBody { get; private set; }
    public AnimationTree HandTree { get; private set; }
    public AnimationNodeStateMachinePlayback PlayHand { get; private set; }
    private CanvasGroup[] BodyGroups;
    
    public AnimatedSprite2D RocketSprite { get; private set; }

    public bool CanRocket { get; private set; } = true;

    public Move? ForcedMove = null;
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
            GetNode<CanvasGroup>("Sprites/RocketGroup")
        ];
        RocketSprite = GetNode<AnimatedSprite2D>("Sprites/RocketGroup/Rocket");
        
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
        if (IsOnFloor())
        {
            Velocity = new Vector2(0, Velocity.Y);
        }
        
        // Advance to next frame
        if (!ForcedMove.HasValue && MoveIndex >= Moves.Count) return;
        var move = ForcedMove ?? Moves[MoveIndex];
        move.OnFrame(this, MoveFrame);
        MoveFrame++;
        
        // Advance to next move
        if (MoveFrame < move.Frames) return;
        MoveFrame = 0;
        if (ForcedMove.HasValue)
            MoveIndex = Moves.Count;
        else
            MoveIndex++;
    }

    public override void AfterFrame() {
        if (IsOnFloor()) {
            bool notRocketing = PlayBody.GetCurrentNode() != "rocket";
            bool notGoingToRocket = PlayBody.GetTravelPath().Count == 0 || PlayBody.GetTravelPath().Last() != "rocket";
            CanRocket = notRocketing && notGoingToRocket;
        }
        
        if (Grabbed != null)
        {
            Grabbed.Velocity = Vector2.Zero;
            Grabbed.GlobalPosition = GlobalPosition + Vector2.Up * 32;
            Grabbed.IsPaused = true;
        }

        // Death logic
        if (ShouldDie())
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

    public bool ShouldDie()
    {
        if (Grabbed is Goal) return false;
        return MoveIndex >= Moves.Count && PastSelf;
    }

    public override void Reset([MaybeNull] Thing parent)
    {
        base.Reset(parent);
        var robo = (Robo)parent;
        MoveIndex = robo?.MoveIndex ?? 0;
        MoveFrame = robo?.MoveFrame ?? 0;
        Grabbed = robo?.Grabbed?.Preview;
        Aberration = robo?.Aberration ?? 0;
        ForcedMove = robo?.ForcedMove;

        var velocity = Velocity;
        Velocity = Vector2.Zero;
        MoveAndSlide();
        Velocity = robo?.Velocity ?? velocity;
        
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

    public static Move Move(string name,
                            int frames,
                            Action<Robo> action = null,
                            Predicate<Robo> doFrame = null,
                            Predicate<Robo> isLegal = null,
                            string animation = "idle",
                            float? xspeed = null,
                            float? yspeed = null) {
        return new Move(
            name,
            frames, 
            (o, frame) => {
                if (frame == 0) {
                    if (animation != null) o.Travel(animation);
                    action?.Invoke(o);
                }

                if (doFrame?.Invoke(o) ?? true) {
                    o.Velocity = new Vector2(xspeed ?? o.Velocity.X, yspeed ?? o.Velocity.Y);
                }
            },
            isLegal
        );
    }

    private static bool IsOnGround(Robo o) => o.IsOnFloor();
    private static bool CanUseRocket(Robo o) => o.CanRocket;
    
    public static Move MoveLeft = Move("MoveLeft", 60, animation:"moving_left", xspeed: -64, doFrame:IsOnGround);
    public static Move MoveRight = Move("MoveRight", 60, animation:"moving_right", xspeed: 64, doFrame:IsOnGround);
    public static Move Wait = Move("Wait", 30, animation:"idle");
    public static Move Ungrab = Move("Ungrab", 30, o => o.Grabbed = null);
    public static Move ThrowLeft = Move("ThrowLeft", 30, o =>
    {
        if (o.Grabbed is null) return;
        o.Grabbed.Velocity = new Vector2(o.Grabbed.IsFrozen ? -512 : -256, o.Grabbed.IsFrozen ? 0 : -128);
        o.Grabbed = null;
    }, isLegal: robo => robo.Grabbed is not null);
    public static Move ThrowRight = Move("ThrowRight", 30, o =>
    {
        if (o.Grabbed is null) return;
        o.Grabbed.Velocity = new Vector2(o.Grabbed.IsFrozen ? 512 : 256, o.Grabbed.IsFrozen ? 0 : -128);
        o.Grabbed = null;
    }, isLegal: robo => robo.Grabbed is not null);
    
    public static Move Grab(Thing thing)
    {
        return Move("Grab", 30, action: o =>
        {
            Thing grabbed = thing.OrPreview(o);
            if (o.CanGrab(grabbed)) {
                o.Grabbed = grabbed;
                o.PlayHand.Travel("grab");

                if (!o.IsPreview && grabbed is Goal goal)
                    o.physics.GoalAction(goal, o);
            }
        });
    }

    public static Move Hover(Direction direction) {
        (float xspeed, string bodyAnim, string handAnim) = direction switch {
            Direction.Left => (-64, "hovering_left", "moving_left"),
            Direction.Right => (64, "hovering_right", "moving_right"),
            _ => (0, "hover", "idle"),
        };
        return new Move("Hover" + direction, 60, (o, frame) => {
            if (frame == 0) {
                o.PlayBody.Travel(bodyAnim);
                o.PlayHand.Travel(handAnim);
            } else if (o.PlayBody.GetCurrentNode() == "transform") {
                o.Velocity = new(o.Velocity.X, o.Velocity.Y);
            } else {
                o.Velocity = new(xspeed, -Gravity);
            }
        }, o => o.Grabbed is null);
    }

    private const float diagonal = 169.71f; 
    public static Move Rocket(Direction direction) {
        (float xspeed, float yspeed, Vector2 rocketPos, float rocketRot) = direction switch {
            Direction.Right => (240f, 0, new Vector2(-15, 5), Mathf.Tau / 2),
            Direction.Left => (-240f, 0, new Vector2(15, 5), 0),
            Direction.Up => (0, -240f, new Vector2(0, 22), Mathf.Tau / 4),
            Direction.Down => (0, 240f, new Vector2(0, -12), Mathf.Tau * 3/4),
            Direction.UpLeft => (-diagonal, -diagonal, new Vector2(12, 17), Mathf.Tau / 8),
            Direction.UpRight => (diagonal, -diagonal, new Vector2(-12, 17), Mathf.Tau *  3/8),
            Direction.DownRight => (diagonal, diagonal, new Vector2(-10, -6), Mathf.Tau * 5/8),
            Direction.DownLeft => (-diagonal, diagonal, new Vector2(10, -6), Mathf.Tau * 7/8),
            _ => throw new Exception("WTF are you even doing???")
        };
        return Move("Rocket" + direction, 30, xspeed:xspeed, yspeed:yspeed, isLegal:CanUseRocket, action: o => {
            o.CanRocket = false;
            o.Grabbed = null;
            o.Travel("rocket");
            o.RocketSprite.Position = rocketPos;
            o.RocketSprite.Rotation = rocketRot;
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
