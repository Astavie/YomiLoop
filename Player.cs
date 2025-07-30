using System;
using Godot;
using System.Collections;

using Move = System.Func<Object, System.Collections.IEnumerable>;

public partial class Player : Node2D
{
    private Object Me { get => GetNode<Object>("%Player"); }
    private Object Preview { get => Me.Preview; }
    private Physics Physics { get => GetNode<Physics>("/root/Physics"); }

    private Move _queued = null;
    public Move Queued
    {
        get => _queued;
        set
        {
            Physics.ResetPreview();
            Preview.Anim = value?.Invoke(Preview).GetEnumerator();
            _queued = value;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Me.Anim != null)
        {
            Physics.StepMovement();
        }

        if (Queued != null)
        {
            if (Preview.Anim == null)
            {
                Physics.ResetPreview();
                Preview.Anim = Queued(Preview).GetEnumerator();
            }
            Physics.StepPreview();
        }
    }

    public void HandleLeftPress()
    {
        Queued = moveLeft;
    }
    
    public void HandleRightPress()
    {
        Queued = moveRight;
    }
    
    public void HandlePerform()
    {
        Me.Anim = Queued(Me).GetEnumerator();
        Queued = null;
    }
    
    private static Move moveRight = c => c.Move(60, xspeed:64);
    private static Move moveLeft = c => c.Move(60, xspeed:-64);
}
