//
//  Copyright (c) 2016 MatchboxMobile
//  Licensed under The MIT License (MIT)
//  http://opensource.org/licenses/MIT
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
//  TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
//  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
//  CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
//  IN THE SOFTWARE.
//
using System;
using Xamarin.Forms;
using System.Collections.Generic;

namespace cards
{
	public class CardStackView : ContentView
	{
		public class Item
		{
			public string Name { get; set;}
			public string Photo { get; set;}
			public string Location { get; set;}
			public string Description { get; set;}
		};
	
	
		// back card scale
		const float BackCardScale = 0.8f;
		// speed of the animations
		const int AnimLength = 250;	
			// 180 / pi
		const float DegreesToRadians = 57.2957795f; 
		// higher the number less the rotation effect
		const float CardRotationAdjuster = 0.3f; 
		// distance a card must be moved to consider to be swiped off
		public int CardMoveDistance {get; set;}

		// two cards
		const int NumCards = 2;
		CardView[] cards = new CardView[NumCards];
		// the card at the top of the stack
		int topCardIndex;
		// distance the card has been moved
		float cardDistance = 0;
		// the last items index added to the stack of the cards
		int itemIndex = 0;
		bool ignoreTouch = false;
		
		// called when a card is swiped left/right with the card index in the ItemSource
		public Action<int> SwipedRight = null;
		public Action<int> SwipedLeft = null;

		public static readonly BindableProperty ItemsSourceProperty =
			BindableProperty.Create(nameof(ItemsSource), typeof(System.Collections.IList), typeof(CardStackView), null,
    		propertyChanged: OnItemsSourcePropertyChanged);
    
		public List<Item> ItemsSource {
			get {
				return (List<Item>)GetValue (ItemsSourceProperty);
			}
			set {
				SetValue (ItemsSourceProperty, value);
				itemIndex = 0;			
			}
		}
		
		private static void OnItemsSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			((CardStackView)bindable).Setup();
		}

		public CardStackView ()
		{			
			RelativeLayout view = new RelativeLayout ();

			// create a stack of cards
			for (int i = 0; i < NumCards; i++) {
				var card = new CardView();
				cards[i] = card;
				card.InputTransparent = true;
				card.IsVisible = false;

				view.Children.Add(
					card,
					Constraint.Constant(0),
					Constraint.Constant(0),
					Constraint.RelativeToParent((parent) =>
					{
						return parent.Width;
					}),
					Constraint.RelativeToParent((parent) =>
					{
						return parent.Height;
					})
				);
			}

			this.BackgroundColor = Color.Black;
			this.Content = view;
			
			var panGesture = new PanGestureRecognizer ();
			panGesture.PanUpdated += OnPanUpdated;
			GestureRecognizers.Add (panGesture);

		}
		
		void Setup()
		{
			// set the top card
			topCardIndex = 0;
			// create a stack of cards
			for (int i = 0; i < Math.Min(NumCards, ItemsSource.Count); i++)	{
				if (itemIndex >= ItemsSource.Count) break;
				var card = cards[i];
				card.Name.Text = ItemsSource[itemIndex].Name;
				card.Location.Text = ItemsSource[itemIndex].Location;
				card.Description.Text = ItemsSource[itemIndex].Description;
				card.Photo.Source = ImageSource.FromFile(ItemsSource[itemIndex].Photo);
				card.IsVisible = true;
				card.Scale = GetScale(i);
				card.RotateTo (0, 0);
				card.TranslateTo (0, - card.Y, 0);
				((RelativeLayout)this.Content).LowerChild (card);
				itemIndex++;
			}
		}
		
		void OnPanUpdated (object sender, PanUpdatedEventArgs e)
		{
			switch (e.StatusType) {
				case GestureStatus.Started:
					HandleTouchStart();
					break;
				case GestureStatus.Running:
					HandleTouch((float)e.TotalX);	
	                break;
				case GestureStatus.Completed:	
					HandleTouchEnd();
					break;
			}
		}	

