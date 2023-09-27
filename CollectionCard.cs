using Godot;
using System;

public partial class CollectionCard : Control
{
	#region Nodes
	
	public TextureRect ImageTextureNode { get; private set; } 
	public Label NameLabelNode { get; private set; }
	public SpinBox AmountSpinNode { get; private set; }
	public HttpRequest ImageRequestNode { get; private set; }
	public ColorRect TransparencyRectNode { get; private set; }
	
	#endregion
	
	public CCard Data { get; private set; }
	
	private Texture2D _defaultTex;

	public override void _Ready()
	{
		#region Node fetching
		
		NameLabelNode = GetNode<Label>("%NameLabel");
		ImageTextureNode = GetNode<TextureRect>("%ImageTexture");
		AmountSpinNode = GetNode<SpinBox>("%AmountSpin");
		ImageRequestNode = GetNode<HttpRequest>("%ImageRequest");
		TransparencyRectNode = GetNode<ColorRect>("%TransparencyRect");
		
		#endregion
		
		_defaultTex = ImageTextureNode.Texture;
	}
	
	public void Load(CCard cCard, Wrapper<MTGCard>? cardW) {
		Data = cCard;
		ImageTextureNode.Texture = _defaultTex;
		AmountSpinNode.Value = cCard.Amount;
		
		if (cardW is null) {
			NameLabelNode.Text = cCard.OracleId;
			return;
		}
		
		NameLabelNode.Text = cardW.Value.Name;
		ImageRequestNode.Request(cardW.Value.ImageURIs["normal"]);
	}
	
	#region Signal connections
	
	private void _on_image_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		var image = new Image();
		image.LoadJpgFromBuffer(body);
		var tex = ImageTexture.CreateFromImage(image);
		ImageTextureNode.Texture = tex;
	}
	
	private void _on_amount_spin_value_changed(double v)
	{
		float target = 0f;
		if (v == 0) 
			target = 0.5f;
		var c = TransparencyRectNode.Color;
		CreateTween().TweenProperty(TransparencyRectNode, "color", new Color(c.R, c.G, c.B, target), .1f);
	}
	
	#endregion
}





