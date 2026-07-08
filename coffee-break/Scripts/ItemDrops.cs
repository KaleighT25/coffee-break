using Godot;
using System;

[GlobalClass]
public partial class ItemDrops : Resource
{
    [Export] public string Title { get; set; }

    [Export] public Texture2D Texture { get; set; }

    [Export] public string Type { get; set; }

    [Export] public float Amount { get; set; }
}