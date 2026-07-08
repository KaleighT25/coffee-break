using Godot;
using System;

public partial class EnemyStandIn : EnemyBase
{
	[ExportGroup("Sword Swing")]
	[Export] public int SwingDamage = 15;
	[Export] public float SwingKnockback = 350f;
	[Export] public float ActiveHitWindow = 0.15f; // slice of AttackDuration the blade is actually live

	private Sprite2D knightSprite;
	private Hitbox swordHitbox;
	private float attackElapsed = 0f;

	public override void _Ready()
	{
		base._Ready();

		knightSprite = GetNode<Sprite2D>("knightSprite");

		swordHitbox = GetNode<Hitbox>("SwordHitbox");
		swordHitbox.OwnerNode = this;
		swordHitbox.Damage = SwingDamage;
		swordHitbox.Knockback = SwingKnockback;
		swordHitbox.Monitoring = false;
	}

	protected override void OnDamaged(int damage)
	{
		base.OnDamaged(damage);
		FlashWhite();
	}

	private async void FlashWhite()
	{
		knightSprite.Modulate = Colors.White;
		await ToSignal(GetTree().CreateTimer(0.08f), "timeout");

		if (IsInstanceValid(knightSprite))
			knightSprite.Modulate = Colors.Gray;
	}

	protected override void OnStateChanged(EnemyState next)
	{
		switch (next)
		{
			case EnemyState.Telegraph:
				knightSprite.Modulate = new Color(1f, 0.55f, 0.55f); // reddish "about to swing" tell
				break;

			case EnemyState.Attack:
				attackElapsed = 0f;
				swordHitbox.Monitoring = false; // turned on mid-swing in TickAttack
				break;

			case EnemyState.Chase:
			case EnemyState.Idle:
				knightSprite.Modulate = Colors.Gray;
				break;
		}
	}

	protected override void TickTelegraph(float dt)
	{
		Velocity = Vector2.Zero;

		// Whiff instead of guaranteeing a hit if the player rolls out of range
		// mid-windup — this is the "reward a good dodge" half of the loop.
		if (player != null && GlobalPosition.DistanceTo(player.GlobalPosition) > AttackRange * 1.4f)
		{
			ChangeState(EnemyState.Recover);
			return;
		}

		if (stateTimer <= 0f)
			ChangeState(EnemyState.Attack);
	}

	protected override void TickAttack(float dt)
	{
		Velocity = Vector2.Zero;
		attackElapsed += dt;

		// Only the middle slice of the swing deals damage, giving the player
		// a real, learnable window to dodge either side of the actual hit.
		float activeStart = (AttackDuration - ActiveHitWindow) / 2f;
		float activeEnd = activeStart + ActiveHitWindow;

		swordHitbox.Monitoring = attackElapsed >= activeStart && attackElapsed <= activeEnd;

		if (stateTimer <= 0f)
		{
			swordHitbox.Monitoring = false;
			ChangeState(EnemyState.Recover);
		}
	}

	protected override bool CanAttackNow()
	{
		// Don't commit to a swing while the player is actively rolling —
		// they'd just dodge through it for free. Wait until they're still,
		// then punish them the moment they stop.
		return player == null || !player.IsRolling;
	}
}
