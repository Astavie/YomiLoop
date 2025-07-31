using System;
using Godot;
using System.Collections.Generic;

public struct Move(String name, int frames, Action<Thing, int> onFrame)
{
	public String Name = name;
    public int Frames = frames;
    public Action<Thing, int> OnFrame = onFrame;
}

public partial class Player : Node2D
{
	private static PackedScene PlayerScene => GD.Load<PackedScene>("res://object.tscn");

	private List<Thing> _pastSelves = [];

	private bool Running = false;
	private Thing Me;
	private Thing Preview => Me.Preview;
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
        Me = PlayerScene.Instantiate<Thing>();
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
        if (Running)
        {
            if (Me.MoveIndex >= Me.Moves.Count)
                Running = false;
            else
                Physics.StepMovement();
        }
        else if (Queued.HasValue)
        {
            if (Preview.MoveIndex >= Preview.Moves.Count)
                Physics.ResetPreview();
            Physics.StepPreview();
        }
    }
    
    public void HandlePerform()
    {
        if (Queued.HasValue)
        {
            Running = true;
            Physics.ResetPreview();
        }
    }

	public void HandleDie()
	{
		// Create new past self
		Queued = die;
		Physics.ResetMovement();
		_pastSelves.Add(Me);
        
        // Create new Me
        Me = PlayerScene.Instantiate<Thing>();
		AddChild(Me);
	}

    private static Move die = new Move("Die", 1, (o, _) => o.IsDead = true);
    private static Move[] moves = new[]
    {
        Thing.Move("Left", 60, xspeed:-64),
        Thing.Move("Right", 60, xspeed: 64),
        Thing.Move("Wait", 60, xspeed: 0),
    };
}
