//
//  SFOFile.cs
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

// Information from http://www.psdevwiki.com/ps3/PARAM.SFO

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SFOParser
{
    public class SFOFile
    {
        public struct sfo_header
        {
            public UInt32 magic; /************ Always PSF */
            public byte version; /********** Usually 1.1 */
            public byte revision;
            public UInt16 padding;
            public UInt32 key_table_start; /** Start offset of key_table */
            public UInt32 data_table_start; /* Start offset of data_table */
            public UInt32 tables_entries; /*** Number of entries in all tables */
        };

        public struct sfo_index_table_entry
        {
            public UInt16 key_offset; /*** param_key offset (relative to start offset of key_table) */
            public UInt16 data_fmt; /***** param_data data type */
            public UInt32 data_len; /***** param_data used bytes */
            public UInt32 data_max_len; /* param_data total bytes */
            public UInt32 data_offset; /** param_data offset (relative to start offset of data_table) */
        };

        sfo_header _SFOHeader;
        List<sfo_index_table_entry> _SFOIndexTable;
        Dictionary<string, object> _SFOEntries;
        Dictionary<string, UInt16> _SFOEntriesTypes;

        public sfo_header SFOHeader
        {
            get
            {
                return SFOHeader;
            }
        }

        public List<sfo_index_table_entry> SFOIndexTable
        {
            get
            {
                return _SFOIndexTable;
            }
        }

        public SFOFile(string filename)
        {
            this.filename = filename;
        }

        public string filename
        {
            get;
            set;
        }

        public UInt32 SFOEntries
        {
            get
            {
                return _SFOIndexTable == null ? 0 : (UInt32)_SFOIndexTable.Count;
            }
        }

        public bool OpenSFO()
        {
            Stream sr;
            BinaryReader br;
            FileInfo fi;

            try
            {
                fi = new FileInfo(filename);

                if (fi.Length < 20) // Impossible to have header.
                    return false;

                sr = File.Open(filename, FileMode.Open, FileAccess.Read);
                br = new BinaryReader(sr);

                _SFOHeader = new sfo_header();
                _SFOHeader.magic = br.ReadUInt32();
                _SFOHeader.version = br.ReadByte();
                _SFOHeader.revision = br.ReadByte();
                _SFOHeader.padding = br.ReadUInt16();
                _SFOHeader.key_table_start = br.ReadUInt32();
                _SFOHeader.data_table_start = br.ReadUInt32();
                _SFOHeader.tables_entries = br.ReadUInt32();

                if(_SFOHeader.magic != 0x46535000)
                    return false;

                if(_SFOHeader.data_table_start >= fi.Length || _SFOHeader.key_table_start >= fi.Length)
                    return false;

                br.Close();
                sr.Close();
                fi = null;

                return true;
            }
            catch (Exception Ex)
            {
                return false;
            }
        }

        public bool ReadIndex()
        {
            Stream sr;
            BinaryReader br;
            FileInfo fi;

            try
            {
                fi = new FileInfo(filename);

                sr = File.Open(filename, FileMode.Open, FileAccess.Read);
                br = new BinaryReader(sr);

                if(fi.Length < 0x24)
                    return false;

                br.BaseStream.Seek(0x14, SeekOrigin.Begin);

                _SFOIndexTable = new List<sfo_index_table_entry>();

                for(int i = 0; i < _SFOHeader.tables_entries; i++)
                {
                    sfo_index_table_entry SFOIndexTableEntry = new sfo_index_table_entry();

                    SFOIndexTableEntry.key_offset = br.ReadUInt16();
                    SFOIndexTableEntry.data_fmt = br.ReadUInt16();
                    SFOIndexTableEntry.data_len = br.ReadUInt32();
                    SFOIndexTableEntry.data_max_len = br.ReadUInt32();
                    SFOIndexTableEntry.data_offset = br.ReadUInt32();

                    _SFOIndexTable.Add(SFOIndexTableEntry);
                }


                br.Close();
                sr.Close();
                fi = null;

                return true;
            }
            catch (Exception Ex)
            {
                return false;
            }
        }

        public bool ParseEntries()
        {
            Stream sr;
            BinaryReader br;
            FileInfo fi;

            try
            {
                fi = new FileInfo(filename);

                sr = File.Open(filename, FileMode.Open, FileAccess.Read);
                br = new BinaryReader(sr);

                _SFOEntries = new Dictionary<string, object>();
                _SFOEntriesTypes = new Dictionary<string, UInt16>();

                foreach(sfo_index_table_entry SFOIndexTableEntry in _SFOIndexTable)
                {
                    if((_SFOHeader.key_table_start + SFOIndexTableEntry.key_offset) > fi.Length)
                        return false;

                    br.BaseStream.Seek(_SFOHeader.key_table_start + SFOIndexTableEntry.key_offset, SeekOrigin.Begin);

                    byte key_char;
                    List<byte> key_char_list = new List<byte>();
                    byte[] key_char_array;
                    string key;

                    key_char = br.ReadByte();
                    while(key_char != 0x00)
                    {
                        key_char_list.Add(key_char);
                        key_char = br.ReadByte();
                    }

                    //key_char_array = new byte[key_char_list.Count+1];
                    key_char_array = new byte[key_char_list.Count];

                    for(int i = 0; i < key_char_list.Count; i++)
                    {
                        key_char_array[i] = key_char_list[i];
                    }
                    //                    key_char_array[key_char_array.Length-1] = 0x00;

                    key = Encoding.UTF8.GetString(key_char_array);

                    if((_SFOHeader.data_table_start + SFOIndexTableEntry.data_offset) > fi.Length)
                        return false;

                    br.BaseStream.Seek(_SFOHeader.data_table_start + SFOIndexTableEntry.data_offset, SeekOrigin.Begin);

                    switch(SFOIndexTableEntry.data_fmt)
                    {
                        case 0x0004:
                            {
                                byte[] data_string_b;
                                string data_string;

                                data_string_b = br.ReadBytes((int)SFOIndexTableEntry.data_len);
                                data_string = Encoding.UTF8.GetString(data_string_b);

                                _SFOEntries.Add(key, data_string);
                                _SFOEntriesTypes.Add(key, 0x0004);

                                break;
                            }
                        case 0x0204:
                            {
                                byte[] data_string_b;
                                string data_string;

                                data_string_b = br.ReadBytes((int)SFOIndexTableEntry.data_len-1);
                                data_string = Encoding.UTF8.GetString(data_string_b);

                                _SFOEntries.Add(key, data_string);
                                _SFOEntriesTypes.Add(key, 0x0204);

                                break;
                            }
                        case 0x0404:
                            {
                                UInt32 data = br.ReadUInt32();

                                _SFOEntries.Add(key, data);
                                _SFOEntriesTypes.Add(key, 0x0404);

                                break;
                            }
                        default:
                            {
                                _SFOEntries.Add(key, null);
                                _SFOEntriesTypes.Add(key, SFOIndexTableEntry.data_fmt);

                                break;
                            }
                    }
                }

                br.Close();
                sr.Close();
                fi = null;

                return true;
            }
            catch (Exception Ex)
            {
                return false;
            }
        }

        public string DecodeAllEntries()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string key in _SFOEntries.Keys)
            {
                string description;

                DecodeEntry(key, out description);

                sb.Append(description);
            }

            return sb.ToString();
        }

        public bool DecodeEntry(string key, out string description)
        {
            description = "";

            if (!_SFOEntries.ContainsKey(key))
                return false;

            StringBuilder sb = new StringBuilder();

            object data;
            UInt16 data_type;

            if (!_SFOEntries.TryGetValue(key, out data))
                return false;
            if (!_SFOEntriesTypes.TryGetValue(key, out data_type))
                return false;

            switch (key)
            {
                case "ACCOUNT_ID":
                case "ACCOUNTID":
                    {
                        sb.AppendFormat("PSN Account ID: {0}", data).AppendLine();
                        break;
                    }
                case "ANALOG_MODE":
                    {
                        if ((UInt32)data == 1)
                            sb.AppendLine("DualShock analog sticks supported and enabled.");
                        else
                            sb.AppendLine("DualShock analog sticks not supported or enabled.");
                        break;
                    }
                case "APP_VER":
                    {
                        sb.AppendFormat("Version: {0}", data).AppendLine();
                        break;
                    }
                case "ATTRIBUTE":
                    {
                        if (((UInt32)data & 0x00000001) == 0x00000001)
                            sb.AppendLine("Supports PSP Remote Play v1");
                        if (((UInt32)data & 0x00000002) == 0x00000002)
                            sb.AppendLine("Can be exported to PSP");
                        if (((UInt32)data & 0x00000004) == 0x00000004)
                            sb.AppendLine("Supports PSP Remote Play v2");
                        if (((UInt32)data & 0x00000008) == 0x00000008)
                            sb.AppendLine("XMB In-Game is forcefully enabled");
                        if (((UInt32)data & 0x00000010) == 0x00000010)
                            sb.AppendLine("XMB In-Game disabled");
                        if (((UInt32)data & 0x00000020) == 0x00000020)
                            sb.AppendLine("Supports XMB In-Game background music");
                        if (((UInt32)data & 0x00000040) == 0x00000040)
                            sb.AppendLine("System Voice Chat");
                        if (((UInt32)data & 0x00000080) == 0x00000080)
                            sb.AppendLine("Supports PSvita Remote Play");
                        if (((UInt32)data & 0x00000100) == 0x00000100)
                            sb.AppendLine("Warns about PlayStation Move usage");
                        if (((UInt32)data & 0x00000200) == 0x00000200)
                            sb.AppendLine("Warns about PlayStation Navigation Controller usage");
                        if (((UInt32)data & 0x00000400) == 0x00000400)
                            sb.AppendLine("Warns about PlayStation Eye usage");
                        if (((UInt32)data & 0x00000800) == 0x00000800)
                            sb.AppendLine("Warns that PlayStation Move needs calibration");
                        if (((UInt32)data & 0x00001000) == 0x00001000)
                            sb.AppendLine("Warns about stereoscopic 3D usage");
                        if (((UInt32)data & 0x00002000) == 0x00002000)
                            sb.AppendLine("Unknown flag (0x00002000) set");
                        if (((UInt32)data & 0x00004000) == 0x00004000)
                            sb.AppendLine("Unknown flag (0x00004000) set");
                        if (((UInt32)data & 0x00008000) == 0x00008000)
                            sb.AppendLine("Unknown flag (0x00008000) set");
                        if (((UInt32)data & 0x00010000) == 0x00010000)
                            sb.AppendLine("Install disc");
                        if (((UInt32)data & 0x00020000) == 0x00020000)
                            sb.AppendLine("Show package icon for this disc");
                        if (((UInt32)data & 0x00040000) == 0x00040000)
                            sb.AppendLine("Unknown flag (0x00040000) set");
                        if (((UInt32)data & 0x00080000) == 0x00080000)
                            sb.AppendLine("Enable purchase icon for this content");
                        if (((UInt32)data & 0x00100000) == 0x00100000)
                            sb.AppendLine("Shows software-intented XMB In-Game");
                        if (((UInt32)data & 0x00200000) == 0x00200000)
                            sb.AppendLine("PCEngine game");
                        if (((UInt32)data & 0x00400000) == 0x00400000)
                            sb.AppendLine("Disable license button");
                        if (((UInt32)data & 0x00800000) == 0x00800000)
                            sb.AppendLine("Supports PlayStation Move");
                        if (((UInt32)data & 0x01000000) == 0x01000000)
                            sb.AppendLine("Unknown flag (0x01000000) set");
                        if (((UInt32)data & 0x02000000) == 0x02000000)
                            sb.AppendLine("Unknown flag (0x02000000) set");
                        if (((UInt32)data & 0x04000000) == 0x04000000)
                            sb.AppendLine("Neo-Geo Game");
                        if (((UInt32)data & 0x08000000) == 0x08000000)
                            sb.AppendLine("Unknown flag (0x08000000) set");
                        if (((UInt32)data & 0x10000000) == 0x10000000)
                            sb.AppendLine("Unknown flag (0x10000000) set");
                        if (((UInt32)data & 0x20000000) == 0x20000000)
                            sb.AppendLine("Unknown flag (0x20000000) set");
                        if (((UInt32)data & 0x40000000) == 0x40000000)
                            sb.AppendLine("Unknown flag (0x40000000) set");
                        if (((UInt32)data & 0x80000000) == 0x80000000)
                            sb.AppendLine("Unknown flag (0x80000000) set");

                        break;
                    }
                case "BOOTABLE":
                    {
                        if ((UInt32)data == 0x00000001)
                            sb.AppendLine("Content is bootable");
                        else
                            sb.AppendLine("Content is not bootable");
                        break;
                    }
                case "CATEGORY":
                    {
                        switch ((string)data)
                        {
                            case "AP":
                                {
                                    sb.AppendLine("Category: Photo application");
                                    break;
                                }
                            case "AM":
                                {
                                    sb.AppendLine("Category: Music application");
                                    break;
                                }
                            case "AV":
                                {
                                    sb.AppendLine("Category: Video application");
                                    break;
                                }
                            case "BV":
                                {
                                    sb.AppendLine("Category: Video broadcast");
                                    break;
                                }
                            case "AT":
                                {
                                    sb.AppendLine("Category: TV application");
                                    break;
                                }
                            case "WT":
                                {
                                    sb.AppendLine("Category: TV web");
                                    break;
                                }
                            case "CB":
                                {
                                    sb.AppendLine("Category: Cell/BE application");
                                    break;
                                }
                            case "HM":
                                {
                                    sb.AppendLine("Category: PlayStation Home");
                                    break;
                                }
                            case "SF":
                                {
                                    sb.AppendLine("Category: PlayStation Store");
                                    break;
                                }
                            case "HG":
                                {
                                    sb.AppendLine("Category: HDD game");
                                    break;
                                }
                            case "2G":
                                {
                                    sb.AppendLine("Category: PlayStation 2 game");
                                    break;
                                }
                            case "2P":
                                {
                                    sb.AppendLine("Category: PS2 Classics");
                                    break;
                                }
                            case "ME":
                            case "1P":
                                {
                                    sb.AppendLine("Category: PSone Classics");
                                    break;
                                }
                            case "MN":
                                {
                                    sb.AppendLine("Category: PlayStation Minis");
                                    break;
                                }
                            case "PE":
                                {
                                    sb.AppendLine("Category: PSP Remasters");
                                    break;
                                }
                            case "PP":
                                {
                                    sb.AppendLine("Category: PlayStation Portable Package");
                                    break;
                                }
                            case "GD":
                                {
                                    sb.AppendLine("Category: Game Data");
                                    break;
                                }
                            case "2D":
                                {
                                    sb.AppendLine("Category: PlayStation 2 Data");
                                    break;
                                }
                            case "SD":
                                {
                                    sb.AppendLine("Category: Savegame Data");
                                    break;
                                }
                            case "MS":
                                {
                                    sb.AppendLine("Category: Memory-Stick Data");
                                    break;
                                }
                            case "DG":
                                {
                                    sb.AppendLine("Category: Disc game");
                                    break;
                                }
                            case "AR":
                                {
                                    sb.AppendLine("Category: Autoinstall root");
                                    break;
                                }
                            case "DP":
                                {
                                    sb.AppendLine("Category: Disc package");
                                    break;
                                }
                            case "IP":
                                {
                                    sb.AppendLine("Category: Install package");
                                    break;
                                }
                            case "TR":
                                {
                                    sb.AppendLine("Category: Theme root");
                                    break;
                                }
                            case "VR":
                                {
                                    sb.AppendLine("Category: Video root");
                                    break;
                                }
                            case "XR":
                                {
                                    sb.AppendLine("Category: Extra root");
                                    break;
                                }
                            case "TI":
                                {
                                    sb.AppendLine("Category: Theme item");
                                    break;
                                }
                            case "VI":
                                {
                                    sb.AppendLine("Category: Video item");
                                    break;
                                }
                            case "DM":
                                {
                                    sb.AppendLine("Category: Disc movie");
                                    break;
                                }
                            case "XI":
                                {
                                    sb.AppendLine("Category: Extra item");
                                    break;
                                }
                            case "ac":
                                {
                                    sb.AppendLine("Category: Additional content");
                                    break;
                                }
                            case "gp":
                                {
                                    sb.AppendLine("Category: Game patch");
                                    break;
                                }
                            case "gd":
                                {
                                    sb.AppendLine("Category: Downloadable game");
                                    break;
                                }
                            case "bd":
                                {
                                    sb.AppendLine("Category: Blu-ray disc");
                                    break;
                                }
                            case "gda":
                                {
                                    sb.AppendLine("Category: System application");
                                    break;
                                }
                            case "gdc":
                                {
                                    sb.AppendLine("Category: Non-game application");
                                    break;
                                }
                            case "gdd":
                                {
                                    sb.AppendLine("Category: BG application");
                                    break;
                                }
                            case "gpc":
                                {
                                    sb.AppendLine("Category: Non-game application patch");
                                    break;
                                }
                            case "gpd":
                                {
                                    sb.AppendLine("Category: BG application patch");
                                    break;
                                }
                            case "sd":
                                {
                                    sb.AppendLine("Category: Savegame data");
                                    break;
                                }
                            case "UG":
                                {
                                    sb.AppendLine("Category: UMD game");
                                    break;
                                }
                            case "MG":
                                {
                                    sb.AppendLine("PSP System Software Update");
                                    break;
                                }
                            default:
                                {
                                    sb.AppendFormat("Unknown category: {0}", data).AppendLine();
                                    break;
                                }
                        }
                        break;
                    }
                case "CONTENT_ID":
                    {
                        sb.AppendFormat("Content ID: {0}", data).AppendLine();
                        break;
                    }
                case "DETAIL":
                    {
                        sb.AppendFormat("Detailed description: {0}", data).AppendLine();
                        break;
                    }
                case "GAMEDATA_ID":
                    {
                        sb.AppendFormat("Game data ID: {0}", data).AppendLine();
                        break;
                    }
                case "ITEM_PRIORITY":
                    {
                        sb.AppendFormat("XMB sorting priority: {0}", data).AppendLine();
                        break;
                    }
                case "LANG":
                    {
                        switch ((UInt32)data)
                        {
                            case 0:
                                {
                                    sb.AppendLine("Language used when trophy was installed: 日本語  (Japanese)");
                                    break;
                                }
                            case 1:
                                {
                                    sb.AppendLine("Language used when trophy was installed: English (United States)");
                                    break;
                                }
                            case 2:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Français (French)");
                                    break;
                                }
                            case 3:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Español (España) ");
                                    break;
                                }
                            case 4:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Deutsch (German)");
                                    break;
                                }
                            case 5:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Italiano (Italian)");
                                    break;
                                }
                            case 6:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Nederlands (Dutch)");
                                    break;
                                }
                            case 7:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Português (Portugal) (Portuguese)");
                                    break;
                                }
                            case 8:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Pyccкий (Russian)");
                                    break;
                                }
                            case 9:
                                {
                                    sb.AppendLine("Language used when trophy was installed: 한국어 (Korean)");
                                    break;
                                }
                            case 10:
                                {
                                    sb.AppendLine("Language used when trophy was installed: 繁體中文 (Traditional chinese)");
                                    break;
                                }
                            case 11:
                                {
                                    sb.AppendLine("Language used when trophy was installed: 简体中文 (Simplified chinese)");
                                    break;
                                }
                            case 12:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Suomi (Finnish)");
                                    break;
                                }
                            case 13:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Svenska (Swedish)");
                                    break;
                                }
                            case 14:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Dansk (Danish)");
                                    break;
                                }
                            case 15:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Norsk (Norwegian)");
                                    break;
                                }
                            case 16:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Polski (Polish)");
                                    break;
                                }
                            case 17:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Português (Brasil) (Brazilian)");
                                    break;
                                }
                            case 18:
                                {
                                    sb.AppendLine("Language used when trophy was installed: English (United Kingdom)");
                                    break;
                                }
                            case 19:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Türkçe (Turkish)");
                                    break;
                                }
                            case 20:
                                {
                                    sb.AppendLine("Language used when trophy was installed: Español (Latinoamérica) (Latin-american Spanish)");
                                    break;
                                }
                            default:
                                {
                                    sb.AppendFormat("Unknown language code {0} used when trophy was installed.", data).AppendLine();
                                    break;
                                }
                        }
                        break;
                    }
                case "LICENSE":
                    {
                        sb.AppendFormat("License: {0}", data).AppendLine();
                        break;
                    }
                case "NP_COMMUNICATION_ID ":
                case "NPCOMMID":
                    {
                        sb.AppendFormat("Network Platform Communication ID: {0}", data).AppendLine();
                        break;
                    }
                case "PARENTALLEVEL":
                case "PARENTAL_LEVEL":
                    {
                        if ((UInt32)data == 0)
                            sb.AppendFormat("Parental level: {0}", data).AppendLine();
                        else
                            sb.AppendLine("Parental level disabled.");
                        break;
                    }
                case "PARENTAL_LEVEL_A":
                    {
                        if ((UInt32)data == 0)
                            sb.AppendFormat("Parental level for Americas (SCEA): {0}", data).AppendLine();
                        else
                            sb.AppendLine("Parental level for Americas (SCEA) is disabled.");
                        break;
                    }
                case "PARENTAL_LEVEL_C":
                    {
                        if ((UInt32)data == 0)
                            sb.AppendFormat("Parental level for China (SCH): {0}", data).AppendLine();
                        else
                            sb.AppendLine("Parental level for China (SCH) is disabled.");
                        break;
                    }
                case "PARENTAL_LEVEL_E":
                    {
                        if ((UInt32)data == 0)
                            sb.AppendFormat("Parental level for Europe/Oceania/India/Asia (SCEE): {0}", data).AppendLine();
                        else
                            sb.AppendLine("Parental level for Europe/Oceania/India/Asia (SCEE) is disabled.");
                        break;
                    }
                case "PARENTAL_LEVEL_H":
                    {
                        if ((UInt32)data == 0)
                            sb.AppendFormat("Parental level for Singapore/Taiwan/Hong Kong (SCEH): {0}", data).AppendLine();
                        else
                            sb.AppendLine("Parental level for Singapore/Taiwan/Hong Kong (SCEH) is disabled.");
                        break;
                    }
                case "PARENTAL_LEVEL_J":
                    {
                        if ((UInt32)data == 0)
                            sb.AppendFormat("Parental level for Japan (SCEJ): {0}", data).AppendLine();
                        else
                            sb.AppendLine("Parental level for Japan (SCEJ) is disabled.");
                        break;
                    }
                case "PARENTAL_LEVEL_K":
                    {
                        if ((UInt32)data == 0)
                            sb.AppendFormat("Parental level for South Korea (SCEK): {0}", data).AppendLine();
                        else
                            sb.AppendLine("Parental level for South Korea (SCEK) is disabled.");
                        break;
                    }
                case "PATCH_FILE":
                    {
                        sb.AppendFormat("Patch file: {0}", data).AppendLine();
                        break;
                    }
                case "PS3_SYSTEM_VER":
                    {
                        sb.AppendFormat("Version of PS3 System Software required for the content: {0}", data).AppendLine();
                        break;
                    }
                case "REGION_DENY":
                    {
                        if (((UInt32)data & 0x00000001) == 0x00000001)
                            sb.AppendLine("Content cannot be used in Japan");
                        else
                            sb.AppendLine("Content can be used in Japan");
                        if (((UInt32)data & 0x00000002) == 0x00000002)
                            sb.AppendLine("Content cannot be used in North America");
                        else
                            sb.AppendLine("Content can be used in North America");
                        if (((UInt32)data & 0x00000004) == 0x00000004)
                            sb.AppendLine("Content cannot be used in Europe, Middle East, Asia");
                        else
                            sb.AppendLine("Content can be used in Europe, Middle East, Asia");
                        if (((UInt32)data & 0x00000008) == 0x00000008)
                            sb.AppendLine("Content cannot be used in South Korea");
                        else
                            sb.AppendLine("Content can be used in South Korea");
                        if (((UInt32)data & 0x00000010) == 0x00000010)
                            sb.AppendLine("Content cannot be used in United Kingdom and Ireland");
                        else
                            sb.AppendLine("Content can be used in United Kingdom and Ireland");
                        if (((UInt32)data & 0x00000020) == 0x00000020)
                            sb.AppendLine("Content cannot be used in Central and South America");
                        else
                            sb.AppendLine("Content can be used in Central and South America");
                        if (((UInt32)data & 0x00000040) == 0x00000040)
                            sb.AppendLine("Content cannot be used in Oceania");
                        else
                            sb.AppendLine("Content can be used in Oceania");
                        if (((UInt32)data & 0x00000080) == 0x00000080)
                            sb.AppendLine("Content cannot be used in Southeast Asia");
                        else
                            sb.AppendLine("Content can be used in Southeast Asia");
                        if (((UInt32)data & 0x00000100) == 0x00000100)
                            sb.AppendLine("Content cannot be used in Taiwan");
                        else
                            sb.AppendLine("Content can be used in Taiwan");
                        if (((UInt32)data & 0x00000200) == 0x00000200)
                            sb.AppendLine("Content cannot be used in Russia, Ukraine, India, Central Asia");
                        else
                            sb.AppendLine("Content can be used in Russia, Ukraine, India, Central Asia");
                        if (((UInt32)data & 0x00000400) == 0x00000400)
                            sb.AppendLine("Content cannot be used in China");
                        else
                            sb.AppendLine("Content can be used in China");
                        if (((UInt32)data & 0x00000800) == 0x00000800)
                            sb.AppendLine("Content cannot be used in Hong Kong");
                        else
                            sb.AppendLine("Content can be used in Hong Kong");

                        break;
                    }
                case "RESOLUTION":
                    {
                        if (((UInt32)data & 0x00000001) == 0x00000001)
                            sb.AppendLine("Game supports 640x480 resolution");
                        if (((UInt32)data & 0x00000002) == 0x00000002)
                            sb.AppendLine("Game supports 768x576 resolution");
                        if (((UInt32)data & 0x00000004) == 0x00000004)
                            sb.AppendLine("Game supports 1280x720 resolution");
                        if (((UInt32)data & 0x00000008) == 0x00000008)
                            sb.AppendLine("Game supports 1920x1080 resolution");
                        if (((UInt32)data & 0x00000010) == 0x00000010)
                            sb.AppendLine("Game supports 704x480 resolution");
                        if (((UInt32)data & 0x00000020) == 0x00000020)
                            sb.AppendLine("Game supports 720x576 resolution");

                        if (((UInt32)data & 0xFFFFFFC0) != 0x00000000)
                            sb.AppendFormat("Unknown supported resolution flags: 0x{0:X8}", (UInt32)data & 0xFFFFFFC0).AppendLine();

                        break;
                    }
                case "SAVEDATA_DETAIL":
                    {
                        sb.AppendFormat("Detail text for savegame: {0}", data).AppendLine();
                        break;
                    }
                case "SAVEDATA_DIRECTORY":
                    {
                        sb.AppendFormat("Directory for savegame: {0}", data).AppendLine();
                        break;
                    }
                case "SOUND_FORMAT":
                    {
                        if (((UInt32)data & 0x00000001) == 0x00000001)
                            sb.AppendLine("Game supports sound in LPCM 2.0 format");
                        if (((UInt32)data & 0x00000004) == 0x00000004)
                            sb.AppendLine("Game supports sound in LPCM 5.1 format");
                        if (((UInt32)data & 0x00000010) == 0x00000010)
                            sb.AppendLine("Game supports sound in LPCM 7.1 format");
                        if (((UInt32)data & 0x00000100) == 0x00000100)
                            sb.AppendLine("Game supports sound in Dolby Digital 5.1 format");
                        if (((UInt32)data & 0x00000200) == 0x00000200)
                            sb.AppendLine("Game supports sound in DTS 5.1 format");

                        if (((UInt32)data & 0xFFFFFCEA) != 0x00000000)
                            sb.AppendFormat("Unknown supported sound format flags: 0x{0:X8}", (UInt32)data & 0xFFFFFCEA).AppendLine();

                        break;
                    }
                case "SUB_TITLE":
                    {
                        sb.AppendFormat("Sub-title: {0}", data).AppendLine();
                        break;
                    }
                case "TARGET_APP_VER":
                    {
                        sb.AppendFormat("Application version this patches: {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_ID":
                    {
                        sb.AppendFormat("Title ID: {0}", data).AppendLine();
                        break;
                    }
                case "TITLE":
                    {
                        sb.AppendFormat("Title: {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_0":
                case "TITLE_00":
                    {
                        sb.AppendFormat("Title (Japanese): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_1":
                case "TITLE_01":
                    {
                        sb.AppendFormat("Title (English, United States): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_2":
                case "TITLE_02":
                    {
                        sb.AppendFormat("Title (French): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_3":
                case "TITLE_03":
                    {
                        sb.AppendFormat("Title (Spanish): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_4":
                case "TITLE_04":
                    {
                        sb.AppendFormat("Title (German): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_5":
                case "TITLE_05":
                    {
                        sb.AppendFormat("Title (Italian): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_6":
                case "TITLE_06":
                    {
                        sb.AppendFormat("Title (Dutch): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_7":
                case "TITLE_07":
                    {
                        sb.AppendFormat("Title (Portuguese): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_8":
                case "TITLE_08":
                    {
                        sb.AppendFormat("Title (Russian): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_9":
                case "TITLE_09":
                    {
                        sb.AppendFormat("Title (Korean): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_10":
                    {
                        sb.AppendFormat("Title (Traditional Chinese): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_11":
                    {
                        sb.AppendFormat("Title (Simplified Chinese): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_12":
                    {
                        sb.AppendFormat("Title (Finnish): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_13":
                    {
                        sb.AppendFormat("Title (Swedish): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_14":
                    {
                        sb.AppendFormat("Title (Danish): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_15":
                    {
                        sb.AppendFormat("Title (Norwegian): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_16":
                    {
                        sb.AppendFormat("Title (Polish): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_17":
                    {
                        sb.AppendFormat("Title (Brazilian): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_18":
                    {
                        sb.AppendFormat("Title (English, United Kingdom): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_19":
                    {
                        sb.AppendFormat("Title (Turkish): {0}", data).AppendLine();
                        break;
                    }
                case "TITLE_20":
                    {
                        sb.AppendFormat("Title (Spanish, Latin America): {0}", data).AppendLine();
                        break;
                    }
                case "VERSION":
                    {
                        sb.AppendFormat("Disc/package version: {0}", data).AppendLine();
                        break;
                    }
                case "CONTENT_URL":
                    {
                        sb.AppendFormat("Content URL: {0}", data).AppendLine();
                        break;
                    }
                case "DISC_ID":
                    {
                        sb.AppendFormat("Disc ID: {0}", data).AppendLine();
                        break;
                    }
                case "DISC_NUMBER":
                    {
                        sb.AppendFormat("Disc sequential number: {0}", data).AppendLine();
                        break;
                    }
                case "DISC_TOTAL":
                    {
                        sb.AppendFormat("Total number of discs used: {0}", data).AppendLine();
                        break;
                    }
                case "PSP2_SYSTEM_VER":
                    {
                        sb.AppendFormat("Version of PSVita System Software required for the content: {0}", data).AppendLine();
                        break;
                    }
                case "PSP_SYSTEM_VER":
                    {
                        sb.AppendFormat("Version of PSP System Software required for the content: {0}", data).AppendLine();
                        break;
                    }
                case "STITLE":
                    {
                        sb.AppendFormat("Sub-title: {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_0":
                case "STITLE_00":
                    {
                        sb.AppendFormat("Sub-title (Japanese): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_1":
                case "STITLE_01":
                    {
                        sb.AppendFormat("Sub-title (English, United States): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_2":
                case "STITLE_02":
                    {
                        sb.AppendFormat("Sub-title (French): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_3":
                case "STITLE_03":
                    {
                        sb.AppendFormat("Sub-title (Spanish): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_4":
                case "STITLE_04":
                    {
                        sb.AppendFormat("Sub-title (German): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_5":
                case "STITLE_05":
                    {
                        sb.AppendFormat("Sub-title (Italian): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_6":
                case "STITLE_06":
                    {
                        sb.AppendFormat("Sub-title (Dutch): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_7":
                case "STITLE_07":
                    {
                        sb.AppendFormat("Sub-title (Portuguese): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_8":
                case "STITLE_08":
                    {
                        sb.AppendFormat("Sub-title (Russian): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_9":
                case "STITLE_09":
                    {
                        sb.AppendFormat("Sub-title (Korean): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_10":
                    {
                        sb.AppendFormat("Sub-title (Traditional Chinese): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_11":
                    {
                        sb.AppendFormat("Sub-title (Simplified Chinese): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_12":
                    {
                        sb.AppendFormat("Sub-title (Finnish): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_13":
                    {
                        sb.AppendFormat("Sub-title (Swedish): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_14":
                    {
                        sb.AppendFormat("Sub-title (Danish): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_15":
                    {
                        sb.AppendFormat("Sub-title (Norwegian): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_16":
                    {
                        sb.AppendFormat("Sub-title (Polish): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_17":
                    {
                        sb.AppendFormat("Sub-title (Brazilian): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_18":
                    {
                        sb.AppendFormat("Sub-title (English, United Kingdom): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_19":
                    {
                        sb.AppendFormat("Sub-title (Turkish): {0}", data).AppendLine();
                        break;
                    }
                case "STITLE_20":
                    {
                        sb.AppendFormat("Sub-title (Spanish, Latin America): {0}", data).AppendLine();
                        break;
                    }
                case "DISC_VERSION":
                    {
                        sb.AppendFormat("Disc version: {0}", data).AppendLine();
                        break;
                    }
                case "UPDATER_VER":
                    {
                        sb.AppendFormat("This updates system software to version {0}.", data).AppendLine();
                        break;
                    }
                default:
                    {
                        if (data_type == 0x0004 || data_type == 0x0204)
                            sb.AppendFormat("Don't know how to decode {0} = \"{1}\"", key, data).AppendLine();
                        else if (data_type == 0x0404)
                            sb.AppendFormat("Don't know how to decode {0} = 0x{1:X8}", key, data).AppendLine();
                        else
                            sb.AppendFormat("Unknown key format 0x{0:X4} for key {1}", data_type, key).AppendLine();

                        break;
                    }
            }

            description = sb.ToString();

            return true;
        }
    }
}

