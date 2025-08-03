using Godot;
using System.Diagnostics.CodeAnalysis;

public partial class Thing : CharacterBody2D
{
    [Export] public bool Grabbable = true;
    [Export] public float GroundDrag = 8f;
    [Export] public float FrozenDrag = 4f;
    [Export] public float AirDrag = 0.67f;
    [Export] public Material CanvasMaterial { get; set; }
    
    public Vector2 Center => new(GlobalPosition.X, GlobalPosition.Y - 9);
    public const float Gravity = 9.8f;

    private Transform2D _initialTransform;
    private Color _initialModulate;
    
    public Thing Preview;
    public bool IsPreview = false;
    public bool IsFrozen = false;
    public bool IsGrabbed = false;
    public bool WasGrabbed = false;
    
    public Physics physics;
    
    public override void _Ready()
    {
        if (!IsPreview)
        {
            Preview = (Thing)Duplicate();
            Preview.IsPreview = true;
            Preview.CanvasMaterial = (Material)CanvasMaterial?.Duplicate(subresources:true);
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
        physics = GetNode<Physics>("/root/Physics");
        physics.RegisterObject(this);

        if (GetNodeOrNull<CanvasGroup>("CanvasGroup") is CanvasGroup canvasGroup) {
            canvasGroup.Material = CanvasMaterial;
        }
    }

    public void StepMovement(double delta)
    {
        Visible = true;
        if (IsGrabbed) {
            WasGrabbed = true;
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

        WasGrabbed = false;
    }
    
    public virtual void OnFrame(double delta) {}
    public virtual void AfterFrame() {}

    public void Highlight(Color? colorMaybe) {
        if (colorMaybe is Color color) {
            CanvasMaterial.Set("shader_parameter/line_colour", color);
            CanvasMaterial.Set("shader_parameter/line_thickness", 1f);
        } else {
            CanvasMaterial.Set("shader_parameter/line_thickness", 0f);
        }
        
    }

    public Thing OrPreview(Thing player) => player.IsPreview ? Preview : this;

    public virtual void Reset([MaybeNull] Thing parent)
    {
        Transform = _initialTransform;
        Modulate = _initialModulate;
        Velocity = parent?.Velocity ?? Vector2.Zero;
        IsGrabbed = parent?.IsGrabbed ?? false;
        IsFrozen = parent?.IsFrozen ?? false;
    }

    public override void _MouseEnter()
    {
        var physicsMe = physics.Me;
        if (physics.State is not PlayState.Grab || physicsMe is null) return;
        if (physicsMe.CanGrab(this)) Highlight(Colors.CornflowerBlue);
    }

    public override void _MouseExit()
    {
        Robo physicsMe = physics.Me;
        if (physics.State is not PlayState.Grab || physicsMe is null) return;
        if (physicsMe.CanGrab(this)) Highlight(Colors.White);
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (physics.State is PlayState.Grab && @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            // clear line
            physics.Me.LineTo(null);

            physics.GrabAction?.Invoke(this);
        }
    }
}
