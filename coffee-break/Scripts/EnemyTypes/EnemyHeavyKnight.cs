using Godot;
using System;

public partial class EnemyHeavyKnight : EnemyBase
{
    private enum AnimState
    {
        IdleLeft, IdleRight, IdleUp, IdleDown,
        WalkLeft, WalkRight, WalkUp, WalkDown,
        StrafeCWLeft, StrafeCWRight, StrafeCWUp, StrafeCWDown,
        StrafeCCWLeft, StrafeCCWRight, StrafeCCWUp, StrafeCCWDown,
        StepBackLeft, StepBackRight, StepBackUp, StepBackDown,
        ThrustLeft, ThrustRight, ThrustUp, ThrustDown
    }

    private enum AttackType { Slash, Thrust }
    private AttackType currentAttack = AttackType.Slash;

    private enum FacingDirection { Left, Right, Up, Down }
    private FacingDirection facingDirection = FacingDirection.Down;
    private AnimationPlayer animationPlayer;


    [ExportGroup("Heavy Swing")]
    [Export] public int SwingDamage = 30;
    [Export] public float SwingKnockback = 450f;
    [Export] public float ActiveHitWindow = 0.15f;

    [ExportGroup("Thrust")]
    [Export] public int ThrustDamage = 22;
    [Export] public float ThrustKnockback = 380f;
    [Export] public float ThrustTelegraphDuration = 0.15f;   // much quicker windup than the slash
    [Export] public float ThrustAttackDuration = 0.18f;
    [Export] public float ThrustActiveWindow = 0.08f;        // brief — a poke, not a swing
    [Export] public float ThrustStepInSpeed = 300f;          // faster dash-in than the heavy swing

    [ExportGroup("Guard")]
    [Export] public float GuardRange = 140f;      // must be >= AttackRange to matter
    [Export] public float BlockHoldDuration = 0.35f;
    [Export] public int CounterDamage = 20;
    [Export] public float CounterKnockback = 300f;
    [Export] public float CounterTelegraph = 0.1f;
    [Export] public float CounterActiveWindow = 0.12f;

    [ExportGroup("Positioning")]
    [Export] public float StrafeInfluence = 0.8f; // 0 = ignore player strafing, 1 = fully mirror it
    [Export] public float StrafeSpeedMultiplier = 0.1f; // how much to slow down while matching a strafing player, when closing distance
    [Export] public float StrafeDetectThreshold = 0.3f; // how "sideways" player movement must be to count as strafing

    [ExportGroup("Darknut Rhythm")]
    [Export] public float PreferredDistance = 110f;     // the range it tries to hold while circling
    [Export] public float PreferredDistanceBand = 20f;   // tolerance before it bothers adjusting in/out
    [Export] public float OrbitSpeed = 70f;              // speed while circling at PreferredDistance
    [Export] public float CircleTimeMin = 1.2f;          // randomized circling duration before it commits
    [Export] public float CircleTimeMax = 2.5f;
    [Export] public float StepInSpeed = 220f;            // fast dash speed during the Telegraph step-in
    [Export] public float StepBackSpeed = 90f;           // retreat speed during Recover

    private float circleTimer = 0f;
    private int orbitDirection = 1; // +1 or -1

    private Sprite2D knightSprite;
    private Sprite2D swordSprite;
    private Hitbox swordHitbox;
    private float attackElapsed = 0f;
    private float counterElapsed = 0f;

    public override void _Ready()
    {
        base._Ready();

        knightSprite = GetNode<Sprite2D>("knightSprite");
        swordSprite = GetNode<Sprite2D>("swordSprite");
        
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        swordHitbox = GetNode<Hitbox>("SwordHitbox");
        swordHitbox.OwnerNode = this;
        swordHitbox.Monitoring = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        if (state == EnemyState.Recover)
        {
            if (player != null)
                FacePlayer();

            PlayStepBackForFacing();
            return;
        }

        // Telegraph/Attack/Block/CounterAttack drive their own animations
        // (tied to their hitbox timing) — this only handles idle/walk/strafe.
        if (state != EnemyState.Idle && state != EnemyState.Chase)
            return;

        if (player != null)
            FacePlayer();

        float speed = Velocity.Length();

        if (speed < 5f)
        {
            PlayIdleForFacing();
            return;
        }

        if (player == null || player.LockedTarget != this)
        {
            PlayWalkForFacing();
            return;
        }

        Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
        Vector2 tangent = new Vector2(-toPlayer.Y, toPlayer.X);

        float radial = Velocity.Normalized().Dot(toPlayer);
        float lateral = Velocity.Normalized().Dot(tangent);

        if (Mathf.Abs(radial) >= Mathf.Abs(lateral))
        {
            PlayWalkForFacing();
        }
        else
        {
            // NOTE: if clockwise/counter-clockwise looks swapped in-game,
            // just flip this condition — screen-space Y-down can flip the
            // sense of "clockwise" depending on how you think about it.
            bool clockwise = lateral > 0f;
            PlayStrafeForFacing(clockwise);
        }
    }

