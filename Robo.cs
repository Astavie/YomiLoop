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
		if (IsPreview) Moves = GetParent<Robo>().Moves;
	}

	public override void OnFrame()
	{
		if (Grabbed != null)
		{
			// TODO: make grabbed follow us
			Grabbed.IsPaused = true;
		}

		if (MoveIndex >= Moves.Count) return;
		var move = Moves[MoveIndex];
		move.OnFrame(this, MoveFrame);
		MoveFrame++;
		
		if (MoveFrame < move.Frames) return;
		MoveFrame = 0;
		MoveIndex++;
	}

	public override void Reset([MaybeNull] Thing parent)
	{
		base.Reset(parent);
		Robo robo = (Robo)parent;
		MoveIndex = robo?.MoveIndex ?? 0;
		MoveFrame = robo?.MoveFrame ?? 0;
		if (robo is not null) {
			playAnimation.Start(robo.playAnimation.GetCurrentNode(), false);
		}
	}

	public static Move Move(string name, int frames, Action<Robo> action = null, float? xspeed = null, float? yspeed = null) {
		return new Move(
			name,
			frames, 
			(o, frame) => {
				if (frame == 0) action?.Invoke(o);
				o.Velocity = new Vector2(xspeed ?? o.Velocity.X, yspeed ?? o.Velocity.Y);
			}
		);
	}

	public static Move MoveLeft = Move("MoveLeft", 60, o => o.playAnimation.Travel("moving_left"), -64);
	public static Move MoveRight = Move("MoveRight", 60, o => o.playAnimation.Travel("moving_right"), 64);
	public static Move Wait = Move("Wait", 30, o => o.playAnimation.Travel("idle"), 0);

	public static Move Grab(Thing thing)
	{
		return Move("Grab", 30, xspeed: 0, action: o =>
		{
			Thing grabbed = thing.OrPreview(o);
			if (grabbed.GlobalPosition.DistanceSquaredTo(o.GlobalPosition) < o.GrabDistance * o.GrabDistance)
			{
				o.Grabbed = grabbed;
			}
		});
	}

}
