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
	public Window AddCardToCollectionWindowNode { get; private set; }
	public CardsList CardsListNode { get; private set; }
	public Window AddCollectionWindowNode { get; private set; }
	public LineEdit NewCollectionNameEditNode { get; private set; }
	
	#endregion
	
	private Collection _current = null;
	
	private Dictionary<string, MTGCard> _cardIndex = new();
	
	public override void _Ready()
	{
		#region Node fetching
		
		CollectionsListNode = GetNode<ItemList>("%CollectionsList");
		CardsContainerNode = GetNode<FlowContainer>("%CardsContainer");
		AddCardToCollectionWindowNode = GetNode<Window>("%AddCardToCollectionWindow");
		CardsListNode = GetNode<CardsList>("%CardsList");
		AddCollectionWindowNode = GetNode<Window>("%AddCollectionWindow");
		NewCollectionNameEditNode = GetNode<LineEdit>("%NewCollectionNameEdit");
		
		#endregion
		
		LoadCollections();
	}
	
	public void SaveCollections() {
		var dir = COLLECTIONS_DIR;
		
		// delete directory if already exists
		if (Directory.Exists(dir)) {
			Directory.Delete(dir, true);
		}

		// create directory
		Directory.CreateDirectory(dir);
		
		for (int i = 0; i < CollectionsListNode.ItemCount; i++) {
			var collection = CollectionsListNode.GetItemMetadata(i).As<Wrapper<Collection>>().Value;
			var fName = collection.Name + ".json";
			var text = collection.ToJson();
			File.WriteAllText(Path.Combine(dir, fName), text);
		}
	}

	private void LoadCollections() {
		var files = Directory.GetFiles(COLLECTIONS_DIR);
		foreach (var file in files) {
			var collection = Collection.FromJson(File.ReadAllText(file));
			AddToCollectionList(collection);
		}
	}

	private void AddToCollectionList(Collection c) {
		var index = CollectionsListNode.AddItem(c.Name);
		CollectionsListNode.SetItemMetadata(index, new Wrapper<Collection>(c));
	}

	private void RemoveCards() {
		foreach (var child in CardsContainerNode.GetChildren())
			child.Free();
	}
	
	private CollectionCard AddCardToCollection(CCard card) {
		var result = CollectionCardPS.Instantiate() as CollectionCard;
		CardsContainerNode.AddChild(result);
		Wrapper<MTGCard>? cardW = null;
		if (_cardIndex.ContainsKey(card.OracleId))
			cardW = new Wrapper<MTGCard>(_cardIndex[card.OracleId]);
		result.Load(card, cardW);
		return result;
	}
	
	private bool CanCreate() {
		var t = NewCollectionNameEditNode.Text;
		if (t.Length == 0) {
			GUtil.Alert(this, "Enter collection name");
			return false;
		}
		
		for (int i = 0; i < CollectionsListNode.ItemCount; i++) {
			var name = CollectionsListNode.GetItemText(i);
			if (name == t) {
				GUtil.Alert(this, "Collections with name " + name + " already exists");
				return false;
			}
		}
		
		return true;
	}

	#region Signal connections
	
	private void _on_collections_list_item_activated(int index)
	{
		var item = CollectionsListNode.GetItemMetadata(index).As<Wrapper<Collection>>();
		var collection = item.Value;
		_current = collection;
		RemoveCards();
		// TODO bad
		// don't know any other way :)
		Vector2 size = (CollectionCardPS.Instantiate() as CollectionCard).CustomMinimumSize;
		foreach (var card in collection.Cards) {
			AddCardToCollection(card);
		}
		// Add button to add new cards
		var bChild = AddCardToCollectionButtonPS.Instantiate() as Button;
		CardsContainerNode.AddChild(bChild);
		bChild.CustomMinimumSize = size;
		bChild.Pressed += _on_add_card_to_collection_button_pressed;
	}
	
	private void _on_cards_card_added(Wrapper<MTGCard> cardW, bool update)
	{
		_cardIndex.Add(cardW.Value.OracleId, cardW.Value);
		CardsListNode.AddItem(cardW);
	}
	
	private void _on_add_card_to_collection_button_pressed() {
		CardsListNode.ResetFilter();
		AddCardToCollectionWindowNode.Show();
	}

	private void _on_cards_list_card_activated(Wrapper<MTGCard> cardW)
	{
		foreach (var child in CardsContainerNode.GetChildren()) {
			switch (child) {
			case CollectionCard cCardNode:
				if (cCardNode.Data.OracleId != cardW.Value.OracleId) continue;
				
				cCardNode.Data.Amount += 1;
				cCardNode.AmountSpinNode.Value += 1;
				AddCardToCollectionWindowNode.Hide();
				return;
			default:
				break;
			}
		}
		
		var c = new CCard();
		c.OracleId = cardW.Value.OracleId;
		c.Amount = 1;
		var last = CardsContainerNode.GetChild(CardsContainerNode.GetChildCount()-1);
		AddCardToCollection(c);
		CardsContainerNode.MoveChild(last, CardsContainerNode.GetChildCount() - 1);
		AddCardToCollectionWindowNode.Hide();
		
		_current.Cards.Add(c);
	}

	private void _on_add_card_to_collection_window_close_requested()
	{
		AddCardToCollectionWindowNode.Hide();
	}

	private void _on_new_button_pressed()
	{
		AddCollectionWindowNode.Show();
	}
	
	private void _on_empty_button_pressed()
	{
		var cc = CanCreate();
		if (!cc) return;
		
		var collection = new Collection();
		collection.Name = NewCollectionNameEditNode.Text;
		collection.Cards = new();
		AddToCollectionList(collection);
		AddCollectionWindowNode.Hide();
		_on_collections_list_item_activated(CollectionsListNode.ItemCount - 1);
	}

	private void _on_file_import_button_pressed()
	{
		// TODO
	}

	private void _on_clipboard_import_button_pressed()
	{
		// TODO
	}

	private void _on_add_collection_window_close_requested()
	{
		AddCollectionWindowNode.Hide();
	}
	
	#endregion
}



