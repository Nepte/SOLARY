namespace SOLARY
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Enregistrer les convertisseurs
            Resources.Add("BoolToColor", new SOLARY.ViewModels.BoolToColorConverter());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            return window;
        }
    }
}
