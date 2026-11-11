using Gvd.Framework.DependencyInjection;
using Gvd.Framework.Hierarchy;
using Gvd.Framework.Services;
using Gvd.Framework.Validation;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace DepoSample
{
    /// <summary>
    /// Şemayı, kuralları, ViewModel kaydını ve oturum fabrikasını kurar. App ve testler bu yöntemleri çağırır.
    /// </summary>
    public static class DepoSetup
    {
        /// <summary>
        /// Bir depodaki en çok raf sayısı.
        /// </summary>
        public const int MaxShelvesPerWarehouse = 50;

        /// <summary>
        /// Şema: düğüm tipleri, alt düğüm sınırları ve ağaçtaki görünüş. Depo → Raf → Ürün.
        /// </summary>
        /// <returns>Yeni şema</returns>
        public static HierarchySchema CreateSchema()
        {
            var schema = new HierarchySchema();

            // Display: ilk değer ağaçtaki addır. İkinci değer özettir: fare satırın üstünde durunca ipucunda görünür.
            schema.Root<WarehouseDto>("Depo")
                .Child<ShelfDto>(max: MaxShelvesPerWarehouse)
                .Display(x => x.Name, (x, node) => $"{x.Code} · {node.Children.Count} raf");
            schema.Node<ShelfDto>("Raf")
                .CreateWith(parent => new ShelfDto { Code = $"R-{parent.Children.Count + 1:00}", MaxLoad = 500 })
                .Child<ProductDto>()
                .Display(x => x.Code, (x, node) => string.Format(CultureInfo.InvariantCulture, "En çok {0} kg · {1} ürün", x.MaxLoad, node.Children.Count));
            schema.Node<ProductDto>("Ürün")
                .CreateWith(parent => new ProductDto { Code = $"U-{parent.Children.Count + 1:000}", Name = "Yeni ürün", UnitWeight = 1 })
                .Display(x => x.Name, (x, node) => $"{x.Code} · {x.Quantity} adet");
            return schema;
        }

        /// <summary>
        /// Kurallar. Yeni bir kural sınıfını buraya ekle.
        /// </summary>
        /// <returns>Kural kümesi</returns>
        public static ValidationRuleSet CreateRules()
        {
            return new ValidationRuleSet()
                .AddValidator(new WarehouseValidator())
                .AddValidator(new ShelfValidator())
                .AddValidator(new ProductValidator())
                // Tablo değişince miktar da yeniden doğrulanır. Miktar tablodan hesaplanır (ProductViewModel.OnListContentChanged).
                .AddDependency<ProductDto>(x => x.StockMovements, x => x.Quantity)
                .AddRules(new ProductHierarchyRules());
        }

        /// <summary>
        /// Liste satırlarının ViewModel'ini üreten fabrika. ProductViewModel tablo için bu fabrikayı kullanır.
        /// </summary>
        /// <returns>ViewModel fabrikası</returns>
        public static IViewModelFactory CreateViewModelFactory()
        {
            return new ServiceCollection().AddGvdFramework().BuildServiceProvider().GetRequiredService<IViewModelFactory>();
        }

        /// <summary>
        /// Her DTO tipi için hangi ViewModel'in oluşacağını söyler. Formu App projesindeki MainWindow.xaml içindeki DataTemplate seçer.
        /// </summary>
        /// <param name="factory">Liste satırlarının ViewModel fabrikası</param>
        /// <returns>ViewModel kaydı</returns>
        public static NodeViewModelRegistry CreateRegistry(IViewModelFactory factory)
        {
            return new NodeViewModelRegistry()
                .Register<WarehouseDto, WarehouseViewModel>(() => new WarehouseViewModel())
                .Register<ShelfDto, ShelfViewModel>(() => new ShelfViewModel())
                .Register<ProductDto, ProductViewModel>(() => new ProductViewModel(factory));
        }

        /// <summary>
        /// Oturum fabrikası: şema, kurallar ve kayıt katmanı.
        /// </summary>
        /// <param name="store">Kayıt katmanı</param>
        /// <returns>Oturum fabrikası</returns>
        public static HierarchySessionFactory CreateSessions(WarehouseStore store)
        {
            return new HierarchySessionFactory(CreateSchema(), CreateRules(), store, store, new JsonDeepCopyService());
        }

        /// <summary>
        /// Workspace: ağaç, seçim, form ViewModel'i, kaydet ve at komutları. Pencere HierarchyWorkspaceView ile çizer.
        /// </summary>
        /// <param name="store">Kayıt katmanı</param>
        /// <param name="prompt">Kaydedilmemiş değişiklik sorusu</param>
        /// <param name="fieldLabel">Durum panelindeki alan adı. Pencere etiket dosyasını kullanır. Testler null verir</param>
        /// <returns>Kökleri yüklü workspace</returns>
        public static HierarchyWorkspace CreateWorkspace(WarehouseStore store, ISaveChangesPrompt prompt, Func<HierarchyNode, string, string>? fieldLabel = null)
        {
            var workspace = new HierarchyWorkspace(CreateSessions(store), CreateRegistry(CreateViewModelFactory()), prompt);
            if (fieldLabel != null)
            {
                workspace.FieldLabel = fieldLabel;
            }

            workspace.SetRoots(store.Roots);
            return workspace;
        }
    }
}