		// to hande when a touch event begins
		public void HandleTouchStart() 
		{			
			cardDistance = 0;
		}

		// to handle te ongoing touch event as the card is moved
		public void HandleTouch(float diff_x) 
		{				
			if (ignoreTouch) {
				return;
			}

			var topCard = cards [topCardIndex];
			var backCard = cards [PrevCardIndex (topCardIndex)];

			// move the top card
			if (topCard.IsVisible) {

				// move the card
				topCard.TranslationX = (diff_x);

				// calculate a angle for the card
				float rotationAngel = (float)(CardRotationAdjuster * Math.Min (diff_x / this.Width, 1.0f));
				topCard.Rotation = rotationAngel * DegreesToRadians;

				// keep a record of how far its moved
				cardDistance = diff_x;
			}

			// scale the backcard
			if (backCard.IsVisible) {
				backCard.Scale = Math.Min (BackCardScale + Math.Abs ((cardDistance / CardMoveDistance) * (1.0f - BackCardScale)), 1.0f);
			}			
		}

		// to handle the end of the touch event
		async public void HandleTouchEnd()
		{			
			ignoreTouch = true;

			var topCard = cards [topCardIndex];

			// if the card was move enough to be considered swiped off
			if (Math.Abs ((int)cardDistance) > CardMoveDistance) {

				// move off the screen
				await topCard.TranslateTo (cardDistance>0?this.Width:-this.Width, 0, AnimLength/2, Easing.SpringOut);
				topCard.IsVisible = false; 
				
				if (SwipedRight != null && cardDistance > 0) {
					SwipedRight(itemIndex);
				}
				else if (SwipedLeft != null)
				{
					SwipedLeft(itemIndex);
				}

				// show the next card
				ShowNextCard ();

			}
			// put the card back in the center
			else {

				// move the top card back to the center
				topCard.TranslateTo ((-topCard.X), - topCard.Y, AnimLength, Easing.SpringOut);
				topCard.RotateTo (0, AnimLength, Easing.SpringOut);

				// scale the back card down
				var prevCard = cards [PrevCardIndex (topCardIndex)];
				await prevCard.ScaleTo(BackCardScale, AnimLength, Easing.SpringOut);

			}	

			ignoreTouch = false;
		}
			
		// show the next card
		void ShowNextCard()
		{
			if (cards[0].IsVisible == false && cards[1].IsVisible == false) {
				Setup();
				return;
			}
			
			var topCard = cards [topCardIndex];
			topCardIndex = NextCardIndex (topCardIndex);

			// if there are more cards to show, show the next card in to place of 
			// the card that was swipped off the screen
			if (itemIndex < ItemsSource.Count) {
				// push it to the back z order
				((RelativeLayout)this.Content).LowerChild(topCard);

				// reset its scale, opacity and rotation
				topCard.Scale = BackCardScale;
				topCard.RotateTo(0, 0);
				topCard.TranslateTo(0, -topCard.Y, 0);

				// set the data
				topCard.Name.Text = ItemsSource[itemIndex].Name;
				topCard.Location.Text = ItemsSource[itemIndex].Location;
				topCard.Description.Text = ItemsSource[itemIndex].Description;
				topCard.Photo.Source = ImageSource.FromFile(ItemsSource[itemIndex].Photo);

				topCard.IsVisible = true;
				itemIndex++;
			}
		}

		// return the next card index from the top
		int NextCardIndex(int topIndex)
		{
			return topIndex == 0 ? 1 : 0;
		}

		// return the prev card index from the yop
		int PrevCardIndex(int topIndex)
		{
			return topIndex == 0 ? 1 : 0;
		}			

		// helper to get the scale based on the card index position relative to the top card
		float GetScale(int index) 
		{			
			return (index == topCardIndex) ? 1.0f : BackCardScale;
		}			
	}
}

