namespace LomThaiText
{
    internal static class ThaiMeshAdjuster
    {
        private const string FatePopupTitlePath =
            "/[UI]/MainUI/Layer_5/StatModPlaceholder/NormalStat(Clone)/Title";
        private const string MentalHeaderPath =
            "/[UI]/MainUI/Layer_3/ActionInfo/MentalBar/Header";

        internal static float GetOverflowShift(
            string path,
            float renderedMaxX,
            float safeMaxX)
        {
            if (path != FatePopupTitlePath && path != MentalHeaderPath) return 0f;

            var overflow = renderedMaxX - safeMaxX;
            return overflow > 0f ? -overflow : 0f;
        }

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

        internal static bool TryGetQuadIndex(
            string text,
            int textIndex,
            bool supportRichText,
            out int quadIndex)
        {
            quadIndex = -1;
            if (string.IsNullOrEmpty(text) || textIndex < 0 || textIndex >= text.Length)
                return false;

            var nextQuad = 0;
            for (var index = 0; index < text.Length; index++)
            {
                if (supportRichText && text[index] == '<')
                {
                    var close = text.IndexOf('>', index + 1);
                    if (close >= 0)
                    {
                        if (textIndex >= index && textIndex <= close) return false;
                        index = close;
                        continue;
                    }
                }

                if (char.IsWhiteSpace(text[index]))
                {
                    if (index == textIndex) return false;
                    continue;
                }

                if (index == textIndex)
                {
                    quadIndex = nextQuad;
                    return true;
                }
                nextQuad++;
            }
            return false;
        }
    }
}
