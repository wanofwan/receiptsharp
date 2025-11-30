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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using ReceiptSharp.Printers;
using System.Threading.Tasks;
//using System.Threading.Tasks;
//using Microsoft.Playwright;
//using PuppeteerSharp;
//using PuppeteerSharp.BrowserData;

namespace ReceiptSharp
{
    public class Receipt
    {
        private string Markdown;
        private PrintOption Options;

        /**
         * Create instance.
         */
        public Receipt(string markdown, string options = "")
        {
            Markdown = markdown;
            // parse parameter
            Options = ParseOption(options);
        }

        /**
         * Return string representing this object.
         * @returns {string} receipt markdown
         */
        public override string ToString()
        {
            return Markdown;
        }

        /**
         * Convert receipt markdown to text.
         * @returns {string} text
         */
        public string ToText()
        {
            PrintOption options = Options.Clone();
            options.Command = new PlainText();
            return Transform(Markdown, options);
        }

        /**
         * Convert receipt markdown to SVG.
         * @returns {string} SVG
         */
        public string ToSvg()
        {
            PrintOption options = Options.Clone();
            options.Command = new Svg();
            return Transform(Markdown, options);
        }

        /**
         * Convert receipt markdown to PNG.
         * @returns {string} PNG as data URL
         */
        public string ToPng()
        {
            return Base64Png(ToSvg()).Result;
        }

        /**
         * Convert receipt markdown to printer commands.
         * @returns {string} printer commands
         */
        public byte[] ToCommand()
        {
            PrintOption options = Options.Clone();
            if (options.AsImage)
            {
                if (options.Landscape)
                {
                    options.Cpl = 48;
                    options.Margin = 0;
                    options.MarginRight = 0;
                }
                string png = Base64Png(ToSvg(), Options).Result;
                options.Command = CreatePrinter(options.Type);
                return Encoding.GetEncoding("ISO-8859-1").GetBytes(Transform("{i:" + Regex.Replace(png, @"^data:.*,", "") + "}", options));
            }
            if (options.Landscape && Regex.IsMatch(options.Type, @"^(escpos|epson|sii|citizen|star[sm]bcs2?)$"))
            {
                options.Type += "90";
            }
            options.Command = CreatePrinter(options.Type);
            return Encoding.GetEncoding("ISO-8859-1").GetBytes(Transform(Markdown, options));
        }

        private class Column
        {
            public LineAlign Align;
            public Dictionary<string, string> Property;
            public string Error;
            public string Hr;
            public string Vr;
            public SymbolData Code;
            public string Image;
            public string Command;
            public string Comment;
            public string[] Text;
            public bool Wrap;
            public int Border;
            public int Width;
            public LineAlign Alignment;
        }
        private class LineState
        {
            public bool Wrap;
            public int Border;
            public int[] Width;
            public LineAlign Align;
            public SymbolData Option;
            public LineMode Line;
            public RuledLine Rules;
        }
        private class RuledLine
        {
            public int Left;
            public int Width;
            public int Right;
            public int[] Widths;
        }
        private class WrappedText
        {
            public string[] Data;
            public double Margin;
            public int Height;
        }
        private enum LineAlign
        {
            Left = 0,
            Center = 1,
            Right = 2
        }
        private enum Border
        {
            Line = -1,
            None = 0,
            Space = 1
        }
        private enum LineMode
        {
            Waiting,
            Ready,
            Running,
            Horizontal
        }
        private Dictionary<string, string> Encodings = new Dictionary<string, string>()
        {
            { "ja", "shiftjis" }, { "ko", "ksc5601" }, { "zh", "gb18030" }, { "zh-hans", "gb18030" }, { "zh-hant", "big5" }, { "th", "tis620" }
        };
        // abbreviations
        private Dictionary<string, string> Abbr = new Dictionary<string, string>()
        {
            { "a", "align" }, { "b", "border" }, { "c", "code" }, { "i", "image" }, { "o", "option" }, { "t", "text" }, { "w", "width" }, { "x", "command" }, { "_", "comment" }
        };
        private LineState State;

