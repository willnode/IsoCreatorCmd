using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BER.CDCat.Export;
using IsoCreatorLib;
using static IsoCreatorLib.IsoCreator;

namespace IsoCreatorCmd
{
    class Program
    {
        static readonly string helptext = @"
ISO CREATOR CMD.

Command Utility for creating ISO 9660 Joliet file.

Syntax: IsoCreatorCmd SOURCE_DIR [OUTPUT] [VOL_NAME]

SOURCE_DIR  Source Directory
OUTPUT  Output ISO filename (default taken from SOURCE_DIR name + "".iso"")
VOL_NAME    Volume name (default taken from SOURCE_DIR name)
";

        static string NiceProgress(string Stat, int Current, int Max)
        {
            string c = Stat;
            int len = 60 - c.Length;
            int frag = Math.Max(1, (Current * len) / Max);
            c += $" [{ new string('=', frag) }>{new string(' ', len - frag)}] { ((float)Current / Max).ToString("P2", CultureInfo.InvariantCulture) } ({Current}/{Max})";
            c += new string(' ', Math.Max(0, 80 - c.Length));
            return c;
        }
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(helptext);
                Environment.Exit(0);
            }
            else if (args.Length >= 1 || args.Length == 3)
            {
                var src = args[0];
                var dest = args.Length >=2 ? args[1] : Path.GetFileName(src);
                var vol = args.Length >= 3 ? args[2] : Path.GetFileName(src);

                if (Path.GetExtension(dest) == "")
                {
                    dest += ".iso";
                }

                Console.WriteLine($"About writing {src} to {dest} with volume name '{vol}'");

                var creator = new IsoCreator();
                var lastAct = "";
                var lastMax = 1;
                creator.Progress += delegate (object sender, ProgressEventArgs e) {
                    if (e.Maximum >= 0)
                    {
                        if (e.Action != null && e.Action != lastAct)
                        {
                            if (lastAct != "")
                            Console.Write(NiceProgress(lastAct, lastMax, lastMax) + "\r");
                            lastAct = e.Action;
                            Console.WriteLine();
                        }
                        lastMax = e.Maximum;
                    }
                    Console.Write(NiceProgress(lastAct, e.Current, lastMax) + "\r");
                };

                creator.Finish += delegate (object sender, FinishEventArgs e) {
                    Console.WriteLine($"Finished: {e.Message}");
                    Environment.Exit(0);
                };

                creator.Abort += delegate (object sender, AbortEventArgs e) {
                    Console.WriteLine($"Aborted: {e.Message}");
                    Environment.Exit(1);
                };

                new Thread(new ParameterizedThreadStart(creator.Folder2Iso))
                    .Start(new IsoCreatorFolderArgs(src, dest, vol));

            }
            else
            {
                Console.WriteLine("Invalid argument length!");
                Environment.Exit(1);
            }
        }
    }
}
