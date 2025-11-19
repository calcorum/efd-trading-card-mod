using Xunit;
using TradingCardMod;

namespace TradingCardMod.Tests
{
    /// <summary>
    /// Unit tests for the CardParser class.
    /// Tests cover parsing, validation, and rarity mapping functionality.
    /// </summary>
    public class CardParserTests
    {
        #region ParseLine Tests

        [Fact]
        public void ParseLine_ValidLineWithoutDescription_ReturnsCard()
        {
            // Arrange
            string line = "Duck Hero | Example Set | 001 | duck_hero.png | Rare | 0.01 | 100";

            // Act
            var card = CardParser.ParseLine(line);

            // Assert
            Assert.NotNull(card);
            Assert.Equal("Duck Hero", card.CardName);
            Assert.Equal("Example Set", card.SetName);
            Assert.Equal(1, card.SetNumber);
            Assert.Equal("duck_hero.png", card.ImageFile);
            Assert.Equal("Rare", card.Rarity);
            Assert.Equal(0.01f, card.Weight);
            Assert.Equal(100, card.Value);
            Assert.Null(card.Description);
        }

        [Fact]
        public void ParseLine_ValidLineWithDescription_ReturnsCardWithDescription()
        {
            // Arrange
            string line = "Golden Quacker | Example Set | 002 | golden_quacker.png | Ultra Rare | 0.01 | 500 | A legendary duck made of pure gold";

            // Act
            var card = CardParser.ParseLine(line);

            // Assert
            Assert.NotNull(card);
            Assert.Equal("Golden Quacker", card.CardName);
            Assert.Equal("A legendary duck made of pure gold", card.Description);
        }

        [Fact]
        public void ParseLine_CommentLine_ReturnsNull()
        {
            // Arrange
            string line = "# This is a comment";

            // Act
            var card = CardParser.ParseLine(line);

            // Assert
            Assert.Null(card);
        }

        [Fact]
        public void ParseLine_EmptyLine_ReturnsNull()
        {
            // Arrange
            string line = "";

            // Act
            var card = CardParser.ParseLine(line);

            // Assert
            Assert.Null(card);
        }

        [Fact]
        public void ParseLine_WhitespaceLine_ReturnsNull()
        {
            // Arrange
            string line = "   \t  ";

            // Act
            var card = CardParser.ParseLine(line);

            // Assert
            Assert.Null(card);
        }

        [Fact]
        public void ParseLine_TooFewFields_ReturnsNull()
        {
            // Arrange - only 6 fields instead of required 7
            string line = "Duck Hero | Example Set | 001 | duck_hero.png | Rare | 0.01";

            // Act
            var card = CardParser.ParseLine(line);

            // Assert
            Assert.Null(card);
        }

        [Fact]
        public void ParseLine_InvalidNumber_ReturnsNull()
        {
            // Arrange - SetNumber is not a valid integer
            string line = "Duck Hero | Example Set | ABC | duck_hero.png | Rare | 0.01 | 100";

            // Act
            var card = CardParser.ParseLine(line);

            // Assert
            Assert.Null(card);
        }

        [Fact]
        public void ParseLine_InvalidWeight_ReturnsNull()
        {
            // Arrange - Weight is not a valid float
            string line = "Duck Hero | Example Set | 001 | duck_hero.png | Rare | heavy | 100";

            // Act
            var card = CardParser.ParseLine(line);

            // Assert
            Assert.Null(card);
        }

        [Fact]
        public void ParseLine_TrimsWhitespace_ReturnsCleanCard()
        {
            // Arrange - extra whitespace around values
            string line = "  Duck Hero  |  Example Set  |  001  |  duck_hero.png  |  Rare  |  0.01  |  100  ";

            // Act
            var card = CardParser.ParseLine(line);

            // Assert
            Assert.NotNull(card);
            Assert.Equal("Duck Hero", card.CardName);
            Assert.Equal("Example Set", card.SetName);
        }

        #endregion

        #region RarityToQuality Tests

        [Theory]
        [InlineData("Common", 2)]
        [InlineData("common", 2)]
        [InlineData("COMMON", 2)]
        [InlineData("Uncommon", 3)]
        [InlineData("Rare", 4)]
        [InlineData("Very Rare", 5)]
        [InlineData("Ultra Rare", 6)]
        [InlineData("Legendary", 6)]
        public void RarityToQuality_ValidRarity_ReturnsCorrectQuality(string rarity, int expectedQuality)
        {
            // Act
            int quality = CardParser.RarityToQuality(rarity);

            // Assert
            Assert.Equal(expectedQuality, quality);
        }

        [Fact]
        public void RarityToQuality_UnknownRarity_ReturnsDefault()
        {
            // Arrange - unknown rarity should default to uncommon (3)
            string rarity = "Super Duper Rare";

            // Act
            int quality = CardParser.RarityToQuality(rarity);

            // Assert
            Assert.Equal(3, quality);
        }

