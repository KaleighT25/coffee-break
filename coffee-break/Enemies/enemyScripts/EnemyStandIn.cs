using Godot;
using System;

public partial class EnemyStandIn : EnemyTemp
{
        private Area2D collisionDamage;
        public override void _Ready()
    {
        AddToGroup("enemies");
        
        collisionDamage = GetNode<Area2D>("collisionDamage");
        collisionDamage.BodyEntered += OnCollisionEntered;
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

        //ApplyKnockback(attackPos);

        if (Health <= 0)
        {
            QueueFree();
        }
    }
}