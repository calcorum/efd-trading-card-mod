using System.Collections.Generic;
using System.IO;
using Xunit;
using TradingCardMod;

namespace TradingCardMod.Tests
{
    /// <summary>
    /// Unit tests for the CardPack, PackSlot, and PackParser classes.
    /// Tests cover pack data structures, default slot configurations, and parsing functionality.
    /// </summary>
    public class PackParserTests
    {
        #region CardPack Tests

        [Fact]
        public void CardPack_GenerateTypeID_ReturnsConsistentValue()
        {
            // Arrange
            var pack = new CardPack
            {
                PackName = "Booster Pack",
                SetName = "Example Set"
            };

            // Act
            int id1 = pack.GenerateTypeID();
            int id2 = pack.GenerateTypeID();

            // Assert - same pack should always generate same ID
            Assert.Equal(id1, id2);
            Assert.True(id1 >= 300000, "Pack TypeID should be in 300000+ range");
            Assert.True(id1 < 400000, "Pack TypeID should be below 400000");
        }

        [Fact]
        public void CardPack_GenerateTypeID_DifferentPacksGetDifferentIDs()
        {
            // Arrange
            var pack1 = new CardPack { PackName = "Booster Pack", SetName = "Set A" };
            var pack2 = new CardPack { PackName = "Premium Pack", SetName = "Set A" };

            // Act
            int id1 = pack1.GenerateTypeID();
            int id2 = pack2.GenerateTypeID();

            // Assert
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void CardPack_GenerateTypeID_SameNameDifferentSetGetsDifferentIDs()
        {
            // Arrange
            var pack1 = new CardPack { PackName = "Booster Pack", SetName = "Set A" };
            var pack2 = new CardPack { PackName = "Booster Pack", SetName = "Set B" };

            // Act
            int id1 = pack1.GenerateTypeID();
            int id2 = pack2.GenerateTypeID();

            // Assert
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void CardPack_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var pack = new CardPack();

            // Assert
            Assert.Equal(string.Empty, pack.PackName);
            Assert.Equal(string.Empty, pack.SetName);
            Assert.Equal(0, pack.Value);
            Assert.Equal(0.1f, pack.Weight);
            Assert.False(pack.IsDefault);
            Assert.NotNull(pack.Slots);
            Assert.Empty(pack.Slots);
        }

        #endregion

        #region PackSlot Tests

        [Fact]
        public void PackSlot_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var slot = new PackSlot();

            // Assert
            Assert.True(slot.UseRarityWeights);
            Assert.NotNull(slot.RarityWeights);
            Assert.Empty(slot.RarityWeights);
            Assert.NotNull(slot.CardWeights);
            Assert.Empty(slot.CardWeights);
        }

        [Fact]
        public void PackSlot_RarityWeights_CanBePopulated()
        {
            // Arrange
            var slot = new PackSlot
            {
                UseRarityWeights = true,
                RarityWeights = new Dictionary<string, float>
                {
                    { "Common", 100f },
                    { "Rare", 10f }
                }
            };

            // Assert
            Assert.Equal(2, slot.RarityWeights.Count);
            Assert.Equal(100f, slot.RarityWeights["Common"]);
            Assert.Equal(10f, slot.RarityWeights["Rare"]);
        }

        [Fact]
        public void PackSlot_CardWeights_CanBePopulated()
        {
            // Arrange
            var slot = new PackSlot
            {
                UseRarityWeights = false,
                CardWeights = new Dictionary<string, float>
                {
                    { "Duck Hero", 50f },
                    { "Golden Quacker", 10f }
                }
            };

            // Assert
            Assert.False(slot.UseRarityWeights);
            Assert.Equal(2, slot.CardWeights.Count);
            Assert.Equal(50f, slot.CardWeights["Duck Hero"]);
        }

        #endregion

        #region DefaultPackSlots Tests