        private string Transform(string markdown, PrintOption printer)
        {
            // initialize state variables
            State = new LineState()
            {
                Wrap = true,
                Border = 1,
                Width = new int[0],
                Align = LineAlign.Center,
                Option = new SymbolData() { Type = "code128", Width = 2, Height = 72, Hri = false, Cell = 3, Level = "l", QuietZone = false },
                Line = LineMode.Waiting,
                Rules = new RuledLine() { Left = 0, Width = 0, Right = 0, Widths = new int[0] }
            };
            // append commands to start printing
            string result = printer.Command.Open(printer);
            // strip bom
            if (markdown[0] == '\ufeff')
            {
                markdown = markdown.Substring(1);
            }
            // parse each line and generate commands
            List<string> res = new List<string>();
            foreach (string line in Regex.Split(markdown.Normalize(), @"\n|\r\n|\r"))
            {
                res.Add(CreateLine(ParseLine(line), printer));
            }
            // if rules is not finished
            switch (State.Line)
            {
                case LineMode.Ready:
                    // set state to cancel rules
                    State.Line = LineMode.Waiting;
                    break;
                case LineMode.Running:
                case LineMode.Horizontal:
                    // append commands to stop rules
                    res.Add(printer.Command.Normal() +
                        printer.Command.Area(State.Rules.Left, State.Rules.Width, State.Rules.Right) +
                        printer.Command.Align(0) +
                        printer.Command.VrStop(State.Rules.Widths) +
                        printer.Command.VrLf(false));
                    State.Line = LineMode.Waiting;
                    break;
                default:
                    break;
            }
            // flip upside down
            if (printer.UpsideDown)
            {
                res.Reverse();
            }
            // append commands
            result += string.Join("", res.ToArray());
            // append commands to end printing
            result += printer.Command.Close();
            return result;
        }

        private Column[] ParseLine(string columns)
        {
            // trim whitespace
            string s = Regex.Replace(columns, @"^[\t ]+|[\t ]+$", "");
            // convert escape characters ('\\', '\{', '\|', '\}') to hexadecimal escape characters
            s = Regex.Replace(s, @"\\[\\{|}]", match => "\\x" + ((int)match.Value[1]).ToString("X2"));
            // append a space if the first column does not start with '|' and is right-aligned
            s = Regex.Replace(s, @"^[^|]*[^\t |]\|", " $&");
            // append a space if the last column does not end with '|' and is left-aligned
            s = Regex.Replace(s, @"\|[^\t |][^|]*$", "$& ");
            // remove '|' at the beginning of the first column
            s = Regex.Replace(s, @"^\|(.*)$", "$1");
            // remove '|' at the end of the last column
            s = Regex.Replace(s, @"^(.*)\|$", "$1");

            // separate text with '|'
            string[] t = s.Split('|');
            // parse columns
            List<Column> line = t.Select((c, i) => ParseColumn(c, i, t.Length)).ToList();

            // if the line is text and the width property is not 'auto'
            if (line.All(el => el.Text != null) && State.Width.Length > 0)
            {
                // if the line has fewer columns
                while (line.Count < State.Width.Length)
                {
                    // fill empty columns
                    line.Add(new Column()
                    {
                        Align = LineAlign.Center,
                        Text = new string[] { "" },
                        Wrap = State.Wrap,
                        Border = State.Border,
                        Width = State.Width[line.Count]
                    });
                }
            }
            return line.ToArray();
        }

