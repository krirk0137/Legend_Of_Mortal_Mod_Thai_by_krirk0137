using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LomThaiText
{
    internal static class MeshTrace
    {
        private static readonly HashSet<string> Seen = new HashSet<string>(StringComparer.Ordinal);

        internal static void AfterPopulate(Text text, VertexHelper mesh)
        {
            if (text == null || mesh == null || !ThaiFontAdjuster.HasThai(text.text)) return;

            string path;
            try { path = Resizer.PathOf(text.transform); }
            catch { return; }

            var raised = RaiseStackedTones(text, mesh);
            if (!path.EndsWith("/Setting/Text", StringComparison.Ordinal)) return;

            try
            {
                var generator = text.cachedTextGenerator;
                var fingerprint = path + "\n" + text.text + "\n"
                    + generator.characterCount + "/" + generator.characterCountVisible + "/"
                    + generator.verts.Count + "/" + mesh.currentVertCount + "/" + mesh.currentIndexCount;
                if (!Seen.Add(fingerprint)) return;

                Plugin.Log.LogInfo(MeshDiagnostics.Describe(
                    path,
                    text.text,
                    generator.characterCount,
                    generator.characterCountVisible,
                    generator.verts.Count,
                    mesh.currentVertCount,
                    mesh.currentIndexCount));
                Plugin.Log.LogInfo("[thai-mesh] glyphs " + GlyphReport(text));
                Plugin.Log.LogInfo("[thai-mesh] quads " + QuadReport(text.text, generator.verts));
                Plugin.Log.LogInfo("[thai-mesh] raisedStackedTones=" + raised);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[thai-mesh] trace failed: " + e.Message);
            }
        }

        private static int RaiseStackedTones(Text text, VertexHelper mesh)
        {
            var value = text.text;
            if (string.IsNullOrEmpty(value) || value.IndexOf('<') >= 0 ||
                value.IndexOf('\n') >= 0 || mesh.currentVertCount != value.Length * 4)
                return 0;

            var lift = Math.Max(1f, text.fontSize * 0.24f);
            var raised = 0;
            for (var index = 0; index < value.Length; index++)
            {
                if (!ThaiMeshAdjuster.ShouldRaiseTone(value, index)) continue;
                var firstVertex = index * 4;
                for (var offset = 0; offset < 4; offset++)
                {
                    var vertex = new UIVertex();
                    mesh.PopulateUIVertex(ref vertex, firstVertex + offset);
                    vertex.position.y += lift;
                    mesh.SetUIVertex(vertex, firstVertex + offset);
                }
                raised++;
            }
            return raised;
        }

        private static string GlyphReport(Text text)
        {
            var result = new StringBuilder();
            var font = text.font;
            for (var i = 0; i < text.text.Length; i++)
            {
                if (i > 0) result.Append(" | ");
                var character = text.text[i];
                CharacterInfo info = new CharacterInfo();
                var found = font != null &&
                    font.GetCharacterInfo(character, out info, text.fontSize, text.fontStyle);
                result.Append(i).Append(':').Append(((int)character).ToString("X4"));
                if (!found)
                {
                    result.Append(" missing");
                    continue;
                }
                result.Append(" adv=").Append(info.advance)
                    .Append(" box=").Append(info.minX).Append(',').Append(info.minY)
                    .Append("..").Append(info.maxX).Append(',').Append(info.maxY);
            }
            return result.ToString();
        }

        private static string QuadReport(string text, IList<UIVertex> vertices)
        {
            var result = new StringBuilder();
            var quadCount = Math.Max(0, (vertices.Count - 4) / 4);
            var count = Math.Min(quadCount, 16);
            for (var quad = 0; quad < count; quad++)
            {
                if (quad > 0) result.Append(" | ");
                var first = vertices[quad * 4];
                var minX = first.position.x;
                var maxX = first.position.x;
                var minY = first.position.y;
                var maxY = first.position.y;
                var minU = first.uv0.x;
                var maxU = first.uv0.x;
                var minV = first.uv0.y;
                var maxV = first.uv0.y;
                var minAlpha = first.color.a;
                for (var offset = 1; offset < 4; offset++)
                {
                    var vertex = vertices[quad * 4 + offset];
                    minX = Math.Min(minX, vertex.position.x);
                    maxX = Math.Max(maxX, vertex.position.x);
                    minY = Math.Min(minY, vertex.position.y);
                    maxY = Math.Max(maxY, vertex.position.y);
                    minU = Math.Min(minU, vertex.uv0.x);
                    maxU = Math.Max(maxU, vertex.uv0.x);
                    minV = Math.Min(minV, vertex.uv0.y);
                    maxV = Math.Max(maxV, vertex.uv0.y);
                    minAlpha = Math.Min(minAlpha, vertex.color.a);
                }
                result.Append('q').Append(quad);
                if (quad < text.Length)
                    result.Append(':').Append(((int)text[quad]).ToString("X4"));
                result.Append(" pos=")
                    .Append(minX.ToString("0.##")).Append(',').Append(minY.ToString("0.##"))
                    .Append("..").Append(maxX.ToString("0.##")).Append(',').Append(maxY.ToString("0.##"))
                    .Append(" uv=")
                    .Append(minU.ToString("0.####")).Append(',').Append(minV.ToString("0.####"))
                    .Append("..").Append(maxU.ToString("0.####")).Append(',').Append(maxV.ToString("0.####"))
                    .Append(" a=").Append(minAlpha);
            }
            return result.Length == 0 ? "<none>" : result.ToString();
        }
    }
}
