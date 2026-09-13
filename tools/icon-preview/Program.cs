using System.Text;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Unity.UI.Icons;
using UnityEngine;

namespace FungusToast.Tools.IconPreview
{
    /// <summary>
    /// Renders every ability icon with the same drawing code Unity uses and writes PNGs plus an
    /// HTML review sheet showing each icon at the sizes the game displays it. Downscaling here is
    /// a plain area average, a close stand-in for Unity's trilinear mip sampling.
    /// </summary>
    internal static class Program
    {
        private static readonly int[] SlotSizes = { 68, 56, 40, 28 };

        private static int Main(string[] args)
        {
            string outDir = "TEMP/icon-sheets";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--out" && i + 1 < args.Length)
                {
                    outDir = args[++i];
                }
            }

            Directory.CreateDirectory(outDir);
            var rows = new List<ReviewRow>();
            rows.AddRange(RenderSurges(Path.Combine(outDir, "surges")));
            rows.AddRange(RenderMycovariants(Path.Combine(outDir, "mycovariants")));

            string html = BuildHtml(rows);
            string htmlPath = Path.Combine(outDir, "icon-review.html");
            File.WriteAllText(htmlPath, html, Encoding.UTF8);
            Console.WriteLine($"Rendered {rows.Count} icons. Review sheet: {Path.GetFullPath(htmlPath)}");
            return 0;
        }

        private static IEnumerable<ReviewRow> RenderSurges(string dir)
        {
            Directory.CreateDirectory(dir);
            foreach (int id in SurgeIcons.SurgeMutationIds)
            {
                Mutation? mutation = MutationRegistry.GetById(id);
                string name = mutation?.Name ?? $"Mutation {id}";
                string summary = FirstSentence(mutation?.Description);
                var canvas = new IconCanvas();
                SurgeIcons.Draw(canvas, id);
                string fileStem = Path.Combine(dir, Slug(name));
                yield return WriteRow("Surge", name, summary, canvas, fileStem);
            }
        }

        private static IEnumerable<ReviewRow> RenderMycovariants(string dir)
        {
            Directory.CreateDirectory(dir);
            foreach (Mycovariant mycovariant in MycovariantRepository.All)
            {
                var canvas = new IconCanvas();
                MycovariantIcons.Draw(canvas, mycovariant);
                string fileStem = Path.Combine(dir, Slug(mycovariant.Name));
                yield return WriteRow("Mycovariant", mycovariant.Name, FirstSentence(mycovariant.Description), canvas, fileStem);
            }
        }

        private static ReviewRow WriteRow(string family, string name, string summary, IconCanvas canvas, string fileStem)
        {
            byte[] full = PngWriter.Encode(canvas.Pixels, canvas.Size, canvas.Size);
            File.WriteAllBytes(fileStem + ".png", full);

            var row = new ReviewRow(family, name, summary, ToDataUri(full), new List<(int, string)>());
            foreach (int size in SlotSizes)
            {
                Color[] small = Downsample(canvas.Pixels, canvas.Size, size);
                byte[] png = PngWriter.Encode(small, size, size);
                File.WriteAllBytes($"{fileStem}_{size}.png", png);
                row.Slots.Add((size, ToDataUri(png)));
            }

            return row;
        }

        /// <summary>Area-average resample from a square source to a square target.</summary>
        private static Color[] Downsample(Color[] source, int sourceSize, int targetSize)
        {
            var result = new Color[targetSize * targetSize];
            float ratio = sourceSize / (float)targetSize;
            for (int ty = 0; ty < targetSize; ty++)
            {
                float y0 = ty * ratio, y1 = (ty + 1) * ratio;
                for (int tx = 0; tx < targetSize; tx++)
                {
                    float x0 = tx * ratio, x1 = (tx + 1) * ratio;
                    float r = 0f, g = 0f, b = 0f, a = 0f, weight = 0f;
                    for (int sy = (int)y0; sy < Math.Min(sourceSize, (int)Math.Ceiling(y1)); sy++)
                    {
                        float wy = Math.Min(y1, sy + 1) - Math.Max(y0, sy);
                        for (int sx = (int)x0; sx < Math.Min(sourceSize, (int)Math.Ceiling(x1)); sx++)
                        {
                            float wx = Math.Min(x1, sx + 1) - Math.Max(x0, sx);
                            float w = wx * wy;
                            Color c = source[sy * sourceSize + sx];
                            r += c.r * w;
                            g += c.g * w;
                            b += c.b * w;
                            a += c.a * w;
                            weight += w;
                        }
                    }

                    result[ty * targetSize + tx] = weight > 0f ? new Color(r / weight, g / weight, b / weight, a / weight) : default;
                }
            }

            return result;
        }

