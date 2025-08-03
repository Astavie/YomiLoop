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
    [Export] public AudioStream DeathSound { get; set; }
    [Export] public AudioStream ThrowSound { get; set; }
    [Export] public AudioStream GrabSound { get; set; }
    [Export] public AudioStream RocketSound { get; set; }
    [Export] public AudioStream VictorySound { get; set; }
    private AudioStreamPlayer audioPlayer;
    
    [Export] public float GrabDistance = 32;
    private Line2D _grabLine;

    public AnimationTree BodyTree { get; private set; }
    public AnimationNodeStateMachinePlayback PlayBody { get; private set; }
    public AnimationTree HandTree { get; private set; }
    public AnimationNodeStateMachinePlayback PlayHand { get; private set; }
    private CanvasGroup[] BodyGroups;
    
    public AnimatedSprite2D RocketSprite { get; private set; }

    public bool CanRocket { get; private set; } = true;
    public bool IsDead { get; set; } = false;
    public int LifeTime { get; set; } = 180;
    public int Age { get; private set; } = 0;
    public bool OldAge => Age >= LifeTime;
    
    public Move? ForcedMove = null;
    public List<Move> Moves = [];
    public int MoveIndex = 0;
    public int MoveFrame = 0;

    private Thing _grabbed;
    public Thing Grabbed {
        get => _grabbed;
        set {
            if (_grabbed is not null) _grabbed.IsGrabbed = false;
            
            if (value is null) {
                PlayHand.Travel("idle");
            } else {
                value.IsGrabbed = true;
            }

            _grabbed = value;
        }
    }
    public bool PastSelf = false;

    public float Aberration
    {
        get => CanvasMaterial.Get("shader_parameter/aberration").AsSingle();
        set => CanvasMaterial.Set("shader_parameter/aberration", value);
    }

    public override void _Ready()
    {
        base._Ready();
        audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
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
        foreach (CanvasGroup canvasGroup in BodyGroups) {
            canvasGroup.Material = CanvasMaterial;
        }
        RocketSprite = GetNode<AnimatedSprite2D>("Sprites/RocketGroup/Rocket");
        
        Travel("idle");
        Advance(0);

        if (IsPreview) {
            Robo parent = GetParent<Robo>();
            Moves = parent.Moves;
            LifeTime = parent.LifeTime;
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

    private void PlaySound(AudioStream audioStream) {
        if (IsPreview) return;
        audioPlayer.Stream = audioStream;
        audioPlayer.Play();
    }

    private void AdvanceMove(Move move) {
        // interrupt player move when about to die
        if (!PastSelf && move.Name != "Loop" && AboutToDie()) {
            MoveFrame = 0;
            MoveIndex = Moves.Count;
            return;
        }

        if (MoveFrame < move.Frames) return;
        MoveFrame = 0;
        if (ForcedMove.HasValue) {
            ForcedMove = null;
            MoveIndex = Moves.Count;
        }
        else MoveIndex++;
    }

    public override void OnFrame(double delta)
    {
        Advance(delta);
        if (IsOnGround(this))
        {
            Velocity = new Vector2(0, Velocity.Y);
        }
        
        // Advance to next frame
        if (!ForcedMove.HasValue && MoveIndex >= Moves.Count) return;
        
        var move = ForcedMove ?? Moves[MoveIndex];
        move.OnFrame(this, MoveFrame);
        MoveFrame++;
        
        // Advance to next move
        AdvanceMove(move);
    }

    public override void AfterFrame() {
        Age++;

        if (IsOnGround(this)) {
            bool notRocketing = PlayBody.GetCurrentNode() != "rocket";
            bool notGoingToRocket = PlayBody.GetTravelPath().Count == 0 || PlayBody.GetTravelPath().Last() != "rocket";
            CanRocket = notRocketing && notGoingToRocket;
        }
        
        if (Grabbed != null)
        {
            Grabbed.Velocity = Vector2.Zero;
            Grabbed.GlobalPosition = GlobalPosition + Vector2.Up * 32;
        }

        // Death logic
        // Only applies to past self, current self must manually take the loop action
        if (PastSelf && AboutToDie()) Die();
    }

    public void Die()
    {
        Velocity = Vector2.Zero;
        Grabbed = null;
        IsFrozen = true;
        Aberration = 1.5f;
        PlayBody.Travel("hurt");
        PlayHand.Travel("RESET");
        Advance(0.001);
        PlaySound(DeathSound);
    }

    public bool AboutToDie()
    {
        if (Grabbed is Goal) return false;
        return IsDead || OldAge;
    }

    public override void Reset([MaybeNull] Thing parent)
    {
        base.Reset(parent);
        var robo = (Robo)parent;
        MoveIndex = robo?.MoveIndex ?? 0;
        MoveFrame = robo?.MoveFrame ?? 0;
        Grabbed = robo?.Grabbed?.Preview;
        Aberration = robo?.Aberration ?? 0;
        IsDead = robo?.IsDead ?? false;
        ForcedMove = robo?.ForcedMove;
        Age = robo?.Age ?? 0;
        
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
            Vector2.Zero, _grabLine.ToLocal(thing.Center)
        ];
    }

    public bool CanGrab(Thing thing)
    {
        if (!thing.Grabbable || this == thing) return false;
        return
            float.Min(thing.Center.DistanceSquaredTo(GlobalPosition), thing.Center.DistanceSquaredTo(Center))
            < GrabDistance * GrabDistance;
    }

    public static Move Move(string name,
                            int frames,
                            Action<Robo> action = null,
                            Predicate<Robo> doFrame = null,
                            Predicate<Robo> isLegal = null,
                            string animation = "idle",
                            float? xspeed = null,
                            float? yspeed = null)
        => new(name, frames, 
            (o, frame) => {
                if (frame == 0) {
                    if (animation != null) o.Travel(animation);
                    action?.Invoke(o);
                }

                if (doFrame?.Invoke(o) ?? true) {
                    o.Velocity = new Vector2(xspeed ?? o.Velocity.X, yspeed ?? o.Velocity.Y);
                }
            }, isLegal);

    private static bool IsOnGround(Robo o) => o.IsOnFloor() && !o.IsGrabbed && !o.WasGrabbed;
    private static bool CanUseRocket(Robo o) => o.CanRocket;
    
    public static Move MoveLeft = Move("MoveLeft", 60, animation:"moving_left", xspeed: -64, doFrame:IsOnGround);
    public static Move MoveRight = Move("MoveRight", 60, animation:"moving_right", xspeed: 64, doFrame:IsOnGround);
    public static Move Wait = Move("Wait", 30, animation:"idle");

    public static Move Throw(Direction direction) {
        (Vector2 throwVec, Vector2 frozenVec) = direction switch {
            Direction.Left => (new Vector2(-256, -128), new Vector2(-512, 0)),
            Direction.Right => (new Vector2(256, -128), new Vector2(512, 0)),
            Direction.Up => (new Vector2(0, -340), new Vector2(0, -512)),
            Direction.UpLeft => (new Vector2(-128, -256), new Vector2(-362, -362)),
            Direction.UpRight => (new Vector2(128, -256), new Vector2(362, -362)),
            Direction.Down => (Vector2.Zero, Vector2.Zero)
        };
        return Move("Throw" + direction, 30, o => {
            if (o.Grabbed is null) return;
            o.PlaySound(o.ThrowSound);
            o.Grabbed.Velocity = o.Grabbed.IsFrozen ? frozenVec : throwVec;
            o.Grabbed = null;
        }, isLegal: robo => robo.Grabbed is not null);
    }
    
    public static Move Grab(Thing thing)
    {
        return Move("Grab", 30, action: o =>
        {
            Thing grabbed = thing.OrPreview(o);
            if (o.CanGrab(grabbed)) {
                o.Grabbed = grabbed;
                o.PlayHand.Travel("grab");
                o.PlaySound(o.GrabSound);
                if (grabbed is Goal goal) {
                    o.PlayBody.Travel("happy");
                    if (o.IsPreview) return;
                    var music = o.GetNode<AnimationPlayer>("/root/Music/AnimationPlayer");
                    music.Play("FADE");
                    AudioStream victorySound = o.VictorySound;
                    o.PlaySound(victorySound);
                    
                    var timer = o.GetTree().CreateTimer(victorySound.GetLength() - 0.8);
                    timer.Timeout += () =>
                    {
                        music.PlayBackwards("FADE");
                        if (!IsInstanceValid(o)) return;
                        o.physics.GoalAction(goal, o);
                    };

                }
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
            }
            if (o.PlayBody.GetCurrentNode() != "transform") {
                o.Velocity = frame == 0 ? new(o.Velocity.X, -Gravity) : new(xspeed, -Gravity);
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
            o.PlaySound(o.RocketSound);
            o.RocketSprite.Position = rocketPos;
            o.RocketSprite.Rotation = rocketRot;
        });
    }

    public static Move Loop = new("Loop", 30, (o, frame) => {
        if (frame > 0) return;
        o.Die();
        
        if (!(o.IsPreview || o.PastSelf))
        {
            o.GetNode<Wipe>("/root/Wipe")
                .DoWipe(() => o.physics.State = PlayState.Reset);
        }
    });

    private void Travel(string name) {
        PlayBody.Travel(name);
        if (Grabbed is null) PlayHand.Travel(name);
    }

    private void Advance(double delta) {
        BodyTree.Advance(delta);
        HandTree.Advance(delta);
    }
}
