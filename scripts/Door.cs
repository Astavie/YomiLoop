using System;
using Godot;
using System.Collections.Generic;

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
}

public partial class Door : Node2D
{
    [Export]
    public PackedScene PlayerScene { get; set; }

    [Export] public bool MeFirst = false;

    private readonly List<Robo> _pastSelves = [];

    private Robo Me;
    private Robo Preview => (Robo)Me.Preview;
    private Physics Physics => GetNode<Physics>("/root/Physics");
    private AnimationPlayer Music => GetNode<AnimationPlayer>("%Music/AnimationPlayer");
    private HFlowContainer Buttons => GetNode<Control>("%ControlUI").GetNode<HFlowContainer>("%Buttons");
    private bool _shouldDie = false;
    
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
        CallDeferred(nameof(SpawnPlayer));
        // SpawnPlayer();
        Physics.GrabAction = HandleGrabClicked;
        
        // Connect button signals
        Buttons.GetNode<BaseButton>("Perform").Pressed += HandlePerform;
        Buttons.GetNode<BaseButton>("Grab").Pressed += HandleGrab;
        Buttons.GetNode<BaseButton>("Wait").Pressed += () => Queued = Robo.Wait;
        Buttons.GetNode<BaseButton>("Move/PopupPanel/Container/Left").Pressed        += QueueMove(Robo.MoveLeft);
        Buttons.GetNode<BaseButton>("Move/PopupPanel/Container/Right").Pressed       += QueueMove(Robo.MoveRight);
        Buttons.GetNode<BaseButton>("Throw/PopupPanel/Container/Left").Pressed       += QueueMove(Robo.ThrowLeft);
        Buttons.GetNode<BaseButton>("Throw/PopupPanel/Container/Right").Pressed      += QueueMove(Robo.ThrowRight);
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/Right").Pressed     += QueueMove(Robo.Rocket(Direction.Right));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/Left").Pressed      += QueueMove(Robo.Rocket(Direction.Left));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/Up").Pressed        += QueueMove(Robo.Rocket(Direction.Up));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/Down").Pressed      += QueueMove(Robo.Rocket(Direction.Down));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/UpRight").Pressed   += QueueMove(Robo.Rocket(Direction.UpRight));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/DownRight").Pressed += QueueMove(Robo.Rocket(Direction.DownRight));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/UpLeft").Pressed    += QueueMove(Robo.Rocket(Direction.UpLeft));
        Buttons.GetNode<BaseButton>("Rocket/PopupPanel/Container/DownLeft").Pressed  += QueueMove(Robo.Rocket(Direction.DownLeft));
        Buttons.GetNode<BaseButton>("Hover/PopupPanel/Container/Left").Pressed       += QueueMove(Robo.Hover(Direction.Left));
        Buttons.GetNode<BaseButton>("Hover/PopupPanel/Container/Right").Pressed      += QueueMove(Robo.Hover(Direction.Right));
        Buttons.GetNode<BaseButton>("Hover/PopupPanel/Container/Up").Pressed         += QueueMove(Robo.Hover(Direction.Up));
    }

    private void SpawnPlayer()
    {
        if (MeFirst)
        {
            foreach (var pastSelf in _pastSelves)
            {
                pastSelf.Moves.Insert(0, Robo.Wait);
            }
        }
        
        Me = PlayerScene.Instantiate<Robo>();
        AddChild(Me);

        if (!MeFirst)
        {
            foreach (var pastSelf in _pastSelves)
            {
                Me.Moves.Add(Robo.Wait);
            }
        }
        Me.Moves.Add(Robo.MoveRight);
        
        Physics.State = PlayState.Running;
        Music.Play("CLEAR");
    }

    private Action QueueMove(Move move) {
        return () => {
            if (move.IsLegal?.Invoke(Me) ?? true) {
                Physics.State = PlayState.Preview;
                Queued = move;
            }
        };
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (_shouldDie)
        {
            _shouldDie = false;
            // Create new past self
            Queued = null;
            Physics.ResetMovement();
            Me.PastSelf = true;
            Preview.PastSelf = true;
            _pastSelves.Add(Me);
        
            // Create new Me
            SpawnPlayer();
            return;
        }
        
        switch (Physics.State)
        {
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

    public void HandleDie()
    {
        _shouldDie = true;
    }

    public void HandleGrab()
    {
        Queued = null;
        Physics.State = PlayState.Grab;
        
        Physics.Me = Me;
        Me.InputPickable = false;
    }

    public void HandleGrabClicked(Thing thing)
    {
        Me.InputPickable = true;
        
        Physics.State = PlayState.Preview;
        
        Queued = Robo.Grab(thing);
    }
}
