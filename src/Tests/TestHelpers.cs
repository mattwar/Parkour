using Parkour;

namespace Tests;

public static class TestHelpers
{
    public static (string textWithoutMarker, int markerPosition) StripMarker(string textWithMarker, string marker = "$")
    {
        var index = textWithMarker.IndexOf(marker);
        if (index >= 0)
        {
            var textWithoutMarker = textWithMarker.Remove(index, marker.Length);
            return (textWithoutMarker, index);
        }
        else
        {
            return (textWithMarker, -1);
        }
    }

    public static IReadOnlyList<T> Concat<T>(params IEnumerable<T>[] inputs) =>
        inputs.SelectMany(x => x).ToArray();
}