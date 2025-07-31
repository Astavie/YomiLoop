using System;
using Godot;
using System.Collections;
using System.Collections.Generic;

public struct Move(String name, int frames, Action<Object, int> onFrame)
{
    public String Name = name;
    public int Frames = frames;
    public Action<Object, int> OnFrame = onFrame;
}

public partial class Player : Node2D
{
    private PackedScene PlayerScene { get => GD.Load<PackedScene>("res://object.tscn"); }

    private List<Object> _pastSelves = new List<Object>();

    private bool Running = false;
    private Object Me;
    private Object Preview { get => Me.Preview; }
    private Physics Physics { get => GetNode<Physics>("/root/Physics"); }
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
        Me = PlayerScene.Instantiate<Object>();
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
        Me = PlayerScene.Instantiate<Object>();
        AddChild(Me);
    }

    private static Move die = new Move("Die", 1, (o, _) => o.IsDead = true);
    private static Move[] moves = new[]
    {
        Object.Move("Left", 60, xspeed:-64),
        Object.Move("Right", 60, xspeed: 64),
        Object.Move("Wait", 60, xspeed: 0),
    };
}
