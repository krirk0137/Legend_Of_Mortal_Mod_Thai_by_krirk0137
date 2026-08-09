using System.Text;

namespace LomThaiText
{
    internal static class MeshDiagnostics
    {
        internal static string Describe(
            string path,
            string text,
            int characterCount,
            int visibleCharacterCount,
            int generatorVertexCount,
            int meshVertexCount,
            int meshIndexCount)
        {
            return "[thai-mesh] " + path
                + " codes=" + Codepoints(text)
                + " chars=" + characterCount
                + " visible=" + visibleCharacterCount
                + " genVerts=" + generatorVertexCount
                + " meshVerts=" + meshVertexCount
                + " indices=" + meshIndexCount;
        }

        private static string Codepoints(string value)
        {
            if (string.IsNullOrEmpty(value)) return "<empty>";
            var result = new StringBuilder(value.Length * 5);
            for (var i = 0; i < value.Length; i++)
            {
                if (i > 0) result.Append(' ');
                result.Append(((int)value[i]).ToString("X4"));
            }
            return result.ToString();
        }
    }
}
