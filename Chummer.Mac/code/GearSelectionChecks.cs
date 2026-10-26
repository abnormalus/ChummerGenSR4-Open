#nullable enable
using System;
using System.Linq;

namespace Chummer;

// Expected purchase contracts retained after retiring legacy presentation.
// Actual native input, modal result and owner restoration are separate native gates.
internal static class GearSelectionChecks
{
    internal static void Run(Character original, Action<bool,string> check)
    {
        var character=new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created=false;
        if(!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
        byte[] snapshot=character.CaptureSnapshot();
        using var adapter=new frmSelectGear(character);
        var model=adapter.CreateModel();
        void Select(string category,string name)
        {
            model.Category=model.Categories.Single(item=>item.Code==category); model.SelectedEntry=model.Items.Single(item=>item.Name==name);
            check(model.SelectedEntry.Name==name,"gear characterization selects synthetic catalog choice "+name);
        }
        string Money(double value)=>string.Format("{0:###,###,##0¥}",value);
        Select("Biotech","Medkit");
        check(model.MinimumRating==1&&model.MaximumRating==6&&model.IsRatingEnabled,"gear rating preserves catalog bounds for a rated item");
        check(model.MinimumQuantity==1&&model.MaximumQuantity==100000&&model.QuantityIncrement==1&&model.Quantity==1&&model.IsQuantityEnabled,"gear ordinary quantity preserves one-unit step and 100000-unit maximum");
        model.Rating=4;model.Quantity=3;check(model.Details?.Cost==Money(1200),"gear preview prices rated quantity without mutating inventory");
        check(!model.Stack&&!model.IsStackVisible,"gear creation draft defaults to unstacked stock");
        Select("Ammunition","Ammo: Regular Ammo");
        check(model.Quantity==10&&model.QuantityIncrement==10&&model.Rating==0&&!model.IsRatingEnabled,"gear selection resets pack quantity and disables unrated input");
        model.Quantity=30;check(model.Details?.Cost==Money(60),"gear ammunition preview divides quantity by catalog pack size");
        Select("Ammunition","Grenade: Fragmentation");
        check(model.IsAerodynamicVisible&&model.Quantity==1&&model.QuantityIncrement==1,"gear grenade transition exposes aerodynamic choice and resets pack size");
        model.Aerodynamic=true;Select("Biotech","Medkit");
        check(!model.Aerodynamic&&!model.IsAerodynamicVisible&&model.MinimumRating==1&&model.MaximumRating==6,"gear leaving grenade clears its option and restores rated bounds");
        model.Rating=3;model.Quantity=2;var accepted=model.AcceptGear(true);
        check(accepted is {Item.Name:"Medkit",Item.Category:"Biotech",Item.Rating:3,Item.Quantity:2,Item.AddAgain:true},"gear Add Again returns exact identity rating and quantity draft");
        using var restrictedAdapter=new frmSelectGear(character){AllowedCategories="Biotech"};var restricted=restrictedAdapter.CreateModel();
        check(!restricted.IsQuantityEnabled,"gear restricted non-ammunition category disables quantity editing");
        check(restrictedAdapter.DialogResult==Majorsilence.Forms.DialogResult.Cancel&&restrictedAdapter.SelectedGear==""&&!restrictedAdapter.AddAgain,"gear unaccepted adapter publishes no selected purchase");
        using var modifiedAdapter=new frmSelectGear(character,true,2,3){AllowedCategories="Commlink Upgrade"};model=modifiedAdapter.CreateModel();
        model.SelectedEntry=model.Items.Single(item=>item.Name=="Response 4");
        check(model.IsDoItYourselfVisible&&model.IsCareer,"gear career presentation exposes markup free and contextual DIY");
        model.DoItYourself=true;model.Markup=20;
        check(model.Details?.Cost==Money(3600)&&model.Details.Availability=="10","gear preview composes DIY markup caller cost and availability modifiers");
        model.FreeCost=true;check(model.Details?.Cost==Money(0),"gear free draft overrides preview price");accepted=model.AcceptGear(false);
        check(accepted is {Item.FreeCost:true,Item.Markup:20,DoItYourself:true},"gear career options survive accepted draft");
        model=new GearPickerModel(character,true,new(InitialCategory:"Matrix Programs"));model.SelectedEntry=model.Items.Single(item=>item.Name=="Analyze");
        model.Rating=3;check(model.IsHackedVisible&&model.Details?.Cost==Money(150),"gear low-rating software uses catalog cost3 tier");
        model.Rating=6;check(model.Details?.Cost==Money(600),"gear medium-rating software uses catalog cost6 tier");
        model.Rating=7;check(model.Details?.Cost==Money(4900),"gear high-rating software uses catalog cost10 tier");
        model.Hacked=true;check(model.Details?.Cost==Money(490),"gear hacked software applies established tenth-price preview");accepted=model.AcceptGear(false);
        check(accepted is {Hacked:true,Item.Rating:7},"gear accepted software draft retains hacked choice and rating");
        var career=new Character();career.LoadSnapshot(snapshot);career.Created=true;byte[] careerSnapshot=career.CaptureSnapshot();
        using var stackAdapter=new frmSelectGear(career,true);model=stackAdapter.CreateModel();
        check(model.IsStackVisible&&model.Stack,"gear created character defaults to stacking eligible purchases");
        stackAdapter.EnableStack=false;model=stackAdapter.CreateModel();check(!model.IsStackVisible&&!model.Stack,"gear disabling stack clears its purchase option");
        check(career.CaptureSnapshot().SequenceEqual(careerSnapshot),"gear career stack drafts preserve complete character snapshot");
        check(character.CaptureSnapshot().SequenceEqual(snapshot),"gear accepted and unaccepted drafts preserve complete character snapshot");
    }
}
