using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

public partial class CollectionCard : Control
{
	#region Nodes
	
	public TextureRect ImageTextureNode { get; private set; } 
	public Label NameLabelNode { get; private set; }
	public SpinBox AmountSpinNode { get; private set; }
	public HttpRequest ImageRequestNode { get; private set; }
	public ColorRect TransparencyRectNode { get; private set; }
	public RichTextLabel PriceLabelNode { get; private set; }
	public Button PrintingButtonNode { get; private set; }
	
	#endregion

	#region Signals
	
	[Signal]
	public delegate void RemoveRequestedEventHandler(Wrapper<CCard> cCardW, Wrapper<ShortCard> cardW);
	
	#endregion
	
	public CCard Data { get; private set; }
	public ShortCard CData { get; private set; }
	
	private Texture2D _defaultTex;
	private string _priceType;
	
	public List<Card> Variations { get; private set; }
	private Card? _variant;
	public Card? Variant {
		get => _variant;
		set {
			_variant = value;
			UpdateVariantData();
		}
	}

	public override void _Ready()
	{
		#region Node fetching
		
		NameLabelNode = GetNode<Label>("%NameLabel");
		ImageTextureNode = GetNode<TextureRect>("%ImageTexture");
		AmountSpinNode = GetNode<SpinBox>("%AmountSpin");
		ImageRequestNode = GetNode<HttpRequest>("%ImageRequest");
		TransparencyRectNode = GetNode<ColorRect>("%TransparencyRect");
		PriceLabelNode = GetNode<RichTextLabel>("%PriceLabel");
		PrintingButtonNode = GetNode<Button>("%PrintingButton");
		
		#endregion
		
		_defaultTex = ImageTextureNode.Texture;
	}
	
	private void UpdateVariantData() {
		PrintingButtonNode.Text = "???";
		if (_variant is null) return;
		PrintingButtonNode.Text = _variant.UID;
		Data.Printing = _variant.ID;

		ImageRequestNode.Request(_variant.ImageURIs["normal"]);
		UpdatePrice();
	}
	
	public void Load(CCard cCard, Wrapper<ShortCard>? cardW, string priceType) {
		ImageTextureNode.Texture = _defaultTex;
		_priceType = priceType;
		Data = cCard;
		CData = cardW.Value;
		
		Variations = CData.GetVariations();
		NameLabelNode.Text = cardW.Value.Name;
		
		if (cardW is null) {
			NameLabelNode.Text = cCard.OracleId;
			return;
		}
		_variant = Variations[0];
		if (cCard.Printing.Length > 0) {
			_variant = null;
			foreach (var v in Variations) {
				if (v.ID == cCard.Printing) {
					_variant = v;
					break;
				}
			}
		}
		if (_variant is null) {
			GUtil.Alert(this, "Failed to find printing ID " + cCard.Printing + " for card " + CData.Name);
//			return;
		}
		UpdateVariantData();
		AmountSpinNode.Value = cCard.Amount;		
		_on_amount_spin_value_changed(cCard.Amount);
	}
	
	public void UpdatePrice(string newPriceType="") {
		if (newPriceType == "")
			newPriceType = _priceType;
		_priceType = newPriceType;
		
		PriceLabelNode.Clear();
		// TODO check for null
		if (_variant.Prices[_priceType] is null) {
			PriceLabelNode.AppendText("-");
			return;
		}
		var singlePrice = double.Parse(_variant.Prices[_priceType], CultureInfo.InvariantCulture);
		var singlePriceS = PriceUtil.GetColoredText(singlePrice, _priceType); 
		var amount = AmountSpinNode.Value;
		var fullPrice = TotalPrice;
		var fullPriceS = "[color=yellow]" + fullPrice + "[/color]";
		PriceLabelNode.AppendText("" + singlePriceS + " x " + amount + " = " + fullPriceS + " (" + _priceType + ")");
	}

	// TODO check for null
	public double TotalPrice => (_variant.Prices[_priceType] is not null ? Math.Round(double.Parse(_variant.Prices[_priceType], CultureInfo.InvariantCulture) * AmountSpinNode.Value, 2) : 0);
	
	private void SetTexture(Texture2D tex) {
		ImageTextureNode.Texture = tex;

	}

	#region Signal connections
	
	private void _on_image_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		if (response_code != 200) {
			// 
			return;
		}
		Task.Run(() => {
			var image = new Image();
			image.LoadJpgFromBuffer(body);
			var tex = ImageTexture.CreateFromImage(image);
			CallDeferred("SetTexture", tex);
		});
	}
	
	private void _on_amount_spin_value_changed(double v)
	{
		Data.Amount = (int)v;
		float target = 0f;
		if (v == 0) 
			target = 0.5f;
		var c = TransparencyRectNode.Color;
		UpdatePrice();
		CreateTween().TweenProperty(TransparencyRectNode, "color", new Color(c.R, c.G, c.B, target), .1f);
	}

	private void _on_remove_button_pressed()
	{
		EmitSignal(SignalName.RemoveRequested, new Wrapper<CCard>(Data), new Wrapper<ShortCard>(CData));
	}
	
	#endregion
}






