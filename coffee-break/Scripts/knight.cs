using Godot;
using System;

public partial class EnemyStandIn : EnemyTemp
{
    [Export] public float speed = 150f;
    [Export] public float detectionRadius = 300f;

        private Area2D collisionDamage;
        private Player playerTarget;

        private Sprite2D knightSprite;
	    private AnimationPlayer animationPlayer;
	    private bool animationLocked = false;

        private enum AnimState
	{
		P1IdleLeft, P1IdleRight, P1IdleUp, P1IdleDown, 
		P1WalkLeft, P1WalkRight, P1WalkUp, P1WalkDown,
        P1StrafeLeft, P1StrafeRight, P1StrafeUp, P1StrafeDown, 
		P1SwordSwingLeft, P1SwordSwingRight, P1SwordSwingUp, P1SwordSwingDown,
        P1SwordResetLeft, P1SwordResetRight, P1SwordResetUp, P1SwordResetDown,
        P1BackSwingLeft, P1BackSwingRight, P1BackSwingUp, P1BackSwingDown,
		P1HitLeft, P1HitRight, P1HitUp, P1HitDown,
        P1StaggerLeft, P1StaggerRight, P1StaggerUp, P1StaggerDown,
        P1ShieldLeft, P1ShieldRight, P1ShieldUp, P1ShieldDown,
        P1ThrustLeft, P1ThrustRight, P1ThrustUp, P1ThrustDown,
        P2IdleLeft, P2IdleRight, P2IdleUp, P2IdleDown,
        P2WalkLeft, P2WalkRight, P2WalkUp, P2WalkDown,
        P2StrafeLeft, P2StrafeRight, P2StrafeUp, P2StrafeDown,
        P2SwordSwingLeft, P2SwordSwingRight, P2SwordSwingUp, P2SwordSwingDown,
        P2SwordResetLeft, P2SwordResetRight, P2SwordResetUp, P2SwordResetDown,
        P2BackSwingLeft, P2BackSwingRight, P2BackSwingUp, P2BackSwingDown,
        P2HitLeft, P2HitRight, P2HitUp, P2HitDown, 
        P2StaggerLeft, P2StaggerRight, P2StaggerUp, P2StaggerDown,
        P2ThrustLeft, P2ThrustRight, P2ThrustUp, P2ThrustDown,
        P2SlamLeft, P2SlamRight, P2SlamUp, P2SlamDown,
        EngageLeft, EngageRight, EngageUp, EngageDown, 
        ArmorCrackLeft, ArmorCrackRight, ArmorCrackUp, ArmorCrackDown, 
        DeathLeft, DeathRight, DeathUp, DeathDown
	}

	private enum FacingDirection{Left, Right, Up, Down}
	private FacingDirection facingDirection = FacingDirection.Down;

    public override void _Ready()
    {
        AddToGroup("enemies");
        
        collisionDamage = GetNode<Area2D>("collisionDamage");
        collisionDamage.BodyEntered += OnCollisionEntered;

        knightSprite = GetNode<Sprite2D>("knightSprite");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	
		animationPlayer.AnimationFinished += OnAnimationFinshed;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (playerTarget == null)
        {
            var players = GetTree().GetNodesInGroup("players");
            if (players.Count > 0)
                playerTarget = players[0] as Player;
        }

        if (playerTarget != null)
        {
            Vector2 toPlayer = playerTarget.GlobalPosition - GlobalPosition;
            float distance = toPlayer.Length();

            if (distance < detectionRadius)
            {
                Vector2 direction = toPlayer.Normalized();
                Velocity = direction * speed;
                MoveAndSlide();
            }
            else
            {
                Velocity = Vector2.Zero;
            }
        }
    }

