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
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ReceiptSharp.Connections;

namespace ReceiptSharp
{
    public class ReceiptSession
    {
        // all states
        private enum State
        {
            Online,
            Print,
            CoverOpen,
            PaperEmpty,
            Error,
            Offline,
            Disconnect,
            DrawerClosed,
            DrawerOpen
        }

        // control commands
        private static byte[] Hello = { 0x10, 0x04, 0x02, 0x1b, 0x06, 0x01, 0x1b, (byte)'@' }; // DLE EOT n ESC ACK SOH ESC @
        private static byte[] SiiAsb = { 0x1d, (byte)'a', 0xff }; // GS a n
        private static byte[] StarAsb = { 0x1b, 0x1e, (byte)'a', 0x01, 0x17 }; // ESC RS a n ETB
        private static byte[] EscPtr = { 0x10, 0x04, 0x02 }; // DLE EOT n
        private static byte[] EscDrw = { 0x10, 0x04, 0x01 }; // DLE EOT n
        private static byte[] EscClr = { 0x10, 0x14, 0x08, 0x01, 0x03, 0x14, 0x01, 0x06, 0x02, 0x08 }; // DLE DC4 n d1 d2 d3 d4 d5 d6 d7
        private static byte[] EscAsb = { 0x1d, (byte)'I', 0x42, 0x1d, (byte)'I', 0x43, 0x1d, (byte)'a', 0xff }; // GS I n GS I n GS a n

        // promise resolver
        private TaskCompletionSource<string> Resolve;
        // status
        private State PrinterStatus = State.Offline;
        // ready
        private bool IsReady = false;
        // update status
        private void Update(State newStatus)
        {
            if (newStatus != PrinterStatus)
            {
                // print response
                if (PrinterStatus == State.Print)
                {
                    Task.Run(() => Resolve?.TrySetResult(newStatus == State.Online ? "Success" : newStatus.ToString()));
                }
                // status event
                PrinterStatus = newStatus;
                StatusChanged?.Invoke(this, PrinterStatus.ToString());
                switch (PrinterStatus)
                {
                    case State.Online:
                        Online?.Invoke(this, EventArgs.Empty);
                        break;
                    case State.CoverOpen:
                        CoverOpen?.Invoke(this, EventArgs.Empty);
                        break;
                    case State.PaperEmpty:
                        PaperEmpty?.Invoke(this, EventArgs.Empty);
                        break;
                    case State.Error:
                        Error?.Invoke(this, EventArgs.Empty);
                        break;
                    case State.Offline:
                        Offline?.Invoke(this, EventArgs.Empty);
                        break;
                    case State.Disconnect:
                        Disconnect?.Invoke(this, EventArgs.Empty);
                        break;
                    default:
                        break;
                }
                // ready event
                if (!IsReady && PrinterStatus == State.Online)
                {
                    Ready?.Invoke(this, EventArgs.Empty);
                    IsReady = true;
                }
            }
        }
        // drawer status
        private State DrawerStatus = State.Offline;
        // invert drawer status
        private bool Invertion = false;
        // update drawer status
        private void UpdateDrawer(State newstatus)
        {
            State d = newstatus;
            // invert drawer status
            if (Invertion)
            {
                switch (d)
                {
                    case State.DrawerClosed:
                        d = State.DrawerOpen;
                        break;
                    case State.DrawerOpen:
                        d = State.DrawerClosed;
                        break;
                    default:
                        break;
                }
            }
            if (d != DrawerStatus)
            {
                // status event
                DrawerStatus = d;
                DrawerChanged?.Invoke(this, DrawerStatus.ToString());
                switch (DrawerStatus)
                {
                    case State.DrawerClosed:
                        DrawerClosed?.Invoke(this, EventArgs.Empty);
                        break;
                    case State.DrawerOpen:
                        DrawerOpen?.Invoke(this, EventArgs.Empty);
                        break;
                    default:
                        break;
                }
            }
        }
        // timer
        private CancellationTokenSource Timeout;
        // printer control language
        private string Printer = "";
        // receive buffer
        private List<byte> Buf = new List<byte>();
        // drain
        private bool Drain = true;
        // connection
        IConnection Conn;

