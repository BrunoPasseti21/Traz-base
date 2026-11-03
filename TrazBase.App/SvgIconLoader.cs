using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TrazBase.App;

internal static class SvgIconLoader
{
    private static readonly Regex Tokens = new(@"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);
    private static readonly Regex Translate = new(@"translate\(\s*([-+\d.eE]+)[ ,]+([-+\d.eE]+)\s*\)", RegexOptions.Compiled);

    public static Bitmap Load(string fileName, int size = 24)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Imagens", fileName);
        var document = XDocument.Load(path);
        var root = document.Root ?? throw new InvalidDataException($"SVG inválido: {fileName}");
        var viewWidth = ReadDimension(root, "width", 500);
        var viewHeight = ReadDimension(root, "height", 500);
        var bitmap = new Bitmap(size, size);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.ScaleTransform(size / viewWidth, size / viewHeight);

        XNamespace svgNamespace = "http://www.w3.org/2000/svg";
        foreach (var element in root.Elements(svgNamespace + "path"))
        {
            var data = (string?)element.Attribute("d");
            var fill = (string?)element.Attribute("fill");
            if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(fill) || fill == "none") continue;

            using var pathGeometry = ParsePath(data);
            var transform = (string?)element.Attribute("transform");
            var translation = transform is null ? Match.Empty : Translate.Match(transform);
            if (translation.Success)
            {
                var offsetX = ParseNumber(translation.Groups[1].Value);
                var offsetY = ParseNumber(translation.Groups[2].Value);
                using var matrix = new Matrix();
                matrix.Translate(offsetX, offsetY);
                pathGeometry.Transform(matrix);
            }

            using var brush = new SolidBrush(ColorTranslator.FromHtml(fill));
            graphics.FillPath(brush, pathGeometry);
        }

        return bitmap;
    }

    private static float ReadDimension(XElement root, string attribute, float fallback)
    {
        var value = (string?)root.Attribute(attribute);
        if (value is null) return fallback;
        var match = Regex.Match(value, @"[-+\d.]+");
        return match.Success && float.TryParse(match.Value, CultureInfo.InvariantCulture, out var dimension) && dimension > 0
            ? dimension
            : fallback;
    }

    private static GraphicsPath ParsePath(string data)
    {
        var tokens = Tokens.Matches(data).Select(match => match.Value).ToArray();
        var path = new GraphicsPath(FillMode.Winding);
        var index = 0;
        var command = ' ';
        var currentX = 0f;
        var currentY = 0f;
        var startX = 0f;
        var startY = 0f;

        while (index < tokens.Length)
        {
            if (char.IsLetter(tokens[index][0])) command = tokens[index++][0];
            var relative = char.IsLower(command);
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                {
                    var x = ReadCoordinate(tokens, ref index);
                    var y = ReadCoordinate(tokens, ref index);
                    if (relative) { x += currentX; y += currentY; }
                    currentX = startX = x;
                    currentY = startY = y;
                    path.StartFigure();
                    command = relative ? 'l' : 'L';
                    break;
                }
                case 'L':
                {
                    var x = ReadCoordinate(tokens, ref index);
                    var y = ReadCoordinate(tokens, ref index);
                    if (relative) { x += currentX; y += currentY; }
                    path.AddLine(currentX, currentY, x, y);
                    currentX = x;
                    currentY = y;
                    break;
                }
                case 'H':
                {
                    var x = ReadCoordinate(tokens, ref index);
                    if (relative) x += currentX;
                    path.AddLine(currentX, currentY, x, currentY);
                    currentX = x;
                    break;
                }
                case 'V':
                {
                    var y = ReadCoordinate(tokens, ref index);
                    if (relative) y += currentY;
                    path.AddLine(currentX, currentY, currentX, y);
                    currentY = y;
                    break;
                }
                case 'C':
                {
                    var x1 = ReadCoordinate(tokens, ref index);
                    var y1 = ReadCoordinate(tokens, ref index);
                    var x2 = ReadCoordinate(tokens, ref index);
                    var y2 = ReadCoordinate(tokens, ref index);
                    var x = ReadCoordinate(tokens, ref index);
                    var y = ReadCoordinate(tokens, ref index);
                    if (relative)
                    {
                        x1 += currentX; y1 += currentY;
                        x2 += currentX; y2 += currentY;
                        x += currentX; y += currentY;
                    }
                    path.AddBezier(currentX, currentY, x1, y1, x2, y2, x, y);
                    currentX = x;
                    currentY = y;
                    break;
                }
                case 'Z':
                    path.CloseFigure();
                    currentX = startX;
                    currentY = startY;
                    command = ' ';
                    break;
                default:
                    path.Dispose();
                    throw new NotSupportedException($"Comando SVG não suportado: {command}");
            }
        }

        return path;
    }

    private static float ReadCoordinate(string[] tokens, ref int index)
    {
        if (index >= tokens.Length || char.IsLetter(tokens[index][0]))
            throw new InvalidDataException("Dados SVG incompletos.");
        return ParseNumber(tokens[index++]);
    }

    private static float ParseNumber(string value) => float.Parse(value, CultureInfo.InvariantCulture);
}