    private void FacePlayer()
    {
        Vector2 dir = player.GlobalPosition - GlobalPosition;

        facingDirection = Math.Abs(dir.X) > Math.Abs(dir.Y)
            ? (dir.X > 0 ? FacingDirection.Right : FacingDirection.Left)
            : (dir.Y > 0 ? FacingDirection.Down : FacingDirection.Up);
    }

    private void PlayIdleForFacing()
    {
        switch (facingDirection)
        {
            case FacingDirection.Left: PlayAnim(AnimState.IdleLeft); break;
            case FacingDirection.Right: PlayAnim(AnimState.IdleRight); break;
            case FacingDirection.Up: PlayAnim(AnimState.IdleUp); break;
            case FacingDirection.Down: PlayAnim(AnimState.IdleDown); break;
        }
    }

    private void PlayWalkForFacing()
    {
        switch (facingDirection)
        {
            case FacingDirection.Left: PlayAnim(AnimState.WalkLeft); break;
            case FacingDirection.Right: PlayAnim(AnimState.WalkRight); break;
            case FacingDirection.Up: PlayAnim(AnimState.WalkUp); break;
            case FacingDirection.Down: PlayAnim(AnimState.WalkDown); break;
        }
    }

    private void PlayStrafeForFacing(bool clockwise)
    {
        if (clockwise)
        {
            switch (facingDirection)
            {
                case FacingDirection.Left: PlayAnim(AnimState.StrafeCWLeft); break;
                case FacingDirection.Right: PlayAnim(AnimState.StrafeCWRight); break;
                case FacingDirection.Up: PlayAnim(AnimState.StrafeCWUp); break;
                case FacingDirection.Down: PlayAnim(AnimState.StrafeCWDown); break;
            }
        }
        else
        {
            switch (facingDirection)
            {
                case FacingDirection.Left: PlayAnim(AnimState.StrafeCCWLeft); break;
                case FacingDirection.Right: PlayAnim(AnimState.StrafeCCWRight); break;
                case FacingDirection.Up: PlayAnim(AnimState.StrafeCCWUp); break;
                case FacingDirection.Down: PlayAnim(AnimState.StrafeCCWDown); break;
            }
        }
    }

    private void PlayThrustForFacing()
    {
        if (player != null)
            FacePlayer();

        switch (facingDirection)
        {
            case FacingDirection.Left: PlayAnim(AnimState.ThrustLeft); break;
            case FacingDirection.Right: PlayAnim(AnimState.ThrustRight); break;
            case FacingDirection.Up: PlayAnim(AnimState.ThrustUp); break;
            case FacingDirection.Down: PlayAnim(AnimState.ThrustDown); break;
        }
    }

    private void PlayStepBackForFacing()
    {
        switch (facingDirection)
        {
            case FacingDirection.Left: PlayAnim(AnimState.StepBackLeft); break;
            case FacingDirection.Right: PlayAnim(AnimState.StepBackRight); break;
            case FacingDirection.Up: PlayAnim(AnimState.StepBackUp); break;
            case FacingDirection.Down: PlayAnim(AnimState.StepBackDown); break;
        }
    }

