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

using System.Collections.Generic;
using System.Globalization;

namespace ReceiptSharp.Printers
{
    //
    // Command base object
    //
    class Base : IPrinter
    {
        /**
         * Character width.
         * @type {number} character width (dots per character)
         */
        public int CharWidth { get; set; } = 12;
        /**
         * Measure text width.
         * @param {string} text string to measure
         * @param {string} encoding codepage
         * @returns {number} string width
         */
        public virtual int MeasureText(string text, string encoding)
        {
            int r = 0;
            IEnumerator<char> t = text.GetEnumerator();
            switch (encoding)
            {
                case "cp932":
                case "shiftjis":
                    while (t.MoveNext())
                    {
                        int d = t.Current;
                        r += d < 0x80 || d == 0xa0 || d == 0xa5 || d == 0x203e || d > 0xff60 && d < 0xffa0 ? 1 : 2;
                    }
                    break;
                case "cp936":
                case "gb18030":
                case "cp949":
                case "ksc5601":
                case "cp950":
                case "big5":
                    while (t.MoveNext())
                    {
                        int d = t.Current;
                        r += d < 0x80 || d == 0xa0 ? 1 : 2;
                    }
                    break;
                case "tis620":
                    bool consonant = false;
                    bool vowel = false;
                    bool tone = false;
                    while (t.MoveNext())
                    {
                        int d = t.Current;
                        if (consonant)
                        {
                            if (d == 0xe31 || d >= 0xe34 && d <= 0xe3a || d == 0xe47)
                            {
                                if (vowel)
                                {
                                    r += 2;
                                    consonant = vowel = tone = false;
                                }
                                else
                                {
                                    vowel = true;
                                }
                            }
                            else if (d >= 0xe48 && d <= 0xe4b)
                            {
                                if (tone)
                                {
                                    r += 2;
                                    consonant = vowel = tone = false;
                                }
                                else
                                {
                                    tone = true;
                                }
                            }
                            else if (d == 0xe33 || d >= 0xe4c && d <= 0xe4e)
                            {
                                if (vowel || tone)
                                {
                                    r += 2;
                                    consonant = vowel = tone = false;
                                }
                                else
                                {
                                    r += d == 0xe33 ? 2 : 1;
                                    consonant = false;
                                }
                            }
                            else if (d >= 0xe01 && d <= 0xe2e)
                            {
                                r++;
                                vowel = tone = false;
                            }
                            else
                            {
                                r += 2;
                                consonant = vowel = tone = false;
                            }
                        }
                        else if (d >= 0xe01 && d <= 0xe2e)
                        {
                            consonant = true;
                        }
                        else
                        {
                            r++;
                        }
                    }
                    if (consonant)
                    {
                        r++;
                    }
                    break;
                default:
                    while (t.MoveNext())
                    {
                        r++;
                    }
                    break;
            }
            return r;
        }
        /**
         * Create character array from string (supporting Thai combining characters).
         * @param {string} text string
         * @param {string} encoding codepage
         * @returns {string[]} array instance
         */
        public virtual string[] ArrayFrom(string text, string encoding)
        {
            List<string> result = new List<string>();
            TextElementEnumerator t = StringInfo.GetTextElementEnumerator(text);
            switch (encoding)
            {
                case "cp932":
                case "shiftjis":
                    while (t.MoveNext())
                    {
                        result.Add(t.GetTextElement().Replace('\\', '\u00a5').Replace('\u203e', '~').Replace('\u301c', '\uff5e'));
                    }
                    break;
                case "tis620":
                    string consonant = "";
                    string vowel = "";
                    string tone = "";
                    while (t.MoveNext())
                    {
                        string c = t.GetTextElement();
                        int d = char.ConvertToUtf32(c, 0);
                        if (consonant.Length > 0)
                        {
                            if (d == 0xe31 || d >= 0xe34 && d <= 0xe3a || d == 0xe47)
                            {
                                if (vowel.Length > 0)
                                {
                                    result.Add(consonant + vowel + tone);
                                    result.Add(c);
                                    consonant = vowel = tone = "";
                                }
                                else
                                {
                                    vowel = c;
                                }
                            }
                            else if (d >= 0xe48 && d <= 0xe4b)
                            {
                                if (tone.Length > 0)
                                {
                                    result.Add(consonant + vowel + tone);
                                    result.Add(c);
                                    consonant = vowel = tone = "";
                                }
                                else
                                {
                                    tone = c;
                                }
                            }
                            else if (d == 0xe33 || d >= 0xe4c && d <= 0xe4e)
                            {
                                if (vowel.Length > 0 || tone.Length > 0)
                                {
                                    result.Add(consonant + vowel + tone);
                                    result.Add(c);
                                    consonant = vowel = tone = "";
                                }
                                else
                                {
                                    result.Add(consonant + c);
                                    consonant = "";
                                }
                            }
                            else if (d >= 0xe01 && d <= 0xe2e)
                            {
                                result.Add(consonant + vowel + tone);
                                consonant = c;
                                vowel = tone = "";
                            }
                            else
                            {
                                result.Add(consonant + vowel + tone);
                                result.Add(c);
                                consonant = vowel = tone = "";
                            }
                        }
                        else if (d >= 0xe01 && d <= 0xe2e)
                        {
                            consonant = c;
                        }
                        else
                        {
                            result.Add(c);
                        }
                    }
                    if (consonant.Length > 0)
                    {
                        result.Add(consonant + vowel + tone);
                    }
                    break;
                default:
                    while (t.MoveNext())
                    {
                        result.Add(t.GetTextElement());
                    }
                    break;
            }
            return result.ToArray();
        }
        /**
         * Start printing.
         * @param {object} printer printer configuration
         * @returns {string} commands
         */
        public virtual string Open(PrintOption printer)
        {
            return "";
        }
        /**
         * Finish printing.
         * @returns {string} commands
         */
        public virtual string Close()
        {
            return "";
        }
        /**
         * Set print area.
         * @param {number} left left margin (unit: characters)
         * @param {number} width print area (unit: characters)
         * @param {number} right right margin (unit: characters)
         * @returns {string} commands
         */
        public virtual string Area(int left, int width, int right)
        {
            return "";
        }
        /**
         * Set line alignment.
         * @param {number} align line alignment (0: left, 1: center, 2: right)
         * @returns {string} commands
         */
        public virtual string Align(int align)
        {
            return "";
        }
        /**
         * Set absolute print position.
         * @param {number} position absolute position (unit: characters)
         * @returns {string} commands
         */
        public virtual string Absolute(double position)
        {
            return "";
        }
        /**
         * Set relative print position.
         * @param {number} position relative position (unit: characters)
         * @returns {string} commands
         */
        public virtual string Relative(double position)
        {
            return "";
        }
        /**
         * Print horizontal rule.
         * @param {number} width line width (unit: characters)
         * @returns {string} commands
         */
        public virtual string Hr(int width)
        {
            return "";
        }
        /**
         * Print vertical rules.
         * @param {number[]} widths vertical line spacing
         * @param {number} height text height (1-6)
         * @returns {string} commands
         */
        public virtual string Vr(int[] widths, int height)
        {
            return "";
        }
        /**
         * Start rules.
         * @param {number[]} widths vertical line spacing
         * @returns {string} commands
         */
        public virtual string VrStart(int[] widths)
        {
            return "";
        }
        /**
         * Stop rules.
         * @param {number[]} widths vertical line spacing
         * @returns {string} commands
         */
        public virtual string VrStop(int[] widths)
        {
            return "";
        }
        /**
         * Print vertical and horizontal rules.
         * @param {number[]} widths1 vertical line spacing (stop)
         * @param {number[]} widths2 vertical line spacing (start)
         * @param {number} dl difference in left position
         * @param {number} dr difference in right position
         * @returns {string} commands
         */
        public virtual string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            return "";
        }
        /**
         * Set line spacing and feed new line.
         * @param {boolean} vr whether vertical ruled lines are printed
         * @returns {string} commands
         */
        public virtual string VrLf(bool vr)
        {
            return "";
        }
        /**
         * Cut paper.
         * @returns {string} commands
         */
        public virtual string Cut()
        {
            return "";
        }
        /**
         * Underline text.
         * @returns {string} commands
         */
        public virtual string Ul()
        {
            return "";
        }
        /**
         * Emphasize text.
         * @returns {string} commands
         */
        public virtual string Em()
        {
            return "";
        }
        /**
         * Invert text.
         * @returns {string} commands
         */
        public virtual string Iv()
        {
            return "";
        }
        /**
         * Scale up text.
         * @param {number} wh number of special character '^' (1-7)
         * @returns {string} commands
         */
        public virtual string Wh(int wh)
        {
            return "";
        }
        /**
         * Cancel text decoration.
         * @returns {string} commands
         */
        public virtual string Normal()
        {
            return "";
        }
        /**
         * Print text.
         * @param {string} text string to print
         * @param {string} encoding codepage
         * @returns {string} commands
         */
        public virtual string Text(string text, string encoding)
        {
            return "";
        }
        /**
         * Feed new line.
         * @returns {string} commands
         */
        public virtual string Lf()
        {
            return "";
        }
        /**
         * Insert commands.
         * @param {string} command commands to insert
         * @returns {string} commands
         */
        public virtual string Command(string command)
        {
            return "";
        }
        /**
         * Print image.
         * @param {string} image image data (base64 png format)
         * @returns {string} commands
         */
        public virtual string Image(string image)
        {
            return "";
        }
        /**
         * Print QR Code.
         * @param {object} symbol QR Code information (data, type, cell, level)
         * @param {string} encoding codepage
         * @returns {string} commands
         */
        public virtual string Qrcode(SymbolData symbol, string encoding)
        {
            return "";
        }
        /**
         * Print barcode.
         * @param {object} symbol barcode information (data, type, width, height, hri)
         * @param {string} encoding codepage
         * @returns {string} commands
         */
        public virtual string Barcode(SymbolData symbol, string encoding)
        {
            return "";
        }
    }
}
