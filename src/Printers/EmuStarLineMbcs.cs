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
    // Command Emulator Star Line Mode MBCS Japanese
    //
    class EmuStarLineMbcs : StarLineMbcs
    {
        // set line spacing and feed new line: (ESC z n) (ESC 0)
        public override string VrLf(bool vr)
        {
            return (vr == UpsideDown && Spacing ? "\u001bz1" : "\u001b0") + Lf();
        }
    }
}
