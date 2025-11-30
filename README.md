# ReceiptSharp

.NET printing library for receipt printers, simple and easy with receipt markdown, printer status support.  

```csharp
using ReceiptSharp;

string example = @"^^^RECEIPT

11/30/2025, 12:34:56 PM
Asparagus | 1| 1.00
Broccoli  | 2| 2.00
Carrot    | 3| 3.00
---
^TOTAL | ^6.00";

ReceiptSession session = new ReceiptSession("192.168.192.168");
session.Ready += async (sender, e) => Console.WriteLine(await session.Print(example));
session.Open();
Console.ReadLine();
session.Close();

Console.WriteLine(new Receipt(example).ToSvg());
```

![example](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/example.png)  



# Features

ReceiptSharp is simple printing library for receipt printers that prints with easy markdown data for receipts and returns printer status. Even without a printer, it can output images.  

ReceiptSharp auto-detects printer models for seamless printing.  

A development tool is provided to edit, preview, and print the receipt markdown.  
https://receiptline.github.io/receiptjs-designer/  



# Receipt printers

- Epson TM series
- Seiko Instruments RP series
- Star MC series
- Citizen CT series
- Fujitsu FP series

Connect with IP address or serial port.  
(LAN, Bluetooth, virtual serial port, and real serial port)  

Epson TM series (South Asia model) and Star MC series (StarPRNT model) can print with device font of Thai characters.  

![printers](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/readme_printer.jpg)  



# ReceiptSession Class

Namespace: ReceiptSharp  

Print receipt markdown text and notify printer status.  

## Examples

```csharp
using ReceiptSharp;

string example = @"^^^RECEIPT

11/30/2025, 12:34:56 PM
Asparagus | 1| 1.00
Broccoli  | 2| 2.00
Carrot    | 3| 3.00
---
^TOTAL | ^6.00";

ReceiptSession session = new ReceiptSession("COM1");
session.StatusChanged += (sender, e) =>
{
    Console.WriteLine(e);
};
session.Ready += async (sender, e) =>
{
    string result = await session.Print(example, "-c 42 -u");
    Console.WriteLine(result);
};
session.Open();
Console.ReadLine();
session.Close();
```


## Constructors

|Name|Description|  
|---|---|
|ReceiptSession(String)|Create instance.|


## Properties

|Name|Description|  
|---|---|
|Status|Printer status.|
|Drawer|Cash drawer status.|


## Methods

|Name|Description|  
|---|---|
|Open()|Open session.|
|Print(String, String)|Print receipt markdown.|
|InvertDrawerState(Boolean)|Invert cash drawer state.|
|Close()|Close session.|


## Events

|Name|Description|  
|---|---|
|StatusChanged|Printer status updated.|
|Ready|Ready to print.|
|Online|Printer is online.|
|CoverOpen|Printer cover is open.|
|PaperEmpty|No receipt paper.|
|Error|Printer error (except cover open and paper empty).|
|Offline|Printer is off or offline.|
|Disconnect|Printer is not connected.|
|DrawerChanged|Drawer status updated.|
|DrawerClosed|Drawer is closed.|
|DrawerOpen|Drawer is open.|


## ReceiptSession Constructor

```csharp
public ReceiptSession(string destination)
```

The ReceiptSession() constructor creates a new ReceiptSession instance.  

### Parameters

- `destination`
  - IP address or serial port of target printer

Serial port options:  

```csharp
"COM1:115200N81"
```

- `<port name>[:<options>]`
- `<options>`
    - `<baud rate>,<parity>,<data bits>,<stop bits>[,<flow control>]`
    - Default: `9600,N,8,1,N`
    - Commas can be omitted
- `<baud rate>`
    - `2400`, `4800`, `9600`, `19200`, `38400`, `57600`, `115200`
- `<parity>`
    - `N`: None, `E`: Even, `O`: Odd
- `<data bits>`
    - `8`, `7`
- `<stop bits>`
    - `1`, `2`
- `<flow control>`
    - `N`: None, `R`: RTS/CTS, `X`: XON/XOFF

### Returns

- A new ReceiptSession instance.


## ReceiptSession.Status Property

```csharp
public string Status { get; }
```

The Status property is a string representing the printer status.  