    private void PlayAnim(AnimState animState)
    {
        // Assumes clip names like "idleUp", "walkLeft", "strafeCWDown",
        // "strafeCCWRight" — rename the strings below if yours differ.
        string animName = animState switch
        {
            AnimState.IdleLeft => "idleLeft",
            AnimState.IdleRight => "idleRight",
            AnimState.IdleUp => "idleUp",
            AnimState.IdleDown => "idleDown",
            AnimState.WalkLeft => "walkLeft",
            AnimState.WalkRight => "walkRight",
            AnimState.WalkUp => "walkUp",
            AnimState.WalkDown => "walkDown",
            AnimState.StrafeCWLeft => "strafeCWLeft",
            AnimState.StrafeCWRight => "strafeCWRight",
            AnimState.StrafeCWUp => "strafeCWUp",
            AnimState.StrafeCWDown => "strafeCWDown",
            AnimState.StrafeCCWLeft => "strafeCCWLeft",
            AnimState.StrafeCCWRight => "strafeCCWRight",
            AnimState.StrafeCCWUp => "strafeCCWUp",
            AnimState.StrafeCCWDown => "strafeCCWDown",
            AnimState.StepBackLeft => "stepBackLeft",
            AnimState.StepBackRight => "stepBackRight",
            AnimState.StepBackUp => "stepBackUp",
            AnimState.StepBackDown => "stepBackDown",
            AnimState.ThrustLeft => "thrustLeft",
            AnimState.ThrustRight => "thrustRight",
            AnimState.ThrustUp => "thrustUp",
            AnimState.ThrustDown => "thrustDown",
            _ => "idleDown"
        };

        if (animationPlayer.CurrentAnimation != animName)
            animationPlayer.Play(animName);
    }

    protected override void OnDamaged(int damage)
    {
        base.OnDamaged(damage);
        FlashColor(Colors.White);
    }

    private async void FlashColor(Color c, Color? returnTo = null)
    {
        knightSprite.Modulate = c;
        await ToSignal(GetTree().CreateTimer(0.08f), "timeout");

        if (IsInstanceValid(knightSprite) && state != EnemyState.Dead)
            knightSprite.Modulate = returnTo ?? Colors.Gray;
    }

    protected override void TickChase(float dt)
    {
        if (player == null) { ChangeState(EnemyState.Idle); return; }

        float dist = GlobalPosition.DistanceTo(player.GlobalPosition);

        if (dist > DeaggroRadius)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        // The player just committed to a nearby swing — raise the guard
        // instead of eating the hit. A lunge is deliberately excluded here
        // so it still reads as "the move that breaks a guard."
        if (dist <= GuardRange && player.IsAttacking && !player.IsLunging)
        {
            ChangeState(EnemyState.Block);
            return;
        }

        circleTimer -= dt;

        // Time's up and we're within striking distance — commit to the attack.
        if (circleTimer <= 0f && dist <= PreferredDistance + PreferredDistanceBand && CanAttackNow())
        {
            currentAttack = ChooseAttack();
            ChangeState(EnemyState.Telegraph);
            return;
        }

        Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
        Vector2 tangent = new Vector2(-toPlayer.Y, toPlayer.X);

        Vector2 moveDir;
        float speed;

        if (dist > PreferredDistance + PreferredDistanceBand)
        {
            // Too far — close the gap, same counter-strafe logic as before
            // so we don't outrun a player who's circling while we approach.
            moveDir = toPlayer;
            speed = MoveSpeed;

            if (player.LockedTarget == this)
            {
                float playerStrafeSide = player.Velocity.Normalized().Dot(tangent);

                if (Mathf.Abs(playerStrafeSide) > StrafeDetectThreshold)
                {
                    moveDir += tangent * -playerStrafeSide * StrafeInfluence;
                    moveDir = moveDir.Normalized();
                    speed *= StrafeSpeedMultiplier;
                }
            }
        }
        else if (dist < PreferredDistance - PreferredDistanceBand)
        {
            // Too close without having committed to an attack yet — back off
            // a little while still circling, rather than just retreating.
            moveDir = (-toPlayer + tangent * orbitDirection * 0.6f).Normalized();
            speed = OrbitSpeed;
        }
        else
        {
            // At range — orbit around the player instead of standing still
            // or continuing to close in.
            moveDir = tangent * orbitDirection;
            speed = OrbitSpeed;
        }

        Velocity = moveDir * speed;
    }

    private AttackType ChooseAttack()
    {
        // Only one attack for now while we test it in isolation — once more
        // attacks exist, replace this with a random pick (or weighted pick)
        // among them, e.g.: return GD.Randf() < 0.5f ? AttackType.Slash : AttackType.Thrust;
        return AttackType.Thrust;
    }

    protected override void TickTelegraph(float dt)
    {
        // The "quick step in" — a fast dash toward the player rather than
        // standing still, covering whatever's left of PreferredDistanceBand.
        if (player != null)
        {
            float dashSpeed = currentAttack == AttackType.Thrust ? ThrustStepInSpeed : StepInSpeed;
            Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
            Velocity = toPlayer * dashSpeed;

            // Whiff instead of guaranteeing a hit if the player bolts away
            // mid-dash — rewards a well-timed dodge.
            if (GlobalPosition.DistanceTo(player.GlobalPosition) > AttackRange * 2f)
            {
                ChangeState(EnemyState.Recover);
                return;
            }
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Attack);
    }

