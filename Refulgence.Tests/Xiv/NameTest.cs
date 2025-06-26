using Refulgence.Xiv;

namespace Refulgence.Tests.Xiv;

public class NameTest
{
    public static IEnumerable<Name> Names
        => [Name.Empty, "g_LightDirection", "Hello, world!", 0xC0FFEEu, 0xEF4E7491u, new(0xABAD1DEAu, "g_LightDirection"),];

    public static IEnumerable<string?> NameValues
        => [string.Empty, "g_LightDirection", "Hello, world!", null, null, "g_LightDirection",];

    public static IEnumerable<uint> NameCrc32s
        => [0u, 0xEF4E7491u, 0xE4928064u, 0xC0FFEEu, 0xEF4E7491u, 0xABAD1DEAu,];

    public static IEnumerable<Name> PairFirsts
        => Names.SelectMany((name, i) => Names.Take(i).Select(_ => name));

    public static IEnumerable<Name> PairSeconds
        => Names.SelectMany((_, i) => Names.Take(i));

    public static IEnumerable<bool> PairEqualities
        =>
        [
            // [1].Equals([..1])
            false,
            // [2].Equals([..2])
            false,
            false,
            // [3].Equals([..3])
            false,
            false,
            false,
            // [4].Equals([..4])
            false,
            true,
            false,
            false,
            // [5].Equals([..5])
            false,
            false,
            false,
            false,
            false,
        ];

    public static IEnumerable<int> PairComparisons
        =>
        [
            // [1].CompareTo([..1])
            1,
            // [2].CompareTo([..2])
            1,
            -1,
            // [3].CompareTo([..3])
            1,
            -1,
            -1,
            // [4].CompareTo([..4])
            1,
            0,
            1,
            1,
            // [5].CompareTo([..5])
            1,
            -1,
            -1,
            1,
            -1,
        ];

    [Test]
    [Sequential]
    public void DataTest(
        [ValueSource(nameof(Names))] Name name,
        [ValueSource(nameof(NameValues))] string value,
        [ValueSource(nameof(NameCrc32s))] uint crc32) =>
        Assert.Multiple(
            () =>
            {
                Assert.That(name.Value,         Is.EqualTo(value));
                Assert.That(name.Crc32,         Is.EqualTo(crc32));
                Assert.That(name.GetHashCode(), Is.EqualTo(unchecked((int)crc32)));
            }
        );

    [Test]
    [Sequential]
    public void ReflexiveComparisonTest(
        [ValueSource(nameof(Names))] Name name) =>
        Assert.Multiple(
            () =>
            {
                Assert.That(name.CompareTo(name), Is.Zero);
            }
        );

    [Test]
    [Sequential]
    public void OtherComparisonTest(
        [ValueSource(nameof(PairFirsts))] Name first,
        [ValueSource(nameof(PairSeconds))] Name second,
        [ValueSource(nameof(PairEqualities))] bool equality,
        [ValueSource(nameof(PairComparisons))] int comparison) =>
        Assert.Multiple(
            () =>
            {
                Assert.That(first.Equals(second),    Is.EqualTo(equality));
                Assert.That(first.CompareTo(second), Is.EqualTo(comparison));

                Assert.That(second.Equals(first),    Is.EqualTo(equality));
                Assert.That(second.CompareTo(first), Is.EqualTo(-comparison));
            }
        );
}
