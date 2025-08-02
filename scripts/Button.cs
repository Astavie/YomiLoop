using Godot;
using System;
using System.Collections.Generic;

public partial class Button : Area2D
{
    private HashSet<Crate> _here = [];
    private HashSet<Crate> _herePreview = [];

    [Export] public NodePath[] connected = [];

    private AnimatedSprite2D sprite;
    
    public override void _Ready() {
        sprite = GetNode<AnimatedSprite2D>("Sprite");
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void ForActivatable(Action<IActivatable> action)
    {
        foreach (var nodePath in connected)
        {
            var node = GetNode(nodePath);
            if (node is IActivatable activatable)
                action(activatable);
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is Crate crate) {
            sprite.Frame = 0;
            if (crate.IsPreview)
            {
                _herePreview.Remove(crate);
                if (_herePreview.Count == 0) ForActivatable(a => a.Preview.Active = false);
            }
            else
            {
                _here.Remove(crate);
                if (_here.Count == 0) ForActivatable(a => a.Active = false);
            }
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Crate crate) {
            sprite.Frame = 1;
            if (crate.IsPreview)
            {
                _herePreview.Add(crate);
                ForActivatable(a => a.Preview.Active = true);
            }
            else
            {
                _here.Add(crate);
                ForActivatable(a => a.Active = true);
            }
        }
    }
}
