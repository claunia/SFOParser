//
//  Program.cs
//
//  Author:
//       Natalia Portillo <claunia@claunia.com>
//
//  Copyright (c) 2014 © Claunia.com
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//

using System;
using System.IO;

namespace SFOParser
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("SFOParser v1.0 - Parses given PARAM.SFO file");
            Console.WriteLine("Copyright (C) 2014 Natalia Portillo");


            if (args.Length != 1)
            {
                Usage();
                Environment.Exit(1);
            }

            Console.WriteLine();

            string filename = args[0];

            if (!File.Exists(filename))
            {
                Console.WriteLine("File \"{0}\" does not exist.", filename);
                Usage();
                Environment.Exit(2);
            }

            SFOFile sfo_file = new SFOFile(filename);

            if (!sfo_file.OpenSFO())
            {
                Console.WriteLine("Unable to read SFO header.");
                Environment.Exit(3);
            }

            if (!sfo_file.ReadIndex())
            {
                Console.WriteLine("Unable to read SFO index.");
                Environment.Exit(4);
            }

            Console.WriteLine("SFO contains {0} entries.", sfo_file.SFOEntries);
            Console.WriteLine("Parsing them...");

            if (!sfo_file.ParseEntries())
            {
                Console.WriteLine("Unable to parse SFO entries.");
                Environment.Exit(5);
            }

            Console.WriteLine("Parsed correctly.");

            Console.WriteLine("All parsed entries:");
            Console.WriteLine("{0}", sfo_file.DecodeAllEntries());
        }

        private static void Usage()
        {
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("\tSFOParser <PARAM.SFO>");
        }
    }
}
