using Godot;
using System;

public partial class Main : Node2D
{
    public static Main Instance;

    [Export] public Node2D World;
    [Export] public CharacterBody2D Player;

    public override void _Ready()
    {
        Instance = this;

        ChangeMap("res://WorldScenes/playerHome.tscn", "StartSpawn");
    }

    public void ChangeMap(string scenePath, string spawnName)
    {
        foreach (Node child in World.GetChildren())
        {
            child.QueueFree();
        }

        PackedScene scene = GD.Load<PackedScene>(scenePath);
        Node2D map = scene.Instantiate<Node2D>();

        World.AddChild(map);

        Marker2D spawn = map.GetNode<Marker2D>(
            "SpawnPoints/" + spawnName
        );

        Player.GlobalPosition = spawn.GlobalPosition;
    }
}
