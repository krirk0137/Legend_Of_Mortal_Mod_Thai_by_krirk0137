namespace LomThaiText
{
    internal static class ThaiMeshAdjuster
    {
        internal static bool ShouldRaiseTone(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index <= 0 || index >= text.Length)
                return false;

            var tone = text[index];
            if (tone < '\u0E48' || tone > '\u0E4C') return false;

            var previous = text[index - 1];
            return previous == '\u0E31' ||
                   (previous >= '\u0E34' && previous <= '\u0E37') ||
                   previous == '\u0E47' ||
                   previous == '\u0E4D';
        }
    }
}
