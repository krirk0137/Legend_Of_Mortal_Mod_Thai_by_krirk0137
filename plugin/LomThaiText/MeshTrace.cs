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
            var horizontalShift = ShiftTargetHorizontally(text, mesh, path);
            if (!IsTraceTarget(path, text.text)) return;

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
                Plugin.Log.LogInfo("[thai-mesh] stacked " +
                    StackedToneReport(text, generator.verts, mesh));
                Plugin.Log.LogInfo("[thai-mesh] raisedStackedTones=" + raised);
                Plugin.Log.LogInfo("[thai-mesh] horizontalShift=" +
                    horizontalShift.ToString("0.##"));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[thai-mesh] trace failed: " + e.Message);
            }
        }

        private static float ShiftTargetHorizontally(
            Text text,
            VertexHelper mesh,
            string path)
        {
            if (mesh.currentVertCount == 0 || text.rectTransform == null) return 0f;

            var maxX = float.MinValue;
            for (var index = 0; index < mesh.currentVertCount; index++)
            {
                var vertex = new UIVertex();
                mesh.PopulateUIVertex(ref vertex, index);
                maxX = Math.Max(maxX, vertex.position.x);
            }

            var shift = ThaiMeshAdjuster.GetOverflowShift(
                path,
                maxX,
                text.rectTransform.rect.xMax);
            if (shift == 0f) return 0f;

            for (var index = 0; index < mesh.currentVertCount; index++)
            {
                var vertex = new UIVertex();
                mesh.PopulateUIVertex(ref vertex, index);
                vertex.position.x += shift;
                mesh.SetUIVertex(vertex, index);
            }
            return shift;
        }

        private static bool IsTraceTarget(string path, string value)
        {
            return path.EndsWith("/Setting/Text", StringComparison.Ordinal) ||
                   value.IndexOf("โชคชะตา", StringComparison.Ordinal) >= 0 ||
                   value.IndexOf("ชื่อ", StringComparison.Ordinal) >= 0 ||
                   value.IndexOf("เรื่อง", StringComparison.Ordinal) >= 0 ||
                   value.IndexOf("อื่น", StringComparison.Ordinal) >= 0;
        }

        private static int RaiseStackedTones(Text text, VertexHelper mesh)
        {
            var value = text.text;
            if (string.IsNullOrEmpty(value) || mesh.currentVertCount % 4 != 0)
                return 0;

            var lift = Math.Max(1f, text.fontSize * 0.24f);
            var raised = 0;
            for (var index = 0; index < value.Length; index++)
            {
                if (!ThaiMeshAdjuster.ShouldRaiseTone(value, index)) continue;
                int quadIndex;
                if (!ThaiMeshAdjuster.TryGetQuadIndex(
                    value, index, text.supportRichText, out quadIndex))
                    continue;
                var firstVertex = quadIndex * 4;
                if (firstVertex + 3 >= mesh.currentVertCount) continue;
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

        private static string StackedToneReport(
            Text text,
            IList<UIVertex> generated,
            VertexHelper mesh)
        {
            var result = new StringBuilder();
            for (var index = 0; index < text.text.Length; index++)
            {
                if (!ThaiMeshAdjuster.ShouldRaiseTone(text.text, index)) continue;
                int quadIndex;
                if (!ThaiMeshAdjuster.TryGetQuadIndex(
                    text.text, index, text.supportRichText, out quadIndex))
                    continue;
                if (result.Length > 0) result.Append(" | ");
                result.Append("i=").Append(index)
                    .Append(" q=").Append(quadIndex)
                    .Append(" upper=").Append(((int)text.text[index - 1]).ToString("X4"))
                    .Append(" tone=").Append(((int)text.text[index]).ToString("X4"))
                    .Append(" size=").Append(text.fontSize)
                    .Append(" gen=").Append(GeneratedBounds(generated, quadIndex))
                    .Append(" mesh=").Append(MeshBounds(mesh, quadIndex));
            }
            return result.Length == 0 ? "<none>" : result.ToString();
        }

        private static string GeneratedBounds(IList<UIVertex> vertices, int quadIndex)
        {
            var first = quadIndex * 4;
            if (first < 0 || first + 3 >= vertices.Count) return "<missing>";
            return Bounds(
                vertices[first].position,
                vertices[first + 1].position,
                vertices[first + 2].position,
                vertices[first + 3].position);
        }

        private static string MeshBounds(VertexHelper mesh, int quadIndex)
        {
            var first = quadIndex * 4;
            if (first < 0 || first + 3 >= mesh.currentVertCount) return "<missing>";
            var vertices = new UIVertex[4];
            for (var offset = 0; offset < 4; offset++)
                mesh.PopulateUIVertex(ref vertices[offset], first + offset);
            return Bounds(
                vertices[0].position,
                vertices[1].position,
                vertices[2].position,
                vertices[3].position);
        }

        private static string Bounds(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var minX = Math.Min(Math.Min(a.x, b.x), Math.Min(c.x, d.x));
            var maxX = Math.Max(Math.Max(a.x, b.x), Math.Max(c.x, d.x));
            var minY = Math.Min(Math.Min(a.y, b.y), Math.Min(c.y, d.y));
            var maxY = Math.Max(Math.Max(a.y, b.y), Math.Max(c.y, d.y));
            return minX.ToString("0.##") + "," + minY.ToString("0.##") +
                   ".." + maxX.ToString("0.##") + "," + maxY.ToString("0.##");
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
