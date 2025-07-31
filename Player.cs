using System;
using Godot;
using System.Collections.Generic;

public struct Move(string name, int frames, Action<Robo, int> onFrame)
{
	public readonly string Name = name;
	public readonly int Frames = frames;
	public readonly Action<Robo, int> OnFrame = onFrame;
}

public enum PlayState
{
	Preview,
	Running,
	Grab,
}

public partial class Player : Node2D
{
	[Export]
	public PackedScene PlayerScene { get; set; }

	private List<Thing> _pastSelves = [];

	private PlayState _playState = PlayState.Preview;
	private Robo Me;
	private Robo Preview => (Robo)Me.Preview;
	private Physics Physics => GetNode<Physics>("/root/Physics");
	private HFlowContainer Buttons { get => GetNode<HFlowContainer>("%Buttons"); }

	private Move? Queued
	{
		get
		{
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

	public override void _Ready()
	{
		// Create player character
		Me = PlayerScene.Instantiate<Robo>();
		AddChild(Me);
		
		// Create movement buttons
		var buttons = Buttons;
		foreach (var move in moves)
		{
			var button = new Button();
			button.Text = move.Name;
			button.Pressed += () => Queued = move;
			buttons.AddChild(button);
		}
	}

    public override void _PhysicsProcess(double delta)
    {
	    switch (_playState)
	    {
		    case PlayState.Running when Me.MoveIndex >= Me.Moves.Count:
			    _playState = PlayState.Preview;
			    break;
		    case PlayState.Running:
			    Physics.StepMovement(delta);
			    break;
		    case PlayState.Preview when Queued.HasValue:
		    {
			    if (Preview.MoveIndex >= Preview.Moves.Count)
				    Physics.ResetPreview();
			    Physics.StepPreview(delta);
			    break;
		    }
	    }
    }
    
    public void HandlePerform()
    {
        if (Queued.HasValue)
        {
	        _playState = PlayState.Running;
            Physics.ResetPreview();
        }
    }

	public void HandleDie()
	{
		// Create new past self
		Queued = null;
		Physics.ResetMovement();
		Me.PastSelf = true;
		Preview.PastSelf = true;
		_pastSelves.Add(Me);
		
		// Create new Me
		Me = PlayerScene.Instantiate<Robo>();
		AddChild(Me);
	}

	public void HandleGrab()
	{
		Queued = null;
		_playState = PlayState.Grab;
		
		Physics.OnClick = HandleClick;
		Me.InputPickable = false;
	}

	public void HandleClick(Thing thing)
	{
		Me.InputPickable = true;
		Physics.OnClick = null;
		
		_playState = PlayState.Preview;

		if (Me.CanGrab(thing))
		{
			Queued = Robo.Grab(thing);
		}
	}

	private static Move[] moves = new[]
	{
		Robo.MoveLeft,
		Robo.MoveRight,
		Robo.Wait,
		Robo.Ungrab,
		Robo.ThrowLeft,
		Robo.ThrowRight,
	};
}
