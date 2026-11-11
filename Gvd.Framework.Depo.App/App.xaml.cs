using Gvd.Framework.Command;
using Gvd.Framework.Resources;
using Gvd.Framework.Wpf;
using Gvd.Framework.Wpf.Theming;
using Gvd.Framework.Wpf.Workspace;
using System.Windows;

namespace DepoSample
{
    /// <summary>
    /// Depo envanteri uygulaması. Adımlar Framework/README.md "Uygulamanın en küçük kurulumu" bölümündeki sıradadır.
    /// </summary>
    /// <remarks>
    /// Şema, kurallar, kayıt katmanı ve ViewModel kaydı Core projesindedir (DepoSetup). Bu sınıf yalnızca pencereyi kurar.
    /// </remarks>
    public partial class App : Application
    {
        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Tema. Tema sözlüğü App.xaml içindedir.
            GvdWpf.Initialize();
            ThemeManager.Apply(this, GvdTheme.Light);

            // 2. Etiketler. Dosya bu projenin DLL'inin içindedir (Resources.tr.xml, csproj içinde Resource).
            GvdWpf.UseResources(GvdWpf.LoadResources(new Uri("/Resources.tr.xml", UriKind.Relative)));

            // Komut hatası: işleyici atanmazsa komuttaki hata uygulamayı kapatır. Burada hata bir ileti kutusunda görünür.
            AsyncRelayCommandBase.DefaultErrorHandler = exception => MessageBox.Show(exception.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);

            // 3-7. Şema, kurallar, kayıt katmanı, ViewModel kaydı ve workspace.
            // FieldLabel: Durum paneli alanı etiketle gösterir. Etiket yoksa alan adı görünür.
            var store = new WarehouseStore();
            var workspace = DepoSetup.CreateWorkspace(
                store,
                new SaveChangesWindowPrompt(),
                (node, property) => GvdWpf.Resources?.GetLabelOrDefault(node.Type.DtoType, property, property) ?? property);

            // 8. Pencere. Formların şablonları MainWindow.xaml içindedir.
            var window = new MainWindow { DataContext = workspace };
            MainWindow = window;
            window.Show();
        }
    }
}
