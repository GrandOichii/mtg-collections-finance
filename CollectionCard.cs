using Godot;
using System;
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
	
	#endregion

	#region Signals
	
	[Signal]
	public delegate void RemoveRequestedEventHandler(Wrapper<CCard> cCardW, Wrapper<ShortCard> cardW);
	
	#endregion
	
	public CCard Data { get; private set; }
	public ShortCard CData { get; private set; }
	
	private Texture2D _defaultTex;

	public override void _Ready()
	{
		#region Node fetching
		
		NameLabelNode = GetNode<Label>("%NameLabel");
		ImageTextureNode = GetNode<TextureRect>("%ImageTexture");
		AmountSpinNode = GetNode<SpinBox>("%AmountSpin");
		ImageRequestNode = GetNode<HttpRequest>("%ImageRequest");
		TransparencyRectNode = GetNode<ColorRect>("%TransparencyRect");
		PriceLabelNode = GetNode<RichTextLabel>("%PriceLabel");
		
		#endregion
		
		_defaultTex = ImageTextureNode.Texture;
	}
	
	private string _priceType;
	
	public void Load(CCard cCard, Wrapper<ShortCard>? cardW, string priceType) {
		_priceType = priceType;
		Data = cCard;
		CData = cardW.Value; 
		ImageTextureNode.Texture = _defaultTex;
		AmountSpinNode.Value = cCard.Amount;
		
		if (cardW is null) {
			NameLabelNode.Text = cCard.OracleId;
			return;
		}
		
		NameLabelNode.Text = cardW.Value.Name;
		_on_amount_spin_value_changed(cCard.Amount);
		ImageRequestNode.Request(cardW.Value.ImageURIs["normal"]);
	}
	
	public void UpdatePrice(string newPriceType="") {
		if (newPriceType == "")
			newPriceType = _priceType;
		_priceType = newPriceType;
		
		PriceLabelNode.Clear();
		if (CData.Prices[_priceType] is null) {
			PriceLabelNode.AppendText("-");
			return;
		}
		var singlePrice = double.Parse(CData.Prices[_priceType]);
		var singlePriceS = PriceUtil.GetColoredText(singlePrice, _priceType); 
		var amount = AmountSpinNode.Value;
		var fullPrice = TotalPrice;
		var fullPriceS = "[color=yellow]" + fullPrice + "[/color]";
		PriceLabelNode.AppendText("" + singlePriceS + " x " + amount + " = " + fullPriceS);
	}

	public double TotalPrice => (CData.Prices[_priceType] is not null ? Math.Round(double.Parse(CData.Prices[_priceType]) * AmountSpinNode.Value, 2) : 0);
	
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






