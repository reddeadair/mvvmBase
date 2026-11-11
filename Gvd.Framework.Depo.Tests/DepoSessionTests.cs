using Gvd.Framework.Hierarchy;
using Gvd.Framework.Validation;

namespace DepoSample.Tests
{
    /// <summary>
    /// Oturum, alan kuralları, kayıt ve geri alma testleri.
    /// </summary>
    public class DepoSessionTests
    {
        /// <summary>
        /// Hatasız depo açılınca hata ve uyarı yoktur. Kaydedilecek değişiklik yoktur.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task Open_CleanWarehouse_HasNoIssues()
        {
            using var context = await DepoTestContext.OpenAsync(1L);

            Assert.Equal(0, context.Session.ErrorCount);
            Assert.Equal(0, context.WarningCount);
            Assert.False(context.Session.IsChanged);
            Assert.False(context.Session.CanSave);
            Assert.Equal(2, context.Session.Hierarchy.Root.Children.Count);
        }

        /// <summary>
        /// İzmir deposunda bir hata (ürün kodu boş) ve iki uyarı (şehir boş, çıkış açıklaması boş) vardır.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task Open_WarehouseWithProblems_CountsErrorsAndWarnings()
        {
            using var context = await DepoTestContext.OpenAsync(2L);

            Assert.Equal(1, context.Session.ErrorCount);
            Assert.Equal(2, context.WarningCount);

            var product = context.Product(0, 0);
            Assert.Equal(MessageLevel.Error, product.Code.MessageLevel);
            Assert.Equal("Ürün kodu boş olamaz.", product.Code.Messages.Message);
            Assert.Equal(MessageLevel.Warning, context.Warehouse().City.MessageLevel);
            Assert.Equal(MessageLevel.Warning, product.StockMovements.Items[1].Note.MessageLevel);
        }

        /// <summary>
        /// Formda alan değişince değer DTO'ya geçer, hata kalkar ve oturum değişmiş olur. Uyarı kaydı durdurmaz.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task EditField_ClearsError_WarningsDoNotBlockSave()
        {
            using var context = await DepoTestContext.OpenAsync(2L);
            var product = context.Product(0, 0);

            product.Code.Value = "U-100";

            Assert.Equal("U-100", product.Dto.Code);
            Assert.Equal(MessageLevel.None, product.Code.MessageLevel);
            Assert.Equal(0, context.Session.ErrorCount);
            Assert.Equal(2, context.WarningCount);
            Assert.True(context.Session.IsChanged);
            Assert.Equal(NodeState.Modified, product.Node.State);
            Assert.True(context.Session.CanSave);

            var status = await context.Session.SaveAsync();

            Assert.Equal(SaveStatus.Saved, status);
            Assert.False(context.Session.IsChanged);
            var stored = (ProductDto)context.Store.GetStored(2L).Children[0].Children[0].Dto;
            Assert.Equal("U-100", stored.Code);
        }

        /// <summary>
        /// Hata varken kayıt yapılmaz. Kayıt katmanı çağrılmaz.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task Save_WithError_ReturnsInvalid()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            context.Warehouse().Code.Value = string.Empty;

            Assert.Equal(1, context.Session.ErrorCount);
            Assert.False(context.Session.CanSave);
            Assert.Equal(SaveStatus.Invalid, await context.Session.SaveAsync());
            Assert.Equal(0, context.Store.SaveCount);
        }

        /// <summary>
        /// Sayı alanında sıfır ve eksi değer hata verir. Büyük yük uyarı verir.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task ShelfMaxLoad_ErrorAndWarningLevels()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            var shelf = context.Shelf(1);

            shelf.MaxLoad.Value = 0;
            Assert.Equal(MessageLevel.Error, shelf.MaxLoad.MessageLevel);

            shelf.MaxLoad.Value = 2500;
            Assert.Equal(MessageLevel.Warning, shelf.MaxLoad.MessageLevel);
            Assert.True(shelf.MaxLoad.IsValid);
            Assert.Equal(1, context.WarningCount);

            shelf.MaxLoad.Value = null;
            Assert.Equal(MessageLevel.Error, shelf.MaxLoad.MessageLevel);
        }

        /// <summary>
        /// "Değişiklikleri At" açılıştaki değerlere döner. Kayıt katmanındaki değer değişmez.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task Discard_RestoresValues()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            context.Warehouse().Name.Value = "Değişen Ad";
            context.Product(0, 0).StockMovements.AddNew();
            context.Session.Hierarchy.Remove(context.ShelfNode(1));
            Assert.True(context.Session.IsChanged);

            context.Session.Discard();

            Assert.False(context.Session.IsChanged);
            Assert.Equal(2, context.Session.Hierarchy.Root.Children.Count);
            Assert.Equal("Ankara Ana Depo", context.Warehouse().Name.Value);
            Assert.Equal(2, context.Product(0, 0).StockMovements.Items.Count);
            Assert.Equal("Ankara Ana Depo", ((WarehouseDto)context.Store.GetStored(1L).Dto).Name);
        }

        /// <summary>
        /// Kaydedilen değer yeni oturumda görünür. Eklenen ürün ve silinen raf da kaydedilir.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task Save_ThenReopen_ShowsSavedTree()
        {
            var store = new WarehouseStore();
            using (var context = await DepoTestContext.OpenAsync(1L, store))
            {
                var hierarchy = context.Session.Hierarchy;
                var newProduct = hierarchy.Add(context.ShelfNode(1), new ProductDto { Code = "U-009", Name = "Pense", UnitWeight = 0.5 });
                Assert.Equal(NodeState.Added, newProduct.State);
                hierarchy.Remove(context.ShelfNode(0));
                Assert.Equal(2, context.Session.ChangeCount);

                Assert.Equal(SaveStatus.Saved, await context.Session.SaveAsync());
            }

            using var reopened = await DepoTestContext.OpenAsync(1L, store);
            var shelves = reopened.Session.Hierarchy.Root.Children;
            Assert.Single(shelves);
            Assert.Equal("R-02", shelves[0].GetDto<ShelfDto>().Code);
            Assert.Equal(2, shelves[0].Children.Count);
            Assert.Equal("Pense", shelves[0].Children[1].GetDto<ProductDto>().Name);
        }

        /// <summary>
        /// Yeni raf ve ürün CreateWith ile başlangıç değeri alır. Yeni ürün kendiliğinden doğrulanır.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task AddThroughWorkspace_UsesCreateWith()
        {
            var store = new WarehouseStore();
            var workspace = DepoSetup.CreateWorkspace(store, new AlwaysDiscardPrompt());
            await workspace.SelectCommand.ExecuteAsync(workspace.Roots[0]);
            var shelfType = Assert.Single(workspace.AddableTypes);

            workspace.AddCommand.Execute(shelfType);

            var shelf = Assert.IsType<ShelfViewModel>(workspace.SelectedViewModel);
            Assert.Equal("R-03", shelf.Code.Value);
            Assert.Equal(500, shelf.MaxLoad.Value);
            Assert.True(workspace.Session!.IsChanged);
        }
    }
}
