using System.Collections.ObjectModel;

namespace SOLARY.ViewModels;

public class WelcomePageViewModel
{
    // Liste des images pour le CarouselView
    public ObservableCollection<string> Images { get; set; }

    public WelcomePageViewModel()
    {
        // Initialise la liste des images
        Images = new ObservableCollection<string>
        {
            "solar.png",    // Noms exacts des fichiers dans Resources/Images
            "test.png",
            "chat.png"
        };
    }
}