        [Fact]
        public void RarityToQuality_WithExtraWhitespace_ReturnsCorrectQuality()
        {
            // Arrange
            string rarity = "  Rare  ";

            // Act
            int quality = CardParser.RarityToQuality(rarity);

            // Assert
            Assert.Equal(4, quality);
        }

        #endregion

        #region IsValidRarity Tests

        [Theory]
        [InlineData("Common", true)]
        [InlineData("Uncommon", true)]
        [InlineData("Rare", true)]
        [InlineData("Very Rare", true)]
        [InlineData("Ultra Rare", true)]
        [InlineData("Legendary", true)]
        [InlineData("Invalid", false)]
        [InlineData("Super Rare", false)]
        [InlineData("", false)]
        public void IsValidRarity_ReturnsExpectedResult(string rarity, bool expected)
        {
            // Act
            bool result = CardParser.IsValidRarity(rarity);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region ValidateCard Tests

        [Fact]
        public void ValidateCard_ValidCard_ReturnsNoErrors()
        {
            // Arrange
            var card = new TradingCard
            {
                CardName = "Duck Hero",
                SetName = "Example Set",
                SetNumber = 1,
                ImageFile = "duck_hero.png",
                Rarity = "Rare",
                Weight = 0.01f,
                Value = 100
            };

            // Act
            var errors = CardParser.ValidateCard(card);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateCard_MissingCardName_ReturnsError()
        {
            // Arrange
            var card = new TradingCard
            {
                CardName = "",
                SetName = "Example Set",
                SetNumber = 1,
                ImageFile = "duck_hero.png",
                Rarity = "Rare",
                Weight = 0.01f,
                Value = 100
            };

            // Act
            var errors = CardParser.ValidateCard(card);

            // Assert
            Assert.Single(errors);
            Assert.Contains("CardName", errors[0]);
        }

        [Fact]
        public void ValidateCard_NegativeValue_ReturnsError()
        {
            // Arrange
            var card = new TradingCard
            {
                CardName = "Duck Hero",
                SetName = "Example Set",
                SetNumber = 1,
                ImageFile = "duck_hero.png",
                Rarity = "Rare",
                Weight = 0.01f,
                Value = -100
            };

            // Act
            var errors = CardParser.ValidateCard(card);

            // Assert
            Assert.Single(errors);
            Assert.Contains("Value", errors[0]);
        }

        [Fact]
        public void ValidateCard_MultipleErrors_ReturnsAllErrors()
        {
            // Arrange - missing card name and negative weight
            var card = new TradingCard
            {
                CardName = "",
                SetName = "Example Set",
                SetNumber = 1,
                ImageFile = "duck_hero.png",
                Rarity = "Rare",
                Weight = -0.01f,
                Value = 100
            };

            // Act
            var errors = CardParser.ValidateCard(card);

            // Assert
            Assert.Equal(2, errors.Count);
        }

        #endregion

        #region TradingCard Tests

        [Fact]
        public void TradingCard_GenerateTypeID_ReturnsConsistentValue()
        {
            // Arrange
            var card = new TradingCard
            {
                CardName = "Duck Hero",
                SetName = "Example Set"
            };

            // Act
            int id1 = card.GenerateTypeID();
            int id2 = card.GenerateTypeID();

            // Assert - same card should always generate same ID
            Assert.Equal(id1, id2);
            Assert.True(id1 >= 100000);
            Assert.True(id1 < 1000000);
        }

        [Fact]
        public void TradingCard_GenerateTypeID_DifferentCardsGetDifferentIDs()
        {
            // Arrange
            var card1 = new TradingCard { CardName = "Duck Hero", SetName = "Set A" };
            var card2 = new TradingCard { CardName = "Golden Quacker", SetName = "Set A" };

            // Act
            int id1 = card1.GenerateTypeID();
            int id2 = card2.GenerateTypeID();

            // Assert
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void TradingCard_GetDescription_WithCustomDescription_ReturnsCustom()
        {
            // Arrange
            var card = new TradingCard
            {
                CardName = "Duck Hero",
                SetName = "Example Set",
                SetNumber = 1,
                Rarity = "Rare",
                Description = "Custom description"
            };

            // Act
            string description = card.GetDescription();

            // Assert
            Assert.Equal("Custom description", description);
        }

        [Fact]
        public void TradingCard_GetDescription_WithoutDescription_ReturnsAutoGenerated()
        {
            // Arrange
            var card = new TradingCard
            {
                CardName = "Duck Hero",
                SetName = "Example Set",
                SetNumber = 1,
                Rarity = "Rare",
                Description = null
            };

            // Act
            string description = card.GetDescription();

            // Assert
            Assert.Equal("Example Set #001 - Rare", description);
        }

        [Fact]
        public void TradingCard_GetQuality_ReturnsCorrectQuality()
        {
            // Arrange
            var card = new TradingCard { Rarity = "Ultra Rare" };

            // Act
            int quality = card.GetQuality();

            // Assert
            Assert.Equal(6, quality);
        }

        #endregion
    }
}
