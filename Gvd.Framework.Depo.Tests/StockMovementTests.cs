using Gvd.Framework.Hierarchy;
using Gvd.Framework.Validation;

namespace DepoSample.Tests
{
    /// <summary>
    /// Stok hareketi tablosu testleri: satır ekleme, silme, taşıma ve satır kuralları.
    /// </summary>
    public class StockMovementTests
    {
        /// <summary>
        /// Yeni satır NewModelFactory ile başlar: bugünün tarihi, giriş tipi, boş miktar. Boş miktar satırda hata verir.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task AddRow_UsesDefaults_AndValidatesRow()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            var product = context.Product(0, 0);

            product.StockMovements.AddCommand.Execute(null);

            var row = product.StockMovements.Items[2];
            Assert.Equal(3, product.StockMovements.Items.Count);
            Assert.Equal(2, row.Model.Order);
            Assert.Equal(MovementType.In, row.Type.Value);
            Assert.Equal(DateTime.Today, row.Date.Value);
            Assert.Equal(MessageLevel.Error, row.Quantity.MessageLevel);
            Assert.Equal(MessageLevel.Error, context.LevelOf(product.Node, "StockMovements[2].Quantity"));
            Assert.Equal(1, context.Session.ErrorCount);
            Assert.True(context.Session.IsChanged);

            row.Quantity.Value = 5;

            Assert.Equal(MessageLevel.None, row.Quantity.MessageLevel);
            Assert.Equal(0, context.Session.ErrorCount);
            Assert.Equal(85, product.Quantity.Value);
        }

        /// <summary>
        /// Boş tarih satırda hata verir. Çıkış satırında boş açıklama uyarı verir.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task RowRules_EmptyDateAndOutNote()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            var row = context.Product(0, 0).StockMovements.Items[1];

            row.Date.Value = null;
            Assert.Equal(MessageLevel.Error, row.Date.MessageLevel);

            row.Date.Value = new DateTime(2026, 11, 10);
            Assert.Equal(MessageLevel.None, row.Date.MessageLevel);

            row.Note.Value = string.Empty;
            Assert.Equal(MessageLevel.Warning, row.Note.MessageLevel);
            Assert.Equal(1, context.WarningCount);

            row.Type.Value = MovementType.In;
            Assert.Equal(MessageLevel.None, row.Note.MessageLevel);
            Assert.Equal(0, context.WarningCount);
        }

        /// <summary>
        /// Satır taşınınca sıra numarası değişir ve satırın hatası satırla gider.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task MoveRow_KeepsOrderAndMessage()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            var product = context.Product(0, 0);
            var list = product.StockMovements;
            var second = list.Items[1];
            second.Quantity.Value = 0;
            Assert.Equal(MessageLevel.Error, context.LevelOf(product.Node, "StockMovements[1].Quantity"));

            list.SelectedItem = second;
            list.MoveUpCommand.Execute(null);

            Assert.Same(second, list.Items[0]);
            Assert.Equal(0, second.Model.Order);
            Assert.Equal(1, list.Items[1].Model.Order);
            Assert.Equal(MessageLevel.Error, second.Quantity.MessageLevel);
            Assert.Equal(MessageLevel.Error, context.LevelOf(product.Node, "StockMovements[0].Quantity"));
            Assert.Equal(MessageLevel.None, context.LevelOf(product.Node, "StockMovements[1].Quantity"));

            list.MoveDownCommand.Execute(null);

            Assert.Same(second, list.Items[1]);
            Assert.Equal(MessageLevel.Error, context.LevelOf(product.Node, "StockMovements[1].Quantity"));
        }

        /// <summary>
        /// Seçili satır silinir. Miktar yeniden hesaplanır. Düğüm değişmiş olur.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task RemoveRow_RecalculatesQuantity()
        {
            using var context = await DepoTestContext.OpenAsync(1L);
            var product = context.Product(0, 0);
            var list = product.StockMovements;

            list.SelectedItem = list.Items[1];
            list.RemoveCommand.Execute(null);

            Assert.Single(list.Items);
            Assert.Single(product.Dto.StockMovements);
            Assert.Equal(100, product.Quantity.Value);
            Assert.Equal(NodeState.Modified, product.Node.State);
        }

        /// <summary>
        /// Tablo değişikliği kaydedilir ve yeni oturumda görünür.
        /// </summary>
        /// <returns>Testin görevi</returns>
        [Fact]
        public async Task SaveRows_ThenReopen()
        {
            var store = new WarehouseStore();
            using (var context = await DepoTestContext.OpenAsync(1L, store))
            {
                var product = context.Product(0, 1);
                product.StockMovements.AddNew(WarehouseStore.Movement("2026-11-07", MovementType.Out, 5, "Satış"));
                Assert.Equal(SaveStatus.Saved, await context.Session.SaveAsync());
            }

            using var reopened = await DepoTestContext.OpenAsync(1L, store);
            var saved = reopened.Product(0, 1);
            Assert.Equal(2, saved.StockMovements.Items.Count);
            Assert.Equal(45, saved.Quantity.Value);
            Assert.Equal("Satış", saved.StockMovements.Items[1].Note.Value);
        }
    }
}
