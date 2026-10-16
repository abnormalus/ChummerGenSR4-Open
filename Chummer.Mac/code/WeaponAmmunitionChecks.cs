#nullable enable
using System;
using System.Linq;

namespace Chummer;

internal static class WeaponAmmunitionChecks
{
    internal static void Run(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.Gear.Clear(); character.Weapons.Clear(); character.Vehicles.Clear();
        var rifle = new Weapon(character) { Name = "Check rifle", Category = "Assault Rifles", WeaponType = "Ranged" };
        var second = new Weapon(character) { Name = "Other rifle", Category = "Assault Rifles", WeaponType = "Ranged" };
        character.Weapons.Add(rifle); character.Weapons.Add(second);
        Gear Ammo(string name, string category = "Assault Rifles", int quantity = 10) =>
            new Gear(character) { Name = name, Category = "Ammunition", Extra = category, Quantity = quantity, Cost = "2" };
        var reserve = Ammo("Ammo: Regular");
        character.Gear.Add(reserve);
        byte[] before = character.CaptureSnapshot();
        var firstView = WeaponAmmunition.Carried(character, rifle);
        var secondView = WeaponAmmunition.Carried(character, second);
        check(firstView.Count == 1 && secondView.Count == 1 && ReferenceEquals(firstView[0], reserve) &&
            ReferenceEquals(secondView[0], reserve) && character.Gear.Count == 1 && character.CaptureSnapshot().SequenceEqual(before),
            "compatible weapon ammunition selections reference one canonical reserve without inventory or persistence changes");
        using (var tree = new Majorsilence.Forms.TreeView())
        {
            var root = new Majorsilence.Forms.TreeNode("Weapons"); tree.Nodes.Add(root);
            root.Nodes.Add(new Majorsilence.Forms.TreeNode { Tag = rifle.InternalId });
            root.Nodes.Add(new Majorsilence.Forms.TreeNode { Tag = second.InternalId });
            WeaponInventoryViews.Refresh(character, tree, false);
            check(root.Nodes.Cast<Majorsilence.Forms.TreeNode>().All(node => node.Nodes.Count == 1 &&
                node.Nodes[0].Tag is WeaponInventoryViews.AmmunitionRow row && row.GearId == reserve.InternalId) &&
                character.CaptureSnapshot().SequenceEqual(before),
                "ammunition tree projection leaves canonical inventory and all serialized costs quantities and identities unchanged");
        }
        reserve.Quantity = 0;
        check(WeaponAmmunition.Carried(character, rifle).Count == 0 && WeaponAmmunition.Carried(character, second).Count == 0,
            "depleted canonical reserve disappears from every compatible ammunition selection");
        reserve.Quantity = 10;
        rifle.AmmoRemaining = 3; rifle.AmmoLoaded = reserve.InternalId;
        rifle.AmmoRemaining--;
        check(reserve.Quantity == 10 && second.AmmoRemaining == 0,
            "loaded ammunition consumption remains distinct from shared reserve and other weapon magazines");

        var container = new Gear(character) { Name = "Check container", Category = "General", Quantity = 0 };
        var child = Ammo("Ammo: APDS"); child.Parent = container; container.Children.Add(child);
        var deep = Ammo("Ammo: Explosive"); deep.Parent = child; child.Children.Add(deep);
        character.Gear.Add(container);
        var wrongClass = Ammo("Ammo: Regular", "Heavy Pistols"); character.Gear.Add(wrongClass);
        var wrongCategory = new Gear(character) { Name = "Ammo: Regular", Category = "General", Extra = "Assault Rifles", Quantity = 10 };
        character.Gear.Add(wrongCategory);
        check(WeaponAmmunition.Carried(character, rifle).SequenceEqual(new[] { reserve, child }),
            "carried ammunition preserves class and category gates and established direct-child containment scope");
        var vehicle = new Vehicle(character); character.Vehicles.Add(vehicle);
        var mounted = Ammo("Ammo: Regular"); vehicle.Gear.Add(mounted);
        var vehicleContainer = new Gear(character) { Name = "Check box", Quantity = 1 };
        vehicleContainer.Children.Add(Ammo("Ammo: APDS")); vehicle.Gear.Add(vehicleContainer);
        check(WeaponAmmunition.Mounted(vehicle, rifle).SequenceEqual(new[] { mounted }),
            "mounted ammunition retains vehicle root stock without borrowing carried or nested supplies");

        var shock = Ammo("Ammo: Stick-n-Shock"); character.Gear.Add(shock); vehicle.Gear.Add(shock);
        character.Options.RestrictStickNShock = true;
        character.Options.StickNShockExcludedWeaponCategories.Add("Assault Rifles");
        check(!WeaponAmmunition.Carried(character, rifle).Contains(shock) && WeaponAmmunition.Mounted(vehicle, rifle).Contains(shock),
            "ammunition policy preserves the existing carried-only Stick-n-Shock preference boundary");
        vehicle.Gear.Remove(shock);
        foreach (var family in new[] {
            (Category: "Grenade Launchers", Names: new[] { "Minigrenade: Flash-Bang" }),
            (Category: "Missile Launchers", Names: new[] { "Missile: HE", "Rocket: HE" }),
            (Category: "Mortar Launchers", Names: new[] { "Mortar Round: HE" }) })
        {
            var launcher = new Weapon(character) { Category = family.Category };
            check(family.Names.All(name => WeaponAmmunition.Matches(launcher, Ammo(name, "Wrong category"))) &&
                !WeaponAmmunition.Matches(launcher, Ammo("Ammo: Regular", family.Category)) &&
                !WeaponAmmunition.Matches(launcher, Ammo(family.Names[0], family.Category, 0)),
                "launcher ammunition retains canonical family and positive-stock rules for " + family.Category);
        }
        var restored = new Character(); restored.LoadSnapshot(character.CaptureSnapshot());
        check(restored.Gear.Single(item => item.InternalId == reserve.InternalId).Quantity == reserve.Quantity &&
            restored.Weapons.Single(item => item.InternalId == rifle.InternalId).AmmoRemaining == 2 &&
            restored.Weapons.Single(item => item.InternalId == rifle.InternalId).AmmoLoaded == reserve.InternalId,
            "shared ammunition selection preserves ordinary reserve GUIDs and loaded-magazine state across legacy saves");
    }
}
