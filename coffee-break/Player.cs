using Godot;
using System;
using System.ComponentModel;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks.Dataflow;

public partial class Player : CharacterBody2D
{
	[Export] public int speed = 50;
	
	[Export] public int maxHealth = 100;
	private int currentHealth;

	[Export] public int maxMagic = 100;
	private int currentMagic;

	[Export] public int maxAwake = 100;
	private int currentAwake;
	
	private Vector2 currentVelocity;
	
	private Sprite2D playerSprite;
	private AnimationPlayer animationPlayer;
	private bool animationLocked = false;

	private Vector2 direction = new Vector2();

	private enum AnimState
	{
		IdleLeft, IdleRight, IdleUp, IdleDown, 
		WalkLeft, WalkRight, WalkUp, WalkDown, 
		SwordSwingLeft, SwordSwingRight, SwordSwingUp, SwordSwingDown
	}

	private enum FacingDirection{Left, Right, Up, Down}
	private FacingDirection facingDirection = FacingDirection.Down;

	public override void _Ready()
	{
		playerSprite = GetNode<Sprite2D>("playerSprite");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	
		animationPlayer.AnimationFinished += OnAnimationFinshed;
	}
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		
		handleInput();
		
		Velocity = currentVelocity;
		MoveAndSlide();
	}
	
	private void handleInput()
	{
		if(!animationLocked)
		{
			currentVelocity = Input.GetVector("Left", "Right", "Up", "Down");
			currentVelocity *= speed;
			WalkingAnimation();
		}

		SwordAnimations();
	}

	private void WalkingAnimation()
	{
		if (Input.IsActionPressed("Left"))
		{
			PlayAnimation((int)AnimState.WalkLeft, false);
			facingDirection = FacingDirection.Left;
		} 
		else if (Input.IsActionPressed("Right"))
		{
			PlayAnimation((int)AnimState.WalkRight, false);
			facingDirection = FacingDirection.Right;
		}
		else if (Input.IsActionPressed("Up"))
		{
			PlayAnimation((int)AnimState.WalkUp, false);
			facingDirection = FacingDirection.Up;
		}
		else if (Input.IsActionPressed("Down"))
		{
			PlayAnimation((int)AnimState.WalkDown, false);
			facingDirection = FacingDirection.Down;
		}
		else
		{
			if(facingDirection == FacingDirection.Left)
			{
				PlayAnimation((int)AnimState.IdleLeft, false);
			}
			else if(facingDirection == FacingDirection.Right)
			{
				PlayAnimation((int)AnimState.IdleRight, false);
			}
			else if(facingDirection == FacingDirection.Up)
			{
				PlayAnimation((int)AnimState.IdleUp, false);
			}
			else if(facingDirection == FacingDirection.Down)
			{
				PlayAnimation((int)AnimState.IdleDown, false);
			}
		}
	}

	private void SwordAnimations()
	{
		if(Input.IsActionJustPressed("Sword"))
		{
			GD.Print("Sword");
			
			if(facingDirection == FacingDirection.Left)
			{
				PlayAnimation((int)AnimState.SwordSwingLeft, true);
			}
			else if(facingDirection == FacingDirection.Right)
			{
				PlayAnimation((int)AnimState.SwordSwingRight, true);
			}
			else if(facingDirection == FacingDirection.Up)
			{
				PlayAnimation((int)AnimState.SwordSwingUp, true);
			}
			else if(facingDirection == FacingDirection.Down)
			{
				PlayAnimation((int)AnimState.SwordSwingDown, true);
			}
		}
	}

	private void PlayAnimation(int stateInt, bool lockAnim)
	{
		AnimState state = (AnimState)stateInt;

		string animName = state switch
		{
			AnimState.IdleLeft => "idleLeft",
			AnimState.IdleRight => "idleRight",
			AnimState.IdleUp => "idleUp",
			AnimState.IdleDown => "idleDown",
			AnimState.WalkLeft => "walkLeft",
			AnimState.WalkRight => "walkRight",
			AnimState.WalkUp => "walkUp",
			AnimState.WalkDown => "walkDown",
			AnimState.SwordSwingLeft => "swordSwingLeft",
			AnimState.SwordSwingRight => "swordSwingRight",
			AnimState.SwordSwingUp => "swordSwingUp",
			AnimState.SwordSwingDown => "swordSwingDown",
			_ => "idleDown"
		};

		if (!animationLocked || lockAnim)
		{
			animationPlayer.Play(animName);
			animationLocked = lockAnim;
		}
	}

	private void OnAnimationFinshed(StringName animName)
	{
		if(animName.ToString().StartsWith("swordSwing"))
		{
			animationLocked = false;
		}
	}
}
