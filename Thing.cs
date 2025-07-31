using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public partial class Thing : CharacterBody2D
{
    public const float Gravity = 9.8f;

    public Transform2D Initial;
    public Thing Preview;
    public bool IsPreview = false;
    public bool IsFrozen = false;
    public bool IsPaused = false;
    
    private Physics Physics => GetNode<Physics>("/root/Physics");

    public override void _Ready()
    {
        if (!IsPreview)
        {
            Preview = (Thing)this.Duplicate();
            Preview.IsPreview = true;
            this.AddChild(Preview);
            InputPickable = true;
        }
        else
        {
            Preview = this;
            Visible = false;
            Transform = Transform2D.Identity;
            Modulate = new Color(0.8f, 0.8f, 1, 0.5f);
        }
        Initial = Transform;
        Physics.RegisterObject(this);
    }

    public void StepMovement()
    {
        if (IsPaused)
        {
            IsPaused = false;
            return;
        }
        
        OnFrame();
        
        if (IsFrozen) return;
        
        Visible = true;
        Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity);
        MoveAndSlide();
    }
    
    public virtual void OnFrame() {}

    public Thing OrPreview(Thing player)
    {
        if (player.IsPreview) return this.Preview;
        return this;
    }

    public virtual void Reset([MaybeNull] Thing parent)
    {
        Transform = Initial;
        Velocity = parent?.Velocity ?? Vector2.Zero;
    }

    public override void _MouseEnter()
    {
        if (Physics.OnClick == null) return;
        Modulate = Colors.LightYellow;
    }

    public override void _MouseExit()
    {
        Modulate = Colors.White;
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            Physics.OnClick?.Invoke(this);
        }
    }
}
