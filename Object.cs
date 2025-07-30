using Godot;
using System;
using System.Collections;

using Move = System.Func<Object, System.Collections.IEnumerable>;

public partial class Object : CharacterBody2D
{
    public static float Gravity = 9.8f;
    
    public Object Preview;
    public bool IsPreview = false;
    
    private IEnumerator _anim = null;
    public IEnumerator Anim
    {
        get => _anim;
        set
        {
            ((IDisposable)_anim)?.Dispose();
            _anim = value;
        }
    }

    public virtual void StepMovement()
    {
        Visible = true;
        if (Anim != null && !Anim.MoveNext())
            Anim = null;
        
        Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity);
        MoveAndSlide();
    }

    public override void _Ready()
    {
        if (!IsPreview)
        {
            Preview = (Object)this.Duplicate();
            Preview.IsPreview = true;
            this.AddChild(Preview);
        }
        else
        {
            Preview = this;
            Visible = false;
            Transform = Transform2D.Identity;
            Modulate = new Color(1, 1, 1, 0.5f);
        }

        var physics = GetNode<Physics>("/root/Physics");
        physics.RegisterObject(this);
    }

    public IEnumerable Move(int frames, float? xspeed = null, float? yspeed = null)
    {
        for (int i = 0; i < frames; i++)
        {
            Velocity = new Vector2(xspeed ?? Velocity.X, yspeed ?? Velocity.Y);
            yield return null;
        }
    }
}
