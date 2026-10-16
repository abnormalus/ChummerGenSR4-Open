# Gear catalog search

Search uses literal, case-insensitive matching against the displayed item name, canonical category and localized category label. Category matches include every eligible item in that category, even when the item name does not contain the search term. Comlink, comlinks and commlinks additionally search for commlink; the original text is also matched so custom names are not lost. Quotes and operators are ordinary text, never XPath.

Eligibility is evaluated before matching through EligibleDefinitions/GearSelectionContext. Sources, allowed categories, capacity signs and device upgrade limits remain authoritative. Displayed search identities still retain name plus canonical category, with the existing confirmation revalidation. Search changes neither inventory nor character serialization.

GearEligibilityChecks compares complete commlink category contents with all aliases, checks localized labels and literal quoted input, and exercises context exclusions. NativeGearEligibilityChecks verifies category searches retain both an eligible item and the required exclusions for hardware and capacity contexts. The combined native CI minimum is 105 checks on both platforms.
