using Godot;
using System;
using System.IO;
using System.Collections.Generic;

public partial class CollectionsTab : TabBar
{
	private static readonly string COLLECTIONS_DIR = "Collections";

	#region Packed scenes
	
	static readonly PackedScene CollectionCardPS = ResourceLoader.Load("res://CollectionCard.tscn") as PackedScene;
	static readonly PackedScene AddCardToCollectionButtonPS = ResourceLoader.Load("res://AddCardToCollectionButton.tscn") as PackedScene;

	#endregion

	#region Nodes
	
	public ItemList CollectionsListNode { get; private set; }
	public FlowContainer CardsContainerNode { get; private set; }
	
	#endregion
	
	private Dictionary<string, MTGCard> _cardIndex = new();
	
	public override void _Ready()
	{
		#region Node fetching
		
		CollectionsListNode = GetNode<ItemList>("%CollectionsList");
		CardsContainerNode = GetNode<FlowContainer>("%CardsContainer");
		
		#endregion

		// load all the collections
		LoadCollections();
	}

	private void LoadCollections() {
		var files = Directory.GetFiles(COLLECTIONS_DIR);
		foreach (var file in files) {
			var collection = Collection.FromJson(File.ReadAllText(file));
			var index = CollectionsListNode.AddItem(collection.Name);
			CollectionsListNode.SetItemMetadata(index, new Wrapper<Collection>(collection));
		}
	}

	#region Signal connections
	
	private void _on_collections_list_item_activated(int index)
	{
		var item = CollectionsListNode.GetItemMetadata(index).As<Wrapper<Collection>>();
		var collection = item.Value;
		RemoveCards();
		Vector2 size = (CollectionCardPS.Instantiate() as CollectionCard).CustomMinimumSize;
		foreach (var card in collection.Cards) {
			var child = CollectionCardPS.Instantiate() as CollectionCard;
			CardsContainerNode.AddChild(child);
			Wrapper<MTGCard>? cardW = null;
			if (_cardIndex.ContainsKey(card.OracleId))
				cardW = new Wrapper<MTGCard>(_cardIndex[card.OracleId]);
			child.Load(card, cardW);
		}
		// Add button to add new cards
		var bChild = AddCardToCollectionButtonPS.Instantiate() as Button;
		CardsContainerNode.AddChild(bChild);
		bChild.CustomMinimumSize = size;
		bChild.Pressed += _on_add_card_to_collection_button_pressed;
	}
	
	private void RemoveCards() {
		foreach (var child in CardsContainerNode.GetChildren())
			child.Free();
	}
	
	private void _on_cards_card_added(Wrapper<MTGCard> cardW, bool update)
	{
		_cardIndex.Add(cardW.Value.OracleId, cardW.Value);
	}
	
	private void _on_add_card_to_collection_button_pressed() {
		GD.Print("Amogus");
	}
	
	#endregion
}




