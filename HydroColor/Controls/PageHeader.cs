using System.Windows.Input;

namespace HydroColor.Controls
{
    // A title bar drawn by the page itself: a back arrow on the left and a centered title.
    // Pages that use it hide the system navigation bar. On iOS, showing and hiding the system bar
    // between pages leaves a black bar at the top for a moment when going back.
    public class PageHeader : ContentView
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title), typeof(string), typeof(PageHeader), "",
            propertyChanged: (bindable, oldValue, newValue) => ((PageHeader)bindable)._title.Text = (string)newValue);

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Run when the back arrow is tapped. When not set, the page goes back one step.
        public static readonly BindableProperty BackCommandProperty = BindableProperty.Create(
            nameof(BackCommand), typeof(ICommand), typeof(PageHeader));

        public ICommand BackCommand
        {
            get { return (ICommand)GetValue(BackCommandProperty); }
            set { SetValue(BackCommandProperty, value); }
        }

        readonly Label _title = new Label
        {
            FontSize = 18,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            Margin = new Thickness(60, 0)
        };

        public PageHeader()
        {
            // The arrow used in the Library list, turned to point left
            Image arrow = new Image
            {
                Source = "right_arrow.png",
                Rotation = 180,
                HeightRequest = 18,
                WidthRequest = 18,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center
            };

            // A larger touch area than the arrow itself
            Grid backArea = new Grid
            {
                WidthRequest = 60,
                HorizontalOptions = LayoutOptions.Start,
                Padding = new Thickness(15, 0, 0, 0),
                BackgroundColor = Colors.Transparent
            };
            backArea.Add(arrow);

            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += async (sender, e) =>
            {
                if (BackCommand != null)
                {
                    if (BackCommand.CanExecute(null))
                    {
                        BackCommand.Execute(null);
                    }
                    return;
                }

                try
                {
                    await Shell.Current.GoToAsync("..", true);
                }
                catch
                {
                }
            };
            backArea.GestureRecognizers.Add(tap);

            Grid layout = new Grid { HeightRequest = 44, BackgroundColor = Colors.Black };
            layout.Add(_title);
            layout.Add(backArea);

            Content = layout;
        }
    }
}
