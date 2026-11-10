namespace Forma.Framework.Catalog.Tests.Queries;

using System.Globalization;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Resources;

using Xunit;

public sealed class CatalogGroupingTests
{
    private static readonly DateTimeOffset Midnight = new(2026, 9, 16, 0, 0, 0, TimeSpan.Zero);

    private static CatalogItem CreateFile(string name, long? size = null, DateTimeOffset? modifiedAt = null)
        => CatalogItem.For(new FileResourceInfo($"/media/{name}") { Size = size, ModifiedAt = modifiedAt });

    public sealed class Groups
    {
        [Theory]
        [InlineData("apple.png", true)]
        [InlineData("Hat.png", true)]
        [InlineData("iris.png", false)]
        [InlineData("", false)]
        public void Should_admit_a_name_by_its_first_character_in_invariant_upper_case(string name, bool admitted)
        {
            // Arrange
            var group = new CatalogNameGroup("AToH", 'A', 'H');
            var item = new CatalogItem { Key = name, Name = name };

            // Act
            var admits = group.Admits(item, CatalogGroupField.Name);

            // Assert
            Assert.Equal(admitted, admits);
        }

        [Theory]
        [InlineData(1024L, false)]
        [InlineData(1025L, true)]
        [InlineData(2048L, true)]
        [InlineData(2049L, false)]
        [InlineData(null, false)]
        public void Should_admit_a_size_above_its_lower_bound_and_at_most_its_upper(long? size, bool admitted)
        {
            // Arrange
            var group = new CatalogSizeGroup("Small", 1024, 2048);

            // Act
            var admits = group.Admits(CreateFile("a.bin", size), CatalogGroupField.Size);

            // Assert
            Assert.Equal(admitted, admits);
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(24 * 60 * 60 - 1, true)]
        [InlineData(24 * 60 * 60, false)]
        public void Should_admit_a_date_from_its_start_and_before_its_end(int secondsAfterMidnight, bool admitted)
        {
            // Arrange
            var group = new CatalogDateGroup("Today", Midnight, Midnight.AddDays(1));

            // Act
            var admits = group.Admits(CreateFile("a.png", modifiedAt: Midnight.AddSeconds(secondsAfterMidnight)), CatalogGroupField.DateModified);

            // Assert
            Assert.Equal(admitted, admits);
        }

        [Fact]
        public void Should_read_the_date_the_grouped_field_names()
        {
            // Arrange
            var group = new CatalogDateGroup("Today", Midnight, null);
            var item = new CatalogItem { Key = "a", Name = "a", CreatedAt = Midnight.AddDays(-3), ModifiedAt = Midnight.AddHours(1) };

            // Act
            var byAdded = group.Admits(item, CatalogGroupField.DateAdded);
            var byModified = group.Admits(item, CatalogGroupField.DateModified);

            // Assert
            Assert.Multiple(
                () => Assert.False(byAdded),
                () => Assert.True(byModified));
        }

        [Fact]
        public void Should_admit_an_item_holding_no_value_of_the_field_in_the_missing_group()
        {
            // Arrange
            var group = new CatalogMissingGroup("Unspecified");

            // Act
            var undated = group.Admits(CreateFile("a.png"), CatalogGroupField.DateAdded);
            var sized = group.Admits(CreateFile("b.png", size: 10), CatalogGroupField.Size);

            // Assert
            Assert.Multiple(
                () => Assert.True(undated),
                () => Assert.False(sized));
        }

        [Fact]
        public void Should_admit_an_item_of_its_kind()
        {
            // Arrange
            var group = new CatalogKindGroup("Image", CatalogItemKind.Image);

            // Act
            var image = group.Admits(CatalogItem.For(new ImageResourceInfo("/media/a.png")), CatalogGroupField.Kind);
            var folder = group.Admits(CatalogItem.ForFolder(new FolderResourceInfo("covers")), CatalogGroupField.Kind);

            // Assert
            Assert.Multiple(
                () => Assert.True(image),
                () => Assert.False(folder));
        }
    }

    public sealed class Grouping
    {
        private static readonly CatalogGrouping Names = new(
            CatalogGroupField.Name,
            [
                new CatalogNameGroup("AToH", 'A', 'H'),
                new CatalogRemainderGroup("Other"),
                new CatalogNameGroup("IToP", 'I', 'P'),
            ]);

        [Fact]
        public void Should_place_an_item_in_the_first_group_that_admits_it_and_the_rest_in_the_remainder_wherever_it_stands()
        {
            // Act
            var places = new[] { CreateFile("apple.png"), CreateFile("kite.png"), CreateFile("zebra.png") }.Select(Names.IndexOf).ToList();

            // Assert
            Assert.Equal([0, 2, 1], places);
        }

        [Fact]
        public void Should_refuse_an_item_no_group_admits_where_there_is_no_remainder()
        {
            // Arrange
            var grouping = new CatalogGrouping(CatalogGroupField.Name, [new CatalogNameGroup("AToH", 'A', 'H')]);

            // Act
            var error = Record.Exception(() => grouping.IndexOf(CreateFile("zebra.png")));

            // Assert
            Assert.IsType<InvalidOperationException>(error);
        }

        [Fact]
        public void Should_count_the_groups_holding_items_in_the_order_they_stand()
        {
            // Act
            var sizes = Names.CountGroups([CreateFile("zebra.png"), CreateFile("apple.png"), CreateFile("hat.png")]);

            // Assert
            Assert.Equal([new CatalogGroupSize("AToH", 2), new CatalogGroupSize("Other", 1)], sizes);
        }

        [Fact]
        public void Should_order_a_held_collection_by_its_groups_before_its_sort_and_count_them_whole()
        {
            // Arrange
            var view = new CatalogCollectionView(item => item.GetKind().ToString(), () => CultureInfo.InvariantCulture)
            {
                Source = [CreateFile("kite.png"), CreateFile("zebra.png"), CreateFile("apple.png"), CreateFile("hat.png")],
                PageSize = 2,
                Grouping = Names
            };

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["apple.png", "hat.png", "zebra.png", "kite.png"], view.MatchingItems.Select(item => item.Name)),
                () => Assert.Equal(["apple.png", "hat.png"], view.VisibleItems.Select(item => item.Name)),
                () => Assert.Equal([new CatalogGroupSize("AToH", 2), new CatalogGroupSize("Other", 1), new CatalogGroupSize("IToP", 1)], view.GroupSizes));
        }
    }

    public sealed class Kinds
    {
        [Fact]
        public void Should_read_an_items_kind_from_what_it_is()
        {
            // Act
            var kinds = new ICatalogItem[]
            {
                CatalogItem.ForFolder(new FolderResourceInfo("covers")),
                CatalogItem.For(new ImageResourceInfo("/media/a.png")),
                CatalogItem.For(new VideoResourceInfo("/media/a.mp4")),
                CatalogItem.For(new AudioResourceInfo("/media/a.mp3")),
                CatalogItem.For(new ArchiveResourceInfo("/media/a.zip")),
                CatalogItem.For(new DocumentResourceInfo("/media/a.pdf")),
                CatalogItem.For(new FileResourceInfo("/media/a.bin")),
                new CatalogItem { Key = "nothing", Name = "Nothing" },
            }.Select(item => item.GetKind());

            // Assert
            Assert.Equal(
                [CatalogItemKind.Folder, CatalogItemKind.Image, CatalogItemKind.Video, CatalogItemKind.Audio, CatalogItemKind.Archive, CatalogItemKind.Document, CatalogItemKind.File, CatalogItemKind.Unknown],
                kinds);
        }
    }
}