    protected override void TickRecover(float dt)
    {
        // Step back to re-establish orbit distance instead of just standing
        // still after the swing.
        if (player != null)
        {
            Vector2 away = (GlobalPosition - player.GlobalPosition).Normalized();
            Velocity = away * StepBackSpeed;
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Chase);
    }

    protected override void TickBlock(float dt)
    {
        Velocity = Vector2.Zero;

        if (stateTimer <= 0f)
            ChangeState(EnemyState.Chase);
    }

    protected override void TickAttack(float dt)
    {
        Velocity = Vector2.Zero;
        attackElapsed += dt;

        float duration = currentAttack == AttackType.Thrust ? ThrustAttackDuration : AttackDuration;
        float activeWindow = currentAttack == AttackType.Thrust ? ThrustActiveWindow : ActiveHitWindow;

        float activeStart = (duration - activeWindow) / 2f;
        float activeEnd = activeStart + activeWindow;

        swordHitbox.Monitoring = attackElapsed >= activeStart && attackElapsed <= activeEnd;

        if (stateTimer <= 0f)
        {
            swordHitbox.Monitoring = false;
            ChangeState(EnemyState.Recover);
        }
    }

    protected override void TickCounter(float dt)
    {
        Velocity = Vector2.Zero;
        counterElapsed += dt;

        bool active = counterElapsed >= CounterTelegraph
                   && counterElapsed <= CounterTelegraph + CounterActiveWindow;

        swordHitbox.Monitoring = active;

        if (stateTimer <= 0f)
        {
            swordHitbox.Monitoring = false;
            ChangeState(EnemyState.Recover);
        }
    }

    protected override void OnStateChanged(EnemyState next)
    {
        switch (next)
        {
            case EnemyState.Telegraph:
                if (currentAttack == AttackType.Thrust)
                {
                    stateTimer = ThrustTelegraphDuration;
                    PlayThrustForFacing(); // one continuous step-in-and-thrust motion
                }
                else
                {
                    knightSprite.Modulate = new Color(1f, 0.5f, 0.5f); // red = about to swing
                }
                break;

            case EnemyState.Attack:
                attackElapsed = 0f;
                swordHitbox.Unblockable = false;
                swordHitbox.Monitoring = false;

                if (currentAttack == AttackType.Thrust)
                {
                    stateTimer = ThrustAttackDuration;
                    swordHitbox.Damage = ThrustDamage;
                    swordHitbox.Knockback = ThrustKnockback;
                }
                else
                {
                    swordHitbox.Damage = SwingDamage;
                    swordHitbox.Knockback = SwingKnockback;
                }
                break;

            case EnemyState.Block:
                knightSprite.Modulate = new Color(0.55f, 0.55f, 1f); // blue = guarding
                stateTimer = BlockHoldDuration;
                break;

            case EnemyState.CounterAttack:
                counterElapsed = 0f;
                swordHitbox.Damage = CounterDamage;
                swordHitbox.Knockback = CounterKnockback;
                swordHitbox.Unblockable = false;
                swordHitbox.Monitoring = false;
                knightSprite.Modulate = new Color(1f, 1f, 0.4f); // yellow = riposte
                stateTimer = CounterTelegraph + CounterActiveWindow + 0.1f;
                break;

            case EnemyState.Chase:
                circleTimer = (float)GD.RandRange(CircleTimeMin, CircleTimeMax);
                orbitDirection = GD.Randf() < 0.5f ? -1 : 1;
                knightSprite.Modulate = Colors.Gray;
                break;

            case EnemyState.Idle:
                knightSprite.Modulate = Colors.Gray;
                break;
        }
    }

    protected override void OnAttackBlocked(AttackData attack)
    {
        FlashColor(Colors.Cyan);
        GD.Print($"{Title} blocked the attack and is countering!");
    }

    protected override void OnAttackAbsorbed(AttackData attack)
    {
        FlashColor(new Color(1f, 1f, 1f, 0.6f), new Color(1f, 1f, 0.4f));
        GD.Print($"{Title} shrugged off a hit while countering!");
    }

    protected override bool CanAttackNow()
    {
        // Don't commit to a swing while the player is actively rolling —
        // wait until they're still, then punish.
        return player == null || !player.IsRolling;
    }
}