        private Column ParseColumn(string column, int index, int length)
        {
            // parsed column object
            Column result = new Column();
            // trim whitespace
            string element = Regex.Replace(column, @"^[\t ]+|[\t ]+$", "");
            // determin alignment from whitespaces around column text
            result.Align = (LineAlign)(1 + (Regex.IsMatch(column, @"^[\t ]") ? 1 : 0) - (Regex.IsMatch(column, @"[\t ]$") ? 1 : 0));
            // parse properties
            if (Regex.IsMatch(element, @"^\{[^{}]*\}$"))
            {
                // extract members
                var property = new Dictionary<string, string>();
                // trim property delimiters
                string s = element.Substring(1, element.Length - 2);
                // convert escape character ('\;') to hexadecimal escape characters
                s = Regex.Replace(s, @"\\;", "\\x3b");
                // separate property with ';'
                string[] members = s.Split(';');
                // parse members
                foreach (string member in members)
                {
                    // parse key-value pair
                    if (!Regex.IsMatch(member, @"^[\t ]*$") &&
                        Regex.Replace(member, @"^[\t ]*([A-Za-z_]\w*)[\t ]*:[\t ]*([^\t ].*?)[\t ]*$", match =>
                        {
                            string key = match.Groups[1].Value;
                            string value = match.Groups[2].Value;
                            key = Regex.Replace(key, @"^[abciotwx_]$", m => Abbr[m.Value]);
                            value = Regex.Replace(value, @"\\n", "\n");
                            property[key] = ParseEscape(value);
                            return "";
                        }) == member)
                    {
                        // invalid members
                        result.Error = member;
                    }
                }
                result.Property = property;
                // if the column is single
                if (length == 1)
                {
                    // parse text property
                    if (property.ContainsKey("text"))
                    {
                        string c = property["text"].ToLower();
                        State.Wrap = !Regex.IsMatch(c, @"^nowrap$");
                    }
                    // parse border property
                    if (property.ContainsKey("border"))
                    {
                        string c = property["border"].ToLower();
                        int previous = State.Border;
                        State.Border = Regex.IsMatch(c, @"^(line|space|none)$") ? (int)Enum.Parse(typeof(Border), c, true) : Regex.IsMatch(c, @"^\d+$") ? int.Parse(c) : 1;
                        // start rules
                        if (previous >= 0 && State.Border < 0)
                        {
                            result.Vr = "+";
                        }
                        // stop rules
                        if (previous < 0 && State.Border >= 0)
                        {
                            result.Vr = "-";
                        }
                    }
                    // parse width property
                    if (property.ContainsKey("width"))
                    {
                        string[] widths = Regex.Split(property["width"].ToLower(), @"[\t ]+|,");
                        State.Width = Array.Exists(widths, t => Regex.IsMatch(t, @"^auto$")) ? new int[0] : widths.Select(c => Regex.IsMatch(c, @"^\*$") ? -1 : Regex.IsMatch(c, @"^\d+$") ? int.Parse(c) : 0).ToArray();
                    }
                    // parse align property
                    if (property.ContainsKey("align"))
                    {
                        string c = property["align"].ToLower();
                        State.Align = (LineAlign)Enum.Parse(typeof(LineAlign), Regex.IsMatch(c, @"^(left|center|right)$") ? c : "center", true);
                    }
                    // parse option property
                    if (property.ContainsKey("option"))
                    {
                        string[] options = Regex.Split(property["option"].ToLower(), @"[\t ]+|,");
                        State.Option = new SymbolData()
                        {
                            Type = Array.Find(options, t => Regex.IsMatch(t, @"^(upc|ean|jan|code39|itf|codabar|nw7|code93|code128|qrcode)$")) ?? "code128",
                            Width = int.Parse(Array.Find(options, t => Regex.IsMatch(t, @"^\d+$") && int.Parse(t) >= 2 && int.Parse(t) <= 4) ?? "2"),
                            Height = int.Parse(Array.Find(options, t => Regex.IsMatch(t, @"^\d+$") && int.Parse(t) >= 24 && int.Parse(t) <= 240) ?? "72"),
                            Hri = Array.Exists(options, t => Regex.IsMatch(t, @"^hri$")),
                            Cell = int.Parse(Array.Find(options, t => Regex.IsMatch(t, @"^\d+$") && int.Parse(t) >= 3 && int.Parse(t) <= 8) ?? "3"),
                            Level = Array.Find(options, t => Regex.IsMatch(t, @"^[lmqh]$")) ?? "l",
                            QuietZone = false
                        };
                    }
                    // parse code property
                    if (property.ContainsKey("code"))
                    {
                        result.Code = new SymbolData()
                        {
                            Data = property["code"],
                            Type = State.Option.Type,
                            Width = State.Option.Width,
                            Height = State.Option.Height,
                            Hri = State.Option.Hri,
                            Cell = State.Option.Cell,
                            Level = State.Option.Level,
                            QuietZone = false
                        };
                    }
                    // parse image property
                    if (property.ContainsKey("image"))
                    {
                        string c = Regex.Replace(property["image"], @"=.*|[^A-Za-z0-9+/]", "");
                        switch (c.Length % 4)
                        {
                            case 1:
                                result.Image = c.Substring(0, c.Length - 1);
                                break;
                            case 2:
                                result.Image = c + "==";
                                break;
                            case 3:
                                result.Image = c + "=";
                                break;
                            default:
                                result.Image = c;
                                break;
                        }
                    }
                    // parse command property
                    if (property.ContainsKey("command"))
                    {
                        result.Command = property["command"];
                    }
                    // parse comment property
                    if (property.ContainsKey("comment"))
                    {
                        result.Comment = property["comment"];
                    }
                }
            }
            // remove invalid property delimiter
            else if (Regex.IsMatch(element, @"[{}]"))
            {
                result.Error = element;
            }
            // parse horizontal rule of special character in text
            else if (length == 1 && Regex.IsMatch(element, @"^-+$|^=+$"))
            {
                result.Hr = element.LastOrDefault().ToString();
            }
            // parse text
            else
            {
                // remove control codes and hexadecimal control codes
                string s = Regex.Replace(element, @"[\x00-\x1f\x7f]|\\x[01][\dA-Fa-f]|\\x7[Ff]", "");
                // convert escape characters ('\-', '\=', '\_', '\"', \`', '\^', '\~') to hexadecimal escape characters
                s = Regex.Replace(s, @"\\[-=_""`^~]", match => "\\x" + ((int)match.Value[1]).ToString("X2"));
                // convert escape character ('\n') to LF
                s = Regex.Replace(s, @"\\n", "\n");
                // convert escape character ('~') to space
                s = Regex.Replace(s, @"~", " ");
                // separate text with '_', '"', '`', '^'(1 or more), '\n'
                string[] t = Regex.Split(s, @"([_""`\n]|\^+)");
                // convert escape characters to normal characters
                result.Text = t.Select(text => ParseEscape(text)).ToArray();
            }
            // set current text wrapping
            result.Wrap = State.Wrap;
            // set current column border
            result.Border = State.Border;
            // set current column width
            if (State.Width.Length == 0)
            {
                // set '*' for all columns when the width property is 'auto'
                result.Width = -1;
            }
            else if (result.Text != null)
            {
                // text: set column width
                result.Width = index < State.Width.Length ? State.Width[index] : 0;
            }
            else if (State.Width.Any(c => c < 0))
            {
                // image, code, command: when the width property includes '*', set '*'
                result.Width = -1;
            }
            else
            {
                // image, code, command: when the width property does not include '*', set the sum of column width and border width
                int[] w = State.Width.Where(c => c > 0).ToArray();
                result.Width = w.Length > 0 ? w.Aggregate(result.Border < 0 ? w.Length + 1 : (w.Length - 1) * result.Border, (a, c) => a + c) : 0;
            }
            // set line alignment
            result.Alignment = State.Align;
            return result;
        }

