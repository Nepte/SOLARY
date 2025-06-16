using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using SOLARY.Services;

namespace SOLARY
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("CeraPro-Black.ttf", "CeraProBlack");
                    fonts.AddFont("CeraPro-Regular.ttf", "CeraProRegular");
                    fonts.AddFont("CeraPro-Medium.ttf", "CeraProMedium");
                    fonts.AddFont("Poppins-Semibold.ttf", "PoppinsSemibold");
                    fonts.AddFont("Poppins-Regular.ttf", "PoppinsRegular");
                    fonts.AddFont("Poppins-Medium.ttf", "PoppinsMedium");
                    fonts.AddFont("Montserrat-Medium.ttf", "MontserratMedium");
                });

            // Ajouter cette configuration dans la méthode CreateMauiApp() après la ligne builder.UseMauiApp<App>()
            builder.ConfigureEssentials(essentials =>
            {
                essentials.UseMapServiceToken("tsPaLJ6DWZPxMNaJ2DV7");
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Configurer les gestionnaires spécifiques à la plateforme
            builder.ConfigureMauiHandlers(handlers =>
            {
#if WINDOWS
                // Configuration spécifique pour Windows
                handlers.AddHandler(typeof(Microsoft.Maui.Controls.WebView), typeof(Microsoft.Maui.Handlers.WebViewHandler));
#elif ANDROID
                // Configuration spécifique pour Android
                handlers.AddHandler(typeof(Microsoft.Maui.Controls.WebView), typeof(Microsoft.Maui.Handlers.WebViewHandler));
#elif IOS
                // Configuration spécifique pour iOS
                handlers.AddHandler(typeof(Microsoft.Maui.Controls.WebView), typeof(Microsoft.Maui.Handlers.WebViewHandler));
#endif
            });

            return builder.Build();
        }
    }
}
