using Gvd.Framework.Hierarchy;
using Gvd.Framework.Validation;

namespace DepoSample.Tests
{
    /// <summary>
    /// Testlerin ortak kurulumu: kayıt katmanı, açık oturum ve form ViewModel'leri. Pencere açmaz.
    /// </summary>
    /// <remarks>
    /// Form ViewModel'ini NodeViewModelCache verir. Workspace de aynı sınıfı kullanır.
    /// Discard sonrası oturum yeni ağaç kurar. Bu yüzden düğümü her seferinde Session.Hierarchy içinden yeniden bul.
    /// </remarks>
    public sealed class DepoTestContext : IDisposable
    {
        private DepoTestContext(WarehouseStore store, HierarchySession session)
        {
            Store = store;
            Session = session;
            Cache = new NodeViewModelCache(DepoSetup.CreateRegistry(DepoSetup.CreateViewModelFactory()), session);
        }

        public WarehouseStore Store { get; }

        public HierarchySession Session { get; }

        public NodeViewModelCache Cache { get; }

        /// <summary>
        /// Oturumdaki uyarılı alan sayısı. Oturumda ErrorCount vardır, uyarı sayısı yoktur. Sonuç deposunun özetinden okunur.
        /// </summary>
        public int WarningCount => Session.Results.GetSummary(Session.Hierarchy.Root).TotalWarnings;

        /// <summary>
        /// Depoyu açar.
        /// </summary>
        /// <param name="warehouseId">Depo kimliği</param>
        /// <param name="store">Kayıt katmanı. Boşsa örnek veriyle yeni kayıt katmanı kurulur</param>
        /// <returns>Açık oturumlu test ortamı</returns>
        public static async Task<DepoTestContext> OpenAsync(long warehouseId, WarehouseStore? store = null)
        {
            store ??= new WarehouseStore();
            var session = await DepoSetup.CreateSessions(store).OpenAsync(warehouseId);
            return new DepoTestContext(store, session);
        }

        /// <summary>
        /// Deponun formu.
        /// </summary>
        /// <returns>Depo ViewModel'i</returns>
        public WarehouseViewModel Warehouse() => (WarehouseViewModel)Cache.Get(Session.Hierarchy.Root);

        /// <summary>
        /// Rafın formu.
        /// </summary>
        /// <param name="shelfIndex">Rafın sırası (0'dan başlar)</param>
        /// <returns>Raf ViewModel'i</returns>
        public ShelfViewModel Shelf(int shelfIndex) => (ShelfViewModel)Cache.Get(ShelfNode(shelfIndex));

        /// <summary>
        /// Ürünün formu.
        /// </summary>
        /// <param name="shelfIndex">Rafın sırası</param>
        /// <param name="productIndex">Ürünün raftaki sırası</param>
        /// <returns>Ürün ViewModel'i</returns>
        public ProductViewModel Product(int shelfIndex, int productIndex) => (ProductViewModel)Cache.Get(ShelfNode(shelfIndex).Children[productIndex]);

        /// <summary>
        /// Rafın düğümü.
        /// </summary>
        /// <param name="shelfIndex">Rafın sırası</param>
        /// <returns>Raf düğümü</returns>
        public HierarchyNode ShelfNode(int shelfIndex) => Session.Hierarchy.Root.Children[shelfIndex];

        /// <summary>
        /// Alanın sonuç deposundaki mesaj seviyesi. Form açılmadan da okunur.
        /// </summary>
        /// <param name="node">Düğüm</param>
        /// <param name="property">Alan adı. Liste satırı için "StockMovements[0].Quantity"</param>
        /// <returns>Mesaj seviyesi</returns>
        public MessageLevel LevelOf(HierarchyNode node, string property) => Session.Results.GetMessage(node, property).Level;

        /// <inheritdoc />
        public void Dispose()
        {
            Cache.Dispose();
        }
    }
}
