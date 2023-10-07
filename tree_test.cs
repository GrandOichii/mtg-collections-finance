using Godot;
using System;

public partial class tree_test : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var tree = GetNode<Tree>("%Tree");
		var root = tree.CreateItem();
		root.SetCellMode(0, TreeItem.TreeCellMode.Check);

		root.SetEditable(0, true);
		root.SetText(0, "root");
		
		for (int i = 0; i < 3; i++) {
			var child = tree.CreateItem(root);
			child.SetCellMode(0, TreeItem.TreeCellMode.Check);
			
			child.SetEditable(0, true);
			child.SetText(0 , "child" + (i+1));

//			child.SetChecked(0, true);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private void _on_tree_check_propagated_to_item(TreeItem item, long column)
	{
		GD.Print(item.IsChecked(0));
//		GD.Print("amogus");
		// Replace with function body.
	}

	private void _on_tree_item_edited()
	{
		var selected = GetNode<Tree>("%Tree").GetSelected();
		selected.PropagateCheck(0);
		// Replace with function body.
	}
}





