using Godot;
using System;

public partial class EnemyStandIn : EnemyTemp
{
    [Export] public float speed = 150f;
    [Export] public float detectionRadius = 300f;

        private Area2D collisionDamage;
        private Player playerTarget;

    public override void _Ready()
    {
        AddToGroup("enemies");
        
        collisionDamage = GetNode<Area2D>("collisionDamage");
        collisionDamage.BodyEntered += OnCollisionEntered;
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
}