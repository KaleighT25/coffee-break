using Godot;

public partial class Camera : Camera2D
{
    [Export] public NodePath playerPath;
    [Export] public float followSpeed = 0.18f;

    private Node2D player;

    public override void _Ready()
    {
        player = GetNode<Node2D>(playerPath);
        MakeCurrent();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player == null) return;

        GlobalPosition = GlobalPosition.Lerp(
            player.GlobalPosition,
            followSpeed
        );
    }
}