### Property Value

- A string representing the printer status.
  - `Online`: Printer is online
  - `Print`: Printer is printing
  - `CoverOpen`: Printer cover is open
  - `PaperEmpty`: No receipt paper
  - `Error`: Printer error (except cover open and paper empty)
  - `Offline`: Printer is off or offline
  - `Disconnect`: Printer is not connected

## ReceiptSession.Drawer Property

```csharp
public string Drawer { get; }
```

The Drawer property is a string representing the cash drawer status.  

### Property Value

- A string representing the cash drawer status.
  - `DrawerClosed`: Drawer is closed
  - `DrawerOpen`: Drawer is open
  - `Offline`: Printer is off or offline
  - `Disconnect`: Printer is not connected


## ReceiptSession.Open Method

```csharp
public void Open()
```

The Open() method starts a session and connects to the target printer.  


## ReceiptSession.Print Method

```csharp
public async Task<string> Print(string markdown, string options = "")
```

The Print() method prints a receipt markdown text.  

### Parameters

- `markdown`
  - Receipt markdown text
- `options`
  - `-c <chars>`: Characters per line
    - Range: `24`-`96`
    - Default: `48`
  - `-l <language>`: Language of receipt markdown text
    - `en`, `fr`, `de`, `es`, `po`, `it`, `ru`, ...: Multilingual (cp437, 852, 858, 866, 1252 characters)
    - `ja`: Japanese (shiftjis characters)
    - `ko`: Korean (ksc5601 characters)
    - `zh-hans`: Simplified Chinese (gb18030 characters)
    - `zh-hant`: Traditional Chinese (big5 characters)
    - `th`: Thai
    - Default: System locale
  - `-s`: Paper saving (reduce line spacing)
  - `-m [<left>][,<right>]`: Print margin
    - Range (left): `0`-`24`
    - Range (right): `0`-`24`
    - Default: `0,0`
  - `-u`: Upside down
  - `-i`: Print as image _Not implemented_
  - `-n`: No paper cut
  - `-b <threshold>`: Image thresholding
    - Range: `0`-`255`
    - Default: Error diffusion
  - `-g <gamma>`: Image gamma correction
    - Range: `0.1`-`10.0`
    - Default: `1.0`
  - `-p <printer>`: Printer control language
    - `escpos`: ESC/POS (Epson)
    - `epson`: ESC/POS (Epson)
    - `sii`: ESC/POS (Seiko Instruments)
    - `citizen`: ESC/POS (Citizen)
    - `fit`: ESC/POS (Fujitsu)
    - `impact`: ESC/POS (TM-U220)
    - `impactb`: ESC/POS (TM-U220 Font B)
    - `generic`: ESC/POS (Generic) _Experimental_
    - `star`: StarPRNT
    - `starline`: Star Line Mode
    - `emustarline`: Command Emulator Star Line Mode
    - `stargraphic`: Star Graphic Mode
    - `starimpact`: Star Mode on dot impact printers _Experimental_
    - `starimpact2`: Star Mode on dot impact printers (Font 5x9 2P-1) _Experimental_
    - `starimpact3`: Star Mode on dot impact printers (Font 5x9 3P-1) _Experimental_
    - Default: Auto detection (`epson`, `sii`, `citizen`, `fit`, `impactb`, `generic`, `star`)
  - `-v`: Landscape orientation
    - Device font support: `escpos`, `epson`, `sii`, `citizen`, `star`
  - `-r <dpi>`: Print resolution for ESC/POS, landscape, and device font
    - Values: `180`, `203`
    - Default: `203`

### Returns

- A Task that fulfills with a string once the print result is ready to be used.
  - `Success`: Printing success
  - `Print`: Printer is printing
  - `CoverOpen`: Printer cover is open
  - `PaperEmpty`: No receipt paper
  - `Error`: Printer error (except cover open and paper empty)
  - `Offline`: Printer is off or offline
  - `Disconnect`: Printer is not connected


## ReceiptSession.InvertDrawerState Method

```csharp
public void InvertDrawerState(bool invert)
```

The InvertDrawerState() method inverts cash drawer state.  

### Parameters

- `invert`
  - If true, invert drawer state


## ReceiptSession.Close Method

