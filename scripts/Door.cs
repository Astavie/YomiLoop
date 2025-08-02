using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

public readonly struct Move(string name, int frames, Action<Robo, int> onFrame, Predicate<Robo> isLegal = null)
{
    public readonly string Name = name;
    public readonly int Frames = frames;
    public readonly Action<Robo, int> OnFrame = onFrame;
    public readonly Predicate<Robo> IsLegal = isLegal;
}

public enum PlayState
{
    Preview,
    Running,
    Grab,
    Reset,
}

public partial class Door : Node2D
{
    [Export]
    public PackedScene PlayerScene { get; set; }
    [Export]
    public PackedScene NextLevel { get; set; }

    [Export] public bool MeFirst = false;
    [Export] public int LifeTime = 180;

    private readonly List<Robo> _pastSelves = [];

    private Robo Me;
    private Robo Preview => (Robo)Me.Preview;
    private Physics Physics;
    private AnimationPlayer Music => GetNode<AnimationPlayer>("/root/Music/AnimationPlayer");
    private HFlowContainer Buttons => GetNode<Control>("%ControlUI").GetNode<HFlowContainer>("%Buttons");
    
    private Move? Queued
    {
        get {
            if (Me is null) return null;
            if (Me.MoveIndex < Me.Moves.Count)
                return Me.Moves[Me.MoveIndex];
            return null;
        }
        set
        {
            Physics.ResetPreview();
            if (Me.MoveIndex < Me.Moves.Count)
            {
                if (value.HasValue)
                    Me.Moves[Me.MoveIndex] = value.Value;
                else
                    Me.Moves.RemoveAt(Me.MoveIndex);
            }
            else if (value.HasValue)
            {
                Me.Moves.Add(value.Value);
            }
        }
    }

