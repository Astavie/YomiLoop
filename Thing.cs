using Godot;
using System.Diagnostics.CodeAnalysis;

public partial class Thing : CharacterBody2D
{
    [Export] public bool Grabbable = true;
    [Export] public float GroundDrag = 8f;
    [Export] public float FrozenDrag = 4f;
    [Export] public float AirDrag = 0.67f;
    
    
    public const float Gravity = 9.8f;

    private Transform2D _initialTransform;
    private Color _initialModulate;
    
    public Thing Preview;
    public bool IsPreview = false;
    public bool IsFrozen = false;
    public bool IsPaused = false;
    
    protected Physics Physics => GetNode<Physics>("/root/Physics");
    
    public override void _Ready()
    {
        if (!IsPreview)
        {
            Preview = (Thing)Duplicate();
            Preview.IsPreview = true;
            AddChild(Preview);
            if (Grabbable) InputPickable = true;
        }
        else
        {
            Preview = this;
            Visible = false;
            Transform = Transform2D.Identity;
            ZIndex = 10;
            Modulate = new Color(0.8f, 0.8f, 1, 0.5f);
        }
        _initialTransform = Transform;
        _initialModulate = Modulate;
        Physics.RegisterObject(this);
    }

    public void StepMovement(double delta)
    {
        Visible = true;
        if (IsPaused)
        {
            IsPaused = false;
            AfterFrame();
            return;
        }

        var drag = IsFrozen ? FrozenDrag : (IsOnFloor() ? GroundDrag : AirDrag);
        Velocity *= (float)(1 - drag * delta);
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
        Transform = _initialTransform;
        Modulate = _initialModulate;
        Velocity = parent?.Velocity ?? Vector2.Zero;
        IsPaused = parent?.IsPaused ?? false;
        IsFrozen = parent?.IsFrozen ?? false;
    }

    public override void _MouseEnter()
    {
        var physicsMe = Physics.Me;
        if (Physics.State is not PlayState.Grab || physicsMe is null) return;
        if (physicsMe.CanGrab(this)) physicsMe.LineTo(this);
    }

    public override void _MouseExit()
    {
        if (Physics.State is not PlayState.Grab || Physics.Me is null) return;
        Physics.Me.LineTo(null);
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (Physics.State is PlayState.Grab && @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            // clear line
            Physics.Me.LineTo(null);

            Physics.GrabAction?.Invoke(this);
        }
    }
}
