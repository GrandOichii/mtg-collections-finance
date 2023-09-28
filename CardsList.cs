using Godot;
using System;
using System.Collections.Generic;

public partial class CardsList : Control
{
	#region Signals
	
	[Signal]
	public delegate void CardActivatedEventHandler(Wrapper<ShortCard> cardW);
	
	#endregion
	
	#region Nodes
	
	public LineEdit FilterLineNode { get; private set; }
	public ItemList ListNode { get; private set; }
	
	#endregion
	
	private List<Wrapper<ShortCard>> _cards = new();
	
	public override void _Ready() {
		#region Node fetching
		
		FilterLineNode = GetNode<LineEdit>("%FilterLine");
		ListNode = GetNode<ItemList>("%List");
		
		#endregion
	}
	
	public void AddItem(Wrapper<ShortCard> cardW) {
		_cards.Add(cardW);
		_addItem(cardW);
	}
	
	private void _addItem(Wrapper<ShortCard> cardW) {
		var index = ListNode.AddItem(cardW.Value.Name);
		ListNode.SetItemMetadata(index, cardW);
	}
	
	public Wrapper<ShortCard> GetItem(int index) {
		return ListNode.GetItemMetadata(index).As<Wrapper<ShortCard>>();
	}
	
	public void ResetFilter() {
		FilterLineNode.Text = "";
		ListNode.Clear();

		foreach (var cardW in _cards)
			_addItem(cardW);
	}
	
	#region Signal connections
	
	private void _on_filter_button_pressed()
	{
		ListNode.Clear();
		
		var filter = FilterLineNode.Text.ToLower();
		foreach (var cardW in _cards) {
			if (!cardW.Value.Name.ToLower().Contains(filter)) continue;
			_addItem(cardW);
		}
	}
	
	private void _on_list_item_activated(int index)
	{
		EmitSignal(SignalName.CardActivated, GetItem(index));
	}
	
	#endregion
}




