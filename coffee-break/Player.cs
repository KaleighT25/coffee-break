using Godot;
using System;
using System.ComponentModel;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text.Json.Serialization;
using System.Threading.Tasks.Dataflow;
using System.Xml;

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

	private Hitbox swordHitBox;

	private Node2D lockedTarget = null;
	[Export] public float lockRadius = 200f;

	[Export] public float knockbackForce = 500f;
	[Export] public float knockbackFriction = 1200f;

	private Vector2 knockbackVelocity = Vector2.Zero;
	private bool isKnockedBack = false;

	private enum AnimState
	{
		IdleLeft, IdleRight, IdleUp, IdleDown, 
		WalkLeft, WalkRight, WalkUp, WalkDown, 
		SwordSwingLeft, SwordSwingRight, SwordSwingUp, SwordSwingDown,
		HitLeft, HitRight, HitUp, HitDown
	}

	private enum FacingDirection{Left, Right, Up, Down}
	private FacingDirection facingDirection = FacingDirection.Down;

	public override void _Ready()
	{
		playerSprite = GetNode<Sprite2D>("playerSprite");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	
		animationPlayer.AnimationFinished += OnAnimationFinshed;

		currentHealth = maxHealth;
		currentMagic = maxMagic;
		currentAwake = maxAwake;  

		swordHitBox = GetNode<Hitbox>("swordHitBox");
		swordHitBox.OwnerNode = this;
	}
	public override void _PhysicsProcess(double delta)
{
    base._PhysicsProcess(delta);

    handleInput();

    if (lockedTarget != null)
    {
        if (!IsInstanceValid(lockedTarget))
        {
            lockedTarget = null;
        }
        else
        {
            FaceLockedTarget();
        }
    }

    Vector2 finalVelocity = currentVelocity;

    if (isKnockedBack)
    {
        finalVelocity = knockbackVelocity;
        knockbackVelocity = knockbackVelocity.MoveToward(Vector2.Zero, knockbackFriction * (float)delta);
        if (knockbackVelocity.Length() < 5f)
        {
            knockbackVelocity = Vector2.Zero;
            isKnockedBack = false;
        }
    }

 
    Velocity = finalVelocity;
    MoveAndSlide();

    GlobalPosition = GlobalPosition.Round();
}

	
	private void handleInput()
	{
		if (Input.IsActionJustPressed("TargetLock"))
		{
			ToggleTargetLock();
		}

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
		if (Input.IsActionPressed("Left") && lockedTarget == null)
		{
			PlayAnimation((int)AnimState.WalkLeft, false);
			facingDirection = FacingDirection.Left;
		} 
		else if (Input.IsActionPressed("Right") && lockedTarget == null)
		{
			PlayAnimation((int)AnimState.WalkRight, false);
			facingDirection = FacingDirection.Right;
		}
		else if (Input.IsActionPressed("Up") && lockedTarget == null)
		{
			PlayAnimation((int)AnimState.WalkUp, false);
			facingDirection = FacingDirection.Up;
		}
		else if (Input.IsActionPressed("Down") && lockedTarget == null)
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

	public void SwordHitboxOn()
	{
		swordHitBox.Monitoring = true;
	}

	public void SwordHitboxOff()
	{
		swordHitBox.Monitoring = false;
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
			AnimState.HitLeft => "hitLeft",
			AnimState.HitRight => "hitRight",
			AnimState.HitUp => "hitUp",
			AnimState.HitDown => "hitDown",
			_ => "idleDown"
		};

		if (!animationLocked || lockAnim)
		{
			if (animationPlayer.CurrentAnimation != animName)
				animationPlayer.Play(animName);

			animationLocked = lockAnim;
		}
	}

	private void OnAnimationFinshed(StringName animName)
	{
		if(animName.ToString().StartsWith("swordSwing") || animName.ToString().StartsWith("hit"))
		{
			animationLocked = false;
		}
	}

	public void TakeDamage(int amount, Vector2 enemyPosition)
    {
    	currentHealth -= amount;

		ApplyKnockback(enemyPosition);

        GD.Print("Player health: " + currentHealth);

        if (currentHealth <= 0)
        {
            GD.Print("Player dead");
        }
    }

	public void ApplyKnockback(Vector2 fromPosition)
	{
		Vector2 direction = (GlobalPosition - fromPosition).Normalized();
		knockbackVelocity = direction * knockbackForce;
		isKnockedBack = true;

		if(facingDirection == FacingDirection.Left)
			{
				PlayAnimation((int)AnimState.HitLeft, true);
			}
			else if(facingDirection == FacingDirection.Right)
			{
				PlayAnimation((int)AnimState.HitRight, true);
			}
			else if(facingDirection == FacingDirection.Up)
			{
				PlayAnimation((int)AnimState.HitUp, true);
			}
			else if(facingDirection == FacingDirection.Down)
			{
				PlayAnimation((int)AnimState.HitDown, true);
			}
	}

	private Node2D FindClosestEnemy()
	{
		var enemies = GetTree().GetNodesInGroup("enemies");

		Node2D closest = null;
		float closestDist = lockRadius;

		foreach (Node node in enemies)
		{
			if (node is Node2D enemy)
			{
				float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);

				if (dist < closestDist)
				{
					closestDist = dist;
					closest = enemy;
				}
			}
		}

		return closest;
	}

	private void ToggleTargetLock()
	{
		GD.Print("target lock pressed");

		if (lockedTarget == null)
		{
			lockedTarget = FindClosestEnemy();
			GD.Print("Locked: ", lockedTarget);
		}
		else
		{
			GD.Print("Unlocked");
			lockedTarget = null;
		}
	}

	private void FaceLockedTarget()
	{
		if (lockedTarget == null) return;
		if(!IsInstanceValid(lockedTarget))
		{
			lockedTarget = null;
			return;
		}

		Vector2 dir = (lockedTarget.GlobalPosition - GlobalPosition);

		if (Math.Abs(dir.X) > Math.Abs(dir.Y))
		{
			facingDirection = dir.X > 0
				? FacingDirection.Right
				: FacingDirection.Left;
		}
		else
		{
			facingDirection = dir.Y > 0
				? FacingDirection.Down
				: FacingDirection.Up;
		}
	}
}
