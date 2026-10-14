# SIN name suggestions

Fake SIN name prompts and their detail editor offer a dice button. It fills the editable field with a first name and surname; it does not accept or save the prompt. Manual names and the existing character file format are preserved.

The data comes from the supplied `Names2020_FirstNames_Sex_Top1000.xlsx` and `Names2020_LastNames_RaceHispanic.xlsx`, titled Frequently Occurring Names in the 2020 Census. All 1,000 first names and 156,621 ranked surnames are included; the surname aggregate “ALL OTHER NAMES” is excluded. The compressed JSON includes names and frequency/sex counts only, with no race or ethnicity columns. Generation is offline.

Character Info's free-text Sex/Gender field selects the male or female first-name pool for recognized values (male/M/man/masculine/trans man, female/F/woman/feminine/trans woman). Each pool contains names whose reported count for that sex is at least the other count and weights them by that sex's frequency. Blank, nonbinary or unrecognized text uses all first names weighted by total frequency. Surnames always use the full list weighted by total frequency; no identity, culture or ethnicity is inferred. These contemporary US name lists are suggestions for fictional identities, not rules for a character's identity or a global naming catalogue.

Other prompts can opt into the same implementation using `<selecttext generator="person-name" />` or `ConfigureNameGenerator(character)`. Name pools are loaded once and sampled using cumulative weights. Errors leave manual input available and log only a diagnostic action/error type.
