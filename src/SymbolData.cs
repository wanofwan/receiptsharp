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

namespace ReceiptSharp
{
    public class SymbolData
    {
        public string Data { get; set; }
        public string Type { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Hri { get; set; }
        public int Cell { get; set; }
        public string Level { get; set; }
        public bool QuietZone { get; set; }
        public SymbolData Clone()
        {
            return (SymbolData)MemberwiseClone();
        }
    }
}