```csharp
public void Close()
```

The Close() method closes the connection and ends the session.  



# Receipt Class

Namespace: ReceiptSharp  

Convert to image, plain text, or printer commands.  


## Examples

```csharp
using ReceiptSharp;

string example = @"^^^RECEIPT

11/30/2025, 12:34:56 PM
Asparagus | 1| 1.00
Broccoli  | 2| 2.00
Carrot    | 3| 3.00
---
^TOTAL | ^6.00";

Receipt receipt = new Receipt(example, "-c 42 -l en");
string svg = receipt.ToSvg();
Console.WriteLine(svg);
string txt = receipt.ToText();
Console.WriteLine(txt);
```


## Constructors

|Name|Description|  
|---|---|
|Receipt(String, String)|Create instance.|


## Methods

|Name|Description|  
|---|---|
|ToSvg()|Convert receipt markdown to SVG.|
|ToPng()|Convert receipt markdown to PNG. _Not implemented_|
|ToText()|Convert receipt markdown to text.|
|ToCommand()|Convert receipt markdown to printer commands.|
|ToString()|Return string representing this object.|


## Receipt Constructor

```csharp
public Receipt(string markdown, string options = "")
```

The Receipt() constructor creates a new Receipt instance.  

### Parameters

- `markdown`
  - Receipt markdown text
- `options`
  - `-p <printer>`: Printer control language
    - `escpos`: ESC/POS (Epson)
    - `epson`: ESC/POS (Epson)
    - `sii`: ESC/POS (Seiko Instruments)
    - `citizen`: ESC/POS (Citizen)
    - `fit`: ESC/POS (Fujitsu)
    - `impact`: ESC/POS (TM-U220)
    - `impactb`: ESC/POS (TM-U220 Font B)
    - `generic`: ESC/POS (Generic) _Experimental_
    - `star`: StarPRNT
    - `starline`: Star Line Mode
    - `emustarline`: Command Emulator Star Line Mode
    - `stargraphic`: Star Graphic Mode
    - `starimpact`: Star Mode on dot impact printers _Experimental_
    - `starimpact2`: Star Mode on dot impact printers (Font 5x9 2P-1) _Experimental_
    - `starimpact3`: Star Mode on dot impact printers (Font 5x9 3P-1) _Experimental_
  - `-c <chars>`: characters per line
    - Range: `24`-`96`
    - Default: `48`
  - `-l <language>`: Language of receipt markdown text
    - `en`, `fr`, `de`, `es`, `po`, `it`, `ru`, ...: Multilingual (cp437, 852, 858, 866, 1252 characters)
    - `ja`: Japanese (shiftjis characters)
    - `ko`: Korean (ksc5601 characters)
    - `zh-hans`: Simplified Chinese (gb18030 characters)
    - `zh-hant`: Traditional Chinese (big5 characters)
    - `th`: Thai
    - Default: System locale
  - `-s`: Paper saving (reduce line spacing)
  - `-m [<left>][,<right>]`: Print margin
    - Range (left): `0`-`24`
    - Range (right): `0`-`24`
    - Default: `0,0`
  - `-u`: Upside down
  - `-i`: Print as image _Not implemented_
  - `-n`: No paper cut
  - `-b <threshold>`: Image thresholding
    - Range: `0`-`255`
    - Default: Error diffusion
  - `-g <gamma>`: Image gamma correction
    - Range: `0.1`-`10.0`
    - Default: `1.0`
  - `-v`: Landscape orientation
    - Device font support: `escpos`, `epson`, `sii`, `citizen`, `star`
  - `-r <dpi>`: Print resolution for ESC/POS, landscape, and device font
    - Values: `180`, `203`
    - Default: `203`

### Returns

- A new Receipt instance.


## Receipt.ToSvg Method

```csharp
public string ToSvg()
```

The ToSvg() method converts to SVG.  

### Returns

- A string representing the SVG.


## Receipt.ToPng Method

```csharp
public string ToPng()
```

The ToPng() method converts to PNG.  

### Returns

- A string representing the PNG in data URL format.


## Receipt.ToText Method

```csharp
public string ToText()
```

The ToText() method converts to plain text.  

### Returns

- A string representing the plain text.


