using Godot;
using System.Collections.Generic;
using System.Linq;

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
        foreach (Object obj in _objects.AsEnumerable().Reverse())
        {
            obj.StepMovement();
        }
    }

    public void ResetPreview()
    {
        foreach (Object obj in _previews.AsEnumerable().Reverse())
        {
            obj.Reset(obj.GetParent<Object>());
            obj.Visible = false;
        }
    }

    public void ResetMovement()
    {
        foreach (Object obj in _objects.AsEnumerable().Reverse())
        {
            obj.Reset(null);
        }
        ResetPreview();
    }
    
    public void StepPreview()
    {
        foreach (Object obj in _previews.AsEnumerable().Reverse())
        {
            obj.StepMovement();
        }
    }
}