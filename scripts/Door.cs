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
    Replaying
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
    private bool _winning => Me.Grabbed is Goal || _pastSelves.Any(o => o.Grabbed is Goal);
    private bool _replaying = false;
    
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
            if (value is null)
                Buttons.GetNode<ControlButton>("Perform").Disable();
            else
                Buttons.GetNode<ControlButton>("Perform").Enable();
            
            ResetGrabHighlights();
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
        Physics.StateChanged += (_, next) => {
            if (next is PlayState.Running) ResetGrabHighlights();
        };
        // save "actual" lifetime to physics (for displaying time left)
        Physics.LifeTime = LifeTime;
        
        LifeTime += 60;
        SpawnPlayer();
        Music.Play("CLEAR");
        // Connect button signals
        Buttons.GetNode<Godot.Button>("../../Reset").Pressed += ResetLevel;
        Buttons.GetNode<Godot.Button>("../../NextLevel").Pressed += DoNextLevel;
        
        Buttons.GetNode<ControlButton>("Rocket").IsLegal = o => Robo.Rocket(Direction.Up).IsLegal(o) && !o.AboutToDie() && !_replaying;
        Buttons.GetNode<ControlButton>("Hover").IsLegal = o => Robo.Hover(Direction.Up).IsLegal(o) && !o.AboutToDie() && !_replaying;
        Buttons.GetNode<ControlButton>("Throw").IsLegal = o => Robo.Throw(Direction.Up).IsLegal(o) && !o.AboutToDie() && !_replaying;
        Buttons.GetNode<ControlButton>("Grab").IsLegal = o => Physics.Objects.Any(o.CanGrab) && !o.AboutToDie() && !_replaying;
        Buttons.GetNode<ControlButton>("Perform").IsLegal = _ => Queued is not null && !_replaying;
        Buttons.GetNode<ControlButton>("Loop").IsLegal = _ => !_replaying;
        Buttons.GetNode<ControlButton>("Wait").IsLegal = o => !o.AboutToDie() && !_replaying;
        Buttons.GetNode<ControlButton>("Move").IsLegal = o => !o.AboutToDie() && !_replaying;

        Buttons.GetNode<ControlButton>("Perform").Used += HandlePerform;
        Buttons.GetNode<ControlButton>("Grab").Used += HandleGrab;
        Buttons.GetNode<ControlButton>("Loop").Used += QueueMove(Robo.Loop);
        Buttons.GetNode<ControlButton>("Wait").Used += QueueMove(Robo.Wait);
        Buttons.GetNode<ControlButton>("Move/PopupPanel/Container/Left").Used += QueueMove(Robo.MoveLeft);
        Buttons.GetNode<ControlButton>("Move/PopupPanel/Container/Right").Used += QueueMove(Robo.MoveRight);
        Buttons.GetNode<ControlButton>("Throw/PopupPanel/Container/Left").Used += QueueMove(Robo.Throw(Direction.Left));
        Buttons.GetNode<ControlButton>("Throw/PopupPanel/Container/Right").Used += QueueMove(Robo.Throw(Direction.Right));
        Buttons.GetNode<ControlButton>("Throw/PopupPanel/Container/Up").Used += QueueMove(Robo.Throw(Direction.Up));
        Buttons.GetNode<ControlButton>("Throw/PopupPanel/Container/UpLeft").Used += QueueMove(Robo.Throw(Direction.UpLeft));
        Buttons.GetNode<ControlButton>("Throw/PopupPanel/Container/UpRight").Used += QueueMove(Robo.Throw(Direction.UpRight));
        Buttons.GetNode<ControlButton>("Throw/PopupPanel/Container/Down").Used += QueueMove(Robo.Throw(Direction.Down));
        Buttons.GetNode<ControlButton>("Rocket/PopupPanel/Container/Right").Used += QueueMove(Robo.Rocket(Direction.Right));
        Buttons.GetNode<ControlButton>("Rocket/PopupPanel/Container/Left").Used += QueueMove(Robo.Rocket(Direction.Left));
        Buttons.GetNode<ControlButton>("Rocket/PopupPanel/Container/Up").Used += QueueMove(Robo.Rocket(Direction.Up));
        Buttons.GetNode<ControlButton>("Rocket/PopupPanel/Container/Down").Used += QueueMove(Robo.Rocket(Direction.Down));
        Buttons.GetNode<ControlButton>("Rocket/PopupPanel/Container/UpRight").Used += QueueMove(Robo.Rocket(Direction.UpRight));
        Buttons.GetNode<ControlButton>("Rocket/PopupPanel/Container/DownRight").Used += QueueMove(Robo.Rocket(Direction.DownRight));
        Buttons.GetNode<ControlButton>("Rocket/PopupPanel/Container/UpLeft").Used += QueueMove(Robo.Rocket(Direction.UpLeft));
        Buttons.GetNode<ControlButton>("Rocket/PopupPanel/Container/DownLeft").Used += QueueMove(Robo.Rocket(Direction.DownLeft));
        Buttons.GetNode<ControlButton>("Hover/PopupPanel/Container/Left").Used += QueueMove(Robo.Hover(Direction.Left));
        Buttons.GetNode<ControlButton>("Hover/PopupPanel/Container/Right").Used += QueueMove(Robo.Hover(Direction.Right));
        Buttons.GetNode<ControlButton>("Hover/PopupPanel/Container/Up").Used += QueueMove(Robo.Hover(Direction.Up));
    }

    private void ResetGrabHighlights() {
        Physics.Objects.Where(o => o.Grabbable).ToList().ForEach(o => o.Highlight(null));
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
    }

    private ControlButton.UsedEventHandler QueueMove(Move move) {
        return () => {
            Physics.State = PlayState.Preview;
            Queued = move;
        };
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
                Music.Play("CLEAR");

                // Create new Me
                if (_replaying) Physics.State = PlayState.Replaying;
                else SpawnPlayer();
                break;
            case PlayState.Running when Me.MoveIndex >= Me.Moves.Count && !_winning:
                Physics.State = PlayState.Preview;
                Physics.ResetPreview();
                Music.Play("EQ");
                break;
            case PlayState.Replaying:
                Physics.StepMovement(delta);
                break;
            case PlayState.Running or PlayState.Replaying:
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
        var grabbables = Physics.Objects.Where(Me.CanGrab).ToList();
        if (grabbables.Count == 1)
        {
            Thing grabbable = grabbables[0];
            Queued = Robo.Grab(grabbable);
            grabbable.Highlight(Colors.CornflowerBlue);
        }
        else
        {
            Queued = null;
            foreach (Thing grabbable in grabbables) {
                grabbable.Highlight(Colors.White);
            }
            Physics.State = PlayState.Grab;
            Me.InputPickable = false;
        }
    }

    public void HandleGrabClicked(Thing thing)
    {
        Me.InputPickable = true;
        Physics.State = PlayState.Preview;
        Queued = Robo.Grab(thing);
    }

    public void ResetLevel()
    {
        GetNode<Wipe>("/root/Wipe").DoWipe(() => CallDeferred(nameof(DoThisLevel)), playSound:true, backwards:true);
    }

    public void DoThisLevel()
    {
        Physics.OnLevelEnd();
        GetTree().ReloadCurrentScene();
    }

    public void HandleGoal(Goal goal, Robo robo) {
        GetNode<Wipe>("/root/Wipe").DoWipe(() => {
            Buttons.GetNode<Godot.Button>("../../NextLevel").Visible = true;
            _replaying = true;
            Physics.State = PlayState.Reset;
        }, playSound:true, backwards:true);
    }

    public void DoNextLevel() {
        GetNode<Wipe>("/root/Wipe").DoWipe(() => {
            Physics.OnLevelEnd();
            GetTree().ChangeSceneToPacked(NextLevel);
        }, playSound:true);
    }

    public override void _Input(InputEvent @event)
    {
        if (_replaying && @event is InputEventKey { Pressed: true, Keycode: Key.Enter })
        {
            DoNextLevel();
        }
        if (Physics.State == PlayState.Preview && @event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            Queued = null;
            GetNode<Control>("%ControlUI").GetNode<Label>("Label").Text = "";
        }
    }
}
