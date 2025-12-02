/*
    PSG9080_ARB arbitrary wave read/write utility
    Copyright (C) 2021  qrp73
    https://github.com/qrp73/PSG9080_ARB

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

// WARNING: upgrade code is not tested!
// I never run it on the real device and it may not work and can damage device.
// Use it at your own risk!
//#define ALLOW_FWUPGRADE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PSG9080_ARB
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 1)
                {
                    var portName = args[0];
                    using (var psg = new PSG9080(portName))
                    {
                        Ping(psg);
                    }
                }
                else if (args.Length == 4)
                {
                    var portName = args[0];
                    int number;
                    if (!int.TryParse(args[2], out number) || number < 1 || number > 15)
                    {
                        Console.WriteLine("ERROR: invalid wave number {0}", args[2]);
                    }
                    else
                    {
                        var fileName = args[3];
                        if (args[1].ToLowerInvariant() == "-read")
                        {
                            using (var psg = new PSG9080(portName))
                            {
                                var model = Ping(psg);
                                var arbwave = ReadARB(psg, number, model);
                                SaveFile(fileName, arbwave, false);
                            }
                        }
                        else if (args[1].ToLowerInvariant() == "-write")
                        {
                            using (var psg = new PSG9080(portName))
                            {
                                var model = Ping(psg);
                                var arbwave = ParseFile(fileName, false);
                                if (arbwave == null)
                                    return;
                                WriteARB(psg, number, arbwave, model);
                            }
                        }
                        else if (args[1].ToLowerInvariant() == "-read16")
                        {
                            using (var psg = new PSG9080(portName))
                            {
                                var model = Ping(psg);
                                var arbwave = ReadARB(psg, number, model);
                                SaveFile(fileName, arbwave, true);
                            }
                        }
                        else if (args[1].ToLowerInvariant() == "-write16")
                        {
                            using (var psg = new PSG9080(portName))
                            {
                                var model = Ping(psg);
                                var arbwave = ParseFile(fileName, true);
                                if (arbwave == null)
                                    return;
                                //File.WriteAllLines("sine-save-14.txt", arbwave.Select(arg=>string.Format("{0}", arg)).ToArray());
                                WriteARB(psg, number, arbwave, model);
                            }
                        }
                        else
                        {
                            Console.WriteLine("ERROR: invalid operation {0}", args[1]);
                        }
                    }
                }
#if ALLOW_FWUPGRADE
                // WARNING: upgrade code is not tested!
                // I never run it on the real device and it may not work and can damage device.
                // Use it at your own risk!
                else if (args.Length == 3 && args[1].ToLowerInvariant() == "-upgrade")
                {
                    var portName = args[0];
                    var fileName = args[2];
                    using (var psg = new PSG9080(portName))
                    {
                        Ping(psg);
                        Upgrade(psg, fileName, true);
                    }
                }
                else if (args.Length == 3 && args[1].ToLowerInvariant() == "-upgradeboot")
                {
                    var portName = args[0];
                    var fileName = args[2];
                    using (var psg = new PSG9080(portName))
                    {
                        Upgrade(psg, fileName, false);
                    }
                }
#endif
                else
                {
                    Console.WriteLine("PSG9080 arbitrary wave read/write utility");
                    Console.WriteLine("(c)2021 qrp73 v1.01");
                    Console.WriteLine("https://github.com/qrp73/PSG9080_ARB");
                    Console.WriteLine();
                    Console.WriteLine("PSG9080_ARB.exe <serial port>");
                    Console.WriteLine("    Test PSG9080 connection");
                    Console.WriteLine();
                    Console.WriteLine("PSG9080_ARB.exe <serial port> -read  <1...15> <file name>");
                    Console.WriteLine("    Read arbitrary wave from PSG9080 to a file");
                    Console.WriteLine();
                    Console.WriteLine("PSG9080_ARB.exe <serial port> -write <1...15> <file name>");
                    Console.WriteLine("    Write arbitrary wave to PSG9080 from a file");
                    Console.WriteLine();
                    Console.WriteLine("PSG9080_ARB.exe <serial port> -read16  <1...15> <file name>");
                    Console.WriteLine("    Read arbitrary wave from PSG9080 to a file with 14 to 16 bit scale");
                    Console.WriteLine();
                    Console.WriteLine("PSG9080_ARB.exe <serial port> -write16 <1...15> <file name>");
                    Console.WriteLine("    Write arbitrary wave to PSG9080 from a file with 16 to 14 bit scale");
                    Console.WriteLine();
#if ALLOW_FWUPGRADE
                    Console.WriteLine("PSG9080_ARB.exe <serial port> -upgrade <file name>");
                    Console.WriteLine("    Upgrade PSG9080 firmware from a file. Include switch to bootloader mode");
                    Console.WriteLine();
                    Console.WriteLine("PSG9080_ARB.exe <serial port> -upgradeboot <file name>");
                    Console.WriteLine("    Upgrade PSG9080 firmware from a file. Use when already in bootloader mode");
                    Console.WriteLine();
#endif
                }
                //Console.ReadKey();
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
            }
        }

        static string Ping(PSG9080 psg)
        {
            var items = psg.GetAll();
            Console.WriteLine("Model: PSG90{0}", items["00"]);
            Console.WriteLine("S/N:   {0}", items["01"]);
            if (items["00"] == "80")
            {
                // PSG9080
                var version = items["02"].Split(',');
                var major = version
                    .Select(arg => arg.Substring(0, arg.Length - 2))
                    .Select(arg => int.Parse(arg))
                    .ToArray();
                var minor = version
                    .Select(arg => arg.Substring(arg.Length - 2))
                    .Select(arg => int.Parse(arg))
                    .ToArray();
                Console.WriteLine("Hardware: v{0}.{1:D02}", major[0], minor[0]);
                Console.WriteLine("Firmware: v{0}.{1:D02}", major[1], minor[1]);
                Console.WriteLine("FPGA:     v{0}.{1:D02}", major[2], minor[2]);
                Console.WriteLine();
            }
            else
            {
                // JDS2800
                Console.WriteLine("Firmware: {0}", items["02"]);
                Console.WriteLine();
            }
            return items["00"];
        }

        private static ushort[] ReadARB(PSG9080 psg, int number, string model)
        {
            if (number < 1 || number > 15)
                throw new ArgumentOutOfRangeException("number");
            
            // CH1: Select the waveform interface
            var text = psg.Execute(":w24=0,1,0,1.");
            if (text != ":ok")
            {
                Console.WriteLine("ERROR: w24: {0}", text);
                return null;
            }

            // ???
            //text = psg.Execute(":w23=0,13592481.");
            //if (text != ":ok")
            //{
            //    Console.WriteLine("ERROR: w23: {0}", text);
            //    return;
            //}

            var sampleCount = model == "60" ? 2048 : 8192;

            // read arb wave
            // ":B01=0."
            var arbId = string.Format(":B{0:D02}", number);
            if (model == "60")
                arbId = string.Format(":b{0:D02}", number);
            var arbRq = string.Format("{0}=0.", arbId);
            psg.WriteLine(arbRq);
            ProgressUpdate(-1);
            text = psg.ReadLineCallback((byte)',', sampleCount, ProgressUpdate);
            ProgressDone();

            if (!text.StartsWith(arbId, StringComparison.InvariantCulture))
            {
                Console.WriteLine("ERROR: {0}: {1}", arbId, text);
                return null;
            }
            var arbwave = text
                .Substring(5)
                .TrimEnd(',')
                .Split(',')
                .Select(arg => ushort.Parse(arg))
                .ToArray();
            Console.WriteLine("{0} samples downloaded", arbwave.Length);
            return arbwave;
        }

        private static void WriteARB(PSG9080 psg, int number, ushort[] arbwave, string model)
        {
            if (number < 1 || number > 15)
                throw new ArgumentOutOfRangeException("number");
            var sampleCount = model == "60" ? 2048 : 8192;
            if (arbwave.Length != sampleCount)
            {
                Console.WriteLine("ERROR: sample count is not {0}", sampleCount);
                return;
            }

            // CH1: Select the waveform interface
            var text = psg.Execute(":w24=0,1,0,1.");
            if (text != ":ok")
            {
                Console.WriteLine("ERROR: w24: {0}", text);
                return;
            }

            // ???
            //text = psg.Execute(":w23=0,13592481.");
            //if (text != ":ok")
            //{
            //    Console.WriteLine("ERROR: w23: {0}", text);
            //    return;
            //}

            // write arb wave
            // ":A01=...,"
            var arbId = string.Format(":A{0:D02}", number);
            if (model == "60")
                arbId = string.Format(":a{0:D02}", number);
            psg.Write(arbId + "=");
            ProgressUpdate(-1);
            for (var i = 0; i < arbwave.Length; i++)
            {
                psg.Write(string.Format("{0},", arbwave[i]));
                ProgressUpdate(i * 100 / arbwave.Length);
            }
            psg.WriteLine(string.Empty);
            ProgressDone();
            
            text = psg.ReadLine();
            if (text != ":ok")
            {
                Console.WriteLine("ERROR: {0}: {1}", arbId, text);
                return;
            }
            Console.WriteLine("{0} samples uploaded", arbwave.Length);
        }

        private static void SaveFile(string fileName, ushort[] data, bool scale14to16)
        {
            var txts = new string[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                var v = data[i];
                // PSG9080 read format is close to original txt format
                if (!scale14to16)
                    v >>= 2;
                txts[i] = string.Format("{0}", v);
            }
            File.WriteAllLines(fileName, txts);
            Console.WriteLine("{0} samples written into the file", txts.Length);
        }

        private static ushort[] ParseFile(string fileName, bool scale16to14)
        {
            var list = new List<ushort>();
            var txts = File.ReadAllLines(fileName);
            txts = txts
                .Select(arg => arg.Trim())
                .ToArray();
            for (var i = 0; i < txts.Length; i++)
            {
                var vtxt = txts[i];
                vtxt = vtxt.Trim().Trim('\t');
                if (vtxt == string.Empty)
                    continue;
                int test;
                if (!int.TryParse(vtxt, out test) ||
                    test < ushort.MinValue ||
                    test > ushort.MaxValue)
                {
                    Console.WriteLine("ERROR: invalid sample at line {0} (={1})", i, vtxt);
                    return null;
                }
                if (scale16to14)
                {
                    // convert 16 to 14 bit (orignal SW doing it)
                    test >>= 2; 
                }
                if (test > 0x3fff)
                {
                    Console.WriteLine("ERROR: sample value out of 14-bit range at line {0} (={1}, allowed range {2}..{3})", i, vtxt, 0, 0x3fff);
                    return null;
                }
                list.Add((ushort)test);
                //var v = (ushort)test;
                // convert 16 to 14 bit (orignal SW doing it)
                //v >>= 2;
                //list.Add(v);
            }
            Console.WriteLine("{0} samples loaded from file", list.Count);
            return list.ToArray();
        }

        
        #region Upgrade

        const ushort TYPE_FPGA = 0x1234;
        const ushort TYPE_MCU  = 0x5678;

        // WARNING: upgrade code is not tested!
        // I never run it on the real device and it may not work and can damage device.
        // Use it at your own risk!
        private static void Upgrade(PSG9080 psg, string fileName, bool enterBootloader)
        {
            var data = File.ReadAllBytes(fileName);
            Console.WriteLine("{0} bytes loaded from the file", data.Length);
            // 56 78 - MCU
            // 12 34 - FPGA
            var type = (data[data.Length - 2] << 8) | data[data.Length - 1];
            Console.WriteLine("Firmware Type:     {0}",
                type == TYPE_FPGA ? "FPGA" :
                type == TYPE_MCU ?  "MCU" :
                type.ToString("x4"));
            var fw_major = data[data.Length - 4];
            var fw_minor = data[data.Length - 3];
            fw_minor = (byte)((fw_minor >> 4) * 10 + (fw_minor & 0xf));
            Console.WriteLine("Firmware Verision: {0}.{1:D02}",
                fw_major,
                fw_minor);
            Console.WriteLine();
            if (type != TYPE_FPGA && type != TYPE_MCU)
            {
                Console.WriteLine("ERROR: unknown firmware type");
                return;
            }
            {
                var tmp = new byte[data.Length-4];
                Array.Copy(data, tmp, tmp.Length);
                data = tmp;
            }
            
            byte hr;
            if (enterBootloader)
            {
                // enter bootloader
                Console.WriteLine("Enter bootloader...");
                psg.WriteLine(":w98=13692548,2560.");
                hr = psg.ReadBootStatus();
                if (hr != 'H')
                {
                    Console.WriteLine("ERROR: enter bootloader failed: 0x{0:x2}", hr);
                    return;
                }
            }
            else
            {
                Console.WriteLine("Make sure bootloader is active on the device.");
            }

            // user confirmation is required on the device
            Console.Write("Continue to upgrade? [yes/no] ");
            var confirm = Console.ReadLine().ToLowerInvariant();
            if (confirm != "y" && confirm != "yes")
            {
                Console.WriteLine("Operation aborted by user");
                return;
            }
            
            // Begin upgrade...
            if (type == TYPE_FPGA)
                psg.Write("AC");
            else if (type == TYPE_MCU)
                psg.Write("AD");
            else
                return;
            hr = psg.ReadBootStatus();
            if (hr != 'D')
            {
                Console.WriteLine("ERROR: invalid response");
                return;
            }

            Console.Write("Are you sure to upgrade? [yes/no] ");
            confirm = Console.ReadLine().ToLowerInvariant();
            if (confirm != "y" && confirm != "yes")
            {
                Console.WriteLine("Operation aborted by user");
                return;
            }

            var blockCount = data.Length / 2048;
            if ((data.Length % 2048) != 0)
                blockCount++;
            blockCount--;
            for (var i = 0; i <= blockCount; i++)
            {
                var buffer = new byte[2048+6+6];
                for (var j = 0; j < buffer.Length - 6; j++)
                    buffer[j] = 0xff;
                for (var j = buffer.Length - 6; j < buffer.Length; j++)
                    buffer[j] = 0x00;
                buffer[0] = (byte)'A';
                buffer[1] = (byte)'B';
                buffer[4] = (byte)i;
                buffer[5] = (byte)blockCount;
                var length = Math.Min(data.Length - i * 2048, 2048);
                Array.Copy(data, i * 2048, buffer, 6, length);
                var crc = crc16_modbus(buffer, 4, 2048 + 2);
                buffer[2] = (byte)crc;
                buffer[3] = (byte)(crc >> 8);

                var attempt = 0;
                do
                {
                    attempt++;
                    Console.Write("transfer block {0,2} of {1,2}...", i, blockCount);
                    psg.WriteBoot(buffer);
                    hr = psg.ReadBootStatus();
                    if (hr != 'C')
                    {
                        Console.WriteLine("ERROR");
                        if (attempt >= 3)
                        {
                            Console.WriteLine("{0} attempts are failed!", attempt);
                            Console.Write("Do you want to retry? [yes/no] ");
                            confirm = Console.ReadLine().ToLowerInvariant();
                            if (confirm == "n" || confirm == "no")
                            {
                                Console.WriteLine("FATAL ERROR: upgrade failed");
                                return;
                            }
                        }
                        continue;
                    }
                    Console.WriteLine("OK");
                } while (hr != 'C');
            }
            Console.WriteLine();
            Console.WriteLine("transfer done");
        }

        static ushort crc16_modbus(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;
            for (var i = 0; i < length; i++)
            {
                crc ^= (ushort)data[offset + i];
                for (var j = 0; j < 8; j++)
                {
                    crc = (ushort)((crc & 0x0001) != 0 ? (crc >> 1) ^ 0xA001 : crc >> 1);
                }
            }
            return crc;
        }

        #endregion Upgrade


        #region Progress

        private static int _progress = -1;

        private static void ProgressUpdate(int progress)
        {
            if (progress == _progress)
                return;
            _progress = progress;
            progress = Math.Max(progress, 0);
            progress = Math.Min(progress, 100);
            var fill = progress / 5;
            Console.Write("\r{0,3}% [{1}{2}]", 
                progress, 
                new string('=', fill), 
                new string(' ', 20 - fill));
        }

        private static void ProgressDone()
        {
            Console.Write("\r{0}\r", new string(' ', 20 + 2 + 5));
            _progress = -1;
        }

        #endregion Progress
    }
}
