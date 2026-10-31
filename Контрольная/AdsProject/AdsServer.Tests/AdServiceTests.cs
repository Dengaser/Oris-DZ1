using AdsModule;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdsModule.Tests
{
    [TestClass]
    public class AdServiceTests
    {
        [TestMethod]
        public void Create_ValidData_ReturnsActiveAdWithId()
        {
            var service = new AdService();

            var (result, ad) = service.Create("Ноутбук Lenovo", "electronics");

            Assert.AreEqual(ServiceResult.Ok, result);
            Assert.IsNotNull(ad);
            Assert.AreEqual(1, ad!.Id);
            Assert.AreEqual("active", ad.Status);
        }

        [TestMethod]
        public void Create_ShortTitle_ReturnsBadRequest()
        {
            var service = new AdService();

            var (result, _) = service.Create("abc", "furniture");

            Assert.AreEqual(ServiceResult.BadRequest, result);
        }

        [TestMethod]
        public void Create_UnknownCategory_ReturnsBadRequest()
        {
            var service = new AdService();

            var (result, _) = service.Create("Красивый диван", "cars");

            Assert.AreEqual(ServiceResult.BadRequest, result);
        }

        [TestMethod]
        public void Create_DuplicateTitleAndCategory_ReturnsConflict()
        {
            var service = new AdService();
            service.Create("Старый шкаф", "furniture");

            var (result, _) = service.Create("старый шкаф", "furniture");

            Assert.AreEqual(ServiceResult.Conflict, result);
        }

        [TestMethod]
        public void Create_SameTitleDifferentCategory_IsAllowed()
        {
            var service = new AdService();
            service.Create("Универсальный товар", "furniture");

            var (result, _) = service.Create("Универсальный товар", "clothing");

            Assert.AreEqual(ServiceResult.Ok, result);
        }

        [TestMethod]
        public void GetAll_FilterByCategory_ReturnsOnlyMatching()
        {
            var service = new AdService();
            service.Create("Телефон Samsung", "electronics");
            service.Create("Куртка зимняя", "clothing");

            var (result, items) = service.GetAll("clothing");

            Assert.AreEqual(ServiceResult.Ok, result);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("clothing", items[0].Category);
        }

        [TestMethod]
        public void Update_InvalidStatus_ReturnsBadRequest()
        {
            var service = new AdService();
            service.Create("Телефон Samsung", "electronics");

            var result = service.Update(1, "Телефон Samsung", "electronics", "deleted");

            Assert.AreEqual(ServiceResult.BadRequest, result);
        }

        [TestMethod]
        public void Update_NotExistingId_ReturnsNotFound()
        {
            var service = new AdService();

            var result = service.Update(99, "Новое название", "furniture", "archived");

            Assert.AreEqual(ServiceResult.NotFound, result);
        }

        [TestMethod]
        public void Update_ToDuplicateOfAnotherAd_ReturnsConflict()
        {
            var service = new AdService();
            service.Create("Телефон Samsung", "electronics");
            service.Create("Телефон Apple", "electronics");

            var result = service.Update(2, "Телефон Samsung", "electronics", "active");

            Assert.AreEqual(ServiceResult.Conflict, result);
        }

        [TestMethod]
        public void Update_ValidData_ChangesAllFields()
        {
            var service = new AdService();
            service.Create("Телефон Samsung", "electronics");

            var result = service.Update(1, "Стол письменный", "furniture", "archived");
            var ad = service.GetById(1);

            Assert.AreEqual(ServiceResult.Ok, result);
            Assert.AreEqual("Стол письменный", ad!.Title);
            Assert.AreEqual("furniture", ad.Category);
            Assert.AreEqual("archived", ad.Status);
        }
    }
}
