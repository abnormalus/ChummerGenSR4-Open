using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Chummer
{
    internal sealed class StreetNameEntry
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string[] Themes { get; set; } = Array.Empty<string>();
        public string[] Roles { get; set; } = Array.Empty<string>();
        public string[] Cultures { get; set; } = Array.Empty<string>();
        public string[] Regions { get; set; } = Array.Empty<string>();
        public string[] Tones { get; set; } = Array.Empty<string>();
        public string Pattern { get; set; } = "single";
        public double Weight { get; set; } = 1;
        public int Quality { get; set; } = 85;
        public int EraStart { get; set; }
        public int? EraEnd { get; set; }
        public string ReviewStatus { get; set; } = "pending";
        public string Sensitivity { get; set; } = "ordinary";
        public string Source { get; set; }
        public string Pronunciation { get; set; }
    }
    internal sealed class StreetCulture
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Note { get; set; }
        public override string ToString() => Label;
    }
    internal sealed class StreetNameConfig
    {
        public int SchemaVersion { get; set; }
        public List<StreetNameEntry> Words { get; set; }
        public List<StreetNameEntry> Curated { get; set; }
        public List<StreetCulture> Cultures { get; set; }
        public string[] Regions { get; set; }
        public Dictionary<string, double> Patterns { get; set; }
        public Dictionary<string, double> Origins { get; set; }
        public Dictionary<string, double> Strengths { get; set; }
        public Dictionary<string, double> CueWeights { get; set; }
        public Dictionary<string, string[]> SkillSignals { get; set; }
        public Dictionary<string, string[]> QualitySignals { get; set; }
        public Dictionary<string, string[]> GroupSignals { get; set; }
        public Dictionary<string, string> EquipmentTypes { get; set; }
        public Dictionary<string, string[]> EquipmentSignals { get; set; }
        public string[] AllowedPairs { get; set; }
        public string[][] BlockedPairs { get; set; }
        public string[] Exclusions { get; set; }
        public string[] Numbers { get; set; }
        public int ScoreCutoff { get; set; }
        public double CuratedShare { get; set; }
        public int MaxAttempts { get; set; }
        public int PoolSize { get; set; }
        public int MaxCharacters { get; set; }
    }
    internal sealed class StreetNamePreferences
    {
        public bool CharacterAware = true;
        public string Region = "", Primary = "", Secondary = "", Strength = "Balanced", Tone = "Any", Length = "Any", Theme = "Automatic", Origin = "Automatic";
        public string Locked = "", LockPosition = "First", UsedNames = "";
    }
    internal sealed class StreetNameSuggestion
    {
        internal string Name, Origin, Pronunciation;
        internal string[] Cultures;
        internal double Score, ThemeFit;
        internal bool Conflict;
        public override string ToString() => Name;
    }
    internal sealed class StreetNameEngine
    {
        internal StreetNameConfig Config { get; }
        private readonly Random random;
        internal StreetNameEngine(StreetNameConfig config, int? seed = null)
        {
            Config = config; Validate(config); random = seed.HasValue ? new Random(seed.Value) : new Random();
        }
        internal static StreetNameEngine Load(int? seed = null)
        {
            string custom = Path.Combine(MacPaths.CustomDataDirectory, "street-names", "config.json");
            string file = File.Exists(custom) ? custom : Path.Combine(AppContext.BaseDirectory, "data", "street-names", "config.json");
            return new StreetNameEngine(JsonSerializer.Deserialize<StreetNameConfig>(File.ReadAllText(file), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }), seed);
        }
        internal static string Canonical(string value) => string.Concat((value ?? "").Normalize(NormalizationForm.FormKC)
            .Where(char.IsLetterOrDigit)).ToUpperInvariant();
        internal static void Validate(StreetNameConfig config)
        {
            if (config == null || config.SchemaVersion != 1 || config.Words == null || config.Curated == null || config.Cultures == null)
                throw new InvalidDataException("Street name config: schemaVersion must be 1 and dictionaries must be present.");
            if (config.Words.Any(w => w == null || string.IsNullOrWhiteSpace(w.Id) || string.IsNullOrWhiteSpace(w.Text)) ||
                config.Words.Select(w => w.Id).Distinct().Count() != config.Words.Count)
                throw new InvalidDataException("Street name config: missing or duplicate word IDs.");
            if (config.Cultures.Any(c => c == null || string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Label)))
                throw new InvalidDataException("Street name config: culture IDs and labels are required.");
            var cultures = config.Cultures.Select(c => c.Id).ToHashSet();
            if (cultures.Count != config.Cultures.Count || config.Regions == null || config.SkillSignals == null ||
                config.GroupSignals == null || config.QualitySignals == null || config.EquipmentSignals == null || config.EquipmentTypes == null)
                throw new InvalidDataException("Street name config: duplicate cultures or missing signal dictionaries.");
            foreach (var word in config.Words.Concat(config.Curated))
                if (word == null || word.Themes == null || word.Roles == null || word.Cultures == null || word.Regions == null || word.Tones == null || string.IsNullOrWhiteSpace(word.Text) || word.Weight <= 0 || !double.IsFinite(word.Weight) ||
                    word.Cultures.Any(c => !cultures.Contains(c)) || word.EraEnd < word.EraStart)
                    throw new InvalidDataException("Street name config: invalid entry or unknown culture reference: " + word.Text);
            foreach (var weights in new[] { config.Patterns, config.Origins, config.Strengths, config.CueWeights })
                if (weights == null || weights.Count == 0 || weights.Values.Any(w => w < 0 || !double.IsFinite(w)))
                    throw new InvalidDataException("Street name config: invalid weights.");
            if (!config.Patterns.Values.Any(w => w > 0) || !config.Origins.Values.Any(w => w > 0) ||
                config.ScoreCutoff < 0 || config.ScoreCutoff > 100 || config.CuratedShare < 0 || config.CuratedShare > 1 ||
                config.MaxAttempts < 1 || config.PoolSize < 5 || config.MaxCharacters < 3 ||
                config.AllowedPairs == null || config.Numbers == null || config.Exclusions == null || config.BlockedPairs == null)
                throw new InvalidDataException("Street name config: invalid rules or generation limits.");
            if (new[] { "skills", "qualities", "equipment", "physical", "random" }.Any(key => !config.CueWeights.ContainsKey(key)) ||
                new[] { "None", "Subtle", "Balanced", "Strong", "Exclusive" }.Any(key => !config.Strengths.ContainsKey(key)) ||
                config.Strengths.Values.Any(weight => weight > 1) || config.Numbers.Length == 0 ||
                config.BlockedPairs.Any(pair => pair == null || pair.Length != 2))
                throw new InvalidDataException("Street name config: missing required weights, invalid strength, or invalid combination rules.");
            var allowed = new[] { "modifier+creature", "modifier+object", "modifier+name", "title+name" };
            if (config.AllowedPairs.Except(allowed).Any()) throw new InvalidDataException("Street name config: unsupported role pair.");
        }
        private T Pick<T>(IEnumerable<T> items, Func<T, double> weight)
        {
            var choices = items.Select(item => (Item: item, Weight: Math.Max(0, weight(item)))).Where(x => x.Weight > 0).ToArray();
            if (choices.Length == 0) return default;
            double cursor = random.NextDouble() * choices.Sum(x => x.Weight);
            foreach (var choice in choices) { cursor -= choice.Weight; if (cursor <= 0) return choice.Item; }
            return choices.Last().Item;
        }
        private static bool Match(string text, string term) => text?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        internal Dictionary<string, double> Themes(Character character, StreetNamePreferences prefs)
        {
            var themes = Config.Words.SelectMany(w => w.Themes).Distinct().ToDictionary(t => t, _ => 0.0);
            void Spread(Dictionary<string, double> cues, double share)
            {
                double total = cues.Values.Sum();
                foreach (var theme in themes.Keys.ToArray()) themes[theme] += share * (total == 0 ? 1.0 / themes.Count : cues.GetValueOrDefault(theme) / total);
            }
            Dictionary<string, double> Signals(IEnumerable<(string Name, double Weight)> values, Dictionary<string, string[]> mapping)
            {
                var scores = new Dictionary<string, double>();
                foreach (var value in values)
                    foreach (var group in mapping)
                        if (group.Value.Any(term => Match(value.Name, term))) scores[group.Key] = scores.GetValueOrDefault(group.Key) + value.Weight;
                return scores;
            }
            if (character == null || !prefs.CharacterAware) Spread(new(), 1);
            else
            {
                var skills = Signals(character.Skills.Where(s => s.Rating > 0).Select(s => (s.Name, (double)s.Rating * s.Rating)), Config.SkillSignals);
                foreach (var group in character.SkillGroups.Where(s => s.Rating > 0))
                    foreach (var mapping in Config.GroupSignals)
                        if (mapping.Value.Any(term => Match(group.Name, term)))
                            skills[mapping.Key] = skills.GetValueOrDefault(mapping.Key) + group.Rating * group.Rating;
                if (character.MAGEnabled) skills["occult"] = skills.GetValueOrDefault("occult") + 4;
                if (character.RESEnabled) skills["matrix"] = skills.GetValueOrDefault("matrix") + 4;
                Spread(skills, Config.CueWeights["skills"]);
                Spread(Signals(character.Qualities.Select(q => (q.Name, 1.0)), Config.QualitySignals), Config.CueWeights["qualities"]);
                var equipment = Signals(character.Gear.Select(g => (g.Name, 1.0)).Concat(character.Cyberware.Select(g => (g.Name, 1.0)))
                    .Concat(character.Weapons.Select(g => (g.Name, 1.0))).Concat(character.Vehicles.Select(g => (g.Name, 1.0))), Config.EquipmentSignals);
                foreach (var type in new[] { ("weapons", character.Weapons.Count), ("cyberware", character.Cyberware.Count), ("vehicles", character.Vehicles.Count) })
                    if (Config.EquipmentTypes.TryGetValue(type.Item1, out var target)) equipment[target] = equipment.GetValueOrDefault(target) + type.Item2;
                Spread(equipment, Config.CueWeights["equipment"]);
                var physical = new Dictionary<string, double>();
                if (character.STR.TotalValue >= 5) physical["combat"] = 1;
                if (character.CHA.TotalValue >= 5) physical["personality"] = 1;
                if (character.Metatype == "Troll" || character.Metatype == "Ork") physical["personality"] = 1; // Tiny/ironic handles remain only a small influence.
                Spread(physical, Config.CueWeights["physical"]);
                Spread(new(), Config.CueWeights["random"]);
            }
            if (prefs.Theme != "Automatic" && themes.ContainsKey(prefs.Theme))
                foreach (var theme in themes.Keys.ToArray()) themes[theme] = theme == prefs.Theme ? 1 : 0;
            return themes;
        }
        private bool Eligible(StreetNameEntry entry, StreetNamePreferences p)
        {
            if (entry.ReviewStatus != "approved" || entry.Sensitivity != "ordinary" || entry.EraStart > 2060 || entry.EraEnd < 2060) return false;
            if (p.Tone != "Any" && !entry.Tones.Contains(p.Tone.ToLowerInvariant())) return false;
            if (p.Theme != "Automatic" && !entry.Themes.Contains(p.Theme)) return false;
            if (p.Strength == "None" && entry.Cultures.Length > 0) return false;
            bool culture = entry.Cultures.Any(c => c == p.Primary || c == p.Secondary);
            if (p.Strength == "Exclusive") return culture;
            return entry.Cultures.Length == 0 || culture;
        }
        internal List<StreetNameSuggestion> Suggest(Character character, StreetNamePreferences p, int count = 5)
        {
            var themes = Themes(character, p);
            var words = Config.Words.Where(w => Eligible(w, p)).ToArray();
            var curated = Config.Curated.Where(w => Eligible(w, p)).ToArray();
            var primary = words.Where(w => w.Cultures.Contains(p.Primary)).ToArray();
            var secondary = words.Where(w => w.Cultures.Contains(p.Secondary)).ToArray();
            double culturalShare = Config.Strengths.GetValueOrDefault(p.Strength, .4);
            if (primary.Length + secondary.Length == 0) culturalShare = 0;
            double General(StreetNameEntry word) => word.Weight * word.Themes.Select(theme => themes.GetValueOrDefault(theme)).DefaultIfEmpty(0).Average();
            double generalTotal = Math.Max(.0001, words.Sum(General));
            double primaryTotal = Math.Max(.0001, primary.Sum(w => w.Weight)), secondaryTotal = Math.Max(.0001, secondary.Sum(w => w.Weight));
            double Weight(StreetNameEntry word)
            {
                double general = (1 - culturalShare) * General(word) / generalTotal;
                double culture = primary.Length > 0 && word.Cultures.Contains(p.Primary) ? culturalShare * (secondary.Length > 0 ? .8 : 1) * word.Weight / primaryTotal : 0;
                if (secondary.Length > 0 && word.Cultures.Contains(p.Secondary)) culture += culturalShare * (primary.Length > 0 ? .2 : 1) * word.Weight / secondaryTotal;
                if (word.Cultures.Length > 0 && !word.Cultures.Any(c => c == p.Primary || c == p.Secondary)) return 0;
                // Normalize weights across the actual dictionary; a region is an independent soft overlay.
                return (general + culture) * (word.Regions.Contains(p.Region) ? 1.4 : 1);
            }
            var pool = new Dictionary<string, StreetNameSuggestion>();
            var used = p.UsedNames.Split(new[] { '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(Canonical).ToHashSet();
            for (int attempt = 0; attempt < Config.MaxAttempts && pool.Count < Config.PoolSize; attempt++)
            {
                string pattern = Pick(Config.Patterns, pair => pair.Value).Key;
                string origin = p.Origin == "Automatic" ? Pick(Config.Origins, pair => pair.Value).Key : p.Origin;
                StreetNameEntry first = null, second = null;
                string name = null;
                double quality = 80;
                if (random.NextDouble() < Config.CuratedShare)
                {
                    var choices = curated.Where(w => w.Pattern == pattern);
                    first = Pick(choices, Weight); if (first == null) continue;
                    name = first.Text; quality = first.Quality;
                }
                else
                {
                    if (pattern == "compound")
                    {
                        // Compounds use approved whole-name exceptions as templates, never arbitrary noun fusion.
                        first = Pick(curated.Where(w => w.Pattern == "compound"), Weight);
                        if (first == null) continue;
                        name = first.Text; quality = first.Quality;
                    }
                    else if (pattern == "modifier" || pattern == "title")
                    {
                        string role = pattern == "modifier" ? "modifier" : "title";
                        first = Pick(words.Where(w => w.Roles.Contains(role)), Weight);
                        if (first == null) continue;
                        second = Pick(words.Where(w => w.Roles.Any(r => Config.AllowedPairs.Contains(role + "+" + r))), Weight);
                        if (second == null || Canonical(first.Text) == Canonical(second.Text) ||
                            Config.BlockedPairs.Any(pair => pair.Contains(first.Text) && pair.Contains(second.Text))) continue;
                        name = first.Text + " " + second.Text;
                    }
                    else
                    {
                        first = Pick(words.Where(w => w.Roles.Contains("standalone") && (pattern != "cultural" || w.Cultures.Length > 0)), Weight);
                        if (first == null) continue;
                        name = first.Text;
                        if (pattern == "number") name += " " + Config.Numbers[random.Next(Config.Numbers.Length)];
                    }
                }
                if (!string.IsNullOrWhiteSpace(p.Locked)) name = p.LockPosition == "Last" ? name + " " + p.Locked.Trim() : p.Locked.Trim() + " " + name;
                int wordCount = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount > 3 || name.Length > Config.MaxCharacters || Config.Exclusions.Any(x => Canonical(x) == Canonical(name)) ||
                    (p.Length == "One Word" && wordCount != 1) || (p.Length == "Multiple Words" && wordCount < 2)) continue;
                string canonical = Canonical(name);
                if (canonical.Length < 2 || pool.ContainsKey(canonical) || name.Split(' ').Select(Canonical).Distinct().Count() < wordCount) continue;
                double score = Math.Min(100, quality + first.Themes.Sum(t => themes.GetValueOrDefault(t)) * 10 - (name.Length > 12 ? 4 : 0));
                if (score < Config.ScoreCutoff) continue;
                var cultures = first.Cultures.Concat(second?.Cultures ?? Array.Empty<string>()).Distinct().ToArray();
                pool[canonical] = new StreetNameSuggestion { Name = name, Score = score, Origin = origin, Cultures = cultures,
                    Pronunciation = first.Pronunciation, Conflict = used.Contains(canonical),
                    ThemeFit = first.Themes.Concat(second?.Themes ?? Array.Empty<string>()).Select(t => themes.GetValueOrDefault(t)).DefaultIfEmpty(.1).Average() };
            }
            var result = new List<StreetNameSuggestion>();
            while (result.Count < count && pool.Count > 0)
            {
                var cultural = pool.Values.Where(item => item.Cultures.Any(c => c == p.Primary || c == p.Secondary)).ToArray();
                var general = pool.Values.Except(cultural).ToArray();
                bool useCulture = cultural.Length > 0 && (general.Length == 0 || random.NextDouble() < culturalShare);
                var source = useCulture ? cultural : general;
                if (useCulture)
                {
                    var fromPrimary = cultural.Where(item => item.Cultures.Contains(p.Primary)).ToArray();
                    var fromSecondary = cultural.Where(item => item.Cultures.Contains(p.Secondary)).ToArray();
                    if (fromPrimary.Length > 0 && fromSecondary.Length > 0)
                        source = random.NextDouble() < .8 ? fromPrimary : fromSecondary;
                }
                // Preserve requested cultural share after deduplication; ranking alone must not
                // drown a small reviewed dictionary in the larger general candidate pool.
                var selected = Pick(source, item => item.Score * Math.Max(.03, item.ThemeFit) * (item.Conflict ? .15 : 1));
                result.Add(selected); pool.Remove(Canonical(selected.Name));
            }
            return result;
        }
    }
}
