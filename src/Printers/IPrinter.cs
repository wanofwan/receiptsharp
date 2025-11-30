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

namespace ReceiptSharp.Printers
{
    //
    // Command base object
    //
    interface IPrinter
    {
        /**
         * Character width.
         * @type {number} character width (dots per character)
         */
        int CharWidth { get; set; }
        /**
         * Measure text width.
         * @param {string} text string to measure
         * @param {string} encoding codepage
         * @returns {number} string width
         */
        int MeasureText(string text, string encoding);
        /**
         * Create character array from string (supporting Thai combining characters).
         * @param {string} text string
         * @param {string} encoding codepage
         * @returns {string[]} array instance
         */
        string[] ArrayFrom(string text, string encoding);
        /**
         * Start printing.
         * @param {object} printer printer configuration
         * @returns {string} commands
         */
        string Open(PrintOption printer);
        /**
         * Finish printing.
         * @returns {string} commands
         */
        string Close();
        /**
         * Set print area.
         * @param {number} left left margin (unit: characters)
         * @param {number} width print area (unit: characters)
         * @param {number} right right margin (unit: characters)
         * @returns {string} commands
         */
        string Area(int left, int width, int right);
        /**
         * Set line alignment.
         * @param {number} align line alignment (0: left, 1: center, 2: right)
         * @returns {string} commands
         */
        string Align(int align);
        /**
         * Set absolute print position.
         * @param {number} position absolute position (unit: characters)
         * @returns {string} commands
         */
        string Absolute(double position);
        /**
         * Set relative print position.
         * @param {number} position relative position (unit: characters)
         * @returns {string} commands
         */
        string Relative(double position);
        /**
         * Print horizontal rule.
         * @param {number} width line width (unit: characters)
         * @returns {string} commands
         */
        string Hr(int width);
        /**
         * Print vertical rules.
         * @param {number[]} widths vertical line spacing
         * @param {number} height text height (1-6)
         * @returns {string} commands
         */
        string Vr(int[] widths, int height);
        /**
         * Start rules.
         * @param {number[]} widths vertical line spacing
         * @returns {string} commands
         */
        string VrStart(int[] widths);
        /**
         * Stop rules.
         * @param {number[]} widths vertical line spacing
         * @returns {string} commands
         */
        string VrStop(int[] widths);
        /**
         * Print vertical and horizontal rules.
         * @param {number[]} widths1 vertical line spacing (stop)
         * @param {number[]} widths2 vertical line spacing (start)
         * @param {number} dl difference in left position
         * @param {number} dr difference in right position
         * @returns {string} commands
         */
        string VrHr(int[] widths1, int[] widths2, int dl, int dr);
        /**
         * Set line spacing and feed new line.
         * @param {boolean} vr whether vertical ruled lines are printed
         * @returns {string} commands
         */
        string VrLf(bool vr);
        /**
         * Cut paper.
         * @returns {string} commands
         */
        string Cut();
        /**
         * Underline text.
         * @returns {string} commands
         */
        string Ul();
        /**
         * Emphasize text.
         * @returns {string} commands
         */
        string Em();
        /**
         * Invert text.
         * @returns {string} commands
         */
        string Iv();
        /**
         * Scale up text.
         * @param {number} wh number of special character '^' (1-7)
         * @returns {string} commands
         */
        string Wh(int wh);
        /**
         * Cancel text decoration.
         * @returns {string} commands
         */
        string Normal();
        /**
         * Print text.
         * @param {string} text string to print
         * @param {string} encoding codepage
         * @returns {string} commands
         */
        string Text(string text, string encoding);
        /**
         * Feed new line.
         * @returns {string} commands
         */
        string Lf();
        /**
         * Insert commands.
         * @param {string} command commands to insert
         * @returns {string} commands
         */
        string Command(string command);
        /**
         * Print image.
         * @param {string} image image data (base64 png format)
         * @returns {string} commands
         */
        string Image(string image);
        /**
         * Print QR Code.
         * @param {object} symbol QR Code information (data, type, cell, level)
         * @param {string} encoding codepage
         * @returns {string} commands
         */
        string Qrcode(SymbolData symbol, string encoding);
        /**
         * Print barcode.
         * @param {object} symbol barcode information (data, type, width, height, hri)
         * @param {string} encoding codepage
         * @returns {string} commands
         */
        string Barcode(SymbolData symbol, string encoding);
    }
}
