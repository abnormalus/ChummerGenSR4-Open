#nullable enable
using System;
using System.ComponentModel;
namespace Chummer;

// Draft fields may be empty while typing. Persona keeps its last valid system
// bound until System is valid again; incomplete drafts never produce inventory.
internal sealed class NexusDraft : INotifyPropertyChanged
{
    private int? processor = 10, system = 1, response = 1, firewall = 1, persona = 3, signal = 3;
    private int personaMinimum = 3;
    private bool free, publishing;
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsCareer { get; }
    public int? Processor { get => processor; set => Set(ref processor, value, 10, 50); }
    public int? System
    {
        get => system;
        set
        {
            if (publishing) return;
            value = Clip(value, 1, 10); if (system == value) return; system = value;
            if (value.HasValue) { personaMinimum = value.Value * 3; if (persona.HasValue) persona = Math.Max(persona.Value, personaMinimum); }
            Changed();
        }
    }
    public int? Response { get => response; set => Set(ref response, value, 1, 10); }
    public int? Firewall { get => firewall; set => Set(ref firewall, value, 1, 10); }
    public int? Persona { get => persona; set => Set(ref persona, value, personaMinimum, 50); }
    public int? Signal { get => signal; set => Set(ref signal, value, 1, 10); }
    public int PersonaMinimum => personaMinimum;
    public bool Free { get => free; set { value &= IsCareer; if (free == value) return; free = value; Changed(); } }
    public bool CanAccept => Selection != null;
    public NexusConfiguration? Selection => processor.HasValue && system.HasValue && response.HasValue && firewall.HasValue && persona.HasValue && signal.HasValue
        ? new NexusConfiguration(processor.Value, system.Value, response.Value, firewall.Value, persona.Value, signal.Value, free) : null;
    public NexusQuote? Quote => Selection?.Calculate();
    public string Cost => Quote is { } quote ? string.Format("{0:###,###,##0¥}", quote.Cost) : "";
    public string ResponseAvailability => Availability(Quote?.ResponseAvailability);
    public string SystemAvailability => Availability(Quote?.SystemAvailability);
    public string FirewallAvailability => Availability(Quote?.FirewallAvailability);
    internal NexusDraft(bool career) { IsCareer = career; }
    private static int? Clip(int? value, int minimum, int maximum) => value.HasValue ? Math.Clamp(value.Value, minimum, maximum) : null;
    private void Set(ref int? field, int? value, int minimum, int maximum)
    { if (publishing) return; value = Clip(value, minimum, maximum); if (field == value) return; field = value; Changed(); }
    private static string Availability(string? value) => (value ?? "").Replace("R", LanguageManager.Instance.GetString("String_AvailRestricted")).Replace("F", LanguageManager.Instance.GetString("String_AvailForbidden"));
    private void Changed()
    {
        // Publish the already-coerced persona together with its new range. Ignore
        // transient feedback when the standard input applies that range first.
        publishing = true;
        try
        {
            foreach (string name in new[] { nameof(Processor), nameof(System), nameof(Response), nameof(Firewall), nameof(PersonaMinimum), nameof(Persona), nameof(Signal), nameof(Free), nameof(CanAccept), nameof(Cost), nameof(ResponseAvailability), nameof(SystemAvailability), nameof(FirewallAvailability) })
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        finally { publishing = false; }
    }
}