        [Fact]
        public void DefaultPackSlots_CommonSlot_HasCorrectWeights()
        {
            // Act
            var slot = DefaultPackSlots.CommonSlot;

            // Assert
            Assert.True(slot.UseRarityWeights);
            Assert.Equal(100f, slot.RarityWeights["Common"]);
            Assert.Equal(30f, slot.RarityWeights["Uncommon"]);
            Assert.Equal(5f, slot.RarityWeights["Rare"]);
            Assert.Equal(1f, slot.RarityWeights["Very Rare"]);
            Assert.Equal(0f, slot.RarityWeights["Ultra Rare"]);
            Assert.Equal(0f, slot.RarityWeights["Legendary"]);
        }

        [Fact]
        public void DefaultPackSlots_UncommonSlot_HasCorrectWeights()
        {
            // Act
            var slot = DefaultPackSlots.UncommonSlot;

            // Assert
            Assert.True(slot.UseRarityWeights);
            Assert.Equal(60f, slot.RarityWeights["Common"]);
            Assert.Equal(80f, slot.RarityWeights["Uncommon"]);
            Assert.Equal(20f, slot.RarityWeights["Rare"]);
        }

        [Fact]
        public void DefaultPackSlots_RareSlot_HasBetterOdds()
        {
            // Act
            var slot = DefaultPackSlots.RareSlot;

            // Assert
            Assert.True(slot.UseRarityWeights);
            // Rare slot should have better odds for rare+ cards
            Assert.True(slot.RarityWeights["Rare"] > slot.RarityWeights["Common"]);
            Assert.True(slot.RarityWeights["Legendary"] > 0f);
        }

        [Fact]
        public void DefaultPackSlots_GetDefaultSlots_ReturnsThreeSlots()
        {
            // Act
            var slots = DefaultPackSlots.GetDefaultSlots();

            // Assert
            Assert.Equal(3, slots.Count);
            Assert.All(slots, s => Assert.True(s.UseRarityWeights));
        }

        [Fact]
        public void DefaultPackSlots_GetDefaultSlots_ReturnsNewInstances()
        {
            // Act
            var slots1 = DefaultPackSlots.GetDefaultSlots();
            var slots2 = DefaultPackSlots.GetDefaultSlots();

            // Assert - should be different list instances
            Assert.NotSame(slots1, slots2);
        }

        #endregion

        #region PackParser.CreateDefaultPack Tests

        [Fact]
        public void CreateDefaultPack_ReturnsPackWithCorrectSetName()
        {
            // Arrange
            string setName = "Example Set";
            string imagesDir = "/fake/path/images";

            // Act
            var pack = PackParser.CreateDefaultPack(setName, imagesDir);

            // Assert
            Assert.Equal(setName, pack.SetName);
            Assert.Equal($"{setName} Pack", pack.PackName);
            Assert.True(pack.IsDefault);
        }

        [Fact]
        public void CreateDefaultPack_HasThreeSlots()
        {
            // Arrange
            string setName = "Example Set";
            string imagesDir = "/fake/path/images";

            // Act
            var pack = PackParser.CreateDefaultPack(setName, imagesDir);

            // Assert
            Assert.Equal(3, pack.Slots.Count);
        }

        [Fact]
        public void CreateDefaultPack_HasCorrectImagePath()
        {
            // Arrange
            string setName = "Example Set";
            string imagesDir = "/path/to/images";

            // Act
            var pack = PackParser.CreateDefaultPack(setName, imagesDir);

            // Assert
            Assert.Equal("pack.png", pack.ImageFile);
            Assert.Contains("pack.png", pack.ImagePath);
        }

        [Fact]
        public void CreateDefaultPack_HasDefaultValues()
        {
            // Arrange
            string setName = "Example Set";
            string imagesDir = "/fake/path";

            // Act
            var pack = PackParser.CreateDefaultPack(setName, imagesDir);

            // Assert
            Assert.Equal(100, pack.Value);
            Assert.Equal(0.1f, pack.Weight);
        }

        #endregion

        #region PackParser.ParseFile Tests

