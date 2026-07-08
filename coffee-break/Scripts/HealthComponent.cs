using Godot;
using System;

public partial class HealthComponent : Node
{
    [Export] public int MaxHealth = 100;

    public int CurrentHealth { get; private set; }

    [Signal]
    public delegate void DamagedEventHandler(int damage);

    [Signal]
    public delegate void DiedEventHandler();

	[Export] public float InvincibleTime = 0.12f;
	private float invincibleTimer = 0;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

	public override void _Process(double delta)
	{
		if (invincibleTimer > 0)
			invincibleTimer -= (float)delta;
	}

    public void Damage(int amount)
	{
		if (CurrentHealth <= 0)
			return;

		if (invincibleTimer > 0)
			return;

		invincibleTimer = InvincibleTime;

		CurrentHealth -= amount;

		EmitSignal(SignalName.Damaged, amount);

		if (CurrentHealth <= 0)
		{
			CurrentHealth = 0;
			EmitSignal(SignalName.Died);
		}
	}
    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
    }
}