    public void OnCollisionEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.TakeDamage(Damage, GlobalPosition);
        }
    }

    public void TakeDamage(int amount, Vector2 attakerPos)
    {
        Health -= amount;
        GD.Print("Enemy health: " + Health);

        if (Health <= 0)
        {
            QueueFree();
        }
    }

    private void PlayAnimation(int stateInt, bool lockAnim, bool reverse = false)
	{
		AnimState state = (AnimState)stateInt;

		string animName = state switch
		{
			AnimState.P1IdleLeft => "p1IdleLeft",
            AnimState.P1IdleRight => "p1IdleRight",
            AnimState.P1IdleUp => "p1IdleUp",
            AnimState.P1IdleDown => "p1IdleDown",
            AnimState.P1WalkLeft => "p1WalkLeft",
            AnimState.P1WalkRight => "p1WalkRight",
            AnimState.P1WalkUp => "p1WalkUp",
            AnimState.P1WalkDown => "p1WalkDown",
            AnimState.P1StrafeLeft => "p1StrafeLeft",
            AnimState.P1StrafeRight => "p1StrafeRight",
            AnimState.P1StrafeUp => "p1StrafeUp",
            AnimState.P1StrafeDown => "p1StafeDown",
            AnimState.P1SwordSwingLeft => "p1SwordSwingLeft",
            AnimState.P1SwordSwingRight => "p1SwordSwingRight",
            AnimState.P1SwordSwingUp => "p1SwordSwingUp",
            AnimState.P1SwordSwingDown => "p1SwordSwingDown",
            AnimState.P1SwordResetLeft => "p1SwordResetLeft",
            AnimState.P1SwordResetRight => "p1SwordResetRight",
            AnimState.P1SwordResetUp => "p1SwordResetUp",
            AnimState.P1SwordResetDown => "p1SwordResetDown",
            AnimState.P1BackSwingLeft => "p1BackSwingLeft",
            AnimState.P1BackSwingRight => "p1BackSwingRight",
            AnimState.P1BackSwingUp => "p1BackSwingUp",
            AnimState.P1BackSwingDown => "p1BackSwingDown",
            AnimState.P1HitLeft => "p1HitLeft",
            AnimState.P1HitRight => "p1HitRight",
            AnimState.P1HitUp => "p1HitUp",
            AnimState.P1HitDown => "p1HitDown",
            AnimState.P1StaggerLeft => "p1StaggerLeft",
            AnimState.P1StaggerRight => "p1StaggerRight",
            AnimState.P1StaggerUp => "p1StaggerUp",
            AnimState.P1StaggerDown => "p1StaggerDown",
            AnimState.P1ShieldLeft => "p1ShieldLeft",
            AnimState.P1ShieldRight => "p1ShieldRight",
            AnimState.P1ShieldUp => "p1ShieldUp",
            AnimState.P1ShieldDown => "p1ShieldDown",
            AnimState.P1ThrustLeft => "p1ThrustLeft",
            AnimState.P1ThrustRight => "p1ThrustRight",
            AnimState.P1ThrustUp => "p1ThrustUp",
            AnimState.P1ThrustDown => "p1ThrustDown",
            AnimState.P2IdleLeft => "p2IdleLeft",
            AnimState.P2IdleRight => "p2IdleRight",
            AnimState.P2IdleUp => "p2IdleUp",
            AnimState.P2IdleDown => "p2IdleDown",
            AnimState.P2WalkLeft => "p2WalkLeft",
            AnimState.P2WalkRight => "p2WalkRight",
            AnimState.P2WalkUp => "p2WalkUp",
            AnimState.P2WalkDown => "p2WalkDown",
            AnimState.P2StrafeLeft => "p2StrafeLeft",
            AnimState.P2StrafeRight => "p2StrafeRight",
            AnimState.P2StrafeUp => "p2StrafeUp",
            AnimState.P2StrafeDown => "p2strafeDown",
            AnimState.P2SwordSwingLeft => "p2SwordSwingLeft",
            AnimState.P2SwordSwingRight => "p2SwordSwingRight",
            AnimState.P2SwordSwingUp => "p2SwordSwingUp",
            AnimState.P2SwordSwingDown => "p2SwordSwingDown",
            AnimState.P2SwordResetLeft => "p2SwordResetLeft",
            AnimState.P2SwordResetRight => "p2SwordResetRight",
            AnimState.P2SwordResetUp => "p2SwordResetUp",
            AnimState.P2SwordResetDown => "p2SwordResetDown",
            AnimState.P2BackSwingLeft => "p2BackSwingLeft",
            AnimState.P2BackSwingRight => "p2BackSwingRight",
            AnimState.P2BackSwingUp => "p2BackSwingUp",
            AnimState.P2BackSwingDown => "p2BackSwingDown",
            AnimState.P2HitLeft => "p2HitLeft",
            AnimState.P2HitRight => "p2HitRight",
            AnimState.P2HitUp => "p2HitUp",
            AnimState.P2HitDown => "p2HitDown",
            AnimState.P2StaggerLeft => "p2StaggerLeft",
            AnimState.P2StaggerRight => "p2StaggerRight",
            AnimState.P2StaggerUp => "p2StaggerUp",
            AnimState.P2StaggerDown => "p2StaggerDown",
            AnimState.P2ThrustLeft => "p2ThrustLeft",
            AnimState.P2ThrustRight => "p2ThrustRight",
            AnimState.P2ThrustUp => "p2ThrustUp",
            AnimState.P2ThrustDown => "p2ThrustDown",
            AnimState.P2SlamLeft => "p2SlamLeft",
            AnimState.P2SlamRight => "p2SlamRight",
            AnimState.P2SlamUp => "p2SlamUp",
            AnimState.P2SlamDown => "p2SlamDown",
            AnimState.EngageLeft => "engageLeft",
            AnimState.EngageRight => "engageRight",
            AnimState.EngageUp => "engageUp",
            AnimState.EngageDown => "engageDown",
            AnimState.ArmorCrackLeft => "armorCrackLeft",
            AnimState.ArmorCrackRight => "armorCrackRight",
            AnimState.ArmorCrackUp => "armorCrackup",
            AnimState.ArmorCrackDown => "armorCrackLeDown",
            AnimState.DeathLeft => "deathLeft",
            AnimState.DeathRight => "deathRight",
            AnimState.DeathUp => "deathUp",
            AnimState.DeathDown => "deathDown",
            _ => "p1IdleDown"
		};

		if (!animationLocked || lockAnim)
		{
			if (animationPlayer.CurrentAnimation != animName)
				animationPlayer.Play(animName);

			animationPlayer.SpeedScale = 1f;

			animationLocked = lockAnim;
		}
	}

    	private void OnAnimationFinshed(StringName animName)
	{
		if(animName.ToString().StartsWith("") || animName.ToString().StartsWith(""))
		{
			animationLocked = false;
		}
	}
}