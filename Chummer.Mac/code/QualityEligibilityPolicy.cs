using System;
using System.Xml;
using System.Xml.XPath;
using Majorsilence.Forms;

namespace Chummer;

// Preserve the established eligibility rules independently of window controls.
// Selection previews use silent checks; acceptance may show the original rule notice.
internal sealed class QualityEligibilityPolicy
{
    private readonly Character _objCharacter;
    private readonly XmlDocument _objXmlDocument;
    private readonly XmlDocument _objMetatypeDocument;
    private readonly XmlDocument _objCritterDocument;
    private readonly string _strIgnoreQuality;

    internal QualityEligibilityPolicy(Character character, XmlDocument catalog,
        XmlDocument metatypes, XmlDocument critters, string ignoredQuality)
    {
        _objCharacter = character;
        _objXmlDocument = catalog;
        _objMetatypeDocument = metatypes;
        _objCritterDocument = critters;
        _strIgnoreQuality = ignoredQuality;
    }

		internal bool RequirementMet(XmlNode objXmlQuality, bool blnShowMessage)
		{
			// Ignore the rules.
			if (_objCharacter.IgnoreRules)
				return true;

			// See if the character already has this Quality and whether or not multiple copies are allowed.
			bool blnAllowMultiple = false;
			if (objXmlQuality["limit"] != null)
			{
				if (objXmlQuality["limit"].InnerText == "no")
					blnAllowMultiple = true;
			}
			if (!blnAllowMultiple)
			{
				// Multiples aren't allowed, so make sure the character does not already have it.
				foreach (Quality objQuality in _objCharacter.Qualities)
				{
					if (objQuality.Name == objXmlQuality["name"].InnerText)
					{
						if (blnShowMessage)
							MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectQuality_QualityLimit"), LanguageManager.Instance.GetString("MessageTitle_SelectQuality_QualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
						return false;
					}
				}
			}

			if (objXmlQuality.InnerXml.Contains("forbidden"))
			{
				bool blnRequirementForbidden = false;
				string strForbidden = "";

				// Loop through the oneof requirements.
				XmlNodeList objXmlForbiddenList = objXmlQuality.SelectNodes("forbidden/oneof");
				foreach (XmlNode objXmlOneOf in objXmlForbiddenList)
				{
					XmlNodeList objXmlOneOfList = objXmlOneOf.ChildNodes;

					foreach (XmlNode objXmlForbidden in objXmlOneOfList)
					{
						if (objXmlForbidden.Name == "quality")
						{
							// Run through all of the Qualities the character has and see if the current forbidden item exists.
							// If so, turn on the RequirementForbidden flag so it cannot be selected.
							foreach (Quality objCharacterQuality in _objCharacter.Qualities)
							{
								if (objCharacterQuality.Name == objXmlForbidden.InnerText && objCharacterQuality.Name != _strIgnoreQuality)
								{
									blnRequirementForbidden = true;
									strForbidden += "\n\t" + objCharacterQuality.DisplayNameShort;
								}
							}
						}
						else if (objXmlForbidden.Name == "metatype")
						{
							// Check the Metatype restriction.
							if (objXmlForbidden.InnerText == _objCharacter.Metatype)
							{
								blnRequirementForbidden = true;
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode["translate"] != null)
									strForbidden += "\n\t" + objNode["translate"].InnerText;
								else
									strForbidden += "\n\t" + objXmlForbidden.InnerText;
							}
						}
						else if (objXmlForbidden.Name == "metatypecategory")
						{
							// Check the Metatype Category restriction.
							if (objXmlForbidden.InnerText == _objCharacter.MetatypeCategory)
							{
								blnRequirementForbidden = true;
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode.Attributes["translate"] != null)
									strForbidden += "\n\t" + objNode.Attributes["translate"].InnerText;
								else
									strForbidden += "\n\t" + objXmlForbidden.InnerText;
							}
						}
						else if (objXmlForbidden.Name == "metavariant")
						{
							// Check the Metavariant restriction.
							if (objXmlForbidden.InnerText == _objCharacter.Metavariant)
							{
								blnRequirementForbidden = true;
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode["translate"] != null)
									strForbidden += "\n\t" + objNode["translate"].InnerText;
								else
									strForbidden += "\n\t" + objXmlForbidden.InnerText;
							}
						}
						else if (objXmlForbidden.Name == "metagenetic")
						{
							// Check to see if the character has a Metagenetic Quality.
							foreach (Quality objQuality in _objCharacter.Qualities)
							{
								XmlNode objXmlCheck = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objQuality.Name + "\"]");
								if (objXmlCheck["metagenetic"] != null)
								{
									if (objXmlCheck["metagenetic"].InnerText.ToLower() == "yes")
									{
										blnRequirementForbidden = true;
										strForbidden += "\n\t" + objQuality.DisplayName;
										break;
									}
								}
							}
						}
					}
				}

				// The character is not allowed to take the Quality, so display a message and uncheck the item.
				if (blnRequirementForbidden)
				{
					if (blnShowMessage)
						MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectQuality_QualityRestriction") + strForbidden, LanguageManager.Instance.GetString("MessageTitle_SelectQuality_QualityRestriction"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
			}

			if (objXmlQuality.InnerXml.Contains("required"))
			{
				string strRequirement = "";
				bool blnRequirementMet = true;

				// Loop through the oneof requirements.
				XmlNodeList objXmlRequiredList = objXmlQuality.SelectNodes("required/oneof");
				foreach (XmlNode objXmlOneOf in objXmlRequiredList)
				{
					bool blnOneOfMet = false;
					string strThisRequirement = "\n" + LanguageManager.Instance.GetString("Message_SelectQuality_OneOf");
					XmlNodeList objXmlOneOfList = objXmlOneOf.ChildNodes;
					foreach (XmlNode objXmlRequired in objXmlOneOfList)
					{
						if (objXmlRequired.Name == "quality")
						{
							// Run through all of the Qualities the character has and see if the current required item exists.
							// If so, turn on the RequirementMet flag so it can be selected.
							foreach (Quality objCharacterQuality in _objCharacter.Qualities)
							{
								if (objCharacterQuality.Name == objXmlRequired.InnerText)
									blnOneOfMet = true;
							}

							if (!blnOneOfMet)
							{
								XmlNode objNode = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metatype")
						{
							// Check the Metatype requirement.
							if (objXmlRequired.InnerText == _objCharacter.Metatype)
								blnOneOfMet = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metatypecategory")
						{
							// Check the Metatype Category requirement.
							if (objXmlRequired.InnerText == _objCharacter.MetatypeCategory)
								blnOneOfMet = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode.Attributes["translate"] != null)
									strThisRequirement += "\n\t" + objNode.Attributes["translate"].InnerText;
								else
									strThisRequirement = "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metavariant")
						{
							// Check the Metavariant requirement.
							if (objXmlRequired.InnerText == _objCharacter.Metavariant)
								blnOneOfMet = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "inherited")
						{
							strThisRequirement += "\n\t" + LanguageManager.Instance.GetString("Message_SelectQuality_Inherit");
						}
						else if (objXmlRequired.Name == "careerkarma")
						{
							// Check Career Karma requirement.
							if (_objCharacter.CareerKarma >= Convert.ToInt32(objXmlRequired.InnerText))
								blnOneOfMet = true;
							else
								strThisRequirement = "\n\t" + LanguageManager.Instance.GetString("Message_SelectQuality_RequireKarma").Replace("{0}", objXmlRequired.InnerText);
						}
						else if (objXmlRequired.Name == "ess")
						{
							// Check Essence requirement.
							if (objXmlRequired.InnerText.StartsWith("-"))
							{
								// Essence must be less than the value.
								if (_objCharacter.Essence < Convert.ToDecimal(objXmlRequired.InnerText.Replace("-", string.Empty), GlobalOptions.Instance.CultureInfo))
									blnOneOfMet = true;
							}
							else
							{
								// Essence must be equal to or greater than the value.
								if (_objCharacter.Essence >= Convert.ToDecimal(objXmlRequired.InnerText, GlobalOptions.Instance.CultureInfo))
									blnOneOfMet = true;
							}

						}
						else if (objXmlRequired.Name == "skill")
						{
							// Check if the character has the required Skill.
							foreach (Skill objSkill in _objCharacter.Skills)
							{
								if (objSkill.Name == objXmlRequired["name"].InnerText)
								{
									if (objSkill.Rating >= Convert.ToInt32(objXmlRequired["val"].InnerText))
									{
										blnOneOfMet = true;
										break;
									}
								}
							}
						}
						else if (objXmlRequired.Name == "attribute")
						{
							// Check to see if an Attribute meets a requirement.
							Attribute objAttribute = _objCharacter.GetAttribute(objXmlRequired["name"].InnerText);

							if (objXmlRequired["total"] != null)
							{
								// Make sure the Attribute's total value meets the requirement.
								if (objAttribute.TotalValue >= Convert.ToInt32(objXmlRequired["total"].InnerText))
									blnOneOfMet = true;
							}
						}
						else if (objXmlRequired.Name == "attributetotal")
						{
							// Check if the character's Attributes add up to a particular total.
							string strAttributes = objXmlRequired["attributes"].InnerText;
							strAttributes = strAttributes.Replace("BOD", _objCharacter.GetAttribute("BOD").Value.ToString());
							strAttributes = strAttributes.Replace("AGI", _objCharacter.GetAttribute("AGI").Value.ToString());
							strAttributes = strAttributes.Replace("REA", _objCharacter.GetAttribute("REA").Value.ToString());
							strAttributes = strAttributes.Replace("STR", _objCharacter.GetAttribute("STR").Value.ToString());
							strAttributes = strAttributes.Replace("CHA", _objCharacter.GetAttribute("CHA").Value.ToString());
							strAttributes = strAttributes.Replace("INT", _objCharacter.GetAttribute("INT").Value.ToString());
							strAttributes = strAttributes.Replace("INT", _objCharacter.GetAttribute("LOG").Value.ToString());
							strAttributes = strAttributes.Replace("WIL", _objCharacter.GetAttribute("WIL").Value.ToString());
							strAttributes = strAttributes.Replace("MAG", _objCharacter.GetAttribute("MAG").Value.ToString());
							strAttributes = strAttributes.Replace("RES", _objCharacter.GetAttribute("RES").Value.ToString());
							strAttributes = strAttributes.Replace("EDG", _objCharacter.GetAttribute("EDG").Value.ToString());

							XmlDocument objXmlDocument = new XmlDocument();
							XPathNavigator nav = objXmlDocument.CreateNavigator();
							XPathExpression xprAttributes = nav.Compile(strAttributes);
						    string temp = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", nav.Evaluate(xprAttributes));
                            if (Convert.ToInt32(temp) >= Convert.ToInt32(objXmlRequired["val"].InnerText))
						    {
						        blnOneOfMet = true;
						    }
						}
						else if (objXmlRequired.Name == "skillgrouptotal")
						{
							// Check if the total combined Ratings of Skill Groups adds up to a particular total.
							int intTotal = 0;
							string[] strGroups = objXmlRequired["skillgroups"].InnerText.Split('+');
							for (int i = 0; i <= strGroups.Length - 1; i++)
							{
								foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
								{
									if (objGroup.Name == strGroups[i])
									{
										intTotal += objGroup.Rating;
										break;
									}
								}
							}

							if (intTotal >= Convert.ToInt32(objXmlRequired["val"].InnerText))
								blnOneOfMet = true;
						}
						else if (objXmlRequired.Name == "cyberwares")
						{
							// Check to see if the character has a number of the required Cyberware/Bioware items.
							int intTotal = 0;

							// Check Cyberware.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberware"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name == objXmlCyberware.InnerText)
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check Bioware.
							foreach (XmlNode objXmlBioware in objXmlRequired.SelectNodes("bioware"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name == objXmlBioware.InnerText)
									{
										intTotal++;
										break;
									}
								}
							}

							// Check Cyberware name that contain a straing.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwarecontains"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name.Contains(objXmlCyberware.InnerText))
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check Bioware name that contain a straing.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("biowarecontains"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name.Contains(objXmlCyberware.InnerText))
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check for Cyberware Plugins.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwareplugin"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									foreach (Cyberware objPlugin in objCyberware.Children)
									{
										if (objPlugin.Name == objXmlCyberware.InnerText)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check for Cyberware Categories.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwarecategory"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Category == objXmlCyberware.InnerText)
										intTotal++;
								}
							}

							if (intTotal >= Convert.ToInt32(objXmlRequired["count"].InnerText))
								blnOneOfMet = true;
						}
						else if (objXmlRequired.Name == "streetcredvsnotoriety")
						{
							// Street Cred must be higher than Notoriety.
							if (_objCharacter.StreetCred >= _objCharacter.Notoriety)
								blnOneOfMet = true;
						}
						else if (objXmlRequired.Name == "damageresistance")
						{
							// Damage Resistance must be a particular value.
							ImprovementManager _objImprovementManager = new ImprovementManager(_objCharacter);
							if (_objCharacter.BOD.TotalValue + _objImprovementManager.ValueOf(Improvement.ImprovementType.DamageResistance) >= Convert.ToInt32(objXmlRequired.InnerText))
								blnOneOfMet = true;
						}
					}

					// Update the flag for requirements met.
					blnRequirementMet = blnRequirementMet && blnOneOfMet;
					strRequirement += strThisRequirement;
				}

				// Loop through the allof requirements.
				objXmlRequiredList = objXmlQuality.SelectNodes("required/allof");
				foreach (XmlNode objXmlAllOf in objXmlRequiredList)
				{
					bool blnAllOfMet = true;
					string strThisRequirement = "\n" + LanguageManager.Instance.GetString("Message_SelectQuality_AllOf");
					XmlNodeList objXmlAllOfList = objXmlAllOf.ChildNodes;
					foreach (XmlNode objXmlRequired in objXmlAllOfList)
					{
						bool blnFound = false;
						if (objXmlRequired.Name == "quality")
						{
							// Run through all of the Qualities the character has and see if the current required item exists.
							// If so, turn on the RequirementMet flag so it can be selected.
							foreach (Quality objCharacterQuality in _objCharacter.Qualities)
							{
								if (objCharacterQuality.Name == objXmlRequired.InnerText)
									blnFound = true;
							}

							if (!blnFound)
							{
								XmlNode objNode = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metatype")
						{
							// Check the Metatype requirement.
							if (objXmlRequired.InnerText == _objCharacter.Metatype)
								blnFound = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metatypecategory")
						{
							// Check the Metatype Category requirement.
							if (objXmlRequired.InnerText == _objCharacter.MetatypeCategory)
								blnFound = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode.Attributes["translate"] != null)
									strThisRequirement += "\n\t" + objNode.Attributes["translate"].InnerText;
								else
									strThisRequirement = "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metavariant")
						{
							// Check the Metavariant requirement.
							if (objXmlRequired.InnerText == _objCharacter.Metavariant)
								blnFound = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "inherited")
						{
							strThisRequirement += "\n\t" + LanguageManager.Instance.GetString("Message_SelectQuality_Inherit");
						}
						else if (objXmlRequired.Name == "careerkarma")
						{
							// Check Career Karma requirement.
							if (_objCharacter.CareerKarma >= Convert.ToInt32(objXmlRequired.InnerText))
								blnFound = true;
							else
								strThisRequirement = "\n\t" + LanguageManager.Instance.GetString("Message_SelectQuality_RequireKarma").Replace("{0}", objXmlRequired.InnerText);
						}
						else if (objXmlRequired.Name == "ess")
						{
							// Check Essence requirement.
							if (objXmlRequired.InnerText.StartsWith("-"))
							{
								// Essence must be less than the value.
								if (_objCharacter.Essence < Convert.ToDecimal(objXmlRequired.InnerText.Replace("-", string.Empty), GlobalOptions.Instance.CultureInfo))
									blnFound = true;
							}
							else
							{
								// Essence must be equal to or greater than the value.
								if (_objCharacter.Essence >= Convert.ToDecimal(objXmlRequired.InnerText, GlobalOptions.Instance.CultureInfo))
									blnFound = true;
							}

						}
						else if (objXmlRequired.Name == "skill")
						{
							// Check if the character has the required Skill.
							foreach (Skill objSkill in _objCharacter.Skills)
							{
								if (objSkill.Name == objXmlRequired["name"].InnerText)
								{
									if (objSkill.Rating >= Convert.ToInt32(objXmlRequired["val"].InnerText))
									{
										blnFound = true;
										break;
									}
								}
							}
						}
						else if (objXmlRequired.Name == "attribute")
						{
							// Check to see if an Attribute meets a requirement.
							Attribute objAttribute = _objCharacter.GetAttribute(objXmlRequired["name"].InnerText);

							if (objXmlRequired["total"] != null)
							{
								// Make sure the Attribute's total value meets the requirement.
								if (objAttribute.TotalValue >= Convert.ToInt32(objXmlRequired["total"].InnerText))
									blnFound = true;
							}
						}
						else if (objXmlRequired.Name == "attributetotal")
						{
							// Check if the character's Attributes add up to a particular total.
							string strAttributes = objXmlRequired["attributes"].InnerText;
							strAttributes = strAttributes.Replace("BOD", _objCharacter.GetAttribute("BOD").Value.ToString());
							strAttributes = strAttributes.Replace("AGI", _objCharacter.GetAttribute("AGI").Value.ToString());
							strAttributes = strAttributes.Replace("REA", _objCharacter.GetAttribute("REA").Value.ToString());
							strAttributes = strAttributes.Replace("STR", _objCharacter.GetAttribute("STR").Value.ToString());
							strAttributes = strAttributes.Replace("CHA", _objCharacter.GetAttribute("CHA").Value.ToString());
							strAttributes = strAttributes.Replace("INT", _objCharacter.GetAttribute("INT").Value.ToString());
							strAttributes = strAttributes.Replace("INT", _objCharacter.GetAttribute("LOG").Value.ToString());
							strAttributes = strAttributes.Replace("WIL", _objCharacter.GetAttribute("WIL").Value.ToString());
							strAttributes = strAttributes.Replace("MAG", _objCharacter.GetAttribute("MAG").Value.ToString());
							strAttributes = strAttributes.Replace("RES", _objCharacter.GetAttribute("RES").Value.ToString());
							strAttributes = strAttributes.Replace("EDG", _objCharacter.GetAttribute("EDG").Value.ToString());

							XmlDocument objXmlDocument = new XmlDocument();
							XPathNavigator nav = objXmlDocument.CreateNavigator();
							XPathExpression xprAttributes = nav.Compile(strAttributes);
						    string temp = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", nav.Evaluate(xprAttributes));
						    if (Convert.ToInt32(temp) >= Convert.ToInt32(objXmlRequired["val"].InnerText))
						    {
						        blnFound = true;
						    }
						}
						else if (objXmlRequired.Name == "skillgrouptotal")
						{
							// Check if the total combined Ratings of Skill Groups adds up to a particular total.
							int intTotal = 0;
							string[] strGroups = objXmlRequired["skillgroups"].InnerText.Split('+');
							for (int i = 0; i <= strGroups.Length - 1; i++)
							{
								foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
								{
									if (objGroup.Name == strGroups[i])
									{
										intTotal += objGroup.Rating;
										break;
									}
								}
							}

							if (intTotal >= Convert.ToInt32(objXmlRequired["val"].InnerText))
								blnFound = true;
						}
						else if (objXmlRequired.Name == "cyberwares")
						{
							// Check to see if the character has a number of the required Cyberware/Bioware items.
							int intTotal = 0;

							// Check Cyberware.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberware"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name == objXmlCyberware.InnerText)
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check Bioware.
							foreach (XmlNode objXmlBioware in objXmlRequired.SelectNodes("bioware"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name == objXmlBioware.InnerText)
									{
										intTotal++;
										break;
									}
								}
							}

							// Check Cyberware name that contain a straing.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwarecontains"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name.Contains(objXmlCyberware.InnerText))
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check Bioware name that contain a straing.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("biowarecontains"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name.Contains(objXmlCyberware.InnerText))
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check for Cyberware Plugins.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwareplugin"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									foreach (Cyberware objPlugin in objCyberware.Children)
									{
										if (objPlugin.Name == objXmlCyberware.InnerText)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check for Cyberware Categories.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwarecategory"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Category == objXmlCyberware.InnerText)
										intTotal++;
								}
							}

							if (intTotal >= Convert.ToInt32(objXmlRequired["count"].InnerText))
								blnFound = true;
						}
						else if (objXmlRequired.Name == "streetcredvsnotoriety")
						{
							// Street Cred must be higher than Notoriety.
							if (_objCharacter.StreetCred >= _objCharacter.Notoriety)
								blnFound = true;
						}
						else if (objXmlRequired.Name == "damageresistance")
						{
							// Damage Resistance must be a particular value.
							ImprovementManager _objImprovementManager = new ImprovementManager(_objCharacter);
							if (_objCharacter.BOD.TotalValue + _objImprovementManager.ValueOf(Improvement.ImprovementType.DamageResistance) >= Convert.ToInt32(objXmlRequired.InnerText))
								blnFound = true;
						}

						// If this item was not found, fail the AllOfMet condition.
						if (!blnFound)
							blnAllOfMet = false;
					}

					// Update the flag for requirements met.
					blnRequirementMet = blnRequirementMet && blnAllOfMet;
					strRequirement += strThisRequirement;
				}

				// The character has not met the requirements, so display a message and uncheck the item.
				if (!blnRequirementMet)
				{
					string strMessage = LanguageManager.Instance.GetString("Message_SelectQuality_QualityRequirement");
					strMessage += strRequirement;

					if (blnShowMessage)
						MessageBox.Show(strMessage, LanguageManager.Instance.GetString("MessageTitle_SelectQuality_QualityRequirement"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
			}

			return true;
		}
}
