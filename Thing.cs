using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public partial class Thing : CharacterBody2D
{
    [Export] public float Drag = 4f;
    
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
            Preview = (Thing)Duplicate();
            Preview.IsPreview = true;
            AddChild(Preview);
            InputPickable = true;
        }
        else
        {
            Preview = this;
            Visible = false;
            Transform = Transform2D.Identity;
            ZIndex = 10;
            Modulate = new Color(0.8f, 0.8f, 1, 0.5f);
        }
        Initial = Transform;
        Physics.RegisterObject(this);
    }

    public void StepMovement(double delta)
    {
        Visible = true;
        if (IsPaused)
        {
            IsPaused = false;
            return;
        }

        Velocity = new Vector2(Velocity.X * (float)(1 - Drag * delta), Velocity.Y);
        if (IsFrozen)
        {
            MoveAndSlide();
        }
        else
        {
            
            OnFrame(delta);

            Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity);
            MoveAndSlide();

            AfterFrame();
        }
    }
    
    public virtual void OnFrame(double delta) {}
    public virtual void AfterFrame() {}

    public Thing OrPreview(Thing player) => player.IsPreview ? Preview : this;

    public virtual void Reset([MaybeNull] Thing parent)
    {
        Transform = Initial;
        Velocity = parent?.Velocity ?? Vector2.Zero;
        IsPaused = parent?.IsPaused ?? false;
        IsFrozen = parent?.IsFrozen ?? false;
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
