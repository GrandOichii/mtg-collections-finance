using Godot;
using System;
using System.Linq;

using System.Collections.Generic;

public partial class ArchenemyTab : TabBar
{
	private static Random _rnd = new();
	
	#region Nodes
	
	public Label CountLabelNode { get; private set; }
	public Tree CardTreeNode { get; private set; }
	public MTGCardBase CardNode { get; private set; }
	public SchemesWindow SchemesWindowNode { get; private set; }

	#endregion

	private TreeItem _root; 
	private Dictionary<string, TreeItem> _setMap = new();
	
	public override void _Ready()
	{
		#region Node fetching
		
		CountLabelNode = GetNode<Label>("%CountLabel");
		CardTreeNode = GetNode<Tree>("%CardTree");
		CardNode = GetNode<MTGCardBase>("%Card");
		SchemesWindowNode = GetNode<SchemesWindow>("%SchemeWindow");
		
		#endregion

		// construct tree

		CreateRoot();

		UpdateTotal();
	}

	private void CreateRoot() {
		_root = CardTreeNode.CreateItem();
		_root.SetCellMode(0, TreeItem.TreeCellMode.Check);
		_root.SetEditable(0, true);
		_root.SetText(0, "All");
	}

	private TreeItem GetSet(string setName) {
		if (!_setMap.ContainsKey(setName)) {
			var item = _root.CreateChild();
			item.SetCellMode(0, TreeItem.TreeCellMode.Check);
			item.SetEditable(0, true);
			item.SetText(0, setName);
			_setMap.Add(setName, item);
		}

		return _setMap[setName];
	}
	
	private void UpdateTotal() {
		var count = 0;
		foreach (var item in _setMap.Values) {
			foreach (var child in item.GetChildren()) {
				if (child.IsChecked(0)) ++count;
			}
		}

		CountLabelNode.Text = "Total: " + count;
	}

	#region Signal connections

	private void _on_cards_card_added(Wrapper<ShortCard> cardW)
	{
		var card = cardW.Value;
		if (card.Layout != "scheme") return;

		var variation = card.GetVariations()[0];

		var set = variation.SetName;
		var setItem = GetSet(set);
		var item = setItem.CreateChild();
		item.SetCellMode(0, TreeItem.TreeCellMode.Check);
		item.SetEditable(0, true);
		item.SetText(0, card.Name);
		item.SetMetadata(0, new Wrapper<Card>(variation));


	}

	private void _on_card_tree_cell_selected()
	{
		var selected = CardTreeNode.GetSelected();
		var data = selected.GetMetadata(0);
		var card = data.As<Wrapper<Card>>();
		if (card is null) return;
		CardNode.Load(card.Value);
	}

	private void _on_card_tree_item_edited()
	{
		var selected = CardTreeNode.GetSelected();
		selected.PropagateCheck(0);
		UpdateTotal();
	}

	private void _on_start_button_pressed()
	{
		var cards = new List<Card>();
		foreach (var item in _setMap.Values) {
			foreach (var child in item.GetChildren()) {
				if (!child.IsChecked(0)) continue;
				
				cards.Add(child.GetMetadata(0).As<Wrapper<Card>>().Value);
			}
		}
		cards = cards.OrderBy(a => _rnd.Next()).ToList();

		SchemesWindowNode.Load(cards);
		SchemesWindowNode.Show();
	}

	private void _on_cards_clear_cards()
	{
		_root.Free();
		_setMap = new();
		CreateRoot();
	}

	#endregion
}