        /**
         * Create instance.
         */
        public ReceiptSession(string destination)
        {
            // open port
            if (IPAddress.TryParse(destination, out _))
            {
                // net
                Conn = new Network(destination);
            }
            else if (SerialPort.GetPortNames().Contains(Regex.Replace(destination, @":.*", "")))
            {
                // serial
                Conn = new Serial(destination);
            }
            else
            {
                // nothing to do
            }
        }

        /**
         * Open session.
         */
        public void Open()
        {
            if (Conn != null)
            {
                Conn.Connected += Conn_Connected;
                Conn.DataSent += Conn_DataSent;
                Conn.ErrorOccurred += Conn_ErrorOccurred;
                Conn.DataReceived += Conn_DataReceived;
                Conn.Disconnected += Conn_Disconnected;
                Conn.Connect();
            }
            else
            {
                Disconnect?.Invoke(this, EventArgs.Empty);
            }
        }

        // drain event
        private void Conn_DataSent(object sender, EventArgs e)
        {
            // write buffer is empty
            Drain = true;
        }

        // open event
        private void Conn_Connected(object sender, EventArgs e)
        {
            Timeout = new CancellationTokenSource();
            Task.Run(async () =>
            {
                try
                {
                    // hello to printer
                    Conn.Send(Hello);
                    // set timer
                    await Task.Delay(3000, Timeout.Token);
                    // buffer clear
                    Conn.Send(new byte[65536]);
                    Conn.Send(Hello);
                    // set timer
                    await Task.Delay(3000, Timeout.Token);
                    // buffer clear
                    Conn.Send(new byte[65536]);
                    Conn.Send(Hello);
                    // set timer
                    await Task.Delay(3000, Timeout.Token);
                    // buffer clear
                    Conn.Send(new byte[65536]);
                    Conn.Send(Hello);
                }
                catch (TaskCanceledException)
                {
                    // nothing to do
                }
            });
        }

        // error event
        private void Conn_ErrorOccurred(object sender, Exception e)
        {
            // clear timer
            Timeout?.Cancel();
            // close port
            Conn?.Disconnect();
        }

        // close event
        private void Conn_Disconnected(object sender, EventArgs e)
        {
            // disconnect event
            Update(State.Disconnect);
            UpdateDrawer(State.Disconnect);
        }

