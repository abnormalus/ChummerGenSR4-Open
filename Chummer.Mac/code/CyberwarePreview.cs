using System;
using System.Xml;
using System.Xml.XPath;

namespace Chummer;

internal sealed record CyberwarePreviewRequest(decimal Rating, bool Bioware, double GradeCostMultiplier,
    double GradeEssenceMultiplier, int GradeAvailabilityModifier, double CharacterEssenceMultiplier,
    double BasicBiowareEssenceMultiplier, double GenetechCostMultiplier, double TransgenicCostMultiplier,
    bool DiscountEnabled, decimal EssenceDiscount, bool Transgenic, bool FreeCost);
internal sealed record CyberwarePreviewResult(string Availability, string Cost, string AvailabilityTest, string Essence, string Capacity);

// Unattached preview only: preserve legacy expression order, rounding and fallback
// conventions. Frozen pre-delegation legacy results independently verify this path.
internal static class CyberwarePreview
{
    internal static CyberwarePreviewResult Calculate(Character character, XmlDocument catalog, XmlNode definition,
        CyberwarePreviewRequest request)
    {
        string availability = "", cost = "", availabilityTest = "", essence = "", capacity = "";
        string effectiveCategory = definition["category"].InnerText;
        // If the Transgenics checkbox has been checked, force it to the Genetech: Transgenics category instead.
        if (request.Transgenic)
            effectiveCategory = "Genetech: Transgenics";

        // Place the Essence cost multiplier in a variable that can be safely modified.
        double characterEssenceMultiplier = request.CharacterEssenceMultiplier;

        // If Basic Bioware is selected, apply the Basic Bioware ESS Multiplier.
        if (effectiveCategory == "Basic")
            characterEssenceMultiplier -= (1 - request.BasicBiowareEssenceMultiplier);

        // Genetech and Genetic Infusions are not subject to Bioware cost multipliers, so if we're looking at either, suppress the multiplier.
        if (effectiveCategory.StartsWith("Genetech") || effectiveCategory.StartsWith("Genetic Infusions"))
            characterEssenceMultiplier = 1;

        if (request.DiscountEnabled)
        {
            double discountMultiplier = Convert.ToDouble(request.EssenceDiscount, GlobalOptions.Instance.CultureInfo) * 0.01;
            characterEssenceMultiplier -= discountMultiplier;
        }

        characterEssenceMultiplier -= (1 - request.GradeEssenceMultiplier);

        // Place the Genetech cost multiplier in a variable that can be safely modified.
        double genetechCostMultiplier = 1;
        // Genetech cost modifier only applies to Genetech.
        if (effectiveCategory.StartsWith("Genetech") || effectiveCategory.StartsWith("Genetic Infusions"))
            genetechCostMultiplier = request.GenetechCostMultiplier;

        // The transgenic category combines the established cost reductions.
        if (effectiveCategory == "Genetech: Transgenics")
            genetechCostMultiplier -= (1 - request.TransgenicCostMultiplier);


        // Extract availability and cost expressions, including rating-based formulas.
        // This is done using XPathExpression.
        XPathNavigator navigator = catalog.CreateNavigator();

        // Avail.
        // If avail contains "F" or "R", remove it from the string so we can use the expression.
        string availabilitySuffix = "";
        string availabilityExpression = definition["avail"].InnerText;
        XPathExpression compiledAvailability;
        if (availabilityExpression.Substring(availabilityExpression.Length - 1, 1) == "F" || availabilityExpression.Substring(availabilityExpression.Length - 1, 1) == "R")
        {
            availabilitySuffix = availabilityExpression.Substring(availabilityExpression.Length - 1, 1);
            // Remove the trailing character if it is "F" or "R".
            availabilityExpression = availabilityExpression.Substring(0, availabilityExpression.Length - 1);
        }
        try
        {
            compiledAvailability = navigator.Compile(availabilityExpression.Replace("Rating", request.Rating.ToString()));
            string evaluatedExpression = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledAvailability));
            int availabilityValue = Convert.ToInt32(evaluatedExpression) + request.GradeAvailabilityModifier;
            // Avail cannot go below 0.
            if (availabilityValue < 0)
                availabilityValue = 0;
            availability = availabilityValue.ToString() + availabilitySuffix;
        }
        catch
        {
            if (definition["avail"].InnerText.StartsWith("FixedValues"))
            {
                string[] fixedValues = definition["avail"].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
                availability = fixedValues[Convert.ToInt32(request.Rating) - 1];
            }
            else
            {
                if (availabilityExpression.StartsWith("+") && availabilityExpression.Contains("Rating"))
                {
                    availabilityExpression = definition["avail"].InnerText.Substring(1, definition["avail"].InnerText.Length - 1);

                    if (availabilityExpression.Substring(availabilityExpression.Length - 1, 1) == "F" || availabilityExpression.Substring(availabilityExpression.Length - 1, 1) == "R")
                    {
                        availabilitySuffix = availabilityExpression.Substring(availabilityExpression.Length - 1, 1);
                        // Remove the trailing character if it is "F" or "R".
                        availabilityExpression = availabilityExpression.Substring(0, availabilityExpression.Length - 1);
                    }

                    compiledAvailability = navigator.Compile(availabilityExpression.Replace("Rating", request.Rating.ToString()));
                    string evaluatedExpression = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledAvailability));
                    int availabilityValue = Convert.ToInt32(evaluatedExpression) + request.GradeAvailabilityModifier;
                    // Avail cannot go below 0;
                    if (availabilityValue < 0)
                        availabilityValue = 0;
                    availability = "+" + availabilityValue.ToString() + availabilitySuffix;
                }
                else
                    availability = definition["avail"].InnerText;
            }
        }
        availability = availability.Replace("R", LanguageManager.Instance.GetString("String_AvailRestricted")).Replace("F", LanguageManager.Instance.GetString("String_AvailForbidden"));

        // Cost.
        // Check for a Variable Cost.
        int availabilityTestCost = 0;
        if (definition["cost"].InnerText.StartsWith("Variable"))
        {
            int minimumCost = 0;
            int maximumCost = 0;
            string costRange = definition["cost"].InnerText.Replace("Variable(", string.Empty).Replace(")", string.Empty);
            if (costRange.Contains("-"))
            {
                string[] fixedValues = costRange.Split('-');
                minimumCost = Convert.ToInt32(fixedValues[0]);
                maximumCost = Convert.ToInt32(fixedValues[1]);
            }
            else
                minimumCost = Convert.ToInt32(costRange.Replace("+", string.Empty));

            if (maximumCost == 0)
            {
                maximumCost = 1000000;
                cost = String.Format("{0:###,###,##0¥+}", minimumCost);
            }
            else
                cost = String.Format("{0:###,###,##0}", minimumCost) + "-" + String.Format("{0:###,###,##0¥}", maximumCost);

            availabilityTestCost = minimumCost;
        }
        else
        {
            try
            {
                XPathExpression compiledCost = navigator.Compile(definition["cost"].InnerText.Replace("Rating", request.Rating.ToString()));
                string evaluatedExpression = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCost));
                cost = String.Format("{0:###,###,##0¥}", Convert.ToInt32((Convert.ToDouble(evaluatedExpression, GlobalOptions.Instance.CultureInfo) * request.GradeCostMultiplier * genetechCostMultiplier)));
                availabilityTestCost = Convert.ToInt32((Convert.ToDouble(evaluatedExpression, GlobalOptions.Instance.CultureInfo) * request.GradeCostMultiplier * genetechCostMultiplier));
            }
            catch
            {
                if (definition["cost"].InnerText.StartsWith("FixedValues"))
                {
                    string[] fixedValues = definition["cost"].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
                    int fixedCost = Convert.ToInt32((Convert.ToDouble(fixedValues[Convert.ToInt32(request.Rating) - 1], GlobalOptions.Instance.CultureInfo) * request.GradeCostMultiplier * genetechCostMultiplier));
                    cost = String.Format("{0:###,###,##0¥}", fixedCost);
                    availabilityTestCost = fixedCost;
                }
                else
                {
                    try
                    {
                        cost = String.Format("{0:###,###,##0¥}", Convert.ToInt32(Convert.ToDouble(definition["cost"].InnerText, GlobalOptions.Instance.CultureInfo) * genetechCostMultiplier));
                        availabilityTestCost = Convert.ToInt32(Convert.ToDouble(definition["cost"].InnerText, GlobalOptions.Instance.CultureInfo) * genetechCostMultiplier);
                    }
                    catch
                    {
                        cost = String.Format("{0:###,###,##0¥}", definition["cost"].InnerText);
                        try
                        {
                            availabilityTestCost = Convert.ToInt32(definition["cost"].InnerText);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        if (request.FreeCost)
        {
            cost = String.Format("{0:###,###,##0¥}", 0);
            availabilityTestCost = 0;
        }

        availabilityTest = character.AvailTest(availabilityTestCost, availability);

        // Essence.
        double essenceValue = 0.0;
        if (definition["ess"].InnerText.StartsWith("FixedValues"))
        {
            string[] fixedValues = definition["ess"].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
            decimal fixedEssence = Convert.ToDecimal(fixedValues[Convert.ToInt32(request.Rating) - 1], GlobalOptions.Instance.CultureInfo);
            essenceValue = Math.Round(Convert.ToDouble(fixedEssence, GlobalOptions.Instance.CultureInfo) * characterEssenceMultiplier, character.Options.EssenceDecimals, MidpointRounding.AwayFromZero);
        }
        else
        {
            XPathExpression compiledEssence = navigator.Compile(definition["ess"].InnerText.Replace("Rating", request.Rating.ToString()));
            string evaluatedExpression = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledEssence));
            essenceValue = Math.Round(Convert.ToDouble(evaluatedExpression, GlobalOptions.Instance.CultureInfo) * characterEssenceMultiplier, character.Options.EssenceDecimals, MidpointRounding.AwayFromZero);
        }
        // Check if the character has Sensitive System.
        if (!request.Bioware)
        {
            foreach (Improvement improvement in character.Improvements)
            {
                if (improvement.ImproveType == Improvement.ImprovementType.SensitiveSystem && improvement.Enabled)
                    essenceValue *= 2.0;
            }
        }
        essence = essenceValue.ToString();

        // Capacity.
        // XPathExpression cannot evaluate while there are square brackets, so remove them if necessary.
        bool bracketedCapacity = definition["capacity"].InnerText.Contains('[');
        string capacityExpression = definition["capacity"].InnerText;
        XPathExpression compiledCapacity;

        if (definition["capacity"].InnerText.Contains("/["))
        {
            int capacitySeparator = definition["capacity"].InnerText.IndexOf("/[");
            string containerCapacity = definition["capacity"].InnerText.Substring(0, capacitySeparator);
            string consumingCapacity = definition["capacity"].InnerText.Substring(capacitySeparator + 1, definition["capacity"].InnerText.Length - capacitySeparator - 1);

            try
            {
                bracketedCapacity = containerCapacity.Contains('[');
                capacityExpression = containerCapacity;
                if (bracketedCapacity)
                    capacityExpression = capacityExpression.Substring(1, capacityExpression.Length - 2);
            }
            catch
            {
            }
            compiledCapacity = navigator.Compile(capacityExpression.Replace("Rating", request.Rating.ToString()));

            try
            {
                if (definition["capacity"].InnerText == "[*]")
                    capacity = "*";
                else
                {
                    if (definition["capacity"].InnerText.StartsWith("FixedValues"))
                    {
                        string[] fixedValues = definition["capacity"].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
                        capacity = fixedValues[Convert.ToInt32(request.Rating) - 1];
                    }
                    else
                    {
                        capacity = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCapacity));
                    }
                }
                if (bracketedCapacity)
                    capacity = "[" + capacity + "]";
            }
            catch
            {
                capacity = "0";
            }

            if (consumingCapacity.Contains("Rating"))
            {
                consumingCapacity = consumingCapacity.Replace("[", string.Empty).Replace("]", string.Empty);
                compiledCapacity = navigator.Compile(consumingCapacity.Replace("Rating", request.Rating.ToString()));
                string evaluatedExpression = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCapacity));
                consumingCapacity = "[" + evaluatedExpression + "]";
            }

            capacity += "/" + consumingCapacity;
        }
        else
        {
            if (bracketedCapacity)
                capacityExpression = capacityExpression.Substring(1, capacityExpression.Length - 2);
            compiledCapacity = navigator.Compile(capacityExpression.Replace("Rating", request.Rating.ToString()));

            if (definition["capacity"].InnerText == "[*]")
                capacity = "*";
            else
            {
                if (definition["capacity"].InnerText.StartsWith("FixedValues"))
                {
                    string[] fixedValues = definition["capacity"].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
                    capacity = fixedValues[Convert.ToInt32(request.Rating) - 1];
                }
                else
                {
                    capacity = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCapacity));
                }
            }
            if (bracketedCapacity)
                capacity = "[" + capacity + "]";
        }
        return new(availability, cost, availabilityTest, essence, capacity);
    }
}
