// Read-only desktop oracle. Inputs and reports remain local; never commit private fixtures.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Chummer;
using Majorsilence.Forms.Headless;
if (args.Length < 2) throw new ArgumentException("INPUT_DIRECTORY OUTPUT_DIRECTORY [ROUNDTRIP_INPUT]");
Directory.CreateDirectory(args[1]);
var paths = typeof(Character).Assembly.GetType("Chummer.MacPaths");
paths.GetField("TestUserDirectory", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, Path.Combine(args[1], "user"));
HeadlessRenderer.Use();
paths.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
GlobalOptions.Instance.AutomaticUpdate = false;
LanguageManager.Instance.Load("en-us", null);
if (args.Length > 2)
{
    var character = new Character { FileName = args[2] };
    if (!character.Load()) throw new InvalidDataException("Round-trip load failed.");
    character.FileName = Path.Combine(args[1], "desktop.chum");
    character.Save();
    Console.WriteLine("Desktop load/save passed.");
    return;
}
foreach (string file in Directory.EnumerateFiles(args[0], "*.chum"))
{
    var character = new Character { FileName = file };
    if (!character.Load()) throw new InvalidDataException("Character load failed.");
    var data = new {
        attributes = new[] { "BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG", "MAG", "RES" }.Select(n => new { name = n, value = character.GetAttribute(n).TotalValue }),
        initiative = character.Initiative, passes = character.InitiativePasses, astral = character.AstralInitiative,
        matrix = character.MatrixInitiative, matrixPasses = character.MatrixInitiativePasses,
        physical = character.PhysicalCM, stun = character.StunCM,
        weapons = character.Weapons.Select(w => new { name = w.Name, damage = w.CalculatedDamage(0, true), ap = w.TotalAP }),
        skills = character.Skills.Select(s => new { name = s.Name, pool = s.TotalRating }),
        armor = character.Armor.Select(a => new { name = a.Name, ballistic = a.TotalBallistic, impact = a.TotalImpact })
    };
    File.WriteAllText(Path.Combine(args[1], Path.GetFileNameWithoutExtension(file) + ".json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Audited one character.");
}
