#nullable enable
using System;
using System.Linq;
using Majorsilence.Forms;

namespace Chummer;

internal static class AmmunitionSelectionPolicy
{
    internal static bool CanBuyFor(Weapon? weapon) => weapon != null && weapon.WeaponType == "Ranged" &&
        weapon.RequireAmmo && weapon.Ammo != "0" && !string.IsNullOrEmpty(weapon.AmmoCategory);

    // Preserve the existing spare-clip compatibility rules in one place so contextual
    // buying and ordinary clip filling cannot disagree about ammunition families.
    internal static bool IsCompatible(string name, string category, CharacterOptions options)
    {
        if (string.IsNullOrEmpty(category)) return false;
        if (name == "Ammo: Stick-n-Shock" && options.RestrictStickNShock && options.StickNShockExcludedWeaponCategories.Contains(category)) return false;
        if (name.Contains("Arrow")) return category == "Bows";
        if (name.Contains("Bolt")) return category == "Crossbows";
        if (name.Contains("Assault Cannon")) return category == "Assault Cannons";
        if (name.Contains("Taser Dart")) return category == "Tasers";
        if (name.Contains("Gauss Rifle")) return category == "Gauss Rifles";
        if (name.Contains("Grenade") || name.Contains("Minigrenade")) return category == "Grenade Launchers";
        if (name.Contains("Missile") || name.Contains("Rocket")) return category == "Missile Launchers";
        if (name.Contains("Mortar")) return category == "Mortar Launchers";
        return category is not ("Bows" or "Crossbows" or "Grenade Launchers" or "Missile Launchers" or "Mortar Launchers");
    }

    internal static bool IsKnownCategory(string category) => category == "Cyberware" ||
        XmlManager.Instance.Load("weapons.xml").SelectNodes("/chummer/categories/category")!
            .Cast<System.Xml.XmlNode>().Any(node => node.InnerText == category);
}

internal static class WeaponAmmunitionMenu
{
    internal static void Attach(ContextMenuStrip menu, Func<Weapon?> selection, Action<Weapon> buy)
    {
        var command = new ToolStripMenuItem(LanguageManager.Instance.GetString("Menu_BuyAmmunition"))
        { Name = "buyWeaponAmmunition", Tag = "Menu_BuyAmmunition", Visible = false };
        command.Click += (_, _) => { var weapon = selection(); if (AmmunitionSelectionPolicy.CanBuyFor(weapon)) buy(weapon!); };
        menu.Opening += (_, _) => command.Visible = command.Enabled = AmmunitionSelectionPolicy.CanBuyFor(selection());
        menu.Items.Add(command);
    }
}
