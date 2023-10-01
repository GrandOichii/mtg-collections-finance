using Godot;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public partial class CollectionsTab : TabBar
{
	private static readonly string COLLECTIONS_DIR = "Collections";
	private static List<CardLineParser> PARSERS = new(){
		new SimpleLineParser(),
		new DeckCardLineParser(),
		new XmageLineParser()
	};

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
	public FileDialog ImportDialogNode { get; private set; }
	public OptionButton DefaultPriceOptionNode { get; private set; }
	public Label TotalPriceLabelNode { get; private set; }
	public ConfirmationDialog RemoveCollectionDialogNode { get; private set; }
	public ConfirmationDialog RemoveCardDialogNode { get; private set; }
	public Control SortButtonsContainerNode { get; private set; }
	public CardViewWindow EditPrintingWindowNode { get; private set; }
	public Button CheapestPrintingsButtonNode { get; private set; }
	
	#endregion
	
	private Collection _current = null;
	private CCard _dueForRemoval = null;
	
	private Dictionary<string, ShortCard> _cardOIDIndex = new();
	private Dictionary<string, ShortCard> _cardNameIndex = new();

	private CollectionCard _edited;
	
	public override void _Ready()
	{
		#region Node fetching
		
		CollectionsListNode = GetNode<ItemList>("%CollectionsList");
		CardsContainerNode = GetNode<FlowContainer>("%CardsContainer");
		AddCardToCollectionWindowNode = GetNode<Window>("%AddCardToCollectionWindow");
		CardsListNode = GetNode<CardsList>("%CardsList");
		AddCollectionWindowNode = GetNode<Window>("%AddCollectionWindow");
		NewCollectionNameEditNode = GetNode<LineEdit>("%NewCollectionNameEdit");
		ImportDialogNode = GetNode<FileDialog>("%ImportDialog");
		DefaultPriceOptionNode = GetNode<OptionButton>("%DefaultPriceOption");
		TotalPriceLabelNode = GetNode<Label>("%TotalPriceLabel");
		RemoveCollectionDialogNode = GetNode<ConfirmationDialog>("%RemoveCollectionDialog");
		RemoveCardDialogNode = GetNode<ConfirmationDialog>("%RemoveCardDialog");
		SortButtonsContainerNode = GetNode<Control>("%SortButtonsContainer");
		EditPrintingWindowNode = GetNode<CardViewWindow>("%EditPrintingWindow");
		CheapestPrintingsButtonNode = GetNode<Button>("%CheapestPrintingsButton");
		
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
		Wrapper<ShortCard>? cardW = null;
//		GD.Print(_cardOIDIndex.Count);
		if (_cardOIDIndex.ContainsKey(card.OracleId))
			cardW = new Wrapper<ShortCard>(_cardOIDIndex[card.OracleId]);
		var priceI = DefaultPriceOptionNode.GetSelectedId();
		var price = DefaultPriceOptionNode.GetItemText(priceI);
		result.Load(card, cardW, price);
		result.AmountSpinNode.ValueChanged += (v) => UpdateTotalPrice();
		result.RemoveRequested += _on_card_remove_requested;
		result.PrintingButtonNode.Pressed += () => EditPrinting(result);
		return result;
	}

	private void EditPrinting(CollectionCard child) {
		_edited = child;
		EditPrintingWindowNode.Load(new(child.CData), child.Variant);
		EditPrintingWindowNode.Show();
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
	
	private void Import(string text) {
		var lines = text.Replace("\r\n", "\n").Split("\n");
		List<string> failed = new();
		var collection = new Collection();
		collection.Cards = new();
		foreach (var line in lines) {
			CCard? card = null;
			foreach (var parser in PARSERS) {
				card = parser.Do(line, _cardNameIndex);
				if (card is not null) break;
			}
			if (card is null) {
				failed.Add(line);
				GD.Print("FAILED TO PARSE: " + line);
				continue;
			}
			bool added = false;
			foreach (var c in collection.Cards) {
				if (c.OracleId == card.OracleId && c.Printing == card.Printing) {
					added = true;
					c.Amount += card.Amount;
					break;
				}
			}
			if (!added)
				collection.Cards.Add(card);
		}

		collection.Name = NewCollectionNameEditNode.Text;
//		collection.Cards = new();
		AddToCollectionList(collection);
		AddCollectionWindowNode.Hide();
		_on_collections_list_item_activated(CollectionsListNode.ItemCount - 1);
	}
	
	public void UpdateTotalPrice() {
		double result = 0;
		foreach (var child in CardsContainerNode.GetChildren()) {
			switch (child) {
			case CollectionCard card:
				result += card.TotalPrice;
				continue;
			default:
				continue;
			}
		}
		TotalPriceLabelNode.Text = "Total price: " + Math.Round(result, 2);
	}

	#region Signal connections
	
	private void _on_collections_list_item_activated(int index)
	{
		var item = CollectionsListNode.GetItemMetadata(index).As<Wrapper<Collection>>();
		var collection = item.Value;
		_current = collection;
		SortButtonsContainerNode.Visible = true;
		CheapestPrintingsButtonNode.Visible = true;
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
		
		UpdateTotalPrice();
	}
	
	private void _on_cards_card_added(Wrapper<ShortCard> cardW, bool update)
	{
		_cardOIDIndex.Add(cardW.Value.OracleId, cardW.Value);
		if (!_cardNameIndex.ContainsKey(cardW.Value.Name))
			_cardNameIndex.Add(cardW.Value.Name, cardW.Value);
		CardsListNode.AddItem(cardW);
	}
	
	private void _on_add_card_to_collection_button_pressed() {
		CardsListNode.ResetFilter();
		AddCardToCollectionWindowNode.Show();
	}

	private void _on_cards_list_card_activated(Wrapper<ShortCard> cardW)
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
		UpdateTotalPrice();
	}

	private void _on_add_card_to_collection_window_close_requested()
	{
		AddCardToCollectionWindowNode.Hide();
	}

	private void _on_new_button_pressed()
	{
		NewCollectionNameEditNode.Text = "";
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
		var cc = CanCreate();
		if (!cc) return;
		
		ImportDialogNode.Show();
	}

	private void _on_clipboard_import_button_pressed()
	{
		var cc = CanCreate();
		if (!cc) return;
		
		Import(DisplayServer.ClipboardGet());
	}

	private void _on_add_collection_window_close_requested()
	{
		AddCollectionWindowNode.Hide();
	}

	private void _on_import_dialog_file_selected(string path)
	{
		var text = File.ReadAllText(path);
		Import(text);
	}
	
	private void _on_default_price_option_item_selected(int index)
	{
		// Replace with function body.
		var price = DefaultPriceOptionNode.GetItemText(index);
		foreach (var child in CardsContainerNode.GetChildren()) {
			switch (child) {
			case CollectionCard card: 
				card.UpdatePrice(price);
				continue;
			default:
				continue;
			}
		}
		
		UpdateTotalPrice();
	}

	private void _on_remove_button_pressed()
	{
		var indicies = CollectionsListNode.GetSelectedItems();
		if (indicies.Length != 1) {
			GUtil.Alert(this, "Select one collection for removal!");
			return;
		}
		var index = indicies[0];
		var name = CollectionsListNode.GetItemText(index);
		RemoveCollectionDialogNode.DialogText = "Remove collection " + name + "?";
		RemoveCollectionDialogNode.Show();
	}

	private void _on_remove_collection_dialog_confirmed()
	{
		var index = CollectionsListNode.GetSelectedItems()[0];
		CollectionsListNode.RemoveItem(index);
		_current = null;
		SortButtonsContainerNode.Visible = false;
		CheapestPrintingsButtonNode.Visible = false;
		
		RemoveCards();
		TotalPriceLabelNode.Text = "";
	}

	private void _on_price_sort_button_pressed()
	{
		var children = CardsContainerNode.GetChildren();
		var button = children[children.Count - 1];
		var sorted = children.OrderBy(child => {
			switch(child) {
			case CollectionCard card:
				return card.TotalPrice;
			default:
				return 0;
			}
		}).ToList();
		foreach (var child in sorted)
			CardsContainerNode.MoveChild(child, 0);
		CardsContainerNode.MoveChild(button, children.Count - 1);
	}

	private void _on_alphabetical_sort_button_pressed()
	{
		var children = CardsContainerNode.GetChildren();
		var button = children[children.Count - 1];
		var sorted = children.OrderBy(child => {
			switch(child) {
			case CollectionCard card:
				return card.CData.Name;
			default:
				return "";
			}
		}).Reverse().ToList();
		foreach (var child in sorted)
			CardsContainerNode.MoveChild(child, 0);
		CardsContainerNode.MoveChild(button, children.Count - 1);
	}

	private void _on_amount_sort_button_pressed()
	{
			var children = CardsContainerNode.GetChildren();
		var button = children[children.Count - 1];
		var sorted = children.OrderBy(child => {
			switch(child) {
			case CollectionCard card:
				return card.AmountSpinNode.Value;
			default:
				return 0;
			}
		}).ToList();
		foreach (var child in sorted)
			CardsContainerNode.MoveChild(child, 0);
		CardsContainerNode.MoveChild(button, children.Count - 1);
	}
	
	private void _on_card_remove_requested(Wrapper<CCard> cCardW, Wrapper<ShortCard> cardW) {
		_dueForRemoval = cCardW.Value;
		RemoveCardDialogNode.DialogText = "Remove card " + cardW.Value.Name + " from collection " + _current.Name + "?";
		RemoveCardDialogNode.Show();
	}

	private void _on_remove_card_dialog_confirmed()
	{
		// remove from collection
//		bool removed = false;
//		foreach (var card in _current.Cards) {
//			if (card == _dueForRemoval) {
//				_current
//			}
//		}
//		if (!removed) {
//			throw new Exception("Failed to remove card with oracle id " + _dueForRemoval.OracleId + " from collection " + _current.Name);
//		}
		_current.Cards.Remove(_dueForRemoval);
		
		// remove from list
		foreach (var child in CardsContainerNode.GetChildren()) {
			switch (child) {
			case CollectionCard card:
				if (card.Data == _dueForRemoval) {
					card.Free();
					UpdateTotalPrice();
					return;
				}
				continue;
			default:
				continue;
			}
		}
	}

	private void _on_edit_printing_window_close_requested()
	{
		_edited.Variant = EditPrintingWindowNode.Variant;
		UpdateTotalPrice();
	}

	private void _on_cheapest_printings_button_pressed()
	{
		foreach (var child in CardsContainerNode.GetChildren()) {
			switch (child) {
			case CollectionCard cardNode:
				Card cheapest = null;
				string cheapestPriceType = "";
				double cheapestPrice = 0;
				foreach (var v in cardNode.Variations) {
					foreach (var pair in v.Prices) {
						if (pair.Value is null) continue;
						if (pair.Key == "tix") continue;
						if (!pair.Key.Contains(DefaultPriceOptionNode.Text)) continue;
						var p = double.Parse(pair.Value);
						if (cheapest is null || (p < cheapestPrice)) {
							cheapest = v;
							cheapestPrice = p;
							cheapestPriceType = pair.Key;
						}
					}
				}
				cardNode.Variant = cheapest;
				cardNode.UpdatePrice(cheapestPriceType);
				break;
			default:
				break;
			}
		}
		UpdateTotalPrice();
	}
	
	#endregion
}