        private string ParseEscape(string s)
        {
            // remove invalid escape sequences
            s = Regex.Replace(s, @"\\$|\\x(.?$|[^\dA-Fa-f].|.[^\dA-Fa-f])", "");
            // ignore invalid escape characters
            s = Regex.Replace(s, @"\\[^x]", "");
            // convert hexadecimal escape characters to normal characters
            s = Regex.Replace(s, @"\\x([\dA-Fa-f]{2})", match => $"{(char)int.Parse(match.Groups[1].Value, NumberStyles.HexNumber)}");
            return s;
        }

        private string CreateLine(Column[] line, PrintOption printer)
        {
            List<string> result = new List<string>();
            // text or property
            bool text = line.All(el => el.Text != null);
            // the first column
            Column column = line[0];
            // remove zero width columns
            List<Column> columns = line.Where(el => el.Width != 0).ToList();
            // remove overflowing columns
            if (text)
            {
                columns = columns.Take(column.Border < 0 ? (printer.Cpl - 1) / 2 : (printer.Cpl + column.Border) / (column.Border + 1)).ToList();
            }
            // fixed columns
            var f = line.Where(el => el.Width > 0);
            // variable columns
            var g = line.Where(el => el.Width < 0);
            // reserved width
            int u = f.Aggregate(0, (a, el) => a + el.Width);
            // free width
            int v = printer.Cpl - u;
            // subtract border width from free width
            if (text && columns.Count > 0)
            {
                v -= column.Border < 0 ? columns.Count + 1 : (columns.Count - 1) * column.Border;
            }
            // number of variable columns
            int n = g.Count();
            // reduce the width of fixed columns when reserved width is too many
            while (n > v)
            {
                f.Aggregate((a, el) => a.Width > el.Width ? a : el).Width--;
                v++;
            }
            // allocate free width among variable columns
            if (n > 0)
            {
                int i = 0;
                foreach (Column el in g)
                {
                    el.Width = (v + i) / n;
                    i++;
                }
                v = 0;
            }
            // print area
            int left = v * (int)column.Alignment / 2;
            int width = printer.Cpl - v;
            int right = v - left;
            // process text
            if (text)
            {
                // wrap text
                var cols = columns.Select(col => WrapText(col, printer));
                // vertical line spacing
                int[] widths = columns.Select(col => col.Width).ToArray();
                // rules
                switch (State.Line)
                {
                    case LineMode.Ready:
                        // append commands to start rules
                        result.Add(printer.Command.Normal() +
                            printer.Command.Area(left, width, right) +
                            printer.Command.Align(0) +
                            printer.Command.VrStart(widths) +
                            printer.Command.VrLf(true));
                        State.Line = LineMode.Running;
                        break;
                    case LineMode.Horizontal:
                        // append commands to print horizontal rule
                        int m = left - State.Rules.Left;
                        int w = width - State.Rules.Width;
                        int l = Math.Min(left, State.Rules.Left);
                        int r = Math.Min(right, State.Rules.Right);
                        result.Add(printer.Command.Normal() +
                            printer.Command.Area(l, printer.Cpl - l - r, r) +
                            printer.Command.Align(0) +
                            printer.Command.VrHr(State.Rules.Widths, widths, m, m + w) +
                            printer.Command.Lf());
                        State.Line = LineMode.Running;
                        break;
                    default:
                        break;
                }
                // save parameters to stop rules
                State.Rules.Left = left;
                State.Rules.Width = width;
                State.Rules.Right = right;
                State.Rules.Widths = widths;
                // maximum number of wraps
                int row = column.Wrap ? cols.Aggregate(1, (a, col) => Math.Max(a, col.Length)) : 1;
                // sort text
                for (int j = 0; j < row; j++)
                {
                    // append commands to set print area and line alignment
                    string res = printer.Command.Normal() +
                        printer.Command.Area(left, width, right) +
                        printer.Command.Align(0);
                    // print position
                    int p = 0;
                    // process vertical rules
                    if (State.Line == LineMode.Running)
                    {
                        // maximum height
                        int height = cols.Aggregate(1, (a, col) => j < col.Length ? Math.Max(a, col[j].Height) : a);
                        // append commands to print vertical rules
                        res += printer.Command.Normal() +
                            printer.Command.Absolute(p++) +
                            printer.Command.Vr(widths, height);
                    }
                    // process each column
                    int i = 0;
                    foreach (WrappedText[] col in cols)
                    {
                        // append commands to set print position of first column
                        res += printer.Command.Absolute(p);
                        // if wrapped text is not empty
                        if (j < col.Length)
                        {
                            // append commands to align text
                            res += printer.Command.Relative(col[j].Margin);
                            // process text
                            string[] data = col[j].Data;
                            for (int k = 0; k < data.Length; k += 2)
                            {
                                // append commands to decorate text
                                int ul = data[k][0] - '0';
                                int em = data[k][1] - '0';
                                int iv = data[k][2] - '0';
                                int wh = data[k][3] - '0';
                                res += printer.Command.Normal();
                                if (ul > 0)
                                {
                                    res += printer.Command.Ul();
                                }
                                if (em > 0)
                                {
                                    res += printer.Command.Em();
                                }
                                if (iv > 0)
                                {
                                    res += printer.Command.Iv();
                                }
                                if (wh > 0)
                                {
                                    res += printer.Command.Wh(wh);
                                }
                                // append commands to print text
                                res += printer.Command.Text(data[k + 1], printer.Encoding);
                            }
                        }
                        // if wrapped text is empty
                        else
                        {
                            res += printer.Command.Normal() + printer.Command.Text(" ", printer.Encoding);
                        }
                        // append commands to set print position of next column
                        p += columns[i].Width + Math.Abs(column.Border);
                        i++;
                    }
                    // append commands to feed new line
                    res += printer.Command.Lf();
                    result.Add(res);
                }
            }
            // process horizontal rule or paper cut
            if (column.Hr != null)
            {
                // process paper cut
                if (column.Hr == "=")
                {
                    switch (State.Line)
                    {
                        case LineMode.Running:
                        case LineMode.Horizontal:
                            // append commands to stop rules
                            result.Add(printer.Command.Normal() +
                                printer.Command.Area(State.Rules.Left, State.Rules.Width, State.Rules.Right) +
                                printer.Command.Align(0) +
                                printer.Command.VrStop(State.Rules.Widths) +
                                printer.Command.VrLf(false));
                            // append commands to cut paper
                            result.Add(printer.Command.Cut());
                            // set state to start rules
                            State.Line = LineMode.Ready;
                            break;
                        default:
                            // append commands to cut paper
                            result.Add(printer.Command.Cut());
                            break;
                    }
                }
                // process horizontal rule
                else
                {
                    switch (State.Line)
                    {
                        case LineMode.Waiting:
                            // append commands to print horizontal rule
                            result.Add(printer.Command.Normal() +
                                printer.Command.Area(left, width, right) +
                                printer.Command.Align(0) +
                                printer.Command.Hr(width) +
                                printer.Command.Lf());
                            break;
                        case LineMode.Running:
                            // set state to print horizontal rule
                            State.Line = LineMode.Horizontal;
                            break;
                        default:
                            break;
                    }
                }
            }
            // process rules
            if (column.Vr != null)
            {
                // start rules
                if (column.Vr == "+")
                {
                    State.Line = LineMode.Ready;
                }
                // stop rules
                else
                {
                    switch (State.Line)
                    {
                        case LineMode.Ready:
                            // set state to cancel rules
                            State.Line = LineMode.Waiting;
                            break;
                        case LineMode.Running:
                        case LineMode.Horizontal:
                            // append commands to stop rules
                            result.Add(printer.Command.Normal() +
                                printer.Command.Area(State.Rules.Left, State.Rules.Width, State.Rules.Right) +
                                printer.Command.Align(0) +
                                printer.Command.VrStop(State.Rules.Widths) +
                                printer.Command.VrLf(false));
                            State.Line = LineMode.Waiting;
                            break;
                        default:
                            break;
                    }
                }
            }
            // process image
            if (column.Image != null)
            {
                // append commands to print image
                result.Add(printer.Command.Normal() +
                    printer.Command.Area(left, width, right) +
                    printer.Command.Align((int)column.Align) +
                    printer.Command.Image(column.Image));
            }
            // process barcode or 2D code
            if (column.Code != null)
            {
                // process 2D code
                if (column.Code.Type == "qrcode")
                {
                    // append commands to print 2D code
                    result.Add(printer.Command.Normal() +
                        printer.Command.Area(left, width, right) +
                        printer.Command.Align((int)column.Align) +
                        printer.Command.Qrcode(column.Code, printer.Encoding));
                }
                // process barcode
                else
                {
                    // append commands to print barcode
                    result.Add(printer.Command.Normal() +
                        printer.Command.Area(left, width, right) +
                        printer.Command.Align((int)column.Align) +
                        printer.Command.Barcode(column.Code, printer.Encoding));
                }
            }
            // process command
            if (column.Command != null)
            {
                // append commands to insert commands
                result.Add(printer.Command.Normal() +
                    printer.Command.Area(left, width, right) +
                    printer.Command.Align((int)column.Align) +
                    printer.Command.Command(column.Command));
            }
            // flip upside down
            if (printer.UpsideDown)
            {
                result.Reverse();
            }
            return string.Join("", result.ToArray());

        }

