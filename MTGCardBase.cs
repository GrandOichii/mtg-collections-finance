using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


public partial class MTGCardBase : TextureRect
{
	#region Nodes
	
	public TextureButton SwitchButtonNode { get; private set; }
	public HttpRequest RequestNode { get; private set; }
	
	#endregion
	
	private Texture2D _default;

	private List<Texture2D> _textures;
	private Card _data;
	private int _textureI;
	public int TextureI {
		get => _textureI;
		set {
			_textureI = value;
			var len = _textures.Count;
			if (_textureI < 0)
				_textureI = len - 1;
			if (_textureI >= len)
				_textureI = 0;
			Texture = _textures[_textureI];
		}
	}
	
	public override void _Ready()
	{
		#region Node fetching
		
		SwitchButtonNode = GetNode<TextureButton>("%SwitchButton");
		RequestNode = GetNode<HttpRequest>("%Request");
		
		#endregion
		
		_default = Texture;
	}

	public void Load(Card card) {
		RequestNode.CancelRequest();
		_textures = new();
		_data = card;
		SwitchButtonNode.Visible = false;
		if (card.Faces is not null) {
			if (card.Faces[0].ImageURIs is not null) {
				SwitchButtonNode.Visible = true;
				RequestNode.Request(card.Faces[0].ImageURIs["normal"]);
				return;
			}
		}
		
		RequestNode.Request(card.ImageURIs["normal"]);

	}
	
	private void AddTexture(Texture2D tex) {
		_textures.Add(tex);
		var count = _textures.Count;
		if (count == 1) {
			TextureI = 0;
		}
		if (_data.Faces is null) return;

		
		if (count == _data.Faces.Count) return;
		if (_data.Faces[count].ImageURIs is null) return;

		RequestNode.Request(_data.Faces[count].ImageURIs["normal"]);
	}
	
	#region Signal connections
	
	private void _on_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		if (response_code != 200) {
			return;
		}
		
		Task.Run(() => {
			var image = new Image();
			image.LoadJpgFromBuffer(body);
			var tex = ImageTexture.CreateFromImage(image);
			CallDeferred("AddTexture", tex);
		});
	}

	private void _on_switch_button_pressed()
	{
		++TextureI;
		// Replace with function body.
	}
	
	#endregion
}



