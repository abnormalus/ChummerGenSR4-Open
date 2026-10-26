using System;
using System.Xml;
using System.Xml.XPath;

namespace Chummer;

internal sealed record GearPreviewRequest(decimal Rating, decimal Quantity, decimal QuantityIncrement,
    decimal Markup, bool DoItYourself, bool Hacked, bool FreeCost, int AvailabilityModifier,
    int CostMultiplier, bool ArmorCapacityOnly, CapacityStyle CapacityStyle);
internal sealed record GearPreviewResult(string Availability, string Cost, string AvailabilityTest, string Capacity);

// Unattached preview boundary. These established calculations are extracted from
// frmSelectGear without changing rounding, variable-price or capacity conventions.
// Purchases, improvements, expenses and inventory mutation remain caller-owned.
internal static class GearPreview
{
    internal static GearPreviewResult Calculate(Character character, XmlDocument catalog,
        XmlNode definition, GearPreviewRequest request)
    {
        int availabilityTestCost = 0;
        string availability = "", cost = "", availabilityTest = "", capacity = "";
        // Preserve the legacy availability, price and capacity expression order.
        // Rounding and variable-price conventions are part of the picker contract.
        XPathNavigator navigator = catalog.CreateNavigator();

        // Avail.
        // If avail contains "F" or "R", remove it from the string so we can use the expression.
        string availabilitySuffix = "";
        string availabilityExpression = "";
        string availabilityPrefix = "";
        if (definition["avail"] != null)
            availabilityExpression = definition["avail"].InnerText;
        if (request.Rating <= 3 && definition["avail3"] != null)
            availabilityExpression = definition["avail3"].InnerText;
        else if (request.Rating <= 6 && definition["avail6"] != null)
            availabilityExpression = definition["avail6"].InnerText;
        else if (request.Rating >= 7 && definition["avail10"] != null)
            availabilityExpression = definition["avail10"].InnerText;

        XPathExpression compiledAvailability;
        if (availabilityExpression.Substring(availabilityExpression.Length - 1, 1) == "F" || availabilityExpression.Substring(availabilityExpression.Length - 1, 1) == "R")
        {
            availabilitySuffix = availabilityExpression.Substring(availabilityExpression.Length - 1, 1);
            // Remove the trailing character if it is "F" or "R".
            availabilityExpression = availabilityExpression.Substring(0, availabilityExpression.Length - 1);
        }
        if (availabilityExpression.Substring(0, 1) == "+")
        {
            availabilityPrefix = "+";
            availabilityExpression = availabilityExpression.Substring(1, availabilityExpression.Length - 1);
        }

        try
        {
            compiledAvailability = navigator.Compile(availabilityExpression.Replace("Rating", request.Rating.ToString()));
            string evaluated = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledAvailability));
            availability = (Convert.ToInt32(evaluated) + request.AvailabilityModifier).ToString() + availabilitySuffix;
        }
        catch
        {
            availability = definition["avail"].InnerText;
        }
        availability = availabilityPrefix + availability.Replace("R", LanguageManager.Instance.GetString("String_AvailRestricted")).Replace("F", LanguageManager.Instance.GetString("String_AvailForbidden"));

        double quantityMultiplier = Convert.ToDouble(request.Quantity / request.QuantityIncrement, GlobalOptions.Instance.CultureInfo);
        if (request.DoItYourself)
            quantityMultiplier *= 0.5;

        // Cost.
        if (definition["cost"] != null)
        {
            try
            {
                XPathExpression compiledCost = navigator.Compile(definition["cost"].InnerText.Replace("Rating", request.Rating.ToString()));
                double previewCost = 0.0;
                string evaluated = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCost));
                previewCost = Convert.ToDouble(evaluated, GlobalOptions.Instance.CultureInfo) * quantityMultiplier;
                previewCost *= 1 + (Convert.ToDouble(request.Markup, GlobalOptions.Instance.CultureInfo) / 100.0);
                if (request.Hacked)
                    previewCost *= 0.1;
                cost = String.Format("{0:###,###,##0¥}", previewCost * request.CostMultiplier);
                availabilityTestCost = Convert.ToInt32(previewCost);
            }
            catch
            {
                cost = definition["cost"].InnerText;
                try
                {
                    availabilityTestCost = Convert.ToInt32(definition["cost"].InnerText);
                }
                catch
                {
                }
            }

            if (definition["cost"].InnerText.StartsWith("Variable"))
            {
                int minimumCost = 0;
                int maximumCost = 0;
                string priceRange = definition["cost"].InnerText.Replace("Variable(", string.Empty).Replace(")", string.Empty);
                if (priceRange.Contains("-"))
                {
                    string[] values = priceRange.Split('-');
                    minimumCost = Convert.ToInt32(values[0]);
                    maximumCost = Convert.ToInt32(values[1]);
                }
                else
                    minimumCost = Convert.ToInt32(priceRange.Replace("+", string.Empty));

                if (maximumCost == 0)
                {
                    maximumCost = 1000000;
                    cost = String.Format("{0:###,###,##0¥+}", minimumCost);
                }
                else
                    cost = String.Format("{0:###,###,##0}", minimumCost) + "-" + String.Format("{0:###,###,##0¥}", maximumCost);

                availabilityTestCost = minimumCost;
            }
        }
        else
        {
            if (request.Rating <= 3)
            {
                XPathExpression compiledCost = navigator.Compile(definition["cost3"].InnerText.Replace("Rating", request.Rating.ToString()));
                double previewCost = 0.0;
                string evaluated = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCost));
                previewCost = Convert.ToDouble(evaluated, GlobalOptions.Instance.CultureInfo) * quantityMultiplier;
                previewCost *= 1 + (Convert.ToDouble(request.Markup, GlobalOptions.Instance.CultureInfo) / 100.0);
                if (request.Hacked)
                    previewCost *= 0.1;
                cost = String.Format("{0:###,###,##0¥}", previewCost * request.CostMultiplier);
                availabilityTestCost = Convert.ToInt32(previewCost);
            }
            else if (request.Rating <= 6)
            {
                XPathExpression compiledCost = navigator.Compile(definition["cost6"].InnerText.Replace("Rating", request.Rating.ToString()));
                double previewCost = 0.0;
                string evaluated = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCost));
                previewCost = Convert.ToDouble(evaluated, GlobalOptions.Instance.CultureInfo) * quantityMultiplier;
                previewCost *= 1 + (Convert.ToDouble(request.Markup, GlobalOptions.Instance.CultureInfo) / 100.0);
                if (request.Hacked)
                    previewCost *= 0.1;
                cost = String.Format("{0:###,###,##0¥}", previewCost * request.CostMultiplier);
                availabilityTestCost = Convert.ToInt32(previewCost);
            }
            else
            {
                XPathExpression compiledCost = navigator.Compile(definition["cost10"].InnerText.Replace("Rating", request.Rating.ToString()));
                double previewCost = 0.0;
                string evaluated = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCost));
                previewCost = Convert.ToDouble(evaluated, GlobalOptions.Instance.CultureInfo) * quantityMultiplier;
                previewCost *= 1 + (Convert.ToDouble(request.Markup, GlobalOptions.Instance.CultureInfo) / 100.0);
                if (request.Hacked)
                    previewCost *= 0.1;
                cost = String.Format("{0:###,###,##0¥}", previewCost * request.CostMultiplier);
                availabilityTestCost = Convert.ToInt32(previewCost);
            }
        }

        if (request.FreeCost)
        {
            cost = String.Format("{0:###,###,##0¥}", 0);
            availabilityTestCost = 0;
        }

        // Update the Avail Test Label.
        availabilityTest = character.AvailTest(availabilityTestCost * request.CostMultiplier, availability);

        // Capacity.
        // XPathExpression cannot evaluate while there are square brackets, so remove them if necessary.
        string capacityValue = "0";
        string capacityField = "capacity";
        if (request.ArmorCapacityOnly)
            capacityField = "armorcapacity";
        bool bracketedCapacity = false;

        if (request.CapacityStyle == CapacityStyle.Standard)
        {
            try
            {
                if (definition[capacityField].InnerText.Contains("/["))
                {
                    int separatorIndex = definition[capacityField].InnerText.IndexOf("/[");
                    string firstCapacity = definition[capacityField].InnerText.Substring(0, separatorIndex);
                    string secondCapacity = definition[capacityField].InnerText.Substring(separatorIndex + 1, definition[capacityField].InnerText.Length - separatorIndex - 1);

                    try
                    {
                        bracketedCapacity = firstCapacity.Contains('[');
                        capacityValue = firstCapacity;
                        if (bracketedCapacity)
                            capacityValue = capacityValue.Substring(1, capacityValue.Length - 2);
                    }
                    catch
                    {
                    }
                    XPathExpression compiledCapacity = navigator.Compile(capacityValue.Replace("Rating", request.Rating.ToString()));

                    try
                    {
                        if (definition[capacityField].InnerText == "[*]")
                            capacity = "*";
                        else
                        {
                            if (definition[capacityField].InnerText.StartsWith("FixedValues"))
                            {
                                string[] values = definition[capacityField].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
                                capacity = values[Convert.ToInt32(request.Rating) - 1];
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
                    capacity += "/" + secondCapacity;
                }
                else
                {
                    try
                    {
                        bracketedCapacity = definition[capacityField].InnerText.Contains('[');
                        capacityValue = definition[capacityField].InnerText;
                        if (bracketedCapacity)
                            capacityValue = capacityValue.Substring(1, capacityValue.Length - 2);
                    }
                    catch
                    {
                    }
                    XPathExpression compiledCapacity = navigator.Compile(capacityValue.Replace("Rating", request.Rating.ToString()));

                    try
                    {
                        if (definition[capacityField].InnerText == "[*]")
                            capacity = "*";
                        else
                        {
                            if (definition[capacityField].InnerText.StartsWith("FixedValues"))
                            {
                                string[] values = definition[capacityField].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
                                capacity = values[Convert.ToInt32(request.Rating) - 1];
                            }
                            else
                            {
                                string evaluated = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCapacity));
                                string calculatedCapacity = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", evaluated);
                                if (calculatedCapacity.Contains('.'))
                                {
                                    decimal decimalCapacity = Convert.ToDecimal(calculatedCapacity, GlobalOptions.Instance.CultureInfo);
                                    int wholeCapacity = Convert.ToInt32(Math.Floor(decimalCapacity));
                                    if (wholeCapacity < 1)
                                        wholeCapacity = 1;
                                    capacity = wholeCapacity.ToString();
                                }
                                else
                                {
                                    capacity = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(compiledCapacity));
                                }
                            }
                        }
                        if (bracketedCapacity)
                            capacity = "[" + capacity + "]";
                    }
                    catch
                    {
                        capacity = "0";
                    }
                }
            }
            catch
            {
                capacity = "0";
            }
        }
        else if (request.CapacityStyle == CapacityStyle.Zero)
            capacity = "[0]";
        else if (request.CapacityStyle == CapacityStyle.PerRating)
        {
            if (request.Rating == 0)
                capacity = "[1]";
            else
                capacity = "[" + request.Rating.ToString() + "]";
        }

        return new GearPreviewResult(availability, cost, availabilityTest, capacity);
    }
}
