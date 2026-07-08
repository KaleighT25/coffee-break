using Godot;
using System;

//written by gavv >_<
//script to control the health bar


public partial class HealthScript : Node
{


	private int maxHealth;
	private int currHealth;
	private int checkHealth;
	private Player player;
	Sprite2D healthBarSprite;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		healthBarSprite = (Sprite2D)GetNode("CurrentHealth");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (player == null)
		{
			player = GetTree().GetFirstNodeInGroup("players") as Player;
			if (player == null)
				return; // Player scene not loaded yet — try again next frame

			maxHealth = player.maxHealth;
			currHealth = player.getCurrentHealth();
			checkHealth = currHealth;
			updateHealth();
		}

		currHealth = player.getCurrentHealth();

		if (checkHealth != currHealth)
		{
			updateHealth();
			checkHealth = currHealth;
		}
	}
	
	//update the bar
	private void updateHealth()
	{
		if (currHealth < 0)
			currHealth = 0;

		float percent = getBarScale(maxHealth, currHealth);

		Vector2 scale = healthBarSprite.Scale;
		scale.X = percent;
		healthBarSprite.Scale = scale;

		if (percent > 0.5f)
		{
			healthBarSprite.Modulate = Colors.Green;
		}
		else if (percent > 0.25f)
		{
			healthBarSprite.Modulate = Colors.Yellow;
		}
		else
		{
			healthBarSprite.Modulate = Colors.Red;
		}
	}
	
	//gets the H-scale in 0->100% of health bar based
	//on current health
	public float getBarScale(int max, int curr) {
		return ((float)curr/(float)max);
	}
}