        [Fact]
        public void ParseFile_NonExistentFile_ReturnsEmptyList()
        {
            // Arrange
            string fakePath = "/nonexistent/packs.txt";
            string imagesDir = "/nonexistent/images";

            // Act
            var packs = PackParser.ParseFile(fakePath, "TestSet", imagesDir);

            // Assert
            Assert.Empty(packs);
        }

        [Fact]
        public void ParseFile_EmptyFile_ReturnsEmptyList()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string imagesDir = Path.GetDirectoryName(tempFile) ?? "";

            try
            {
                File.WriteAllText(tempFile, "");

                // Act
                var packs = PackParser.ParseFile(tempFile, "TestSet", imagesDir);

                // Assert
                Assert.Empty(packs);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseFile_CommentOnlyFile_ReturnsEmptyList()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string imagesDir = Path.GetDirectoryName(tempFile) ?? "";

            try
            {
                File.WriteAllText(tempFile, "# This is a comment\n# Another comment");

                // Act
                var packs = PackParser.ParseFile(tempFile, "TestSet", imagesDir);

                // Assert
                Assert.Empty(packs);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseFile_ValidPackDefinition_ReturnsPack()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string imagesDir = Path.GetDirectoryName(tempFile) ?? "";

            try
            {
                // Format: PackName | Image | Value | Weight
                // Slots are indented with RARITY: or CARDS: prefix
                string content = @"# Test pack file
Premium Pack | premium.png | 500 | 0.05
  RARITY: Common:50, Rare:50
";
                File.WriteAllText(tempFile, content);

                // Act
                var packs = PackParser.ParseFile(tempFile, "TestSet", imagesDir);

                // Assert
                Assert.Single(packs);
                Assert.Equal("Premium Pack", packs[0].PackName);
                Assert.Equal("TestSet", packs[0].SetName);
                Assert.Equal(500, packs[0].Value);
                Assert.Equal(0.05f, packs[0].Weight);
                Assert.Single(packs[0].Slots);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseFile_MultipleSlots_ParsesAllSlots()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string imagesDir = Path.GetDirectoryName(tempFile) ?? "";

            try
            {
                string content = @"Test Pack | test.png | 100
  RARITY: Common:100
  RARITY: Uncommon:100
  RARITY: Rare:100
";
                File.WriteAllText(tempFile, content);

                // Act
                var packs = PackParser.ParseFile(tempFile, "TestSet", imagesDir);

                // Assert
                Assert.Single(packs);
                Assert.Equal(3, packs[0].Slots.Count);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseFile_MultiplePacks_ParsesAll()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string imagesDir = Path.GetDirectoryName(tempFile) ?? "";

            try
            {
                string content = @"Pack One | one.png | 100
  RARITY: Common:100

Pack Two | two.png | 200
  RARITY: Rare:100
";
                File.WriteAllText(tempFile, content);

                // Act
                var packs = PackParser.ParseFile(tempFile, "TestSet", imagesDir);

                // Assert
                Assert.Equal(2, packs.Count);
                Assert.Equal("Pack One", packs[0].PackName);
                Assert.Equal("Pack Two", packs[1].PackName);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseFile_CardSpecificWeights_ParsesCorrectly()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string imagesDir = Path.GetDirectoryName(tempFile) ?? "";

            try
            {
                string content = @"Special Pack | special.png | 1000
  CARDS: Duck Hero:50, Golden Quacker:10
";
                File.WriteAllText(tempFile, content);

                // Act
                var packs = PackParser.ParseFile(tempFile, "TestSet", imagesDir);

                // Assert
                Assert.Single(packs);
                var slot = packs[0].Slots[0];
                Assert.False(slot.UseRarityWeights);
                Assert.Equal(2, slot.CardWeights.Count);
                Assert.Equal(50f, slot.CardWeights["Duck Hero"]);
                Assert.Equal(10f, slot.CardWeights["Golden Quacker"]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion
    }
}
