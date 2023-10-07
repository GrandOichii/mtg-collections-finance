using Godot;
using System;
using System.Collections.Generic;


public partial class SchemesWindow : Window
{
	#region Packed scenes
	
	static readonly PackedScene OngoingSchemePS = ResourceLoader.Load("res://OngoingScheme.tscn") as PackedScene;
	
	#endregion
	
	#region Nodes

	public MTGCardBase CardNode { get; private set; }
	public FlowContainer OngoingSchemesNode { get; private set; }

	#endregion
	
	private List<Card> _cards;
	
	private int _cardI;
	private int CardI {
		get => _cardI;
		set {
			_cardI = value;
			var count = _cards.Count;
			if (_cardI < 0)
				_cardI = count - 1;
			if (_cardI >= count)
				_cardI = 0;
			CardNode.Load(_cards[_cardI]);
		}
	}

	public override void _Ready()
	{
		#region Node fetching
		
		CardNode = GetNode<MTGCardBase>("%Card");
		OngoingSchemesNode = GetNode<FlowContainer>("%OngoingSchemes");
		
		#endregion
	}

	public void Load(List<Card> cards) {
		_cards = cards;
		
		RemoveOngoing();
		CardI = 0;
	}
	
	private void RemoveOngoing() {
		foreach (var child in OngoingSchemesNode.GetChildren())
			child.Free();
	}
	
	#region Signal connections

	private void _on_next_button_pressed()
	{
		++CardI;
	}

	private void _on_add_to_ongoing_button_pressed()
	{
		var tex = CardNode.Texture;
		var child = OngoingSchemePS.Instantiate() as OngoingScheme;
		OngoingSchemesNode.AddChild(child);
		child.Texture = tex;
	}

	private void _on_close_requested()
	{
		Hide();
	}
 
	#endregion
}


