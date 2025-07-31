using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public partial class Robo : Thing
{
	[Export] public float GrabDistance = 48;

	private AnimationTree animationTree;
	private AnimationNodeStateMachinePlayback playAnimation;
	
	public List<Move> Moves = [];
	public int MoveIndex = 0;
	public int MoveFrame = 0;
	public Thing Grabbed;

    public override void _Ready()
    {
        base._Ready();
        animationTree = GetNode<AnimationTree>("AnimationTree");
        playAnimation = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
        
        animationTree.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
        playAnimation.Travel("idle");
        
        if (IsPreview)
            Moves = GetParent<Robo>().Moves;
    }

    public override void OnFrame(double delta)
    {
	    animationTree.Advance(delta);
	    
        if (MoveIndex >= Moves.Count) return;
        var move = Moves[MoveIndex];
        move.OnFrame(this, MoveFrame);
        MoveFrame++;
        
        if (MoveFrame < move.Frames) return;
        MoveFrame = 0;
        MoveIndex++;
    }

    public override void AfterFrame()
    {
	    if (Grabbed == null) return;
	    Grabbed.Velocity = Vector2.Zero;
	    Grabbed.GlobalPosition = GlobalPosition + Vector2.Up * 32;
	    Grabbed.IsPaused = true;
    }

    public override void Reset([MaybeNull] Thing parent)
    {
        base.Reset(parent);
        var robo = (Robo)parent;
        MoveIndex = robo?.MoveIndex ?? 0;
        MoveFrame = robo?.MoveFrame ?? 0;
        Grabbed = robo?.Grabbed?.Preview;
        if (robo is not null) {
	        playAnimation.Start(robo.playAnimation.GetCurrentNode(), false);
        }
        else
        {
	        playAnimation.Travel("RESET");
	        animationTree.Advance(1);
        }
    }

    public bool CanGrab(Thing thing)
    {
	    return thing.GlobalPosition.DistanceSquaredTo(GlobalPosition) < GrabDistance * GrabDistance;
    }

	public static Move Move(string name, int frames, Action<Robo> action = null, string animation = "idle", float? xspeed = 0, float? yspeed = null) {
		return new Move(
			name,
			frames, 
			(o, frame) => {
				if (frame == 0)
				{
					if (animation != null) o.playAnimation.Travel(animation);
					action?.Invoke(o);
				}
				o.Velocity = new Vector2(xspeed ?? o.Velocity.X, yspeed ?? o.Velocity.Y);
			}
		);
	}

	public static Move MoveLeft = Move("MoveLeft", 60, animation:"moving_left", xspeed: -64);
	public static Move MoveRight = Move("MoveRight", 60, animation:"moving_right", xspeed: 64);
	public static Move Wait = Move("Wait", 30, animation:"idle");
	public static Move Ungrab = Move("Ungrab", 30, o => o.Grabbed = null);
	public static Move ThrowLeft = Move("ThrowLeft", 30, o =>
	{
		o.Grabbed.Velocity = new Vector2(-512, -64);
		o.Grabbed = null;
	});
	public static Move ThrowRight = Move("ThrowRight", 30, o =>
	{
		o.Grabbed.Velocity = new Vector2(512, -64);
		o.Grabbed = null;
	});
	
	public static Move Grab(Thing thing)
	{
		return Move("Grab", 30, action: o =>
		{
			Thing grabbed = thing.OrPreview(o);
			if (o.CanGrab(grabbed))
				o.Grabbed = grabbed;
		});
	}

}
