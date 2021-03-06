﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using OpenVIII.Battle;
using OpenVIII.Battle.Dat;
using Abilities = OpenVIII.Battle.Dat.Abilities;

namespace OpenVIII.Dat_Dump
{
    internal static class DumpMonsterAndCharacterDat
    {
        #region Fields

        public static ConcurrentDictionary<int, DatFile> MonsterData = new ConcurrentDictionary<int, DatFile>();
        private static readonly ConcurrentDictionary<int, DatFile> CharacterData = new ConcurrentDictionary<int, DatFile>();

        #endregion Fields

        #region Properties

        private static string Ls => CultureInfo.CurrentCulture.TextInfo.ListSeparator;

        #endregion Properties

        #region Methods

        public static async Task LoadMonsters()
        {
            if (!MonsterData.IsEmpty) return;
            //one issue with this is animations aren't loaded. because it requires all the geometry and skeleton loaded...
            // so the sequence dump is probably less useful or broken.
            Task<bool> addMonster(int i)
            => Task.Run(() => MonsterData.TryAdd(i,
                        MonsterDatFile.CreateInstance(i, 
                            Sections.AnimationSequences | Sections.Information)));

            await Task.WhenAll(Enumerable.Range(0, 200).Select(addMonster));
        }

        public static async Task Process()
        {
            var xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t", // note: default is two spaces
                NewLineOnAttributes = true,
                OmitXmlDeclaration = false
            };
            using (var csv2File = new StreamWriter(new FileStream("MonsterAttacks.csv", FileMode.Create, FileAccess.Write, FileShare.ReadWrite), System.Text.Encoding.UTF8))
            {
                using (var csvFile = new StreamWriter(new FileStream("SequenceDump.csv", FileMode.Create, FileAccess.Write, FileShare.ReadWrite), System.Text.Encoding.UTF8))
                {
                    await LoadMonsters();
                    //header for monster attacks
                    csv2File.WriteLine($"{nameof(Enemy)}{Ls}" +
                        $"{nameof(Enemy.EII.Data.FileName)}{Ls}" +
                        $"{nameof(Abilities)}{Ls}" +
                        $"Number{Ls}" +
                        $"{nameof(Abilities.Animation)}{Ls}" +
                        $"Type{Ls}" +
                        $"ID{Ls}" +
                        $"Name{Ls}");
                    //header for animation info
                    csvFile.WriteLine($"Type{Ls}Type ID{Ls}Name{Ls}Animation Count{Ls}Sequence Count{Ls}Sequence ID{Ls}Offset{Ls}Bytes");
                    using (var xmlWriter = XmlWriter.Create("SequenceDump.xml", xmlWriterSettings))
                    {
                        xmlWriter.WriteStartDocument();
                        xmlWriter.WriteStartElement("dat");

                        XmlMonsterData(xmlWriter, csvFile, csv2File);
                        XmlCharacterData(xmlWriter, csvFile);

                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndDocument();
                    }
                }
            }

            Console.Write("Press [Enter] key to continue...  ");
            Console.ReadLine();
        }

        private static string XmlAnimations(XmlWriter xmlWriter, DatFile battleDatFile)
        {
            var count = $"{battleDatFile.Animations.Count}";
            xmlWriter.WriteStartElement("animations");
            xmlWriter.WriteAttributeString("Count", count);
            xmlWriter.WriteEndElement();
            return count;
        }