    public override void _Ready() {
        // account for entering move
        Physics = GetNode<Physics>("/root/Physics");
        Physics.GrabAction = HandleGrabClicked;
        Physics.GoalAction = HandleGoal;
        // save "actual" lifetime to physics (for displaying time left)
        Physics.LifeTime = LifeTime;
        
        LifeTime += 60;
        SpawnPlayer();
        // Connect button signals

        
        Buttons.GetNode<ControlButton>("Rocket").IsLegal = o => Robo.Rocket(Direction.Up).IsLegal(o) && !o.AboutToDie();
        Buttons.GetNode<ControlButton>("Hover").IsLegal = o => Robo.Hover(Direction.Up).IsLegal(o) && !o.AboutToDie();
        Buttons.GetNode<ControlButton>("Throw").IsLegal = o => Robo.Throw(Direction.Up).IsLegal(o) && !o.AboutToDie();
        Buttons.GetNode<ControlButton>("Grab").IsLegal = o => Physics.Objects.Any(t => t != o && t.Grabbable && o.CanGrab(t)) && !o.AboutToDie();
        Buttons.GetNode<ControlButton>("Perform").IsLegal = _ => true;
        Buttons.GetNode<ControlButton>("Loop").IsLegal = _ => true;

        Buttons.GetNode<BaseButton>("Perform").Pressed += HandlePerform;
        Buttons.GetNode<BaseButton>("Grab").Pressed += HandleGrab;
        Buttons.GetNode<BaseButton>("Loop").Pressed += QueueMove(Robo.Loop);
        Buttons.GetNode<BaseButton>("Wait").Pressed += QueueMove(Robo.Wait);
        Buttons.GetNode<BaseButton>("Move/PopupPanel/Container/Left").Pressed += QueueMove(Robo.MoveLeft);
        Buttons.GetNode<BaseButton>("Move/PopupPanel/Container/Right").Pressed += QueueMove(Robo.MoveRight);
        Buttons.GetNode<BaseButton>("Throw/PopupPanel/Container/Left").Pressed += QueueMove(Robo.Throw(Direction.Left));
        Buttons.GetNode<BaseButton>("Throw/PopupPanel/Container/Right").Pressed += QueueMove(Robo.Throw(Direction.Right));
        Buttons.GetNode<BaseButton>("Throw/PopupPanel/Container/Up").Pressed += QueueMove(Robo.Throw(Direction.Up));
        Buttons.GetNode<BaseButton>("Throw/PopupPanel/Container/UpLeft").Pressed += QueueMove(Robo.Throw(Direction.UpLeft));
        Buttons.GetNode<BaseButton>("Throw/PopupPanel/Container/UpRight").Pressed += QueueMove(Robo.Throw(Direction.UpRight));
        Buttons.GetNode<BaseButton>("Throw/PopupPanel/Container/Down").Pressed += QueueMove(Robo.Throw(Direction.Down));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/Right").Pressed += QueueMove(Robo.Rocket(Direction.Right));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/Left").Pressed += QueueMove(Robo.Rocket(Direction.Left));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/Up").Pressed += QueueMove(Robo.Rocket(Direction.Up));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/Down").Pressed += QueueMove(Robo.Rocket(Direction.Down));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/UpRight").Pressed += QueueMove(Robo.Rocket(Direction.UpRight));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/DownRight").Pressed += QueueMove(Robo.Rocket(Direction.DownRight));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/UpLeft").Pressed += QueueMove(Robo.Rocket(Direction.UpLeft));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/DownLeft").Pressed += QueueMove(Robo.Rocket(Direction.DownLeft));
        Buttons.GetNode<BaseButton>("Hover/PopupPanel/Container/Left").Pressed += QueueMove(Robo.Hover(Direction.Left));
        Buttons.GetNode<BaseButton>("Hover/PopupPanel/Container/Right").Pressed += QueueMove(Robo.Hover(Direction.Right));
        Buttons.GetNode<BaseButton>("Hover/PopupPanel/Container/Up").Pressed += QueueMove(Robo.Hover(Direction.Up));
    }

    private void SpawnPlayer() {
        if (MeFirst)
        {
            foreach (var pastSelf in _pastSelves) {
                pastSelf.LifeTime += 60;
                ((Robo)pastSelf.Preview).LifeTime += 60;
                pastSelf.Moves.Insert(0, Robo.Wait);
                pastSelf.Moves.Insert(0, Robo.Wait);
            }
        }

        Me = PlayerScene.Instantiate<Robo>();
        Me.LifeTime = LifeTime;
        Physics.Me = Me;
        AddChild(Me);

        if (!MeFirst)
        {
            foreach (var pastSelf in _pastSelves)
            {
                Me.LifeTime += 60;
                Preview.LifeTime += 60;
                Me.Moves.Add(Robo.Wait);
                Me.Moves.Add(Robo.Wait);
            }
        }
        Me.Moves.Add(Robo.MoveRight);
        
        Physics.State = PlayState.Running;
        Music.Play("CLEAR");
    }

    private Action QueueMove(Move move) {
        return () => {
            Physics.State = PlayState.Preview;
            Queued = move;
        };
    }

    public void OnDeath()
    {
    }
    
    public override void _PhysicsProcess(double delta)
    {
        switch (Physics.State)
        {
            case PlayState.Reset:
                // Create new past self
                Physics.ResetMovement();
                Me.PastSelf = true;
                Preview.PastSelf = true;
                _pastSelves.Add(Me);
        
                // Create new Me
                SpawnPlayer();
                break;
            case PlayState.Running when Me.MoveIndex >= Me.Moves.Count:
                Physics.State = PlayState.Preview;
                Music.Play("EQ");
                break;
            case PlayState.Running:
                Physics.StepMovement(delta);
                break;
            case PlayState.Preview when Queued is not null:
            {
                Physics.StepPreview(delta);
                if (Preview.MoveIndex >= Preview.Moves.Count)
                    Physics.ResetPreview();
                break;
            }
        }
    }
    
    public void HandlePerform()
    {
        if (Queued.HasValue)
        {
            Physics.State = PlayState.Running;
            Music.Play("CLEAR");
            Physics.ResetPreview();
        }
    }

    public void HandleGrab()
    {
        Queued = null;
        Physics.State = PlayState.Grab;
        
        Me.InputPickable = false;
    }

    public void HandleGrabClicked(Thing thing)
    {
        Me.InputPickable = true;
        
        Physics.State = PlayState.Preview;
        
        Queued = Robo.Grab(thing);
    }

    public void HandleGoal(Goal goal, Robo robo)
    {
        GetNode<Wipe>("/root/Wipe").DoWipe(() => CallDeferred(nameof(DoNextLevel)));
    }

    public void DoNextLevel()
    {
        Physics.OnLevelEnd();
        if (NextLevel is not null)
            GetNode("/root").AddChild(NextLevel.Instantiate());
        GetParent().Free();
    }
}
