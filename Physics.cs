using Godot;
using System.Collections.Generic;

public partial class Physics : Node
{
    private List<Object> _objects = new List<Object>();
    private List<Object> _previews = new List<Object>();
    
    public void RegisterObject(Object obj)
    {
        if (obj.IsPreview)
        {
            _previews.Add(obj);
        }
        else
        {
            _objects.Add(obj);
        }
    }

    public void StepMovement()
    {
        foreach (Object obj in _objects)
        {
            obj.StepMovement();
        }
    }

    public void ResetPreview()
    {
        foreach (Object obj in _previews)
        {
            obj.Visible = false;
            obj.Transform = Transform2D.Identity;
            obj.Velocity = obj.GetParent<Object>().Velocity;
        }
    }
    
    public void StepPreview()
    {
        foreach (Object obj in _previews)
        {
            obj.StepMovement();
        }
    }
}