## Receipt.ToCommand Method

```csharp
public byte[] ToCommand()
```

The ToCommand() method converts to printer commands.  

### Returns

- A byte array representing the printer commands.


## Receipt.ToString Method

```csharp
public override string ToString()
```

The ToString() method returns a string representing the receipt markdown text.  

### Returns

- A string representing the receipt markdown text.



# Receipt Markdown

This language conforms to the OFSC ReceiptLine Specification.  
https://www.ofsc.or.jp/receiptline/en/  

ReceiptLine is the receipt description language that expresses the output image of small roll paper.  
It supports printing paper receipts using a receipt printer and displaying electronic receipts on a POS system or smartphone.  
It can be described simply with receipt markdown text data that does not depend on the paper width.  


## Syntax

### Railroad diagram

**_document_**  
![document](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/document.png)  

**_line_**  
![line](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/line.png)  

**_columns_**  
![columns](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/columns.png)  

**_column_**  
![column](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/column.png)  

**_text_**  
![text](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/text.png)  

**_char_**  
![char](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/char.png)  

**_escape_**  
![escape](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/escape.png)  

**_ws (whitespace)_**  
![ws](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/ws.png)  

**_property_**  
![property](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/property.png)  

**_member_**  
![member](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/member.png)  

**_key_**  
![key](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/key.png)  