        private WrappedText[] WrapText(Column column, PrintOption printer)
        {
            List<WrappedText> result = new List<WrappedText>();
            // remaining spaces
            int space = column.Width;
            // text height
            int height = 1;
            // text data
            List<string> res = new List<string>();
            // text decoration flags
            bool ul = false;
            bool em = false;
            bool iv = false;
            int wh = 0;
            // process text and text decoration
            int i = 0;
            foreach (string text in column.Text)
            {
                // process text
                if (i % 2 == 0)
                {
                    // if text is not empty
                    string[] t = printer.Command.ArrayFrom(text, printer.Encoding);
                    while (t.Length > 0)
                    {
                        // measure character width
                        int w = 0;
                        int j = 0;
                        while (j < t.Length)
                        {
                            w = printer.Command.MeasureText(t[j], printer.Encoding) * (wh < 2 ? wh + 1 : wh - 1);
                            // output before protruding
                            if (w > space)
                            {
                                break;
                            }
                            space -= w;
                            w = 0;
                            j++;
                        }
                        // if characters fit
                        if (j > 0)
                        {
                            // append text decoration information
                            res.Add((ul ? "1" : "0") + (em ? "1" : "0") + (iv ? "1" : "0") + wh);
                            // append text
                            res.Add(string.Join("", t.Take(j).ToArray()));
                            // update text height
                            height = Math.Max(height, wh < 3 ? wh : wh - 1);
                            // remaining text
                            t = t.Skip(j).ToArray();
                        }
                        // if character is too big
                        if (w > column.Width)
                        {
                            // do not output
                            t = t.Skip(1).ToArray();
                            continue;
                        }
                        // if there is no spece left
                        if (w > space || space == 0)
                        {
                            // wrap text automatically
                            result.Add(new WrappedText
                            {
                                Data = res.ToArray(),
                                Margin = space * (double)column.Align / 2,
                                Height = height
                            });
                            space = column.Width;
                            res.Clear();
                            height = 1;
                        }
                    }
                }
                // process text decoration
                else
                {
                    // update text decoration flags
                    switch (text)
                    {
                        case "\n":
                            // wrap text manually
                            result.Add(new WrappedText
                            {
                                Data = res.ToArray(),
                                Margin = space * (double)column.Align / 2,
                                Height = height
                            });
                            space = column.Width;
                            res.Clear();
                            height = 1;
                            break;
                        case "_":
                            ul = !ul;
                            break;
                        case "\"":
                            em = !em;
                            break;
                        case "`":
                            iv = !iv;
                            break;
                        default:
                            int d = Math.Min(text.Length, 7);
                            wh = wh == d ? 0 : d;
                            break;
                    }
                }
                i++;
            }
            // output last text
            if (res.Count > 0)
            {
                result.Add(new WrappedText
                {
                    Data = res.ToArray(),
                    Margin = space * (double)column.Align / 2,
                    Height = height
                });
            }
            return result.ToArray();
        }

