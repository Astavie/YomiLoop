using Godot;
using System;
using System.Linq;

public partial class TimeLeft : Control {
    
    private Physics physics;

    public override void _Ready() {
        physics = GetNode<Physics>("/root/Physics");
    }

    public override void _Process(double delta) {
        if (physics.State is PlayState.Running || physics.State is PlayState.Replaying)
        {
            int time = (int)float.Ceiling((float)(physics.Me.LifeTime - physics.Me.Age) / 30);
            time = int.Max(0, time);
            
            Visible = time < 100;

            if (!physics.Objects.Any(o => o is Goal && o.IsGrabbed))
            {
                GetNode<Label>("Time").Text = time.ToString();
                GetNode<Control>("MustLoop").Visible = time == 0 || physics.Me.AboutToDie();
            }
        }
    }

}
