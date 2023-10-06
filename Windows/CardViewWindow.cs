using Godot;
using System;
using System.Globalization;
using System.Threading.Tasks;

using System.Collections.Generic;

public partial class CardViewWindow : Window
{
	#region Nodes
	
	public MTGCardBase CardNode { get; private set; }
	public Label NameLabelNode { get; private set; }
	public Label TextLabelNode { get; private set; }
	public RichTextLabel PricesLabelNode { get; private set; }
	public ItemList PrintingsListNode { get; private set; }
	public LineEdit SetNameFilterEditNode { get; private set; }
	
	#endregion
	
	private List<Card> _variations;
	private Card _variant;
	public Card Variant { 
		get => _variant;
		set {
			_variant = value;
			UpdateInfo();
		} 
	}
	
	public override void _Ready()
	{
		#region Node fetching
		
		CardNode = GetNode<MTGCardBase>("%Card");
		NameLabelNode = GetNode<Label>("%NameLabel");
		TextLabelNode = GetNode<Label>("%TextLabel");
		PricesLabelNode = GetNode<RichTextLabel>("%PricesLabel");
		PrintingsListNode = GetNode<ItemList>("%PrintingsList");
		SetNameFilterEditNode = GetNode<LineEdit>("%SetNameFilterEdit");

		#endregion		
	}

	public void Load(Wrapper<ShortCard> cardW, Card variant=null) {
		var card = cardW.Value;
		NameLabelNode.Text = card.Name;
		TextLabelNode.Text = card.Text;

		PrintingsListNode.Clear();
		_variations = card.GetVariations();
		SetNameFilterEditNode.Text = "";
		_on_filter_button_pressed();
		if (variant is null) {
			Variant = _variations[0];
			return;
		}
		Variant = variant;
	}

	private void UpdateInfo() {
		PricesLabelNode.Clear();
		foreach (var pair in _variant.Prices) {
			var text = "-";
			if (pair.Value is not null) {
				text = pair.Value;
				var v = Math.Round(float.Parse(text, CultureInfo.InvariantCulture), 2);
				text = PriceUtil.GetColoredText(v, pair.Key);
			}
			PricesLabelNode.AppendText(pair.Key + ": " + text + "\n");
		}
		
		CardNode.Load(Variant);
	}
	
	#region Signal connections
	
	private void _on_close_requested()
	{
		Hide();
	}

	private void _on_printings_list_item_activated(int index)
	{
		Variant = PrintingsListNode.GetItemMetadata(index).As<Wrapper<Card>>().Value;
	}

	private void _on_filter_button_pressed()
	{
		PrintingsListNode.Clear();
		var filter = SetNameFilterEditNode.Text;
		foreach (var v in _variations) {
			if (!v.SetName.ToLower().Contains(filter.ToLower())) continue;
			var index = PrintingsListNode.AddItem(v.UID);
			PrintingsListNode.SetItemMetadata(index, new Wrapper<Card>(v));
		}
	}
	
	#endregion
}