        private PrintOption ParseOption(string options)
        {
            // parameters
            Dictionary<char, bool> param1 = new Dictionary<char, bool>()
            {
                { 'u', false }, // upside down
                { 'v', false }, // landscape orientation
                { 's', false }, // paper saving
                { 'n', false }, // no paper cut
                { 'i', false } // print as image
            };
            Dictionary<char, string> param2 = new Dictionary<char, string>()
            {
                { 'p', "" }, // printer control language
                { 'c', "-1" }, // characters per line
                { 'r', "-1" }, // print resolution for -v
                { 'm', "-1,-1" }, // print margin
                { 'b', "-1" }, // image thresholding
                { 'g', "-1" }, // image gamma correction
                { 'l', CultureInfo.CurrentCulture.Name } // language of source file
            };
            // arguments
            string[] argv = options?.Split(' ') ?? Array.Empty<string>();

            // parse arguments
            for (int i = 0; i < argv.Length; i++)
            {
                string key = argv[i];
                if (Regex.IsMatch(key, @"^-[uvsni]$"))
                {
                    // option without value
                    param1[key[1]] = true;
                }
                else if (Regex.IsMatch(key, "^-[pcrmbgl]$"))
                {
                    // option with value
                    if (i < argv.Length - 1)
                    {
                        string value = argv[i + 1];
                        if (Regex.IsMatch(value, "^[^-]"))
                        {
                            param2[key[1]] = value;
                            i++;
                        }
                    }
                }
                else
                {
                    // undefined option
                }
            }
            // language
            string l = param2['l'].ToLower();
            l = l.Substring(0, Regex.IsMatch(l, "^zh-han[st]") ? 7 : 2);
            // printer control language
            string p = param2['p'].ToLower();
            if (!Regex.IsMatch(p, "^(escpos|epson|sii|citizen|fit|impactb?|generic|star(line|graphic|impact[23]?)?|emustarline)$"))
            {
                p = "base";
            }
            else if (Regex.IsMatch(p, "^(emu)?star(line)?$"))
            {
                p += (Regex.IsMatch(l, "^(ja|ko|zh)") ? "m" : "s") + "bcs" + (Regex.IsMatch(l, "^(ko|zh)") ? "2" : "");
            }
            // string to number
            int.TryParse(param2['c'], out int c);
            int[] m = (param2['m'] + ",").Split(',').Select(n => {
                int.TryParse(n, out int d);
                return d;
            }).ToArray();
            int.TryParse(param2['r'], out int r);
            int.TryParse(param2['b'], out int b);
            double.TryParse(param2['g'], out double g);
            // options
            return new PrintOption()
            {
                AsImage = param1['i'],
                Landscape =  param1['v'],
                Resolution = r == 180 ? r : 203,
                Cpl = c >= 24 && c <= 96 ? c : 48,
                Encoding = Encodings.ContainsKey(l) ? Encodings[l] : "multilingual",
                Gradient = !(b >= 0 && b <= 255),
                Gamma = g >= 0.1 && g <= 10.0 ? g : 1.0,
                Threshold = b >= 0 && b <= 255 ? b : 128,
                UpsideDown = param1['u'],
                Spacing = !param1['s'],
                Cutting = !param1['n'],
                Margin = m[0] >= 0 && m[0] <= 24 ? m[0] : 0,
                MarginRight = m[1] >= 0 && m[1] <= 24 ? m[1] : 0,
                Type = p
            };
        }

