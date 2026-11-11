using Gvd.Framework.Hierarchy;
using Gvd.Framework.Wpf.Controls;
using Gvd.Framework.Wpf.Theming;
using System.ComponentModel;
using System.Windows;

namespace DepoSample
{
    /// <summary>
    /// Ana pencere. İçinde Framework'ün hazır workspace ekranı vardır. DataContext bir HierarchyWorkspace olmalı.
    /// </summary>
    public partial class MainWindow : GvdWindow
    {
        private bool closeConfirmed;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Kaydedilmemiş değişiklik varken pencere kapanmadan önce kayıt sorusunu sorar.
        /// </summary>
        /// <remarks>
        /// Kapatma beklenemez. Bu yüzden önce kapatmayı iptal et. Kullanıcı kaydet ya da at derse pencereyi yeniden kapat.
        /// Örnek: Framework/Apps/Gvd.Framework.Gallery/MainWindow.xaml.cs.
        /// </remarks>
        /// <param name="e">Kapatma olayı</param>
        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (closeConfirmed || DataContext is not HierarchyWorkspace workspace)
            {
                return;
            }

            e.Cancel = true;
            if (await workspace.CloseAsync())
            {
                closeConfirmed = true;

                // Close() kapanış olayının içinden çağrılamaz; WPF hata verir. Bu yüzden sıraya alınır.
                _ = Dispatcher.BeginInvoke(new Action(Close));
            }
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle(Application.Current);
        }
    }
}
