using Godot;
using System;

public enum EnemyState
{
    Idle,
    Chase,
    Telegraph,
    Attack,
    Recover,
    Block,
    CounterAttack,
    Hitstun,
    Parried,
    Dead
}

[GlobalClass]
public partial class EnemyBase : CharacterBody2D, IDamageable
{
    [Export] public string Title { get; set; }
    [Export] public Godot.Collections.Array<ItemDrops> ItemDrops = new();
    [Export] public int Damage { get; set; }

    [ExportGroup("Movement")]
    [Export] public float MoveSpeed = 90f;
    [Export] public float AggroRadius = 220f;
    [Export] public float DeaggroRadius = 320f;
    [Export] public float AttackRange = 60f;

    [ExportGroup("Combat Timing")]
    [Export] public float TelegraphDuration = 0.35f;
    [Export] public float AttackDuration = 0.25f;
    [Export] public float RecoverDuration = 0.4f;
    [Export] public float HitstunDuration = 0.2f;

    [ExportGroup("Parry")]
    [Export] public float ParriedStunDuration = 1.5f; // how long it's stunned and open after a parry
    [Export] public float ParryKnockback = 400f;

    [ExportGroup("Contact Damage")]
    [Export] public bool DealsContactDamage = true;
    [Export] public NodePath ContactAreaPath = "collisionDamage";
    [Export] public float ContactKnockback = 250f;

    protected HealthComponent health;
    protected Player player; // single-player assumption; swap for a registry if that changes
    protected EnemyState state = EnemyState.Idle;
    protected float stateTimer = 0f;
    protected Vector2 knockbackVelocity = Vector2.Zero;
    protected Area2D contactArea;

    public override void _Ready()
    {
        AddToGroup("enemies");

        health = GetNode<HealthComponent>("HealthComponent");
        health.Damaged += OnDamaged;
        health.Died += OnDeath;

        EnsurePlayerReference();

        if (DealsContactDamage && HasNode(ContactAreaPath))
        {
            contactArea = GetNode<Area2D>(ContactAreaPath);
            contactArea.BodyEntered += OnContactBodyEntered;
        }
    }

    private void EnsurePlayerReference()
    {
        if (player == null)
        {
            player = GetTree().GetFirstNodeInGroup("players") as Player;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (state == EnemyState.Dead)
            return;

        EnsurePlayerReference();

        if (knockbackVelocity.Length() > 5f)
        {
            Velocity = knockbackVelocity;
            knockbackVelocity = knockbackVelocity.MoveToward(Vector2.Zero, 900f * dt);
            MoveAndSlide();
            return; // knockback overrides normal state logic while it plays out
        }

        stateTimer -= dt;

        switch (state)
        {
            case EnemyState.Idle: TickIdle(dt); break;
            case EnemyState.Chase: TickChase(dt); break;
            case EnemyState.Telegraph: TickTelegraph(dt); break;
            case EnemyState.Attack: TickAttack(dt); break;
            case EnemyState.Recover: TickRecover(dt); break;
            case EnemyState.Block: TickBlock(dt); break;
            case EnemyState.CounterAttack: TickCounter(dt); break;
            case EnemyState.Hitstun: TickHitstun(dt); break;
            case EnemyState.Parried: TickParried(dt); break;
        }

        MoveAndSlide();
    }

    // ---------------------------------------------------------------
    // Default per-state behavior. Override any of these in a subclass
    // to give an enemy type its own personality without touching the
    // shared plumbing (health, knockback, contact damage, death).
    // ---------------------------------------------------------------

    protected virtual void TickIdle(float dt)
    {
        Velocity = Vector2.Zero;

        if (player != null && GlobalPosition.DistanceTo(player.GlobalPosition) <= AggroRadius)
            ChangeState(EnemyState.Chase);
    }

    protected virtual void TickChase(float dt)
    {
        if (player == null) { ChangeState(EnemyState.Idle); return; }

        float dist = GlobalPosition.DistanceTo(player.GlobalPosition);

        if (dist > DeaggroRadius)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        if (dist <= AttackRange && CanAttackNow())
        {
            ChangeState(EnemyState.Telegraph);
            return;
        }

        Velocity = (player.GlobalPosition - GlobalPosition).Normalized() * MoveSpeed;
    }

    protected virtual void TickTelegraph(float dt)
    {
        Velocity = Vector2.Zero;

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Attack);
    }

