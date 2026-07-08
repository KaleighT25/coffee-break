using Godot;
using System;

public partial class ToNewScene : Area2D
{
     [Export] public string TargetScenePath;
     [Export] public string TargetSpawnPoint;

      public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }
     private void OnBodyEntered(Node2D body)
    {
        if (body is Player)
        {
            GameManager.NextSpawnPoint = TargetSpawnPoint;
            
            CallDeferred(nameof(ChangeScene));
        }
    }

    private void ChangeScene()
    {
         Main.Instance.ChangeMap(
            TargetScenePath,
            TargetSpawnPoint
        );
    }
}
