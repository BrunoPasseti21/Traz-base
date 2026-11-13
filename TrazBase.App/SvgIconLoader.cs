using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TrazBase.App;

internal static class SvgIconLoader
{
    private static readonly Regex Tokens = new(@"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);
    private static readonly Regex Translate = new(@"translate\(\s*([-+\d.eE]+)[ ,]+([-+\d.eE]+)\s*\)", RegexOptions.Compiled);

    public static Bitmap Load(string fileName, int size = 24, Color? currentColor = null)
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
            // O atributo fill pode estar no elemento ou herdado da raiz <svg> (caso atualColor dos ícones Claro/Escuro).
            var fill = (string?)element.Attribute("fill") ?? (string?)root.Attribute("fill");
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

            using var brush = new SolidBrush(string.Equals(fill, "currentColor", StringComparison.OrdinalIgnoreCase)
                ? currentColor ?? Color.White
                : ColorTranslator.FromHtml(fill));
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
                case 'Q':
                {
                    var qx = ReadCoordinate(tokens, ref index);
                    var qy = ReadCoordinate(tokens, ref index);
                    var x = ReadCoordinate(tokens, ref index);
                    var y = ReadCoordinate(tokens, ref index);
                    if (relative)
                    {
                        qx += currentX; qy += currentY;
                        x += currentX; y += currentY;
                    }
                    // Conversão de Bézier quadrática para cúbica.
                    var c1x = currentX + 2f / 3f * (qx - currentX);
                    var c1y = currentY + 2f / 3f * (qy - currentY);
                    var c2x = x + 2f / 3f * (qx - x);
                    var c2y = y + 2f / 3f * (qy - y);
                    path.AddBezier(currentX, currentY, c1x, c1y, c2x, c2y, x, y);
                    currentX = x;
                    currentY = y;
                    break;
                }
                case 'A':
                {
                    var rx = ReadCoordinate(tokens, ref index);
                    var ry = ReadCoordinate(tokens, ref index);
                    var rotation = ReadCoordinate(tokens, ref index);
                    var largeArc = ReadCoordinate(tokens, ref index) != 0;
                    var sweep = ReadCoordinate(tokens, ref index) != 0;
                    var x = ReadCoordinate(tokens, ref index);
                    var y = ReadCoordinate(tokens, ref index);
                    if (relative) { x += currentX; y += currentY; }
                    AddEllipticalArc(path, currentX, currentY, rx, ry, rotation, largeArc, sweep, x, y);
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

    // Converte um arco elíptico SVG (comando A) em segmentos de Bézier cúbica, ≤ 90 graus cada.
    private static void AddEllipticalArc(System.Drawing.Drawing2D.GraphicsPath path, float x1, float y1, float rx, float ry, float rotationDegrees, bool largeArc, bool sweep, float x2, float y2)
    {
        if (rx == 0 || ry == 0 || (x1 == x2 && y1 == y2))
        {
            path.AddLine(x1, y1, x2, y2);
            return;
        }
        rx = Math.Abs(rx);
        ry = Math.Abs(ry);
        var phi = rotationDegrees * Math.PI / 180.0;
        var cosPhi = Math.Cos(phi);
        var sinPhi = Math.Sin(phi);

        var halfDx = (x1 - x2) / 2f;
        var halfDy = (y1 - y2) / 2f;
        var x1p = (float)(cosPhi * halfDx + sinPhi * halfDy);
        var y1p = (float)(-sinPhi * halfDx + cosPhi * halfDy);

        // Reduz os raios quando não alcançam o ponto final do arco.
        var lambda = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
        if (lambda > 1)
        {
            var s = (float)Math.Sqrt(lambda);
            rx *= s;
            ry *= s;
        }

        var sign = largeArc != sweep ? 1f : -1f;
        var rx2 = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
        var ry2 = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
        var centerCoeff = sign * (float)Math.Sqrt(Math.Max(0, rx2 / ry2));
        var cxp = (float)(centerCoeff * rx * y1p / ry);
        var cyp = (float)(-centerCoeff * ry * x1p / rx);

        var centerX = (float)(cosPhi * cxp - sinPhi * cyp + (x1 + x2) / 2f);
        var centerY = (float)(sinPhi * cxp + cosPhi * cyp + (y1 + y2) / 2f);

        static float AngleBetween(float ux, float uy, float vx, float vy)
        {
            var dot = ux * vx + uy * vy;
            var len = (float)(Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy)));
            var angle = (float)Math.Acos(Math.Clamp(dot / len, -1f, 1f));
            return ux * vy - uy * vx < 0 ? -angle : angle;
        }

        var theta1 = AngleBetween(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
        var deltaTheta = AngleBetween((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
        if (!sweep && deltaTheta > 0) deltaTheta -= (float)(2 * Math.PI);
        else if (sweep && deltaTheta < 0) deltaTheta += (float)(2 * Math.PI);

        var segmentCount = (int)Math.Ceiling(Math.Abs(deltaTheta) / (Math.PI / 2));
        var delta = deltaTheta / segmentCount;
        var handleLength = (float)(4f / 3f * Math.Tan(delta / 4));

        var angle = theta1;
        var startX = x1;
        var startY = y1;
        for (var i = 0; i < segmentCount; i++)
        {
            var nextAngle = angle + delta;
            var cosA = Math.Cos(angle);
            var sinA = Math.Sin(angle);
            var cosB = Math.Cos(nextAngle);
            var sinB = Math.Sin(nextAngle);

            var endX2 = (float)(centerX + rx * cosB * cosPhi - ry * sinB * sinPhi);
            var endY2 = (float)(centerY + rx * cosB * sinPhi + ry * sinB * cosPhi);

            var c1x = (float)(startX + handleLength * (-rx * sinA * cosPhi - ry * cosA * sinPhi));
            var c1y = (float)(startY + handleLength * (-rx * sinA * sinPhi + ry * cosA * cosPhi));
            var c2x = (float)(endX2 - handleLength * (-rx * sinB * cosPhi - ry * cosB * sinPhi));
            var c2y = (float)(endY2 - handleLength * (-rx * sinB * sinPhi + ry * cosB * cosPhi));

            path.AddBezier((float)startX, (float)startY, c1x, c1y, c2x, c2y, endX2, endY2);
            angle = nextAngle;
            startX = endX2;
            startY = endY2;
        }
    }

    private static float ReadCoordinate(string[] tokens, ref int index)
    {
        if (index >= tokens.Length || char.IsLetter(tokens[index][0]))
            throw new InvalidDataException("Dados SVG incompletos.");
        return ParseNumber(tokens[index++]);
    }

    private static float ParseNumber(string value) => float.Parse(value, CultureInfo.InvariantCulture);
}
