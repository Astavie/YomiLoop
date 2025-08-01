using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Physics : Node {
    [Signal]
    public delegate void StateChangedEventHandler(PlayState current, PlayState next);
    
    private readonly List<Thing> _objects =  [];
    private readonly List<Thing> _previews = [];
    public Robo Me = null;
    public Action<Thing> GrabAction = null;

    private PlayState _state = PlayState.Preview;
    public PlayState State {
        get => _state;
        set {
            EmitSignalStateChanged(_state, value);
            _state = value;
        }
    }

    public void RegisterObject(Thing obj)
    {
        if (obj.IsPreview)
            _previews.Add(obj);
        else
            _objects.Add(obj);
    }

    public void StepMovement(double delta)
    {
        foreach (Thing obj in _objects.AsEnumerable().Reverse())
        {
            obj.StepMovement(delta);
        }
    }

    public void ResetPreview()
    {
        foreach (Thing obj in _previews.AsEnumerable().Reverse())
        {
            obj.Reset(obj.GetParent<Thing>());
            obj.Visible = false;
        }
    }

    public void ResetMovement()
    {
        foreach (Thing obj in _objects.AsEnumerable().Reverse())
        {
            obj.Reset(null);
        }
        ResetPreview();
    }
    
    public void StepPreview(double delta)
    {
        foreach (Thing obj in _previews.AsEnumerable().Reverse())
        {
            obj.StepMovement(delta);
        }
    }
}