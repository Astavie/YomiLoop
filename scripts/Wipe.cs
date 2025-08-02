using Godot;
using System;

public partial class Wipe : Node2D
{
    private Action _nextCallback;
    
    public override void _Ready()
    {
        GetNode<Timer>("Timer").Timeout += OnTimeout;
    }

    private void OnTimeout()
    {
        GetNode<AnimationPlayer>("AnimationPlayer").Play("Out");
        _nextCallback?.Invoke();
    }

    public void DoWipe(Action callback)
    {
        _nextCallback = callback;
        GetNode<AnimationPlayer>("AnimationPlayer").Play("In");
        GetNode<Timer>("Timer").Start();
    }
}
