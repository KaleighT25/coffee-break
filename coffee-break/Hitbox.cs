using Godot;
using System;

public partial class Hitbox : Area2D
{
	[Export] public int Damage {get; set;}
	public Node2D OwnerNode;

	public override void _Ready()
	{
		Monitoring = false;
		AreaEntered += OnAreaEntered;
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is Hurtbox hurtbox)
		{
			hurtbox.ReceiveHit(this);
		}
	}
}
