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
        if (DamageableOwner.HasMethod("TakeDamage"))
        {
            DamageableOwner.Call(
                "TakeDamage",
                hitbox.Damage,
                hitbox.OwnerNode.GlobalPosition
            );
        }
    }
}
