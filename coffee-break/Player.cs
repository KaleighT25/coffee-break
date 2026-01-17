using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export]
	private int speed = 50;
	
	private Vector2 currentVelocity;
	
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		
		handleInput();
		
		Velocity = currentVelocity;
		MoveAndSlide();
	}
	
	private void handleInput()
	{
		currentVelocity = Input.GetVector("Left", "Right", "Up", "Down");
		currentVelocity *= speed;
	}
}
