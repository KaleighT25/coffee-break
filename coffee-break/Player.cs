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

	private float lastStrafeSide = 0;
	private Hitbox swordHitBox;

	private Node2D lockedTarget = null;
	[Export] public float lockRadius = 200f;

	[Export] public float rollSpeed = 220f;
	[Export] public float rollDuration = 0.25f;

	[Export] public float sideRollSpeed = 180f;
	[Export] public float backstepSpeed = 140f;

	[Export] public float backflipSpeed = 250f;
	[Export] public float backflipDuration = 0.3f;

	private bool isRolling = false;
	private Vector2 rollVelocity = Vector2.Zero;
	private float rollTimer = 0f;

	private bool isBackflipping = false;
	private Vector2 backflipVelocity = Vector2.Zero;
	private float backflipTimer = 0f;

	[Export] public float knockbackForce = 500f;
	[Export] public float knockbackFriction = 1200f;

	private Vector2 knockbackVelocity = Vector2.Zero;
	private bool isKnockedBack = false;

	private enum AnimState
	{
		IdleLeft, IdleRight, IdleUp, IdleDown, 
		WalkLeft, WalkRight, WalkUp, WalkDown, 
		SwordSwingLeft, SwordSwingRight, SwordSwingUp, SwordSwingDown,
		HitLeft, HitRight, HitUp, HitDown,
		RollLeft, RollRight, RollUp, RollDown,
		BackflipLeft, BackflipRight, BackflipUp, BackflipDown
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
		
		if (isBackflipping)
		{
			finalVelocity = backflipVelocity;
			backflipTimer -= (float)delta;
			if (backflipTimer <= 0)
			{
				isBackflipping = false;
				animationLocked = false;
			}
		}
		else if (isRolling)
		{
			finalVelocity = rollVelocity;
			rollTimer -= (float)delta;

			if (rollTimer <= 0)
			{
				isRolling = false;
				animationLocked = false;
			}
		}
		else if (isKnockedBack)
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

		if (lockedTarget != null && IsStrafingSideways())
		{
			Vector2 toTarget = (lockedTarget.GlobalPosition - GlobalPosition).Normalized();
			Vector2 tangent = new Vector2(-toTarget.Y, toTarget.X);

			float side = currentVelocity.Normalized().Dot(tangent);

			playerSprite.Position = new Vector2(side * 2f, 0); // lean 2 pixels
		}
		else
		{
			playerSprite.Position = Vector2.Zero;
		}

		float currentSide = 0;

		if (lockedTarget != null && currentVelocity != Vector2.Zero)
		{
			Vector2 toTarget = (lockedTarget.GlobalPosition - GlobalPosition).Normalized();
			Vector2 tangent = new Vector2(-toTarget.Y, toTarget.X);
			currentSide = currentVelocity.Normalized().Dot(tangent);
		}

		if (Mathf.Sign(currentSide) != Mathf.Sign(lastStrafeSide))
		{
			if (animationPlayer.CurrentAnimation.StartsWith("walk"))
				animationPlayer.Seek(0, true);
		}

		lastStrafeSide = currentSide;

		GlobalPosition = GlobalPosition.Round();
	}

	
	private void handleInput()
	{
		if(!animationLocked)
		{
			if (lockedTarget != null)
			{
				currentVelocity = GetStrafeVelocity();
			}
			else
			{
			currentVelocity = Input.GetVector("Left", "Right", "Up", "Down") * speed;
			}

			if(currentVelocity.Length() < 0.5f)
				currentVelocity = Vector2.Zero;

			WalkingAnimation();
		}

		SwordAnimations();

		if (Input.IsActionJustPressed("TargetLock"))
		{
			ToggleTargetLock();
		}

		if (Input.IsActionJustPressed("Action") && !isRolling && !isKnockedBack)
		{
			StartDodge();
		}
	}

	private void WalkingAnimation()
	{
		bool reverseWalk = false;

		if (lockedTarget != null && currentVelocity != Vector2.Zero)
		{
			Vector2 toTarget = (lockedTarget.GlobalPosition - GlobalPosition).Normalized();

			float dot = currentVelocity.Normalized().Dot(toTarget);

			reverseWalk = dot < -0.3f;
		}
		
		if (currentVelocity == Vector2.Zero)
		{
			PlayIdleForFacing();
			return;
		}

		Vector2 v = currentVelocity;

		if (Math.Abs(v.X) > Math.Abs(v.Y))
		{
			if (v.X > 0)
			{
				PlayAnimation((int)AnimState.WalkRight, false, reverseWalk);
				if (lockedTarget == null) facingDirection = FacingDirection.Right;
			}
			else
			{
				PlayAnimation((int)AnimState.WalkLeft, false, reverseWalk);
				if (lockedTarget == null) facingDirection = FacingDirection.Left;
			}
		}
		else
		{
			if (v.Y > 0)
			{
				PlayAnimation((int)AnimState.WalkDown, false, reverseWalk);
				if (lockedTarget == null) facingDirection = FacingDirection.Down;
			}
			else
			{
				PlayAnimation((int)AnimState.WalkUp, false, reverseWalk);
				if (lockedTarget == null) facingDirection = FacingDirection.Up;
			}
		}
	}

	private void PlayIdleForFacing()
	{
		switch (facingDirection)
		{
			case FacingDirection.Left:
				PlayAnimation((int)AnimState.IdleLeft, false); break;
			case FacingDirection.Right:
				PlayAnimation((int)AnimState.IdleRight, false); break;
			case FacingDirection.Up:
				PlayAnimation((int)AnimState.IdleUp, false); break;
			case FacingDirection.Down:
				PlayAnimation((int)AnimState.IdleDown, false); break;
		}
	}


	private void SwordAnimations()
	{
		if(Input.IsActionJustPressed("Sword"))
		{
			GD.Print("Sword");
			
			switch(facingDirection)
			{
				case FacingDirection.Left:
					PlayAnimation((int)AnimState.SwordSwingLeft, true); break;
				case FacingDirection.Right:
					PlayAnimation((int)AnimState.SwordSwingRight, true); break;
				case FacingDirection.Up:
					PlayAnimation((int)AnimState.SwordSwingUp, true); break;
				case FacingDirection.Down:
					PlayAnimation((int)AnimState.SwordSwingDown, true); break;
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

	private void StartDodge()
	{

		/*
		change dodge to match holding input buttuns instead
		backfliping when 'up'
		front roll when 'down'
		side roll when strafing

		side roll animation, roll up = rollUp, roll down = rollDown
		roll right = rollRight, roll left = rollLeft

		sideroll animations might be to much to do, can add later
		*/
		
		Vector2 input = Input.GetVector("Left", "Right", "Up", "Down");

		if (lockedTarget != null && IsInstanceValid(lockedTarget))
		{
			Vector2 toTarget = (lockedTarget.GlobalPosition - GlobalPosition).Normalized();
			Vector2 tangent = new Vector2(-toTarget.Y, toTarget.X);

			float forwardDot = input.Normalized().Dot(toTarget);
			float sideDot = input.Normalized().Dot(tangent);

			if (forwardDot < -0.3f || input == Vector2.Zero)
			{
				StartBackflip(-toTarget);
			}
			else if (Mathf.Abs(sideDot) > Mathf.Abs(forwardDot))
			{
				Vector2 sideDir = Mathf.Sign(sideDot) * tangent;
				rollVelocity = sideDir * sideRollSpeed;
				rollTimer = rollDuration;
				isRolling = true;
				animationLocked = true;
				PlayRollAnimation(sideDir);
			}
			else
			{
				rollVelocity = toTarget * rollSpeed;
				rollTimer = rollDuration;
				isRolling = true;
				animationLocked = true;
				PlayRollAnimation(toTarget);
			}
		}
		else
		{
			if (input == Vector2.Zero)
				input = FacingToVector();

			rollVelocity = input.Normalized() * rollSpeed;
			rollTimer = rollDuration;
			isRolling = true;
			animationLocked = true;
			PlayRollAnimation(input);
		}
	}


	private Vector2 FacingToVector()
	{
		return facingDirection switch
		{
			FacingDirection.Left => Vector2.Left,
			FacingDirection.Right => Vector2.Right,
			FacingDirection.Up => Vector2.Up,
			_ => Vector2.Down
		};
	}

	private void PlayRollAnimation(Vector2 dir)
	{
		if (Math.Abs(dir.X) > Math.Abs(dir.Y))
		{
			if (dir.X > 0)
				PlayAnimation((int)AnimState.RollRight, true);
			else
				PlayAnimation((int)AnimState.RollLeft, true);
		}
		else
		{
			if (dir.Y > 0)
				PlayAnimation((int)AnimState.RollDown, true);
			else
				PlayAnimation((int)AnimState.RollUp, true);
		}
	}

	private void StartBackflip(Vector2 direction)
	{
		isBackflipping = true;
		backflipTimer = backflipDuration;
		animationLocked = true;
		backflipVelocity = direction.Normalized() * backflipSpeed;

		if (Math.Abs(direction.X) > Math.Abs(direction.Y))
		{
			if (direction.X > 0) PlayAnimation((int)AnimState.BackflipRight, true);
			else PlayAnimation((int)AnimState.BackflipLeft, true);
		}
		else
		{
			if (direction.Y > 0) PlayAnimation((int)AnimState.BackflipDown, true);
			else PlayAnimation((int)AnimState.BackflipUp, true);
		}
	}

	private void PlayAnimation(int stateInt, bool lockAnim, bool reverse = false)
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
			AnimState.RollLeft => "rollLeft",
			AnimState.RollRight => "rollRight",
			AnimState.RollUp => "rollUp",
			AnimState.RollDown => "rollDown",
			AnimState.BackflipLeft => "backflipLeft",
			AnimState.BackflipRight => "backflipRight",
			AnimState.BackflipUp => "backflipUp",
			AnimState.BackflipDown => "backflipDown",
			_ => "idleDown"
		};

		if (!animationLocked || lockAnim)
		{
			if (animationPlayer.CurrentAnimation != animName)
				animationPlayer.Play(animName);

			if(animName.StartsWith("walk"))
			{
				float scale = reverse ? -1f : 1f;

				if(lockedTarget != null && IsStrafingSideways())
					scale *= 0.85f;

				animationPlayer.SpeedScale = scale;
			}
			else
			{
				animationPlayer.SpeedScale = 1f;
			}

			animationLocked = lockAnim;
		}
	}

	private bool IsStrafingSideways()
	{
		if (lockedTarget == null || currentVelocity == Vector2.Zero)
			return false;


		Vector2 toTarget = (lockedTarget.GlobalPosition - GlobalPosition).Normalized();
			float dot = currentVelocity.Normalized().Dot(toTarget);

		return Mathf.Abs(dot) < 0.35f;
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

	private Vector2 GetStrafeVelocity()
	{
		if (lockedTarget == null || !IsInstanceValid(lockedTarget))
		return Vector2.Zero;

		Vector2 toTarget = (lockedTarget.GlobalPosition - GlobalPosition).Normalized();

		Vector2 tangent = new Vector2(-toTarget.Y, toTarget.X);

		float forward = Input.GetActionStrength("Down") - Input.GetActionStrength("Up");
		float strafe = Input.GetActionStrength("Right") - Input.GetActionStrength("Left");

		Vector2 move =
			(toTarget * forward) +
			(tangent * strafe);

		return move.Normalized() * speed;
	}
}
