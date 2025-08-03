using Godot;
using System;

public partial class Wipe : Node2D
{
    private Action _nextCallback;
    private bool _backwards;
    
    public override void _Ready()
    {
        GetNode<Timer>("Timer").Timeout += OnTimeout;
    }

    private void OnTimeout()
    {
        if (_backwards)
            GetNode<AnimationPlayer>("AnimationPlayer").PlayBackwards("In");
        else
            GetNode<AnimationPlayer>("AnimationPlayer").Play("Out");
        _nextCallback?.Invoke();
    }

    public void DoWipe(Action callback, bool playSound = false, bool backwards = false)
    {
        if (GetNode<AnimationPlayer>("AnimationPlayer").IsPlaying()) return;
        
        if (playSound)
        {
            if (backwards)
                GetNode<AudioStreamPlayer>("AudioPlayerBackwards").Play();
            else
                GetNode<AudioStreamPlayer>("AudioPlayer").Play();
        }
        
        _nextCallback = callback;
        _backwards = backwards;
        
        if (backwards)
            GetNode<AnimationPlayer>("AnimationPlayer").PlayBackwards("Out");
        else
            GetNode<AnimationPlayer>("AnimationPlayer").Play("In");
        GetNode<Timer>("Timer").Start();
    }
}
