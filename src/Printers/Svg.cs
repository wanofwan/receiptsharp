/*
Copyright 2025 Open Foodservice System Consortium

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

// QR Code is a registered trademark of DENSO WAVE INCORPORATED.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReceiptSharp.Printers
{
    //
    // SVG
    //
    class Svg : Base
    {
        private string ReceiptId;
        private int SvgWidth;
        private int SvgHeight;
        private string SvgContent;
        private int LineMargin;
        private int LineAlign;
        private int LineWidth;
        private int LineHeight;
        private string TextElement;
        private Dictionary<string, string> TextAttributes;
        private double TextPosition;
        private int TextScale;
        private string TextEncoding;
        private int FeedMinimum;
        // printer configuration
        private bool Spacing;
        // start printing:
        public override string Open(PrintOption printer)
        {
            ReceiptId = Guid.NewGuid().ToString();
            SvgWidth = printer.Cpl * CharWidth;
            SvgHeight = 0;
            SvgContent = "";
            LineMargin = 0;
            LineAlign = 0;
            LineWidth = printer.Cpl;
            LineHeight = 1;
            TextElement = "";
            TextAttributes = new Dictionary<string, string>();
            TextPosition = 0.0;
            TextScale = 1;
            TextEncoding = printer.Encoding;
            FeedMinimum = (int)Math.Round(CharWidth * (printer.Spacing ? 2.5 : 2), MidpointRounding.AwayFromZero);
            Spacing = printer.Spacing;
            return "";
        }
        // finish printing:
        public override string Close()
        {
            string font = "monospace";
            int size = CharWidth * 2;
            string style = "";
            string lang = "";            
            switch (TextEncoding) 
            {
                case "cp932":
                case "shiftjis":
                    font = "'BIZ UDGothic', 'MS Gothic', 'San Francisco', 'Osaka-Mono', monospace";
                    style = "@import url(\"https://fonts.googleapis.com/css2?family=BIZ+UDGothic&display=swap\");";
                    lang = "ja";
                    break;
                case "cp936":
                case "gb18030":
                    size -= 2;
                    lang = "zh-Hans";
                    break;
                case "cp949":
                case "ksc5601":
                    size -= 2;
                    lang = "ko";
                    break;
                case "cp950":
                case "big5":
                    size -= 2;
                    lang = "zh-Hant";
                    break;
                case "tis620":
                    font = "'Sarabun', monospace";
                    size -= 4;
                    style = "@import url(\"https://fonts.googleapis.com/css2?family=Sarabun&display=swap\");";
                    lang = "th";
                    break;
                default:
                    font = "'Courier Prime', 'Courier New', 'Courier', monospace";
                    size -= 2;
                    style = "@import url(\"https://fonts.googleapis.com/css2?family=Courier+Prime&display=swap\");";
                    break;
            }
            if (style.Length > 0)
            {
                style = $"<style type=\"text/css\"><![CDATA[{style}]]></style>";
            }
            if (lang.Length > 0)
            {
                lang = " xml:lang=\"" + lang + "\"";
            }
            return $"<svg width=\"{SvgWidth}px\" height=\"{SvgHeight}px\" viewBox=\"0 0 {SvgWidth} {SvgHeight}\" preserveAspectRatio=\"xMinYMin meet\" " +
                $"xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" version=\"1.1\">{style}" +
                $"<defs><filter id=\"receipt-{ReceiptId}\" x=\"0\" y=\"0\" width=\"100%\" height=\"100%\"><feFlood flood-color=\"#000\"/><feComposite in2=\"SourceGraphic\" operator=\"out\"/></filter></defs>" +
                $"<g font-family=\"{font}\" fill=\"#000\" font-size=\"{size}\" dominant-baseline=\"text-after-edge\" text-anchor=\"middle\"{lang}>{SvgContent}</g></svg>\n";
        }
        // set print area:
        public override string Area(int left, int width, int right)
        {
            LineMargin = left;
            LineWidth = width;
            return "";
        }
        // set line alignment:
        public override string Align(int align)
        {
            LineAlign = align;
            return "";
        }
        // set absolute print position:
        public override string Absolute(double position)
        {
            TextPosition = position;
            return "";
        }
        // set relative print position:
        public override string Relative(double position)
        {
            TextPosition += position;
            return "";
        }
        // print horizontal rule:
        public override string Hr(int width)
        {
            int w = CharWidth;
            string path = $"<path d=\"M0,{w}h{w * width}\" fill=\"none\" stroke=\"#000\" stroke-width=\"2\"/>";
            SvgContent += $"<g transform=\"translate({LineMargin * w},{SvgHeight})\">{path}</g>";
            return "";
        }
        // print vertical rules:
        public override string Vr(int[] widths, int height)
        {
            int w = CharWidth, u = w / 2, v = (w + w) * height;
            string path = $"<path d=\"{widths.Aggregate($"M{u},0v{v}", (a, width) => $"{a}m{w * width + w},{-v}v{v}")}\" fill=\"none\" stroke=\"#000\" stroke-width=\"2\"/>";
            SvgContent += $"<g transform=\"translate({LineMargin * w},{SvgHeight})\">{path}</g>";
            return "";
        }
        // start rules:
        public override string VrStart(int[] widths)
        {
            int w = CharWidth, u = w / 2;
            string path = $"<path d=\"{Regex.Replace(widths.Aggregate($"M{u},{w + w}v{-u}q0,{-u},{u},{-u}", (a, width) => $"{a}h{w * width}h{u}v{w}m0,{-w}h{u}"), @"h\d+v\d+m0,-\d+h\d+$", $"q{u},0,{u},{u}v{u}")}\" fill=\"none\" stroke=\"#000\" stroke-width=\"2\"/>";
            SvgContent += $"<g transform=\"translate({LineMargin * w},{SvgHeight})\">{path}</g>";
            return "";
        }
        // stop rules:
        public override string VrStop(int[] widths)
        {
            int w = CharWidth, u = w / 2;
            string path = $"<path d=\"{Regex.Replace(widths.Aggregate($"M{u},0v{u}q0,{u},{u},{u}", (a, width) => $"{a}h{w * width}h{u}v{-w}m0,{w}h{u}"), @"h\d+v-\d+m0,\d+h\d+$", $"q{u},0,{u},{-u}v{-u}")}\" fill=\"none\" stroke=\"#000\" stroke-width=\"2\"/>";
            SvgContent += $"<g transform=\"translate({LineMargin * w},{SvgHeight})\">{path}</g>";
            return "";
        }
        // print vertical and horizontal rules:
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            int w = CharWidth, u = w / 2;
            string path1 = $"<path d=\"{Regex.Replace(widths1.Aggregate($"M{u},0{(dl > 0 ? $"v{u}q0,{u},{u},{u}" : $"v{w}h{u}")}", (a, width) => $"{a}h{w * width}h{u}v{-w}m0,{w}h{u}"), @"h\d+v-\d+m0,\d+h\d+$", dr < 0 ? $"q{u},0,{u},{-u}v{-u}" : $"h{u}v{-w}")}\" fill=\"none\" stroke=\"#000\" stroke-width=\"2\"/>";
            SvgContent += $"<g transform=\"translate({(LineMargin + Math.Max(-dl, 0)) * w},{SvgHeight})\">{path1}</g>";
            string path2 = $"<path d=\"{Regex.Replace(widths2.Aggregate($"M{u},{w + w}{(dl < 0 ? $"v{-u}q0,{-u},{u},{-u}" : $"v{-w}h{u}")}", (a, width) => $"{a}h{w * width}h{u}v{w}m0,{-w}h{u}"), @"h\d+v\d+m0,-\d+h\d+$", dr > 0 ? $"q{u},0,{u},{u}v{u}" : $"h{u}v{w}")}\" fill=\"none\" stroke=\"#000\" stroke-width=\"2\"/>";
            SvgContent += $"<g transform=\"translate({(LineMargin + Math.Max(dl, 0)) * w},{SvgHeight})\">{path2}</g>";
            return "";
        }
        // set line spacing and feed new line:
        public override string VrLf(bool vr)
        {
            FeedMinimum = (int)Math.Round(CharWidth * (!vr && Spacing ? 2.5 : 2), MidpointRounding.AwayFromZero);
            return Lf();
        }
        // cut paper:
        public override string Cut()
        {
            string path = $"<path d=\"M12,12.5l-7.5,-3a2,2,0,1,1,.5,0M12,11.5l-7.5,3a2,2,0,1,0,.5,0\" fill=\"none\" stroke=\"#000\" stroke-width=\"1\"/><path d=\"M12,12l10,-4q-1,-1,-2.5,-1l-10,4v2l10,4q1.5,0,2.5,-1z\" fill=\"#000\"/><path d=\"M24,12h{SvgWidth - 24}\" fill=\"none\" stroke=\"#000\" stroke-width=\"2\" stroke-dasharray=\"2\"/>";
            SvgContent += $"<g transform=\"translate(0,{SvgHeight})\">{path}</g>";
            return Lf();
        }
        // underline text:
        public override string Ul()
        {
            TextAttributes["text-decoration"] = "underline";
            return "";
        }
        // emphasize text:
        public override string Em()
        {
            TextAttributes["stroke"] = "#000";
            return "";
        }
        // invert text:
        public override string Iv()
        {
            TextAttributes["filter"] = $"url(#receipt-{ReceiptId})";
            return "";
        }
        // scale up text:
        public override string Wh(int wh)
        {
            int w = wh < 2 ? wh + 1 : wh - 1;
            int h = wh < 3 ? wh : wh - 1;
            TextAttributes["transform"] = $"scale({w},{h})";
            LineHeight = Math.Max(LineHeight, h);
            TextScale = w;
            return "";
        }
        // cancel text decoration:
        public override string Normal()
        {
            TextAttributes.Clear();
            TextScale = 1;
            return "";
        }
        private readonly Dictionary<string, string> XmlEscape = new Dictionary<string, string>()
        {
            { " ", "&#xa0;" }, { "&", "&amp;" }, { "<", "&lt;" }, {">", "&gt;" }
        };
        // print text:
        public override string Text(string text, string encoding)
        {
            double p = TextPosition;
            string tspan = ArrayFrom(text, encoding).Aggregate("", (a, c) => {
                int q = MeasureText(c, encoding) * TextScale;
                double r = (p + p + q) * CharWidth / (TextScale + TextScale);
                p += q;
                return $"{a}<tspan x=\"{r}\">{Regex.Replace(c, @"[ &<>]", m => XmlEscape[m.Value])}</tspan>";
            });
            string attr = TextAttributes.Aggregate("", (a, c) => $"{a} {c.Key}=\"{c.Value}\"");
            TextElement += $"<text{attr}>{tspan}</text>";
            TextPosition += MeasureText(text, encoding) * TextScale;
            return "";
        }
        // feed new line:
        public override string Lf()
        {
            int h = LineHeight * CharWidth * 2;
            if (TextElement.Length > 0)
            {
                SvgContent += $"<g transform=\"translate({LineMargin * CharWidth},{SvgHeight + h})\">{TextElement}</g>";
            }
            SvgHeight += Math.Max(h, FeedMinimum);
            LineHeight = 1;
            TextElement = "";
            TextPosition = 0.0;
            return "";
        }
        // insert commands:
        public override string Command(string command)
        {
            return "";
        }
        // print image:
        public override string Image(string image)
        {
            byte[] png = Convert.FromBase64String(image);
            byte[] header = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d, (byte)'I', (byte)'H', (byte)'D', (byte)'R' };
            int imgWidth = 0;
            int imgHeight = 0;
            if (png.Length >= 24 && png.Take(16).SequenceEqual(header))
            {
                imgWidth = png[16] << 24 | png[17] << 16 | png[18] << 8 | png[19];
                imgHeight = png[20] << 24 | png[21] << 16 | png[22] << 8 | png[23];
            }
            string imgData = $"<image xlink:href=\"data:image/png;base64,{image}\" x=\"0\" y=\"0\" width=\"{imgWidth}\" height=\"{imgHeight}\"/>";
            int margin = LineMargin * CharWidth + (LineWidth * CharWidth - imgWidth) * LineAlign / 2;
            SvgContent += $"<g transform=\"translate({margin},{SvgHeight})\">{imgData}</g>";
            SvgHeight += imgHeight;
            return "";
        }
        // print QR Code:
        public override string Qrcode(SymbolData symbol, string encoding)
        {
            if (symbol.Data.Length > 0)
            {
                byte[,] matrix = QRCodeGenerator.Generate(symbol);
                int w = matrix.GetLength(1);
                int h = w;
                int c = symbol.Cell;
                string path = "<path d=\"";
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (matrix[y, x] == 1)
                        {
                            path += $"M{x * c},{y * c}l{c},0 0,{c} -{c},0 0,-{c}z ";
                        }
                    }
                }
                path += "\" stroke=\"transparent\" fill=\"black\"/>";
                int margin = LineMargin * CharWidth + (LineWidth * CharWidth - w * c) * LineAlign / 2;
                SvgContent += $"<g transform=\"translate({margin},{SvgHeight})\">{path}</g>";
                SvgHeight += h * c;
            }
            return "";
        }
        // print barcode:
        public override string Barcode(SymbolData symbol, string encoding)
        {

            BarcodeForm bar = BarcodeGenerator.Generate(symbol);
            int h = bar.Height;
            if (bar.Length > 0)
            {
                int width = bar.Length;
                int height = h + (bar.Hri ? CharWidth * 2 + 2 : 0);
                // draw barcode
                string path = "<path d=\"";
                int i = 0;
                bar.Widths.Aggregate(0, (p, w) => {
                    if (i++ % 2 == 1)
                    {
                        path += $"M{p},0h{w}v{h}h{-w}z";
                    }
                    return p + w;
                });
                path += "\" fill=\"#000\"/>";
                // draw human readable interpretation
                if (bar.Hri)
                {
                    int m = (width - (MeasureText(bar.Text, encoding) - 1) * CharWidth) / 2;
                    i = 0;
                    string tspan = ArrayFrom(bar.Text, encoding).Aggregate("", (a, c) => $"{a}<tspan x=\"{m + CharWidth * i++}\">{Regex.Replace(c.ToString(), @"[ &<>]", r => XmlEscape[r.Value])}</tspan>");
                    path += $"<text y=\"{height}\">{tspan}</text>";
                }
                int margin = LineMargin * CharWidth + (LineWidth * CharWidth - width) * LineAlign / 2;
                SvgContent += $"<g transform=\"translate({margin},{SvgHeight})\">{path}</g>";
                SvgHeight += height;
            }
            return "";
        }
    }
}
