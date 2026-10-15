using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer
{
    // Session-local history. Snapshot only inventory; monetary/ledger changes are reversed as
    // deltas so later unrelated character edits and rewards remain intact.
    internal sealed class InventoryUndo
    {
        private sealed class Snapshot
        {
            internal string Inventory;
            internal string[] Improvements;
            internal Dictionary<string, string> Expenses;
            internal int Nuyen, Karma, Magic, Resonance;
            internal bool MagicEnabled, ResonanceEnabled;
        }
        private readonly Character character;
        private readonly Form owner;
        private readonly Action refresh;
        private readonly List<(Snapshot Before, Snapshot After)> actions = new List<(Snapshot, Snapshot)>();
        private readonly List<ToolStripMenuItem> commands = new List<ToolStripMenuItem>();
        private readonly HashSet<ContextMenuStrip> attachedMenus = new HashSet<ContextMenuStrip>();
        private Snapshot current;
        private bool queued;
        internal bool Restoring { get; private set; }
        internal int Count => actions.Count;
        internal InventoryUndo(Character character, Form owner = null, Action refresh = null)
        { this.character = character; this.owner = owner; this.refresh = refresh;
            try { current = Capture(); } catch (Exception error) { AppDiagnostics.Record("inventory.history-start-failed", error); } }

        private static string Xml(Action<XmlTextWriter> write)
        { using var text = new StringWriter(); using (var writer = new XmlTextWriter(text)) write(writer); return text.ToString(); }
        private static bool InventoryImprovement(Improvement item) => new[] { Improvement.ImprovementSource.Gear,
            Improvement.ImprovementSource.Armor, Improvement.ImprovementSource.ArmorMod, Improvement.ImprovementSource.Cyberware,
            Improvement.ImprovementSource.Bioware, Improvement.ImprovementSource.Nanotech, Improvement.ImprovementSource.Genetech,
            Improvement.ImprovementSource.StackedFocus }.Contains(item.ImproveSource);
        private Snapshot Capture()
        {
            string inventory = Xml(w => {
                w.WriteStartElement("inventory");
                void Group<T>(string name, IEnumerable<T> items, Action<T, XmlTextWriter> save)
                { w.WriteStartElement(name); foreach (var item in items) save(item, w); w.WriteEndElement(); }
                Group("gear", character.Gear, (g, writer) => { if (g is Commlink commlink) commlink.Save(writer);
                    else if (g is OperatingSystem system) system.Save(writer); else g.Save(writer); });
                Group("armor", character.Armor, (x, writer) => x.Save(writer));
                Group("weapons", character.Weapons, (x, writer) => x.Save(writer));
                Group("cyberware", character.Cyberware, (x, writer) => x.Save(writer));
                Group("vehicles", character.Vehicles, (x, writer) => x.Save(writer));
                Group("lifestyles", character.Lifestyles, (x, writer) => x.Save(writer));
                Group("foci", character.Foci, (x, writer) => x.Save(writer));
                Group("stackedfoci", character.StackedFoci, (x, writer) => x.Save(writer));
                Group("locations", character.Locations, (x, writer) => writer.WriteElementString("location", x));
                Group("armorbundles", character.ArmorBundles, (x, writer) => writer.WriteElementString("location", x));
                Group("weaponlocations", character.WeaponLocations, (x, writer) => writer.WriteElementString("location", x));
                w.WriteEndElement();
            });
            return new Snapshot { Inventory = inventory, Nuyen = character.Nuyen, Karma = character.Karma,
                Magic = character.MAG.Value, Resonance = character.RES.Value,
                MagicEnabled = character.MAGEnabled, ResonanceEnabled = character.RESEnabled,
                Improvements = character.Improvements.Where(InventoryImprovement).Select(i => Xml(i.Save)).ToArray(),
                Expenses = character.ExpenseEntries.ToDictionary(e => e.InternalId, e => Xml(e.Save)) };
        }
        internal void QueueObserve()
        {
            if (Restoring || queued || owner == null || owner.IsDisposed) return;
            queued = true;
            owner.BeginInvoke(new Action(() => { queued = false; if (owner.IsDisposed) return;
                try { Observe(); } catch (Exception error) { actions.Clear(); current = null; UpdateCommands(); AppDiagnostics.Record("inventory.history-capture-failed", error); } }));
        }
        internal void Observe()
        {
            if (Restoring) return;
            var next = Capture();
            if (current != null && next.Inventory != current.Inventory)
            {
                actions.Add((current, next));
                if (actions.Count > 5) actions.RemoveAt(0);
            }
            current = next;
            UpdateCommands();
        }
        // Serialized history has no references to the retired editor or character model.
        internal void ContinueAfterImport(InventoryUndo previous)
        {
            if (previous == null || current == null) return;
            actions.Clear();
            actions.AddRange(previous.actions);
            if (previous.current != null && previous.current.Inventory != current.Inventory)
                actions.Add((previous.current, current));
            while (actions.Count > 5) actions.RemoveAt(0);
            UpdateCommands();
        }
        private static XmlNode Node(string xml) { var doc = new XmlDocument(); doc.LoadXml(xml); return doc.DocumentElement; }
        private void RestoreInventory(string xml)
        {
            var root = Node(xml);
            List<T> Load<T>(string group, Func<T> create, Action<T, XmlNode> load) => root.SelectNodes(group + "/*").Cast<XmlNode>()
                .Select(node => { var item = create(); load(item, node); return item; }).ToList();
            var gear = root.SelectNodes("gear/*").Cast<XmlNode>().Select(node => {
                string category = node["category"]?.InnerText;
                if (category == "Commlink" || category == "Commlink Upgrade") { var item = new Commlink(character); item.Load(node); return (Gear)item; }
                if (category == "Commlink Operating System") { var item = new OperatingSystem(character); item.Load(node); return (Gear)item; }
                var plain = new Gear(character); plain.Load(node); return plain;
            }).ToList();
            // Construct everything before replacing collections; a failed load leaves live items intact.
            var armor = Load("armor", () => new Armor(character), (x, n) => x.Load(n));
            var weapons = Load("weapons", () => new Weapon(character), (x, n) => x.Load(n));
            var cyberware = Load("cyberware", () => new Cyberware(character), (x, n) => x.Load(n));
            var vehicles = Load("vehicles", () => new Vehicle(character), (x, n) => x.Load(n));
            var lifestyles = Load("lifestyles", () => new Lifestyle(character), (x, n) => x.Load(n));
            var foci = Load("foci", () => new Focus(), (x, n) => x.Load(n));
            var stacked = Load("stackedfoci", () => new StackedFocus(character), (x, n) => x.Load(n));
            void Replace<T>(List<T> target, IEnumerable<T> values) { target.Clear(); target.AddRange(values); }
            Replace(character.Gear, gear); Replace(character.Armor, armor); Replace(character.Weapons, weapons);
            Replace(character.Cyberware, cyberware); Replace(character.Vehicles, vehicles); Replace(character.Lifestyles, lifestyles);
            Replace(character.Foci, foci); Replace(character.StackedFoci, stacked);
            Replace(character.Locations, root.SelectNodes("locations/location").Cast<XmlNode>().Select(n => n.InnerText));
            Replace(character.ArmorBundles, root.SelectNodes("armorbundles/location").Cast<XmlNode>().Select(n => n.InnerText));
            Replace(character.WeaponLocations, root.SelectNodes("weaponlocations/location").Cast<XmlNode>().Select(n => n.InnerText));
        }
        private void PatchImprovements(Snapshot before, Snapshot after)
        {
            var remove = after.Improvements.ToList();
            var add = before.Improvements.ToList();
            foreach (string common in before.Improvements) { int index = remove.IndexOf(common); if (index >= 0) { remove.RemoveAt(index); add.Remove(common); } }
            foreach (string xml in remove) { var item = character.Improvements.FirstOrDefault(i => Xml(i.Save) == xml); if (item != null) character.Improvements.Remove(item); }
            foreach (string xml in add) { var item = new Improvement(); item.Load(Node(xml)); character.Improvements.Add(item); }
        }
        internal bool Undo()
        {
            Observe();
            if (actions.Count == 0) return false;
            var action = actions[actions.Count - 1];
            var live = Capture();
            Restoring = true;
            try
            {
                int nuyen = checked(character.Nuyen - (action.After.Nuyen - action.Before.Nuyen));
                int karma = checked(character.Karma - (action.After.Karma - action.Before.Karma));
                RestoreInventory(action.Before.Inventory);
                PatchImprovements(action.Before, action.After);
                foreach (string id in action.Before.Expenses.Keys.Union(action.After.Expenses.Keys))
                {
                    action.Before.Expenses.TryGetValue(id, out string before);
                    action.After.Expenses.TryGetValue(id, out string after);
                    if (before == after) continue;
                    character.ExpenseEntries.RemoveAll(e => e.InternalId == id);
                    if (before != null) { var entry = new ExpenseLogEntry(); entry.Load(Node(before)); character.ExpenseEntries.Add(entry); }
                }
                character.Nuyen = nuyen; character.Karma = karma;
                character.MAG.Value -= action.After.Magic - action.Before.Magic;
                character.RES.Value -= action.After.Resonance - action.Before.Resonance;
                if (action.Before.MagicEnabled != action.After.MagicEnabled && character.MAGEnabled == action.After.MagicEnabled)
                    character.MAGEnabled = action.Before.MagicEnabled;
                if (action.Before.ResonanceEnabled != action.After.ResonanceEnabled && character.RESEnabled == action.After.ResonanceEnabled)
                    character.RESEnabled = action.Before.ResonanceEnabled;
                refresh?.Invoke();
                current = Capture();
                actions.RemoveAt(actions.Count - 1);
                UpdateCommands();
                AppDiagnostics.Record("inventory.undo");
                return true;
            }
            catch
            {
                RestoreInventory(live.Inventory);
                character.Nuyen = live.Nuyen; character.Karma = live.Karma;
                character.MAG.Value = live.Magic; character.RES.Value = live.Resonance;
                character.MAGEnabled = live.MagicEnabled; character.RESEnabled = live.ResonanceEnabled;
                character.Improvements.RemoveAll(InventoryImprovement);
                foreach (string xml in live.Improvements) { var item = new Improvement(); item.Load(Node(xml)); character.Improvements.Add(item); }
                character.ExpenseEntries.Clear();
                foreach (string xml in live.Expenses.Values) { var item = new ExpenseLogEntry(); item.Load(Node(xml)); character.ExpenseEntries.Add(item); }
                current = live;
                throw;
            }
            finally { Restoring = false; }
        }
        internal void Attach(ToolStripMenuItem edit, params ContextMenuStrip[] menus)
        {
            ToolStripMenuItem Command()
            {
                var command = new ToolStripMenuItem { Name = "undoInventoryChange" };
                command.Click += (_, _) => { try { Undo(); } catch (Exception error) { AppDiagnostics.Record("inventory.undo-failed", error); AppErrors.Inform(owner, "InventoryUndo_Failed"); } };
                commands.Add(command); return command;
            }
            if (edit != null) edit.DropDownItems.Add(Command());
            foreach (var menu in menus.Where(m => m != null && attachedMenus.Add(m))) menu.Items.Add(Command());
            UpdateCommands();
        }
        private void UpdateCommands()
        {
            foreach (var command in commands) { command.Enabled = Count > 0;
                command.Text = LanguageManager.Instance.GetString("InventoryUndo_Command") + (Count > 0 ? " (" + Count + ")" : ""); }
        }
    }
}
