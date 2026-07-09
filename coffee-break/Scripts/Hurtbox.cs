using Godot;
using System;

public partial class Hurtbox : Area2D
{
    public Node DamageableOwner;

    public override void _Ready()
    {
        DamageableOwner = GetParent();
    }

    public void ReceiveHit(Hitbox hitbox)
    {
        if (DamageableOwner is IDamageable damageable)
        {
            var attack = new AttackData
            {
                Damage = hitbox.Damage,
                Knockback = hitbox.Knockback,
                Unblockable = hitbox.Unblockable,
                Parryable = hitbox.Parryable,
                Source = hitbox.OwnerNode,
                Origin = hitbox.OwnerNode != null
                    ? hitbox.OwnerNode.GlobalPosition
                    : ((Node2D)hitbox).GlobalPosition
            };

            damageable.TakeDamage(attack);
        }
    }
}