        private static void XmlCharacterData(XmlWriter xmlWriter, TextWriter csvFile)
        {
            xmlWriter.WriteStartElement("characters");
            for (var i = 0; i <= 10; i++)
            {
                DatFile test = CharacterDatFile.CreateInstance(i, 0);
                if (test != null && CharacterData.TryAdd(i, test))
                {
                }

                if (!CharacterData.TryGetValue(i, out var battleDat) || battleDat == null) continue;
                const string type = "character";
                xmlWriter.WriteStartElement(type);
                var id = i.ToString();
                xmlWriter.WriteAttributeString("ID", id);
                var name = Memory.Strings.GetName((Characters)i);
                xmlWriter.WriteAttributeString("name", name);
                var prefix0 = $"{type}{Ls}{id}{Ls}";
                var prefix1 = $"{name}";
                prefix1 += $"{Ls}{XmlAnimations(xmlWriter, battleDat)}";
                XmlSequences(xmlWriter, battleDat, csvFile, $"{prefix0}{prefix1}");
                XmlWeaponData(xmlWriter, i, ref battleDat, csvFile, prefix1);
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
        }

        private static void XmlMonsterData(XmlWriter xmlWriter, StreamWriter csvFile, TextWriter csv2File)
        {
            xmlWriter.WriteStartElement("monsters");
            for (var i = 0; i <= 200; i++)
            {
                if (!MonsterData.TryGetValue(i, out var battleDat) || battleDat == null) continue;
                const string type = "monster";
                var id = i.ToString();
                var name = battleDat.Information.Name ?? new FF8String("");
                var prefix = $"{type}{Ls}{id}{Ls}{name}";
                xmlWriter.WriteStartElement(type);
                xmlWriter.WriteAttributeString("ID", id);
                xmlWriter.WriteAttributeString("name", name);
                prefix += $"{Ls}{XmlAnimations(xmlWriter, battleDat)}";
                XmlSequences(xmlWriter, battleDat, csvFile, prefix);
                xmlWriter.WriteEndElement();
                var e = Enemy.Load(new EnemyInstanceInformation { Data = battleDat });
                void addAbility(string fieldName, Abilities a, int number)
                {
                    csv2File.WriteLine($"{name}{Ls}" +
                                       $"{battleDat.FileName}{Ls}" +
                                       $"{fieldName}{Ls}" +
                                       $"{number}{Ls}" +
                                       $"{a.Animation}{Ls}" +
                                       $"{(a.Item != null ? nameof(a.Item) : a.Magic != null ? nameof(a.Magic) : a.Monster != null ? nameof(a.Monster) : "")}{Ls}" +
                                       $"{a.Item?.ID ?? (a.Magic?.MagicDataID ?? (a.Monster?.EnemyAttackID ?? 0))}{Ls}" +
                                       $"\"{(a.Item != null ? a.Item.Value.Name : a.Magic != null ? a.Magic.Name : a.Monster != null ? a.Monster.Name : new FF8String(""))}\"{Ls}");
                }
                void addAbilities(string fieldName, IReadOnlyList<Abilities> abilities)
                {
                    if (abilities == null) return;
                    for (var number = 0; number < e.Info.AbilitiesLow.Length; number++)
                    {
                        var a = abilities[number];
                        addAbility(fieldName, a, number);
                    }
                }
                addAbilities(nameof(e.Info.AbilitiesLow), e.Info.AbilitiesLow);
                addAbilities(nameof(e.Info.AbilitiesMed), e.Info.AbilitiesMed);
                addAbilities(nameof(e.Info.AbilitiesHigh), e.Info.AbilitiesHigh);
            }
            xmlWriter.WriteEndElement();
        }

        private static void XmlSequences(XmlWriter xmlWriter, DatFile battleDatFile, TextWriter csvFile, string prefix)
        {
            xmlWriter.WriteStartElement("sequences");
            var count = $"{battleDatFile.Sequences?.Count ?? 0}";
            xmlWriter.WriteAttributeString("Count", count);
            if (battleDatFile.Sequences != null)
                foreach (var s in battleDatFile.Sequences)
                {
                    xmlWriter.WriteStartElement("sequence");
                    var id = s.ID.ToString();
                    var offset = s.Offset.ToString("X");
                    var bytes = s.Count.ToString();

                    xmlWriter.WriteAttributeString("ID", id);
                    xmlWriter.WriteAttributeString("offset", offset);
                    xmlWriter.WriteAttributeString("bytes", bytes);

                    csvFile?.Write($"{prefix ?? ""}{Ls}{count}{Ls}{id}{Ls}{s.Offset}{Ls}{bytes}");
                    foreach (var b in s)
                    {
                        xmlWriter.WriteString($"{b:X2} ");
                        csvFile?.Write($"{Ls}{b}");
                    }
                    csvFile?.Write(Environment.NewLine);
                    xmlWriter.WriteEndElement();
                }
            csvFile?.Flush();
            xmlWriter.WriteEndElement();
        }

        private static void XmlWeaponData(XmlWriter xmlWriter, int characterID, ref DatFile r, TextWriter csvFile, string prefix1)
        {
            var weaponData = new ConcurrentDictionary<int, DatFile>();
            xmlWriter.WriteStartElement("weapons");
            for (var i = 0; i <= 40; i++)
            {
                DatFile test;
                if (characterID == 1 || characterID == 9)
                    test = WeaponDatFile.CreateInstance(characterID, i, r);
                else
                    test = WeaponDatFile.CreateInstance(characterID, i);
                if (test != null && weaponData.TryAdd(i, test))
                {
                }

                if (!weaponData.TryGetValue(i, out var battleDat) || battleDat == null) continue;
                const string type = "weapon";
                var id = i.ToString();
                xmlWriter.WriteStartElement(type);
                xmlWriter.WriteAttributeString("ID", id);
                var index = ModuleBattleDebug.Weapons[(Characters) characterID]?.Select(((b, i1) => new {i, b}))
                    .FirstOrDefault(v => v.b == i)?.i;
                if(!index.HasValue) continue;
                var currentWeaponData = Memory.KernelBin.WeaponsData.FirstOrDefault(v =>
                    v.Character == (Characters) characterID && v.AltID == checked((byte)index.Value));

                if (currentWeaponData != default)
                {
                    xmlWriter.WriteAttributeString("name", currentWeaponData.Name);

                    var prefix = $"{type}{Ls}{id}{Ls}{currentWeaponData.Name}/{prefix1}"; //bringing over name from character.
                    //xmlWriter.WriteAttributeString("name", Memory.Strings.GetName((Characters)i));

                    XmlAnimations(xmlWriter, battleDat);
                    XmlSequences(xmlWriter, battleDat, csvFile, prefix);
                }
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
        }

        #endregion Methods
    }
}