        /*
        // Playwright
        private async Task<string> Base64Png(string svg, PrintOption options = null)
        {
            string png = "";
            int c = Svg.CharWidth;
            Match match = Regex.Match(svg, @"width=""(\d+)px"" height=""(\d+)px""");
            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out int w);
                int.TryParse(match.Groups[2].Value, out int h);
                string t = "";
                if (options?.Landscape ?? false)
                {
                    int m = options?.Margin * c ?? 0;
                    int n = options?.MarginRight * c ?? 0;
                    (w, h) = (h, w + m + n);
                    t = $"svg{{padding-left:{m}px;padding-right:{n}px;transform-origin:top left;transform:rotate(-90deg) translateX(-{h}px)}}";
                }
                var playwright = await Playwright.CreateAsync();
                var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Channel = "chrome", Headless = true });
                var page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = w, Height = h } });
                await page.SetContentAsync($"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>*{{margin:0;background:transparent}}{t}</style></head><body>{svg}</body></html>");
                png = "data:image/png;base64," + Convert.ToBase64String(await page.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png, OmitBackground = true }));
                await browser.CloseAsync();
            }
            return png;
        }
        */
        /*
        // Puppeteer
        private async Task<string> Base64Png(string svg, PrintOption options = null)
        {
            string png = "";
            int c = Svg.CharWidth;
            Match match = Regex.Match(svg, @"width=""(\d+)px"" height=""(\d+)px""");
            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out int w);
                int.TryParse(match.Groups[2].Value, out int h);
                string t = "";
                if (options?.Landscape ?? false)
                {
                    int m = options?.Margin * c ?? 0;
                    int n = options?.MarginRight * c ?? 0;
                    (w, h) = (h, w + m + n);
                    t = $"svg{{padding-left:{m}px;padding-right:{n}px;transform-origin:top left;transform:rotate(-90deg) translateX(-{h}px)}}";
                }
                var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Channel = ChromeReleaseChannel.Stable,
                    Headless = true,
                    DefaultViewport = new ViewPortOptions { Width = w, Height = h }
                });
                var page = await browser.NewPageAsync();
                await page.SetContentAsync($"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>*{{margin:0;background:transparent}}{t}</style></head><body>{svg}</body></html>");
                png = "data:image/png;base64," + await page.ScreenshotBase64Async();
                await browser.CloseAsync();
            }
            return png;
        }
        */
        private Task<string> Base64Png(string svg, PrintOption options = null)
        {
            return Task.FromResult("");
        }

