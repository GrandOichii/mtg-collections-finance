using Godot;
using System;

public partial class CardInDeck : VBoxContainer
{
	#region Nodes
	
	public TextureRect ImageTextureNode { get; private set; } 
	public SpinBox AmountSpinNode { get; private set; }
	
	#endregion

	public override void _Ready()
	{
		#region Node fetching
		
		ImageTextureNode = GetNode<TextureRect>("%ImageTexture");
		AmountSpinNode = GetNode<SpinBox>("%AmountSpin");
		
		#endregion
	}
	
	public void Load(Wrapper<MTGCard> card) {
		
	}
}
