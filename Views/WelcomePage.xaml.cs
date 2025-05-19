using SOLARY.Views;
using System.Windows.Input;

namespace SOLARY.Views
{
    public partial class WelcomePage : ContentPage
    {
        private readonly List<string> _images = new() { "empreinte_carbone.png", "test.png", "chat.png" };
        private int _currentIndex = 0;

        public ICommand NavigateToLoginCommand { get; }

        public WelcomePage()
        {
            InitializeComponent();

            // Initialiser la commande
            NavigateToLoginCommand = new Command(async () => await NavigateToLoginPage());
            BindingContext = this; // Définir le contexte de binding

            SetupImageContainer();
            StartImageRotationAsync();
        }

        private async Task NavigateToLoginPage()
        {
            await Navigation.PushAsync(new LoginPage());
        }

        private void SetupImageContainer()
        {
            ImageContainer.Children.Add(new Image
            {
                Source = _images[_currentIndex],
                Aspect = Aspect.AspectFill,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            });

            UpdateProgressBars(_currentIndex);
        }

        private async void StartImageRotationAsync()
        {
            while (true)
            {
                await Task.Delay(1800);

                _currentIndex = (_currentIndex + 1) % _images.Count;
                await TransitionToNextImage(_images[_currentIndex]);
            }
        }

        private async Task TransitionToNextImage(string nextImageSource)
        {
            var currentImage = ImageContainer.Children.FirstOrDefault() as VisualElement;
            if (currentImage != null)
            {
                var nextImage = new Image
                {
                    Source = nextImageSource,
                    Aspect = Aspect.AspectFill,
                    Opacity = 0,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };

                ImageContainer.Children.Add(nextImage);

                UpdateProgressBars(_currentIndex);

                await Task.WhenAll(
                    nextImage.FadeTo(1, 200),
                    currentImage.FadeTo(0, 200)
                );

                ImageContainer.Children.Remove(currentImage);
            }
        }

        private void UpdateProgressBars(int index)
        {
            Bar1.BackgroundColor = Colors.White;
            Bar2.BackgroundColor = Colors.White;
            Bar3.BackgroundColor = Colors.White;

            switch (index)
            {
                case 0:
                    Bar1.BackgroundColor = Color.FromArgb("#FFD602");
                    break;
                case 1:
                    Bar2.BackgroundColor = Color.FromArgb("#FFD602");
                    break;
                case 2:
                    Bar3.BackgroundColor = Color.FromArgb("#FFD602");
                    break;
            }
        }
    }
}