        private static string BuildHtml(List<ReviewRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>Ability icon review</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{background:#26271F;color:#F1F3EE;font:14px/1.4 system-ui,sans-serif;margin:24px}");
            sb.AppendLine("h1{font-size:18px;margin:0 0 4px} p.note{color:#B6BEAF;margin:0 0 16px}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;max-width:1100px}");
            sb.AppendLine("th,td{border-bottom:1px solid #424837;padding:10px 12px;text-align:left;vertical-align:middle}");
            sb.AppendLine("th{color:#D9DED3;font-weight:600;font-size:12px;text-transform:uppercase;letter-spacing:.04em}");
            sb.AppendLine("td.name{font-weight:600;white-space:nowrap} td.name small{display:block;color:#B6BEAF;font-weight:400}");
            sb.AppendLine("td.summary{color:#D9DED3;max-width:320px}");
            sb.AppendLine("img{display:block;image-rendering:auto} td.slots{white-space:nowrap} td.slots span{display:inline-block;vertical-align:bottom;margin-right:14px;text-align:center;color:#B6BEAF;font-size:11px}");
            sb.AppendLine(".ctx{background:#34382C;padding:6px;border-radius:4px;display:inline-block}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<h1>Ability icon review</h1>");
            sb.AppendLine("<p class=\"note\">Rendered from the same drawing code Unity uses. Slot sizes are the UI sizes at 1920x1080; the game shows twice that on a 4K display. Downscaling is an area average, a close stand-in for Unity mip sampling.</p>");
            sb.AppendLine("<table><thead><tr><th>Icon</th><th>128px source</th><th>In-game slots</th><th>What it shows</th></tr></thead><tbody>");
            foreach (var row in rows)
            {
                sb.Append("<tr><td class=\"name\">").Append(Escape(row.Name)).Append("<small>").Append(Escape(row.Family)).Append("</small></td>");
                sb.Append("<td><span class=\"ctx\"><img src=\"").Append(row.FullDataUri).Append("\" width=\"128\" height=\"128\" alt=\"\"></span></td>");
                sb.Append("<td class=\"slots\">");
                foreach (var (size, uri) in row.Slots)
                {
                    sb.Append("<span><span class=\"ctx\"><img src=\"").Append(uri).Append("\" width=\"").Append(size).Append("\" height=\"").Append(size).Append("\" alt=\"\"></span><br>").Append(size).Append("</span>");
                }

                sb.Append("</td><td class=\"summary\">").Append(Escape(row.Summary)).AppendLine("</td></tr>");
            }

            sb.AppendLine("</tbody></table></body></html>");
            return sb.ToString();
        }

        private static string FirstSentence(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return string.Empty;
            }

            string text = description.Replace("\n", " ");
            int cut = text.IndexOf("<b>", StringComparison.Ordinal);
            if (cut > 0)
            {
                text = text[..cut];
            }

            return text.Trim();
        }

        private static string ToDataUri(byte[] png) => "data:image/png;base64," + Convert.ToBase64String(png);

        private static string Slug(string name) => new string(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');

        private static string Escape(string text) => System.Net.WebUtility.HtmlEncode(text);

        private sealed record ReviewRow(string Family, string Name, string Summary, string FullDataUri, List<(int Size, string DataUri)> Slots);
    }
}
