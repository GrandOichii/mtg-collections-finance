using Godot;
using System;

public partial class Main : CanvasLayer
{
	#region Nodes
	
	public Control SavedPopinNode { get; private set; }
	public CollectionsTab CollectionsTabNode { get; private set; }
	
	#endregion
	
	public override void _Ready()
	{
		#region Node fetching
		
		SavedPopinNode = GetNode<Control>("%SavedPopin");
		CollectionsTabNode = GetNode<CollectionsTab>("%Collections");
		
		#endregion
		
		SavedPopinNode.Position = new(0, -SavedPopinNode.Size.Y);
	}
	
	public override void _Input(InputEvent e) {
		if (e.IsActionPressed("save-collections"))
			SaveCollections();
	}

	private void SaveCollections() {
		CollectionsTabNode.SaveCollections();
		
		var t = CreateTween();
		t.TweenProperty(SavedPopinNode, "position", new Vector2(0, 0), .2f);
		t.TweenInterval(.4f);
		t.TweenProperty(SavedPopinNode, "position", new Vector2(0, -SavedPopinNode.Size.Y), .2f);
	}

}