**_value_**  
![value](https://raw.githubusercontent.com/receiptline/receiptsharp/main/images/value.png)  


## Grammar

### Structure

The receipt is made of a table, which separates each column with a pipe `|`.  

|Line|Content|Description|
|---|---|---|
|_column_<br><code>&#x7c;</code> _column_ <code>&#x7c;</code><br><code>&#x7c;</code> _column_<br>_column_ <code>&#x7c;</code>|Text<br>Property|Single column|
|_column_ <code>&#x7c;</code> _column_ <br><code>&#x7c;</code> _column_ <code>&#x7c;</code> _column_ <code>&#x7c;</code><br><code>&#x7c;</code> _column_ <code>&#x7c;</code> _column_<br>_column_ <code>&#x7c;</code> _column_ <code>&#x7c;</code>|Text|Double column|
|_column_ <code>&#x7c;</code> _..._ <code>&#x7c;</code> _column_<br><code>&#x7c;</code> _column_ <code>&#x7c;</code> _..._ <code>&#x7c;</code> _column_ <code>&#x7c;</code><br><code>&#x7c;</code> _column_ <code>&#x7c;</code> _..._ <code>&#x7c;</code> _column_<br>_column_ <code>&#x7c;</code> _..._ <code>&#x7c;</code> _column_ <code>&#x7c;</code>|Text|Multiple columns|

### Alignment

The column is attracted to the pipe `|` like a magnet.  
<code>&#x2423;</code> means one or more whitespaces.  

|Column|Description|
|---|---|
|_column_<br><code>&#x7c;</code>_column_<code>&#x7c;</code><br><code>&#x7c;&#x2423;</code>_column_<code>&#x2423;&#x7c;</code>|Center|
|<code>&#x7c;</code>_column_<br><code>&#x7c;</code>_column_<code>&#x2423;&#x7c;</code><br>_column_<code>&#x2423;&#x7c;</code>|Left|
|_column_<code>&#x7c;</code><br><code>&#x7c;&#x2423;</code>_column_<code>&#x7c;</code><br><code>&#x7c;&#x2423;</code>_column_|Right|

### Text

The text is valid for any column.  

```
Asparagus | 0.99
Broccoli | 1.99
Carrot | 2.99
---
^TOTAL | ^5.97
```

Characters are printed in a monospace font (12 x 24 px).  
Wide characters are twice as wide as Latin characters (24 x 24 px).  
Control characters are ignored.  

### Special characters in text

Special characters are assigned to characters that are rarely used in the receipt.  

|Special character|Description|
|---|---|
|`\`|Character escape|
|<code>&#x7c;</code>|Column delimiter|
|`{`|Property delimiter (Start)|
|`}`|Property delimiter (End)|
|`-` (1 or more, exclusive)|Horizontal rule|
|`=` (1 or more, exclusive)|Paper cut|
|`~`|Space|
|`_`|Underline|
|`"`|Emphasis|
|`` ` ``|Invert|
|`^`|Double width|
|`^^`|Double height|
|`^^^`|2x size|
|`^^^^`|3x size|
|`^^^^^`|4x size|
|`^^^^^^`|5x size|
|`^^^^^^^` (7 or more)|6x size|

### Escape sequences in text

Escape special characters.  

|Escape sequence|Description|
|---|---|
|`\\`|&#x5c;|
|<code>&#x5c;&#x7c;</code>|&#x7c;|
|`\{`|&#x7b;|
|`\}`|&#x7d;|
|`\-`|&#x2d; (Cancel horizontal rule)|
|`\=`|&#x3d; (Cancel paper cut)|
|`\~`|&#x7e;|
|`\_`|&#x5f;|
|`\"`|&#x5f;|
|``\` ``|&#x60;|
|`\^`|&#x5e;|
|`\n`|Wrap text manually|
|`\x`_nn_|Hexadecimal character code|
|`\`_char_ (Others)|Ignore|

### Properties

The property is valid for lines with a single column.  
Text, images, barcodes, and 2D codes cannot be placed on the same line.  

```
{ width: * 10; comment: the column width is specified in characters }
```

|Key|Abbr|Value|Default|Description|
|---|---|---|---|---|
|`image`|`i`|_base64 png format_|-|Insert image<br>Drag a PNG file, hold [Shift] and drop it on a blank line<br>(Recommended: monochrome, critical chunks only)|
|`code`|`c`|_textdata_|-|Insert barcode / 2D code|
|`command`|`x`|_textdata_|-|Insert device-specific commands|
|`comment`|`_`|_textdata_|-|Insert comment|
|`option`|`o`|_see below_|`code128 2 72 nohri 3 l`|Set barcode / 2D code options<br>(Options are separated by commas or one or more whitespaces)|
|`border`|`b`|`line`<br>`space`<br>`none`<br>`0` - `2`|`space`|Set column border (chars)<br>(Border width: line=1, space=1, none=0)|
|`width`|`w`|`auto`<br>`*`<br>`0` -|`auto`<br>(`*` for all columns)|Set column widths (chars)<br>(Widths are separated by commas or one or more whitespaces)|
|`align`|`a`|`left`<br>`center`<br>`right`|`center`|Set line alignment<br>(Valid when line width &lt; characters per line)|
|`text`|`t`|`wrap`<br>`nowrap`|`wrap`|Set text wrapping|

### Barcode options

Barcode options are separated by commas or one or more whitespaces.  

|Barcode option|Description|
|---|---|
|`upc`|UPC-A, UPC-E<br>(Check digit can be omitted)|
|`ean`<br>`jan`|EAN-13, EAN-8<br>(Check digit can be omitted)|
|`code39`|CODE39|
|`itf`|Interleaved 2 of 5|
|`codabar`<br>`nw7`|Codabar (NW-7)|
|`code93`|CODE93|
|`code128`|CODE128|
|`2` - `4`|Barcode module width (px)|
|`24` - `240`|Barcode module height (px)|
|`hri`|With human readable interpretation|
|`nohri`|Without human readable interpretation|

### 2D code options

2D code options are separated by commas or one or more whitespaces.  

|2D code option|Description|
|---|---|
|`qrcode`|QR Code|
|`3` - `8`|Cell size (px)|
|`l`<br>`m`<br>`q`<br>`h`|Error correction level|

### Special characters in property values

Special characters in property values are different from special characters in text.  

|Special character|Description|
|---|---|
|`\`|Character escape|
|<code>&#x7c;</code>|Column delimiter|
|`{`|Property delimiter (Start)|
|`}`|Property delimiter (End)|
|`:`|Key-value separator|
|`;`|Key-value delimiter|

### Escape sequences in property values

Escape special characters.  

|Escape sequence|Description|
|---|---|
|`\\`|&#x5c;|
|<code>&#x5c;&#x7c;</code>|&#x7c;|
|`\{`|&#x7b;|
|`\}`|&#x7d;|
|`\;`|&#x3b;|
|`\n`|New line|
|`\x`_nn_|Hexadecimal character code|
|`\`_char_ (Others)|Ignore|
