using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class EDHCard : Control
{
	#region Nodes
	
	public TextureRect ImageTextureNode { get; private set; }
	public Label NameLabelNode { get; private set; }
	public Label SynergyLabelNode { get; private set; }
	public HttpRequest ImageRequestNode { get; private set; }
	
	#endregion
	
	public ShortCard Card { get; private set; }
	private List<Card> _variations;
	public EDHDataCard Data {
		get;
		private set;
	}
	
	public override void _Ready()
	{
		#region Node fetching

		ImageTextureNode = GetNode<TextureRect>("%ImageTexture");
		NameLabelNode = GetNode<Label>("%NameLabel");
		SynergyLabelNode = GetNode<Label>("%SynergyLabel");
		ImageRequestNode = GetNode<HttpRequest>("%ImageRequest");
				
		#endregion
	}
	
	public void Load(ShortCard card, EDHDataCard edhCard) {
		Card = card;
		_variations = card.GetVariations();
		Data = edhCard;
		
		NameLabelNode.Text = Card.Name;
		
		var color = new Color(1, 0, 0);
		var st = Data.Synergy.ToString();
		if (Data.Synergy > 0) {
			st = "+" + st;
			color = new Color(0, 1, 0);
		}
		if (Data.Synergy == 0) {
			color = new Color(.5f, .5f, .5f);
		}
		SynergyLabelNode.Set("theme_override_colors/font_color", color);
		SynergyLabelNode.Text = st;

		ImageRequestNode.Request(_variations[0].ImageURIs["normal"]);
	}

	private void SetTexture(Texture2D tex) {
		ImageTextureNode.Texture = tex;
	}

	#region Signal connections

	private void _on_image_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		if (response_code != 200) {
			GUtil.Alert(this, "Failed to load image for card " + Data.Name);
			return;
		}
		Task.Run(() => {
			var image = new Image();
			image.LoadJpgFromBuffer(body);
			var tex = ImageTexture.CreateFromImage(image);
			CallDeferred("SetTexture", tex);
		});
	}

	#endregion
}