        private Base CreatePrinter(string type)
        {
            // create command object
            switch (type)
            {
                case "escpos":
                    return new Thermal();
                case "epson":
                    return new Thermal();
                case "sii":
                    return new Sii();
                case "citizen":
                    return new Citizen();
                case "fit":
                    return new Fit();
                case "impact":
                    return new Impact();
                case "impactb":
                    return new ImpactB();
                case "generic":
                    return new Generic();
                case "starsbcs":
                    return new StarSbcs();
                case "starmbcs":
                    return new StarMbcs();
                case "starmbcs2":
                    return new StarMbcs2();
                case "starlinesbcs":
                    return new StarLineSbcs();
                case "starlinembcs":
                    return new StarLineMbcs();
                case "starlinembcs2":
                    return new StarLineMbcs2();
                case "emustarlinesbcs":
                    return new EmuStarLineSbcs();
                case "emustarlinembcs":
                    return new EmuStarLineMbcs();
                case "emustarlinembcs2":
                    return new EmuStarLineMbcs2();
                case "stargraphic":
                    return new StarGraphic();
                case "starimpact":
                    return new StarImpact();
                case "starimpact2":
                    return new StarImpact2();
                case "starimpact3":
                    return new StarImpact3();
                case "escpos90":
                    return new Thermal90();
                case "epson90":
                    return new Thermal90();
                case "sii90":
                    return new Sii90();
                case "citizen90":
                    return new Citizen90();
                case "starsbcs90":
                    return new StarSbcs90();
                case "starmbcs90":
                    return new StarMbcs90();
                case "starmbcs290":
                    return new StarMbcs290();
                default:
                    return new Base();
            }
        }
    }
}
