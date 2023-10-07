using Godot;
using System;

public partial class OngoingScheme : TextureRect
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	#region Signal connections

	private void _on_close_button_pressed()
	{
		QueueFree();
	}
	
	#endregion
}