    protected virtual void TickAttack(float dt)
    {
        Velocity = Vector2.Zero;

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Recover);
    }

    protected virtual void TickRecover(float dt)
    {
        Velocity = Vector2.Zero;

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Chase);
    }

    protected virtual void TickHitstun(float dt)
    {
        Velocity = Vector2.Zero;

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Chase);
    }

    // Override to play a dedicated stunned/reeling animation. Base just
    // stands still for ParriedStunDuration — the knockback shove itself is
    // already handled by the shared knockbackVelocity system in
    // _PhysicsProcess before this ever runs.
    protected virtual void TickParried(float dt)
    {
        Velocity = Vector2.Zero;

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Chase);
    }

    // Override to raise a guard, play a block animation, etc. Base does
    // nothing but return to Chase when the state timer runs out — a
    // subclass that never enters Block never needs to touch this.
    protected virtual void TickBlock(float dt)
    {
        Velocity = Vector2.Zero;

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Chase);
    }

    // Override to swing a fast riposte after a successful block.
    protected virtual void TickCounter(float dt)
    {
        Velocity = Vector2.Zero;

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Recover);
    }

    // Override to add cooldowns, line-of-sight checks, "don't attack while
    // player is mid-roll" logic, etc.
    protected virtual bool CanAttackNow() => true;

    protected void ChangeState(EnemyState next)
    {
        state = next;
        stateTimer = next switch
        {
            EnemyState.Telegraph => TelegraphDuration,
            EnemyState.Attack => AttackDuration,
            EnemyState.Recover => RecoverDuration,
            EnemyState.Hitstun => HitstunDuration,
            EnemyState.Parried => ParriedStunDuration,
            _ => 0f
        };
        OnStateChanged(next);
    }

    // Override to trigger animations/VFX per state without touching the
    // state machine logic itself.
    protected virtual void OnStateChanged(EnemyState next) { }

    // ---------------------------------------------------------------
    // Damage / death
    // ---------------------------------------------------------------

    public virtual void TakeDamage(AttackData attack)
    {
        if (state == EnemyState.Dead) return;

        if (!attack.Unblockable && (state == EnemyState.Block || state == EnemyState.CounterAttack))
        {
            if (state == EnemyState.Block)
            {
                OnAttackBlocked(attack);
                ChangeState(EnemyState.CounterAttack);
            }
            else
            {
                // Already countering — poise through it instead of letting a
                // second hit interrupt the counter-swing mid-animation.
                OnAttackAbsorbed(attack);
            }
            return;
        }

        health.Damage(attack.Damage);

        Vector2 dir = (GlobalPosition - attack.Origin).Normalized();
        knockbackVelocity = dir * attack.Knockback;

        ChangeState(EnemyState.Hitstun);
    }

    // Called by the player when they successfully parry this enemy's attack.
    // Base handles the generic stun + knockback-away-from-player; override
    // to add a flash, sound, or custom reaction on top.
    public virtual void OnParried(Player parrier)
    {
        if (state == EnemyState.Dead) return;

        GD.Print($"{Name}: PARRIED!");

        Vector2 dir = (GlobalPosition - parrier.GlobalPosition).Normalized();
        knockbackVelocity = dir * ParryKnockback;

        ChangeState(EnemyState.Parried);
    }

    // Override to flash/sound-cue a hit that landed but was absorbed while
    // countering (no damage, no interruption).
    protected virtual void OnAttackAbsorbed(AttackData attack) { }

    // Override to flash the shield, play a block sound/animation, etc.
    protected virtual void OnAttackBlocked(AttackData attack)
    {
        GD.Print($"{Title} blocked the attack!");
    }

    // Override for hit-flash, sound, etc. — base already logs + fires knockback.
    protected virtual void OnDamaged(int damage)
    {
        GD.Print($"{Title} took {damage} damage. HP: {health.CurrentHealth}");
    }

    protected virtual void OnDeath()
    {
        state = EnemyState.Dead;
        GD.Print($"{Title} died!");
        QueueFree();
    }

    protected virtual void OnContactBodyEntered(Node2D body)
    {
        if (body is Player player && state != EnemyState.Dead)
        {
            player.TakeDamage(new AttackData
            {
                Damage = Damage,
                Knockback = ContactKnockback,
                Origin = GlobalPosition,
                Source = this
            });
        }
    }
}