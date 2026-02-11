using Godot;
using System;

[GlobalClass]
public partial class EnemyTemp : CharacterBody2D
{
	[Export] public string Title { get; set; }

	[Export] public Godot.Collections.Array<ItemDrops> ItemDrops = new();

	[Export] public int Health { get; set; }

	[Export] public int Damage { get; set; }
}