        // data event
        private void Conn_DataReceived(object sender, byte[] e)
        {
            // append data
            Buf.AddRange(e);
            // parse response
            int len;
            do
            {
                len = Buf.Count;
                switch (Printer)
                {
                    case "":
                        if ((Buf[0] & 0xf0) == 0xb0)
                        {
                            // sii: initialized response
                            // clear data
                            Buf.RemoveAt(0);
                            // clear timer
                            Timeout.Cancel();
                            // printer control language
                            Printer = "sii";
                            // enable automatic status
                            Conn.Send(SiiAsb);
                        }
                        else if ((Buf[0] & 0x91) == 0x01)
                        {
                            // star: automatic status
                            if (len > 1)
                            {
                                int l = (Buf[0] >> 2 & 0x18 | Buf[0] >> 1 & 0x07) + (Buf[1] >> 6 & 0x02);
                                // check length
                                if (l <= len)
                                {
                                    // printer
                                    if ((Buf[2] & 0x20) == 0x20)
                                    {
                                        // cover open event
                                        Update(State.CoverOpen);
                                    }
                                    else if ((Buf[5] & 0x08) == 0x08)
                                    {
                                        // paper empty event
                                        Update(State.PaperEmpty);
                                    }
                                    else if ((Buf[3] & 0x2c) != 0 || (Buf[4] & 0x0a) != 0)
                                    {
                                        // error event
                                        Update(State.Error);
                                    }
                                    else
                                    {
                                        // nothing to do
                                    }
                                    // cash drawer
                                    UpdateDrawer((Buf[2] & 0x04) == 0x04 ? State.DrawerOpen : State.DrawerClosed);
                                    // clear data
                                    Buf.RemoveRange(0, l);
                                    // clear timer
                                    Timeout.Cancel();
                                    // printer control language
                                    Printer = "star";
                                    // enable automatic status
                                    Conn.Send(StarAsb);
                                }
                            }
                        }
                        else if ((Buf[0] & 0x93) == 0x12)
                        {
                            // escpos: realtime status
                            if ((Buf[0] & 0x97) == 0x16)
                            {
                                // cover open event
                                Update(State.CoverOpen);
                            }
                            else if ((Buf[0] & 0xb3) == 0x32)
                            {
                                // paper empty event
                                Update(State.PaperEmpty);
                            }
                            else if ((Buf[0] & 0xd3) == 0x52)
                            {
                                // error event
                                Update(State.Error);
                            }
                            else
                            {
                                // initial state
                                PrinterStatus = State.Offline;
                            }
                            // clear data
                            Buf.RemoveAt(0);
                            // clear timer
                            Timeout.Cancel();
                            // printer control language
                            Printer = "escpos";
                            // get drawer status
                            Conn.Send(EscDrw);
                        }
                        else if ((Buf[0] & 0x93) == 0x10)
                        {
                            // escpos: automatic status
                            if (len > 3 && (Buf[1] & 0x90) == 0 && (Buf[2] & 0x90) == 0 && (Buf[3] & 0x90) == 0)
                            {
                                // clear data
                                Buf.RemoveRange(0, 4);
                            }
                        }
                        else if (Buf[0] == 0x35 || Buf[0] == 0x37 || Buf[0] == 0x3b || Buf[0] == 0x3d || Buf[0] == 0x5f)
                        {
                            // escpos: block data
                            int i = Buf.IndexOf(0);
                            // check length
                            if (i > 0)
                            {
                                // clear data
                                Buf.RemoveRange(0, i + 1);
                            }
                        }
                        else
                        {
                            // other
                            Buf.RemoveAt(0);
                        }
                        break;

                    case "sii":
                        if ((Buf[0] & 0xf0) == 0x80)
                        {
                            // sii: status
                            if (PrinterStatus == State.Print && Drain)
                            {
                                // online event
                                Update(State.Online);
                            }
                            // clear data
                            Buf.RemoveAt(0);
                        }
                        else if ((Buf[0] & 0xf0) == 0xc0)
                        {
                            // sii: automatic status
                            if (len > 7)
                            {
                                // printer
                                if ((Buf[1] & 0xf8) == 0xd8)
                                {
                                    // cover open event
                                    Update(State.CoverOpen);
                                }
                                else if ((Buf[1] & 0xf1) == 0xd1)
                                {
                                    // paper empty event
                                    Update(State.PaperEmpty);
                                }
                                else if ((Buf[0] & 0x0b) != 0)
                                {
                                    // error event
                                    Update(State.Error);
                                }
                                else if (PrinterStatus != State.Print)
                                {
                                    // online event
                                    Update(State.Online);
                                }
                                else
                                {
                                    // nothing to do
                                }
                                // cash drawer
                                UpdateDrawer((Buf[3] & 0xf8) == 0xd8 ? State.DrawerClosed : State.DrawerOpen);
                                // clear data
                                Buf.RemoveRange(0, 8);
                            }
                        }
                        else
                        {
                            // sii: other
                            Buf.RemoveAt(0);
                        }
                        break;

                    case "star":
                        if ((Buf[0] & 0xf1) == 0x21)
                        {
                            // star: automatic status
                            if (len > 1)
                            {
                                int l = (Buf[0] >> 2 & 0x08 | Buf[0] >> 1 & 0x07) + (Buf[1] >> 6 & 0x02);
                                // check length
                                if (l <= len)
                                {
                                    // printer
                                    if ((Buf[2] & 0x20) == 0x20)
                                    {
                                        // cover open event
                                        Update(State.CoverOpen);
                                    }
                                    else if ((Buf[5] & 0x08) == 0x08)
                                    {
                                        // paper empty event
                                        Update(State.PaperEmpty);
                                    }
                                    else if ((Buf[3] & 0x2c) != 0 || (Buf[4] & 0x0a) != 0)
                                    {
                                        // error event
                                        Update(State.Error);
                                    }
                                    else if (PrinterStatus != State.Print)
                                    {
                                        // online event
                                        Update(State.Online);
                                    }
                                    else if (Drain)
                                    {
                                        // online event
                                        Update(State.Online);
                                    }
                                    else
                                    {
                                        // nothing to do
                                    }
                                    // cash drawer
                                    UpdateDrawer((Buf[2] & 0x04) == 0x04 ? State.DrawerOpen : State.DrawerClosed);
                                    // clear data
                                    Buf.RemoveRange(0, l);
                                }
                            }
                        }
                        else
                        {
                            // star: other
                            Buf.RemoveAt(0);
                        }
                        break;

                    case "escpos":
                        if ((Buf[0] & 0x93) == 0x12)
                        {
                            // escpos: realtime status
                            // cash drawer
                            UpdateDrawer((Buf[0] & 0x97) == 0x16 ? State.DrawerClosed : State.DrawerOpen);
                            // clear data
                            Buf.RemoveAt(0);
                            // clear timer
                            Timeout.Cancel();
                            if (PrinterStatus != State.Offline)
                            {
                                // printer control language
                                Printer = "";
                                Timeout = new CancellationTokenSource();
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        // set timer
                                        await Task.Delay(3000, Timeout.Token);
                                        // get printer status
                                        Conn.Send(EscPtr);
                                    }
                                    catch (TaskCanceledException)
                                    {
                                        // nothing to do
                                    }
                                });
                            }
                            else
                            {
                                // printer control language
                                Printer = "generic";
                                Timeout = new CancellationTokenSource();
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        // get model info and enable automatic status
                                        Conn.Send(EscAsb);
                                        // set timer
                                        await Task.Delay(3000, Timeout.Token);
                                        // buffer clear
                                        Conn.Send(EscClr);
                                        // set timer
                                        await Task.Delay(3000, Timeout.Token);
                                        // buffer clear
                                        Conn.Send(new byte[65536]);
                                        Conn.Send(EscClr);
                                    }
                                    catch (TaskCanceledException)
                                    {
                                        // nothing to do
                                    }
                                });
                            }
                        }
                        else if ((Buf[0] & 0x93) == 0x10)
                        {
                            // escpos: automatic status
                            if (len > 3 && (Buf[1] & 0x90) == 0 && (Buf[2] & 0x90) == 0 && (Buf[3] & 0x90) == 0)
                            {
                                // clear data
                                Buf.RemoveRange(0, 4);
                            }
                        }
                        else if (Buf[0] == 0x35 || Buf[0] == 0x37 || Buf[0] == 0x3b || Buf[0] == 0x3d || Buf[0] == 0x5f)
                        {
                            // escpos: block data
                            int i = Buf.IndexOf(0);
                            // check length
                            if (i > 0)
                            {
                                // clear data
                                Buf.RemoveRange(0, i + 1);
                            }
                        }
                        else
                        {
                            // other
                            Buf.RemoveAt(0);
                        }
                        break;

                    default:
                        // check response type
                        if ((Buf[0] & 0x90) == 0)
                        {
                            // escpos: status
                            if (PrinterStatus == State.Print && Drain)
                            {
                                // online event
                                Update(State.Online);
                            }
                            // clear data
                            Buf.RemoveAt(0);
                        }
                        else if ((Buf[0] & 0x93) == 0x10)
                        {
                            // escpos: automatic status
                            if (len > 3 && (Buf[1] & 0x90) == 0 && (Buf[2] & 0x90) == 0 && (Buf[3] & 0x90) == 0)
                            {
                                // printer
                                if ((Buf[0] & 0x20) == 0x20)
                                {
                                    // cover open event
                                    Update(State.CoverOpen);
                                }
                                else if ((Buf[2] & 0x0c) == 0x0c)
                                {
                                    // paper empty event
                                    Update(State.PaperEmpty);
                                }
                                else if ((Buf[1] & 0x2c) != 0)
                                {
                                    // error event
                                    Update(State.Error);
                                }
                                else if (PrinterStatus != State.Print)
                                {
                                    // online event
                                    Update(State.Online);
                                }
                                else
                                {
                                    // nothing to do
                                }
                                // cash drawer
                                UpdateDrawer((Buf[0] & 0x04) == 0x04 ? State.DrawerClosed : State.DrawerOpen);
                                // clear data
                                Buf.RemoveRange(0, 4);
                            }
                        }
                        else if (Buf[0] == 0x35 || Buf[0] == 0x37 || Buf[0] == 0x3b || Buf[0] == 0x3d || Buf[0] == 0x5f)
                        {
                            // escpos: block data
                            int i = Buf.IndexOf(0);
                            // check length
                            if (i > 0)
                            {
                                // clear data
                                List<byte> block = Buf.GetRange(0, i + 1);
                                Buf.RemoveRange(0, i + 1);
                                if (block[0] == 0x5f)
                                {
                                    // clear timer
                                    Timeout.Cancel();
                                    // model info
                                    string model = Encoding.ASCII.GetString(block.ToArray(), 1, block.Count - 2).ToLower();

                                    if (Printer == "generic" && Regex.IsMatch(model, @"^(epson|citizen|fit)$")) {
                                        // escpos thermal
                                        Printer = model;
                                    }
                                        else if (Printer == "epson" && Regex.IsMatch(model, @"^tm-u"))
                                    {
                                        // escpos impact
                                        Printer = "impactb";
                                    }
                                    else
                                    {
                                        // nothing to do
                                    }
                                }
                                else if (block[0] == 0x3b)
                                {
                                    // power on
                                    if (block[1] == 0x31)
                                    {
                                        // printer control language
                                        Printer = "";
                                        // hello to printer
                                        Conn.Send(Hello);
                                    }
                                    // offline event
                                    Update(State.Offline);
                                    UpdateDrawer(State.Offline);
                                }
                                else if (block[0] == 0x37)
                                {
                                    // buffer clear
                                    if (block[1] == 0x25)
                                    {
                                        // clear timer
                                        Timeout.Cancel();
                                        // get model info and enable automatic status
                                        Conn.Send(EscAsb);
                                    }
                                }
                                else
                                {
                                    // nothing to do
                                }
                            }
                        }
                        else if ((Buf[0] & 0x93) == 0x12)
                        {
                            // escpos: realtime status
                            // clear timer
                            Timeout.Cancel();
                            // cash drawer
                            UpdateDrawer((Buf[0] & 0x97) == 0x16 ? State.DrawerClosed : State.DrawerOpen);
                            // clear data
                            Buf.RemoveAt(0);
                        }
                        else
                        {
                            // escpos: other
                            Buf.RemoveAt(0);
                        }
                        break;
                }
            }
            while (Buf.Count > 0 && Buf.Count < len);
        }

        /**
         * Printer status.
         * @type {string} printer status
         */
        public string Status
        {
            get
            {
                return PrinterStatus.ToString();
            }
        }

        /**
         * Cash drawer status.
         * @type {string} cash drawer status
         */
        public string Drawer
        {
            get
            {
                return DrawerStatus.ToString();
            }
        }

        /**
         * Invert cash drawer state.
         * @param {boolean} invert invert cash drawer state
         */
        public void InvertDrawerState(bool invert)
        {
            Invertion = !!invert;
            switch (DrawerStatus)
            {
                case State.DrawerClosed:
                    DrawerStatus = State.DrawerOpen;
                    break;
                case State.DrawerOpen:
                    DrawerStatus = State.DrawerClosed;
                    break;
                default:
                    break;
            }
        }

        // control commands
        private static byte[] StarHead = new byte[] { 0x1b, 0x1e, (byte)'a', 0x00 };
        private static byte[] StarTail1 = new byte[] { 0x1b, 0x1d, 0x03, 0x01, 0x00, 0x00 };
        private static byte[] StarTail2 = new byte[] { 0x1b, 0x1d, 0x03, 0x01, 0x00, 0x00, 0x04 };
        private static byte[] StarTail3 = new byte[] { 0x1b, 0x06, 0x01 };
        private static byte[] EscHead = new byte[] { 0x1b, (byte)'@', 0x1d, (byte)'a', 0x00 };

        /**
         * Print receipt markdown.
         * @param {string} markdown receipt markdown
         * @param {string} [options] print options
         * @returns {string} print result
         */
        public async Task<string> Print(string markdown, string options = "")
        {
            // online or ready
            if (PrinterStatus == State.Online)
            {
                // asynchronous printing
                Resolve = new TaskCompletionSource<string>();
                // print event
                Update(State.Print);
                // convert markdown to printer command
                byte[] command = new Receipt(markdown, $"-p {Printer} {options}").ToCommand();
                // write command
                if (Regex.IsMatch(Printer, @"^star$")) {
                    // star
                    if (command.Skip(2).Take(StarHead.Length).SequenceEqual(StarHead))
                    {
                        command[5] = 0x01; // ESC @ ESC RS a n
                    }
                    else if (command.Take(StarHead.Length).SequenceEqual(StarHead))
                    {
                        command[3] = 0x01; // ESC RS a n
                    }
                    if (command.Skip(command.Length - StarTail1.Length).SequenceEqual(StarTail1))
                    {
                        byte[] b = new byte[command.Length - StarTail1.Length + 1];
                        Buffer.BlockCopy(command, 0, b, 0, command.Length - StarTail1.Length);
                        b[b.Length - 1] = 0x17; // ETB
                        command = b;
                    }
                    else if (command.Skip(command.Length - StarTail2.Length).SequenceEqual(StarTail2))
                    {
                        byte[] b = new byte[command.Length - StarTail2.Length + 1];
                        Buffer.BlockCopy(command, 0, b, 0, command.Length - StarTail2.Length);
                        b[b.Length - 1] = 0x17; // ETB
                        command = b;
                    }
                    else if (command.Skip(command.Length - StarTail3.Length).SequenceEqual(StarTail3))
                    {
                        byte[] b = new byte[command.Length - StarTail3.Length + 1];
                        Buffer.BlockCopy(command, 0, b, 0, command.Length - StarTail3.Length);
                        b[b.Length - 1] = 0x17; // ETB
                        command = b;
                    }
                    _ = Task.Run(() => Conn.Send(command));
                }
                else
                {
                    // escpos
                    if (command.Take(EscHead.Length).SequenceEqual(EscHead))
                    {
                        command[4] = 0xff; // ESC @ GS a n
                    }
                    _ = Task.Run(() => Conn.Send(command));
                }
                return await Resolve.Task;
            }
            else {
                // print response
                return PrinterStatus.ToString();
            }
        }

        /**
         * Close session.
         */
        public void Close() {
            // clear timer
            Timeout?.Cancel();
            // close port
            Conn?.Disconnect();
        }

        /**
         * Event listeners.
         */
        public event EventHandler<string> StatusChanged;
        public event EventHandler Ready;
        public event EventHandler Online;
        public event EventHandler CoverOpen;
        public event EventHandler PaperEmpty;
        public event EventHandler Error;
        public event EventHandler Offline;
        public event EventHandler Disconnect;
        public event EventHandler<string> DrawerChanged;
        public event EventHandler DrawerClosed;
        public event EventHandler DrawerOpen;
    }
}
