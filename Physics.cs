using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Physics : Node
{
    private List<Thing> _objects = new List<Thing>();
    private List<Thing> _previews = new List<Thing>();
    
    public void RegisterObject(Thing obj)
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
        foreach (Thing obj in _objects.AsEnumerable().Reverse())
        {
            obj.StepMovement();
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
    
    public void StepPreview()
    {
        foreach (Thing obj in _previews.AsEnumerable().Reverse())
        {
            obj.StepMovement();
        }
    }
}