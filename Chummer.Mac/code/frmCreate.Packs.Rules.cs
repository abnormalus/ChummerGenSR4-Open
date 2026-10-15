using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

// Transitional adapter for the established PACKS rules. It runs only against the
// private workspace, keeping its control-based calculations out of the live editor.
public partial class frmCreate
{
    private static XmlNode RequirePacksEntry(XmlNode entry) => entry
        ?? throw new InvalidDataException("PACKS references unavailable catalog data.");

        // Reuses the established kit rules on an isolated model and private controls.
        private async System.Threading.Tasks.Task ApplyPacksKitAsync(XmlNode objXmlKit, bool blnCreateChildren, BulkWork work)
        {
            _packsWork = work;
			// Update Qualities.
			if (objXmlKit["qualities"] != null)
			{
                AppDiagnostics.Record("packs.phase-qualities");
				XmlDocument objXmlQualityDocument = XmlManager.Instance.Load("qualities.xml");

				// Positive Qualities.
				foreach (XmlNode objXmlQuality in objXmlKit.SelectNodes("qualities/positive/quality"))
				{
					await work.StepAsync();
					XmlNode objXmlQualityNode = RequirePacksEntry(objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQuality.InnerText + "\"]"));

					TreeNode objNode = new TreeNode();
					List<Weapon> objWeapons = new List<Weapon>();
					List<TreeNode> objWeaponNodes = new List<TreeNode>();
					Quality objQuality = new Quality(_objCharacter);
					string strForceValue = "";

					if (objXmlQuality.Attributes["select"] != null)
						strForceValue = objXmlQuality.Attributes["select"].InnerText;

					objQuality.Create(objXmlQualityNode, _objCharacter, QualitySource.Selected, objNode, objWeapons, objWeaponNodes, strForceValue);
					if (objQuality.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					_objCharacter.Qualities.Add(objQuality);

					treQualities.Nodes[0].Nodes.Add(objNode);
					treQualities.Nodes[0].Expand();

					// Add any created Weapons to the character.
					foreach (Weapon objWeapon in objWeapons)
						_objCharacter.Weapons.Add(objWeapon);

					// Create the Weapon Node if one exists.
					foreach (TreeNode objWeaponNode in objWeaponNodes)
					{
						objWeaponNode.ContextMenuStrip = cmsWeapon;
						treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
						treWeapons.Nodes[0].Expand();
					}
				}

				// Negative Qualities.
				foreach (XmlNode objXmlQuality in objXmlKit.SelectNodes("qualities/negative/quality"))
				{
					await work.StepAsync();
					XmlNode objXmlQualityNode = RequirePacksEntry(objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQuality.InnerText + "\"]"));

					TreeNode objNode = new TreeNode();
					List<Weapon> objWeapons = new List<Weapon>();
					List<TreeNode> objWeaponNodes = new List<TreeNode>();
					Quality objQuality = new Quality(_objCharacter);
					string strForceValue = "";

					if (objXmlQuality.Attributes["select"] != null)
						strForceValue = objXmlQuality.Attributes["select"].InnerText;

					objQuality.Create(objXmlQualityNode, _objCharacter, QualitySource.Selected, objNode, objWeapons, objWeaponNodes, strForceValue);
					if (objQuality.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					_objCharacter.Qualities.Add(objQuality);

					treQualities.Nodes[1].Nodes.Add(objNode);
					treQualities.Nodes[1].Expand();

					// Add any created Weapons to the character.
					foreach (Weapon objWeapon in objWeapons)
						_objCharacter.Weapons.Add(objWeapon);

					// Create the Weapon Node if one exists.
					foreach (TreeNode objWeaponNode in objWeaponNodes)
					{
						objWeaponNode.ContextMenuStrip = cmsWeapon;
						treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
						treWeapons.Nodes[0].Expand();
					}
				}
			}

			// Update Attributes.
			if (objXmlKit["attributes"] != null)
			{
                AppDiagnostics.Record("packs.phase-attributes");
				// Reset all Attributes back to 1 so we don't go over any BP limits.
				nudBOD.Value = nudBOD.Minimum;
				nudAGI.Value = nudAGI.Minimum;
				nudREA.Value = nudREA.Minimum;
				nudSTR.Value = nudSTR.Minimum;
				nudCHA.Value = nudCHA.Minimum;
				nudINT.Value = nudINT.Minimum;
				nudLOG.Value = nudLOG.Minimum;
				nudWIL.Value = nudWIL.Minimum;
				nudEDG.Value = nudEDG.Minimum;
				nudMAG.Value = nudMAG.Minimum;
				nudRES.Value = nudRES.Minimum;
				foreach (XmlNode objXmlAttribute in objXmlKit["attributes"])
				{
					await work.StepAsync();
					// The Attribute is calculated as given value - (6 - Metatype Maximum) so that each Metatype has the values from the file adjusted correctly.
					switch (objXmlAttribute.Name)
					{
						case "bod":
							nudBOD.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.BOD.MetatypeMaximum);
							break;
						case "agi":
							nudAGI.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.AGI.MetatypeMaximum);
							break;
						case "rea":
							nudREA.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.REA.MetatypeMaximum);
							break;
						case "str":
							nudSTR.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.STR.MetatypeMaximum);
							break;
						case "cha":
							nudCHA.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.CHA.MetatypeMaximum);
							break;
						case "int":
							nudINT.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.INT.MetatypeMaximum);
							break;
						case "log":
							nudLOG.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.LOG.MetatypeMaximum);
							break;
						case "wil":
							nudWIL.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.WIL.MetatypeMaximum);
							break;
						case "mag":
							nudMAG.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.MAG.MetatypeMaximum);
							break;
						case "res":
							nudRES.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.RES.MetatypeMaximum);
							break;
						default:
							nudEDG.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.EDG.MetatypeMaximum);
							break;
					}
				}
			}

			// Update Skills.
			if (objXmlKit["skills"] != null)
			{
                AppDiagnostics.Record("packs.phase-skills");
				// Active Skills.
				foreach (XmlNode objXmlSkill in objXmlKit.SelectNodes("skills/skill"))
				{
					await work.StepAsync();
					if (objXmlSkill["name"].InnerText.Contains("Exotic"))
					{
						int i = panActiveSkills.Controls.Count;
						Skill objSkill = new Skill(_objCharacter);

						SkillControl objSkillControl = new SkillControl();
						objSkillControl.SkillObject = objSkill;
						objSkillControl.Width = 510;

						// Attach an EventHandler for the RatingChanged and SpecializationChanged Events.
						objSkillControl.RatingChanged += objActiveSkill_RatingChanged;
						objSkillControl.SpecializationChanged += objSkill_SpecializationChanged;
						objSkillControl.SkillName = objXmlSkill["name"].InnerText;

						switch (objXmlSkill["name"].InnerText)
						{
							case "Exotic Ranged Weapon":
							case "Exotic Melee Weapon":
								objSkill.Attribute = "AGI";
								objSkillControl.SkillCategory = "Combat Active";
								objSkill.Default = true;
								break;
							default:
								objSkill.Attribute = "REA";
								objSkillControl.SkillCategory = "Vehicle Active";
								objSkill.Default = false;
								break;
						}
						objSkill.ExoticSkill = true;
						_objCharacter.Skills.Add(objSkill);

						objSkillControl.SkillRatingMaximum = 6;

						// Make sure it's not going above the maximum number.
						if (Convert.ToInt32(objXmlSkill["rating"].InnerText) > objSkillControl.SkillRatingMaximum)
							objSkillControl.SkillRating = objSkillControl.SkillRatingMaximum;
						else
							objSkillControl.SkillRating = Convert.ToInt32(objXmlSkill["rating"].InnerText);

						if (objXmlSkill["spec"] != null)
							objSkillControl.SkillSpec = objXmlSkill["spec"].InnerText;
						else
							objSkillControl.SkillSpec = "";

						// Set the SkillControl's Location since scrolling the Panel causes it to actually change the child Controls' Locations.
						objSkillControl.Location = new Point(0, objSkillControl.Height * i + panActiveSkills.AutoScrollPosition.Y);
						panActiveSkills.Controls.Add(objSkillControl);
					}
					else
					{
						// Find the correct Skill Control.
						SkillControl objSkillControl = new SkillControl();
						foreach (SkillControl objControl in panActiveSkills.Controls)
						{
							if (objControl.SkillName == objXmlSkill["name"].InnerText)
							{
								objSkillControl = objControl;
								break;
							}
						}

						// Make sure it's not going above the maximum number.
						if (Convert.ToInt32(objXmlSkill["rating"].InnerText) > objSkillControl.SkillRatingMaximum)
							objSkillControl.SkillRating = objSkillControl.SkillRatingMaximum;
						else
							objSkillControl.SkillRating = Convert.ToInt32(objXmlSkill["rating"].InnerText);

						if (objXmlSkill["spec"] != null)
							objSkillControl.SkillSpec = objXmlSkill["spec"].InnerText;
						else
							objSkillControl.SkillSpec = "";
					}
				}

				// Skill Groups.
				foreach (XmlNode objXmlGroup in objXmlKit.SelectNodes("skills/skillgroup"))
				{
					await work.StepAsync();
					// Find the correct SkillGroupControl.
					SkillGroupControl objSkillGroupControl = new SkillGroupControl(_objCharacter.Options);
					foreach (SkillGroupControl objControl in panSkillGroups.Controls)
					{
						if (objControl.GroupName == objXmlGroup["name"].InnerText)
						{
							objSkillGroupControl = objControl;
							break;
						}
					}

					// Make sure it's not going above the maximum number.
					if (Convert.ToInt32(objXmlGroup["rating"].InnerText) > objSkillGroupControl.GroupRatingMaximum)
						objSkillGroupControl.GroupRating = objSkillGroupControl.GroupRatingMaximum;
					else
						objSkillGroupControl.GroupRating = Convert.ToInt32(objXmlGroup["rating"].InnerText);
				}
			}

			// Update Knowledge Skills.
			if (objXmlKit["knowledgeskills"] != null)
			{
                AppDiagnostics.Record("packs.phase-knowledgeskills");
				foreach (XmlNode objXmlSkill in objXmlKit.SelectNodes("knowledgeskills/skill"))
				{
					await work.StepAsync();
					int i = panKnowledgeSkills.Controls.Count;
					Skill objSkill = new Skill(_objCharacter);
					objSkill.Name = objXmlSkill["name"].InnerText;

					SkillControl objSkillControl = new SkillControl();
					objSkillControl.SkillObject = objSkill;

					// Attach an EventHandler for the RatingChanged and SpecializationChanged Events.
					objSkillControl.RatingChanged += objKnowledgeSkill_RatingChanged;
					objSkillControl.SpecializationChanged += objSkill_SpecializationChanged;
					objSkillControl.DeleteSkill += objKnowledgeSkill_DeleteSkill;

					objSkillControl.KnowledgeSkill = true;
					objSkillControl.AllowDelete = true;
					objSkillControl.SkillRatingMaximum = 6;
					// Set the SkillControl's Location since scrolling the Panel causes it to actually change the child Controls' Locations.
					objSkillControl.Location = new Point(0, objSkillControl.Height * i + panKnowledgeSkills.AutoScrollPosition.Y);
					panKnowledgeSkills.Controls.Add(objSkillControl);

					objSkillControl.SkillName = objXmlSkill["name"].InnerText;

					// Make sure it's not going above the maximum number.
					if (Convert.ToInt32(objXmlSkill["rating"].InnerText) > objSkillControl.SkillRatingMaximum)
						objSkillControl.SkillRating = objSkillControl.SkillRatingMaximum;
					else
						objSkillControl.SkillRating = Convert.ToInt32(objXmlSkill["rating"].InnerText);

					if (objXmlSkill["spec"] != null)
						objSkillControl.SkillSpec = objXmlSkill["spec"].InnerText;
					else
						objSkillControl.SkillSpec = "";

					if (objXmlSkill["category"] != null)
						objSkillControl.SkillCategory = objXmlSkill["category"].InnerText;

					_objCharacter.Skills.Add(objSkill);
				}
			}

			// Select a Martial Art.
			if (objXmlKit["selectmartialart"] != null)
			{
                AppDiagnostics.Record("packs.phase-selectmartialart");
                await work.StepAsync();
				string strForcedValue = "";
				int intRating = 1;
				if (objXmlKit["selectmartialart"].Attributes["select"] != null)
					strForcedValue = objXmlKit["selectmartialart"].Attributes["select"].InnerText;
				if (objXmlKit["selectmartialart"].Attributes["rating"] != null)
					intRating = Convert.ToInt32(objXmlKit["selectmartialart"].Attributes["rating"].InnerText);

				frmSelectMartialArt frmPickMartialArt = new frmSelectMartialArt(_objCharacter);
				frmPickMartialArt.ForcedValue = strForcedValue;
				frmPickMartialArt.ShowDialog(_packsPromptOwner ?? this);
                if (frmPickMartialArt.DialogResult != DialogResult.OK) throw new OperationCanceledException();

				if (frmPickMartialArt.DialogResult != DialogResult.Cancel)
				{
					// Open the Martial Arts XML file and locate the selected piece.
					XmlDocument objXmlMartialArtDocument = XmlManager.Instance.Load("martialarts.xml");

					XmlNode objXmlArt = RequirePacksEntry(objXmlMartialArtDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + frmPickMartialArt.SelectedMartialArt + "\"]"));

					TreeNode objNode = new TreeNode();
					MartialArt objMartialArt = new MartialArt(_objCharacter);
					objMartialArt.Create(objXmlArt, objNode, _objCharacter);
					objMartialArt.Rating = intRating;
					_objCharacter.MartialArts.Add(objMartialArt);

					objNode.ContextMenuStrip = cmsMartialArts;

					treMartialArts.Nodes[0].Nodes.Add(objNode);
					treMartialArts.Nodes[0].Expand();

					treMartialArts.SelectedNode = objNode;
				}
			}

			// Update Martial Arts.
			if (objXmlKit["martialarts"] != null)
			{
                AppDiagnostics.Record("packs.phase-martialarts");
				// Open the Martial Arts XML file and locate the selected art.
				XmlDocument objXmlMartialArtDocument = XmlManager.Instance.Load("martialarts.xml");

				foreach (XmlNode objXmlArt in objXmlKit.SelectNodes("martialarts/martialart"))
				{
					await work.StepAsync();
					TreeNode objNode = new TreeNode();
					MartialArt objArt = new MartialArt(_objCharacter);
					XmlNode objXmlArtNode = RequirePacksEntry(objXmlMartialArtDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + objXmlArt["name"].InnerText + "\"]"));
					objArt.Create(objXmlArtNode, objNode, _objCharacter);
					objArt.Rating = Convert.ToInt32(objXmlArt["rating"].InnerText);
					_objCharacter.MartialArts.Add(objArt);

					// Check for Advantages.
					foreach (XmlNode objXmlAdvantage in objXmlArt.SelectNodes("advantages/advantage"))
					{
						TreeNode objChildNode = new TreeNode();
						MartialArtAdvantage objAdvantage = new MartialArtAdvantage(_objCharacter);
						XmlNode objXmlAdvantageNode = RequirePacksEntry(objXmlMartialArtDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + objXmlArt["name"].InnerText + "\"]/advantages/advantage[. = \"" + objXmlAdvantage.InnerText + "\"]"));
						objAdvantage.Create(objXmlAdvantageNode, _objCharacter, objChildNode);
						if (objAdvantage.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
						objArt.Advantages.Add(objAdvantage);

						objNode.Nodes.Add(objChildNode);
						objNode.Expand();
					}

					treMartialArts.Nodes[0].Nodes.Add(objNode);
					treMartialArts.Nodes[0].Expand();
				}

				// Maneuvers.
				foreach (XmlNode objXmlManeuver in objXmlKit.SelectNodes("martialarts/maneuver"))
				{
					await work.StepAsync();
					TreeNode objNode = new TreeNode();
					MartialArtManeuver objManeuver = new MartialArtManeuver(_objCharacter);
					XmlNode objXmlManeuverNode = RequirePacksEntry(objXmlMartialArtDocument.SelectSingleNode("/chummer/maneuvers/maneuver[name = \"" + objXmlManeuver.InnerText + "\"]"));
					objManeuver.Create(objXmlManeuverNode, objNode);
					if (objManeuver.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					objNode.ContextMenuStrip = cmsMartialArtManeuver;
					_objCharacter.MartialArtManeuvers.Add(objManeuver);

					treMartialArts.Nodes[1].Nodes.Add(objNode);
					treMartialArts.Nodes[1].Expand();
				}
			}

			// Update Adept Powers.
			if (objXmlKit["powers"] != null)
			{
                AppDiagnostics.Record("packs.phase-powers");
				// Open the Powers XML file and locate the selected power.
				XmlDocument objXmlPowerDocument = XmlManager.Instance.Load("powers.xml");

				foreach (XmlNode objXmlPower in objXmlKit.SelectNodes("powers/power"))
				{
					await work.StepAsync();
					XmlNode objXmlPowerNode = RequirePacksEntry(objXmlPowerDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objXmlPower["name"].InnerText + "\"]"));

					int i = panPowers.Controls.Count;

					Power objPower = new Power(_objCharacter);
					_objCharacter.Powers.Add(objPower);

					PowerControl objPowerControl = new PowerControl();
					objPowerControl.PowerObject = objPower;

					// Attach an EventHandler for the PowerRatingChanged Event.
					objPowerControl.PowerRatingChanged += objPower_PowerRatingChanged;
					objPowerControl.DeletePower += objPower_DeletePower;

					objPowerControl.PowerName = objXmlPowerNode["name"].InnerText;
					objPowerControl.PointsPerLevel = Convert.ToDecimal(objXmlPowerNode["points"].InnerText, GlobalOptions.Instance.CultureInfo);
					if (objXmlPowerNode["levels"].InnerText == "no")
					{
						objPowerControl.LevelEnabled = false;
					}
					else
					{
						objPowerControl.LevelEnabled = true;
						if (objXmlPowerNode["levels"].InnerText != "yes")
							objPower.MaxLevels = Convert.ToInt32(objXmlPowerNode["levels"].InnerText);
					}

					objPower.Source = objXmlPowerNode["source"].InnerText;
					objPower.Page = objXmlPowerNode["page"].InnerText;
					if (objXmlPowerNode["doublecost"] != null)
						objPower.DoubleCost = false;

					if (objXmlPowerNode.InnerXml.Contains("bonus"))
					{
						objPower.Bonus = objXmlPowerNode["bonus"];

						if (objXmlPower["name"].Attributes["select"] != null)
							_objImprovementManager.ForcedValue = objXmlPower["name"].Attributes["select"].InnerText;

						if (!_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Power, objPower.InternalId, objPower.Bonus, false, Convert.ToInt32(objPower.Rating), objPower.DisplayNameShort)) throw new OperationCanceledException();
						objPowerControl.Extra = _objImprovementManager.SelectedValue;
					}

					objPowerControl.Top = i * objPowerControl.Height;
					panPowers.Controls.Add(objPowerControl);

					// Set the Rating of the Power if applicable.
					if (objXmlPower["rating"] != null)
						objPowerControl.PowerLevel = Convert.ToInt32(objXmlPower["rating"].InnerText);
				}
			}

			// Update Complex Forms.
			if (objXmlKit["programs"] != null)
			{
                AppDiagnostics.Record("packs.phase-programs");
				// Open the Programs XML file and locate the selected program.
				XmlDocument objXmlProgramDocument = XmlManager.Instance.Load("programs.xml");

				foreach (XmlNode objXmlProgram in objXmlKit.SelectNodes("programs/program"))
				{
					await work.StepAsync();
					XmlNode objXmlProgramNode = RequirePacksEntry(objXmlProgramDocument.SelectSingleNode("/chummer/programs/program[name = \"" + objXmlProgram["name"].InnerText + "\"]"));

					string strForceValue = "";
					if (objXmlProgram.Attributes["select"] != null)
						strForceValue = objXmlProgram.Attributes["select"].InnerText;

					TreeNode objNode = new TreeNode();
					TechProgram objProgram = new TechProgram(_objCharacter);
					objProgram.Create(objXmlProgramNode, _objCharacter, objNode, strForceValue);
					if (objProgram.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();

					// Set the Rating of the Power if applicable.
					if (objXmlProgram["rating"] != null)
						objProgram.Rating = Convert.ToInt32(objXmlProgram["rating"].InnerText);

					// Check for Program Options.
					foreach (XmlNode objXmlOption in objXmlProgram.SelectNodes("options/option"))
					{
						string strChildForceValue = "";
						if (objXmlOption.Attributes["select"] != null)
							strChildForceValue = objXmlOption.Attributes["select"].InnerText;

						XmlNode objXmlOptionNode = RequirePacksEntry(objXmlProgramDocument.SelectSingleNode("/chummer/options/option[name = \"" + objXmlOption["name"].InnerText + "\"]"));

						TreeNode objChildNode = new TreeNode();
						TechProgramOption objOption = new TechProgramOption(_objCharacter);
						objOption.Create(objXmlOptionNode, _objCharacter, objChildNode, strChildForceValue);
						if (objOption.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
						objChildNode.ContextMenuStrip = cmsComplexFormPlugin;

						// Set the Rating of the Option if applicable.
						if (objXmlOption["rating"] != null)
							objOption.Rating = Convert.ToInt32(objXmlOption["rating"].InnerText);

						objProgram.Options.Add(objOption);
						objNode.Nodes.Add(objChildNode);
						objNode.Expand();
					}

					switch (objProgram.Category)
					{
						case "Advanced":
							treComplexForms.Nodes[0].Nodes.Add(objNode);
							treComplexForms.Nodes[0].Expand();
							break;
						case "ARE Programs":
							treComplexForms.Nodes[1].Nodes.Add(objNode);
							treComplexForms.Nodes[1].Expand();
							break;
						case "Autosoft":
							treComplexForms.Nodes[2].Nodes.Add(objNode);
							treComplexForms.Nodes[2].Expand();
							break;
						case "Common Use":
							treComplexForms.Nodes[3].Nodes.Add(objNode);
							treComplexForms.Nodes[3].Expand();
							break;
						case "Hacking":
							treComplexForms.Nodes[4].Nodes.Add(objNode);
							treComplexForms.Nodes[4].Expand();
							break;
						case "Malware":
							treComplexForms.Nodes[5].Nodes.Add(objNode);
							treComplexForms.Nodes[5].Expand();
							break;
						case "Sensor Software":
							treComplexForms.Nodes[6].Nodes.Add(objNode);
							treComplexForms.Nodes[6].Expand();
							break;
						case "Skillsofts":
							treComplexForms.Nodes[7].Nodes.Add(objNode);
							treComplexForms.Nodes[7].Expand();
							break;
						case "Tactical AR Software":
							treComplexForms.Nodes[8].Nodes.Add(objNode);
							treComplexForms.Nodes[8].Expand();
							break;
					}

					_objCharacter.TechPrograms.Add(objProgram);

					_objFunctions.SortTree(treComplexForms);
				}
			}

			// Update Spells.
			if (objXmlKit["spells"] != null)
			{
                AppDiagnostics.Record("packs.phase-spells");
				XmlDocument objXmlSpellDocument = XmlManager.Instance.Load("spells.xml");

				foreach (XmlNode objXmlSpell in objXmlKit.SelectNodes("spells/spell"))
				{
					await work.StepAsync();
					// Make sure the Spell has not already been added to the character.
					bool blnFound = false;
					foreach (TreeNode nodSpell in treSpells.Nodes[0].Nodes)
					{
						if (nodSpell.Text == objXmlSpell.InnerText)
						{
							blnFound = true;
							break;
						}
					}

					// The Spell is not in the list, so add it.
					if (!blnFound)
					{
						string strForceValue = "";
						if (objXmlSpell.Attributes["select"] != null)
							strForceValue = objXmlSpell.Attributes["select"].InnerText;

						XmlNode objXmlSpellNode = RequirePacksEntry(objXmlSpellDocument.SelectSingleNode("/chummer/spells/spell[name = \"" + objXmlSpell.InnerText + "\"]"));

						Spell objSpell = new Spell(_objCharacter);
						TreeNode objNode = new TreeNode();
						objSpell.Create(objXmlSpellNode, _objCharacter, objNode, strForceValue);
						if (objSpell.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
						objNode.ContextMenuStrip = cmsSpell;
						_objCharacter.Spells.Add(objSpell);

						switch (objSpell.Category)
						{
							case "Combat":
								treSpells.Nodes[0].Nodes.Add(objNode);
								treSpells.Nodes[0].Expand();
								break;
							case "Detection":
								treSpells.Nodes[1].Nodes.Add(objNode);
								treSpells.Nodes[1].Expand();
								break;
							case "Health":
								treSpells.Nodes[2].Nodes.Add(objNode);
								treSpells.Nodes[2].Expand();
								break;
							case "Illusion":
								treSpells.Nodes[3].Nodes.Add(objNode);
								treSpells.Nodes[3].Expand();
								break;
							case "Manipulation":
								treSpells.Nodes[4].Nodes.Add(objNode);
								treSpells.Nodes[4].Expand();
								break;
							case "Geomancy Ritual":
								treSpells.Nodes[5].Nodes.Add(objNode);
								treSpells.Nodes[5].Expand();
								break;
						}

						_objFunctions.SortTree(treSpells);
					}
				}
			}

			// Update Spirits.
			if (objXmlKit["spirits"] != null)
			{
                AppDiagnostics.Record("packs.phase-spirits");
				foreach (XmlNode objXmlSpirit in objXmlKit.SelectNodes("spirits/spirit"))
				{
					await work.StepAsync();
					int i = panSpirits.Controls.Count;

					Spirit objSpirit = new Spirit(_objCharacter);
					_objCharacter.Spirits.Add(objSpirit);

					SpiritControl objSpiritControl = new SpiritControl();
					objSpiritControl.SpiritObject = objSpirit;
					objSpiritControl.EntityType = SpiritType.Spirit;

					// Attach an EventHandler for the ServicesOwedChanged Event.
					objSpiritControl.ServicesOwedChanged += objSpirit_ServicesOwedChanged;
					objSpiritControl.ForceChanged += objSpirit_ForceChanged;
					objSpiritControl.BoundChanged += objSpirit_BoundChanged;
					objSpiritControl.DeleteSpirit += objSpirit_DeleteSpirit;

					objSpiritControl.Name = objXmlSpirit["name"].InnerText;
					objSpiritControl.Force = Convert.ToInt32(objXmlSpirit["force"].InnerText);
					objSpiritControl.ServicesOwed = Convert.ToInt32(objXmlSpirit["services"].InnerText);

					objSpiritControl.Top = i * objSpiritControl.Height;
					panSpirits.Controls.Add(objSpiritControl);
				}
			}

			// Update Lifestyles.
			if (objXmlKit["lifestyles"] != null)
			{
                AppDiagnostics.Record("packs.phase-lifestyles");
				XmlDocument objXmlLifestyleDocument = XmlManager.Instance.Load("lifestyles.xml");

				foreach (XmlNode objXmlLifestyle in objXmlKit.SelectNodes("lifestyles/lifestyle"))
				{
					await work.StepAsync();
					string strName = objXmlLifestyle["name"].InnerText;
					int intMonths = Convert.ToInt32(objXmlLifestyle["months"].InnerText);

					// Create the Lifestyle.
					TreeNode objNode = new TreeNode();
					Lifestyle objLifestyle = new Lifestyle(_objCharacter);

					XmlNode objXmlLifestyleNode = RequirePacksEntry(objXmlLifestyleDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"" + strName + "\"]"));
					if (objXmlLifestyleNode != null)
					{
						// This is a standard Lifestyle, so just use the Create method.
						objLifestyle.Create(objXmlLifestyleNode, objNode);
						if (objLifestyle.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
						objLifestyle.Months = intMonths;
					}
					else
					{
						// This is an Advanced Lifestyle, so build it manually.
						objLifestyle.Name = strName;
						objLifestyle.Months = intMonths;
						objLifestyle.Cost = Convert.ToInt32(objXmlLifestyle["cost"].InnerText);
						objLifestyle.Dice = Convert.ToInt32(objXmlLifestyle["dice"].InnerText);
						objLifestyle.Multiplier = Convert.ToInt32(objXmlLifestyle["multiplier"].InnerText);
						objLifestyle.Comforts = objXmlLifestyle["comforts"].InnerText;
						objLifestyle.Entertainment = objXmlLifestyle["entertainment"].InnerText;
						objLifestyle.Necessities = objXmlLifestyle["necessities"].InnerText;
						objLifestyle.Neighborhood = objXmlLifestyle["neighborhood"].InnerText;
						objLifestyle.Security = objXmlLifestyle["security"].InnerText;
						objLifestyle.Source = "RC";
						objLifestyle.Page = "154";

						foreach (XmlNode objXmlQuality in objXmlLifestyle.SelectNodes("qualities/quality"))
							objLifestyle.Qualities.Add(objXmlQuality.InnerText);

						objNode.Text = strName;
					}

					// Add the Lifestyle to the character and Lifestyle Tree.
					if (objLifestyle.Comforts != "")
						objNode.ContextMenuStrip = cmsAdvancedLifestyle;
					else
						objNode.ContextMenuStrip = cmsLifestyleNotes;
					_objCharacter.Lifestyles.Add(objLifestyle);
					treLifestyles.Nodes[0].Nodes.Add(objNode);
					treLifestyles.Nodes[0].Expand();
				}
			}

			// Update NuyenBP.
			if (objXmlKit["nuyenbp"] != null)
			{
                AppDiagnostics.Record("packs.phase-nuyenbp");
                await work.StepAsync();
				int intAmount = Convert.ToInt32(objXmlKit["nuyenbp"].InnerText);
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
					intAmount *= 2;

				// Make sure we don't go over the field's maximum which would throw an Exception.
				if (nudNuyen.Value + intAmount > nudNuyen.Maximum)
					nudNuyen.Value = nudNuyen.Maximum;
				else
					nudNuyen.Value += intAmount;
			}

			// Update Armor.
			if (objXmlKit["armors"] != null)
			{
                AppDiagnostics.Record("packs.phase-armors");
				XmlDocument objXmlArmorDocument = XmlManager.Instance.Load("armor.xml");

				foreach (XmlNode objXmlArmor in objXmlKit.SelectNodes("armors/armor"))
				{
					await work.StepAsync();
					XmlNode objXmlArmorNode = RequirePacksEntry(objXmlArmorDocument.SelectSingleNode("/chummer/armors/armor[name = \"" + objXmlArmor["name"].InnerText + "\"]"));

					Armor objArmor = new Armor(_objCharacter);
					TreeNode objNode = new TreeNode();
					objArmor.Create(objXmlArmorNode, objNode, cmsArmorMod, false, blnCreateChildren);
					if (objArmor.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					_objCharacter.Armor.Add(objArmor);

					// Look for Armor Mods.
					if (objXmlArmor["mods"] != null)
					{
						foreach (XmlNode objXmlMod in objXmlArmor.SelectNodes("mods/mod"))
						{
							List<Weapon> lstWeapons = new List<Weapon>();
							List<TreeNode> lstWeaponNodes = new List<TreeNode>();
							XmlNode objXmlModNode = RequirePacksEntry(objXmlArmorDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objXmlMod["name"].InnerText + "\"]"));
							ArmorMod objMod = new ArmorMod(_objCharacter);
							TreeNode objModNode = new TreeNode();
							int intRating = 0;
							if (objXmlMod["rating"] != null)
								intRating = Convert.ToInt32(objXmlMod["rating"].InnerText);
							objMod.Create(objXmlModNode, objModNode, intRating, lstWeapons, lstWeaponNodes);
							if (objMod.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
							objModNode.ContextMenuStrip = cmsArmorMod;
							objMod.Parent = objArmor;

							objArmor.ArmorMods.Add(objMod);

							objNode.Nodes.Add(objModNode);
							objNode.Expand();

							// Add any Weapons created by the Mod.
							foreach (Weapon objWeapon in lstWeapons)
								_objCharacter.Weapons.Add(objWeapon);

							foreach (TreeNode objWeaponNode in lstWeaponNodes)
							{
								objWeaponNode.ContextMenuStrip = cmsWeapon;
								treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
								treWeapons.Nodes[0].Expand();
							}
						}
					}

					XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");
					foreach (XmlNode objXmlGear in objXmlArmor.SelectNodes("gears/gear"))
						await AddPACKSGear(objXmlGearDocument, objXmlGear, objNode, objArmor, cmsArmorGear, blnCreateChildren);

					objNode.ContextMenuStrip = cmsArmor;
					treArmor.Nodes[0].Nodes.Add(objNode);
					treArmor.Nodes[0].Expand();
				}
			}

			// Update Weapons.
			if (objXmlKit["weapons"] != null)
			{
                AppDiagnostics.Record("packs.phase-weapons");
				XmlDocument objXmlWeaponDocument = XmlManager.Instance.Load("weapons.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("weapons/weapon").Count;
				int i = 0;
				foreach (XmlNode objXmlWeapon in objXmlKit.SelectNodes("weapons/weapon"))
				{
					await work.StepAsync();
					i++;
					pgbProgress.Value = i;
					await work.CheckpointAsync();

					XmlNode objXmlWeaponNode = RequirePacksEntry(objXmlWeaponDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objXmlWeapon["name"].InnerText + "\"]"));

					Weapon objWeapon = new Weapon(_objCharacter);
					TreeNode objNode = new TreeNode();
					objWeapon.Create(objXmlWeaponNode, _objCharacter, objNode, cmsWeapon, cmsWeaponAccessory, cmsWeaponMod, blnCreateChildren);
					if (objWeapon.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					_objCharacter.Weapons.Add(objWeapon);

					// Look for Weapon Accessories.
					if (objXmlWeapon["accessories"] != null)
					{
						foreach (XmlNode objXmlAccessory in objXmlWeapon.SelectNodes("accessories/accessory"))
						{
							XmlNode objXmlAccessoryNode = RequirePacksEntry(objXmlWeaponDocument.SelectSingleNode("/chummer/accessories/accessory[name = \"" + objXmlAccessory["name"].InnerText + "\"]"));
							WeaponAccessory objMod = new WeaponAccessory(_objCharacter);
							TreeNode objModNode = new TreeNode();
							string strMount = "";
							if (objXmlAccessory["mount"] != null)
								strMount = objXmlAccessory["mount"].InnerText;
							objMod.Create(objXmlAccessoryNode, objModNode, strMount);
							if (objMod.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
							objModNode.ContextMenuStrip = cmsWeaponAccessory;
							objMod.Parent = objWeapon;

							objWeapon.WeaponAccessories.Add(objMod);

							XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");
							foreach (XmlNode objXmlGear in objXmlAccessory.SelectNodes("gears/gear"))
								await AddPACKSGear(objXmlGearDocument, objXmlGear, objModNode, objMod, cmsWeaponAccessoryGear, blnCreateChildren);

							objNode.Nodes.Add(objModNode);
							objNode.Expand();
						}
					}

					// Look for Weapon Mods.
					if (objXmlWeapon["mods"] != null)
					{
						foreach (XmlNode objXmlMod in objXmlWeapon.SelectNodes("mods/mod"))
						{
							XmlNode objXmlModNode = RequirePacksEntry(objXmlWeaponDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objXmlMod["name"].InnerText + "\"]"));
							WeaponMod objMod = new WeaponMod(_objCharacter);
							TreeNode objModNode = new TreeNode();
							objMod.Create(objXmlModNode, objModNode);
							if (objMod.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
							objModNode.ContextMenuStrip = cmsWeaponMod;
							objMod.Parent = objWeapon;

							objWeapon.WeaponMods.Add(objMod);

							objNode.Nodes.Add(objModNode);
							objNode.Expand();
						}
					}

					// Look for an Underbarrel Weapon.
					if (objXmlWeapon["underbarrel"] != null)
					{
						XmlNode objXmlUnderbarrelNode = RequirePacksEntry(objXmlWeaponDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objXmlWeapon["underbarrel"].InnerText + "\"]"));

						Weapon objUnderbarrelWeapon = new Weapon(_objCharacter);
						TreeNode objUnderbarrelNode = new TreeNode();
						objUnderbarrelWeapon.Create(objXmlUnderbarrelNode, _objCharacter, objUnderbarrelNode, cmsWeapon, cmsWeaponAccessory, cmsWeaponMod, blnCreateChildren);
						if (objUnderbarrelWeapon.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
						objWeapon.UnderbarrelWeapons.Add(objUnderbarrelWeapon);
						objNode.Nodes.Add(objUnderbarrelNode);
						objNode.Expand();
					}

					objNode.ContextMenuStrip = cmsWeapon;
					treWeapons.Nodes[0].Nodes.Add(objNode);
					treWeapons.Nodes[0].Expand();

					await work.CheckpointAsync();
				}
			}

			// Update Cyberware.
			if (objXmlKit["cyberwares"] != null)
			{
                AppDiagnostics.Record("packs.phase-cyberwares");
				XmlDocument objXmlCyberwareDocument = XmlManager.Instance.Load("cyberware.xml");
				XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("cyberwares/cyberware").Count;
				int i = 0;
				foreach (XmlNode objXmlCyberware in objXmlKit.SelectNodes("cyberwares/cyberware"))
				{
					await work.StepAsync();
					i++;
					pgbProgress.Value = i;
					await work.CheckpointAsync();

					List<Weapon> objWeapons = new List<Weapon>();
					List<TreeNode> objWeaponNodes = new List<TreeNode>();
					TreeNode objNode = new TreeNode();
					Cyberware objCyberware = new Cyberware(_objCharacter);
					Grade objGrade = objCyberware.ConvertToCyberwareGrade(objXmlCyberware["grade"].InnerText, Improvement.ImprovementSource.Cyberware);

					int intRating = 0;
					if (objXmlCyberware["rating"] != null)
						intRating = Convert.ToInt32(objXmlCyberware["rating"].InnerText);

					XmlNode objXmlCyberwareNode = RequirePacksEntry(objXmlCyberwareDocument.SelectSingleNode("/chummer/cyberwares/cyberware[name = \"" + objXmlCyberware["name"].InnerText + "\"]"));
					objCyberware.Create(objXmlCyberwareNode, _objCharacter, objGrade, Improvement.ImprovementSource.Cyberware, intRating, objNode, objWeapons, objWeaponNodes, true, blnCreateChildren);
					if (objCyberware.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					_objCharacter.Cyberware.Add(objCyberware);

					// Add any children.
					if (objXmlCyberware["cyberwares"] != null)
					{
						foreach (XmlNode objXmlChild in objXmlCyberware.SelectNodes("cyberwares/cyberware"))
						{
							TreeNode objChildNode = new TreeNode();
							Cyberware objChildCyberware = new Cyberware(_objCharacter);

							int intChildRating = 0;
							if (objXmlChild["rating"] != null)
								intChildRating = Convert.ToInt32(objXmlChild["rating"].InnerText);

							XmlNode objXmlChildNode = RequirePacksEntry(objXmlCyberwareDocument.SelectSingleNode("/chummer/cyberwares/cyberware[name = \"" + objXmlChild["name"].InnerText + "\"]"));
							objChildCyberware.Create(objXmlChildNode, _objCharacter, objGrade, Improvement.ImprovementSource.Cyberware, intChildRating, objChildNode, objWeapons, objWeaponNodes, true, blnCreateChildren);
							if (objChildCyberware.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
							objCyberware.Children.Add(objChildCyberware);
							objChildNode.ContextMenuStrip = cmsCyberware;

							foreach (XmlNode objXmlGear in objXmlChild.SelectNodes("gears/gear"))
								await AddPACKSGear(objXmlGearDocument, objXmlGear, objChildNode, objChildCyberware, cmsCyberwareGear, blnCreateChildren);

							objNode.Nodes.Add(objChildNode);
							objNode.Expand();
						}
					}

					foreach (XmlNode objXmlGear in objXmlCyberware.SelectNodes("gears/gear"))
						await AddPACKSGear(objXmlGearDocument, objXmlGear, objNode, objCyberware, cmsCyberwareGear, blnCreateChildren);

					objNode.ContextMenuStrip = cmsCyberware;
					treCyberware.Nodes[0].Nodes.Add(objNode);
					treCyberware.Nodes[0].Expand();

					// Add any Weapons created by the Gear.
					foreach (Weapon objWeapon in objWeapons)
						_objCharacter.Weapons.Add(objWeapon);

					foreach (TreeNode objWeaponNode in objWeaponNodes)
					{
						objWeaponNode.ContextMenuStrip = cmsWeapon;
						treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
						treWeapons.Nodes[0].Expand();
					}

					await work.CheckpointAsync();
				}

				_objFunctions.SortTree(treCyberware);
			}

			// Update Bioware.
			if (objXmlKit["biowares"] != null)
			{
                AppDiagnostics.Record("packs.phase-biowares");
				XmlDocument objXmlBiowareDocument = XmlManager.Instance.Load("bioware.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("biowares/bioware").Count;
				int i = 0;

				foreach (XmlNode objXmlBioware in objXmlKit.SelectNodes("biowares/bioware"))
				{
					await work.StepAsync();
					i++;
					pgbProgress.Value = i;
					await work.CheckpointAsync();

					List<Weapon> objWeapons = new List<Weapon>();
					List<TreeNode> objWeaponNodes = new List<TreeNode>();
					TreeNode objNode = new TreeNode();
					Cyberware objCyberware = new Cyberware(_objCharacter);
					Grade objGrade = objCyberware.ConvertToCyberwareGrade(objXmlBioware["grade"].InnerText, Improvement.ImprovementSource.Bioware);

					int intRating = 0;
					if (objXmlBioware["rating"] != null)
						intRating = Convert.ToInt32(objXmlBioware["rating"].InnerText);

					XmlNode objXmlBiowareNode = RequirePacksEntry(objXmlBiowareDocument.SelectSingleNode("/chummer/biowares/bioware[name = \"" + objXmlBioware["name"].InnerText + "\"]"));
					objCyberware.Create(objXmlBiowareNode, _objCharacter, objGrade, Improvement.ImprovementSource.Bioware, intRating, objNode, objWeapons, objWeaponNodes, true, blnCreateChildren);
					if (objCyberware.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					_objCharacter.Cyberware.Add(objCyberware);

					objNode.ContextMenuStrip = cmsBioware;
					treCyberware.Nodes[1].Nodes.Add(objNode);
					treCyberware.Nodes[1].Expand();

					// Add any Weapons created by the Gear.
					foreach (Weapon objWeapon in objWeapons)
						_objCharacter.Weapons.Add(objWeapon);

					foreach (TreeNode objWeaponNode in objWeaponNodes)
					{
						objWeaponNode.ContextMenuStrip = cmsWeapon;
						treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
						treWeapons.Nodes[0].Expand();
					}

					await work.CheckpointAsync();
				}

				_objFunctions.SortTree(treCyberware);
			}

			// Update Gear.
			if (objXmlKit["gears"] != null)
			{
                AppDiagnostics.Record("packs.phase-gears");
				XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("gears/gear").Count;
				int i = 0;

				foreach (XmlNode objXmlGear in objXmlKit.SelectNodes("gears/gear"))
				{
					await work.StepAsync();
					i++;
					pgbProgress.Value = i;
					await work.CheckpointAsync();

					await AddPACKSGear(objXmlGearDocument, objXmlGear, treGear.Nodes[0], _objCharacter, cmsGear, blnCreateChildren);

					await work.CheckpointAsync();
				}
			}

			// Update Vehicles.
			if (objXmlKit["vehicles"] != null)
			{
                AppDiagnostics.Record("packs.phase-vehicles");
				XmlDocument objXmlVehicleDocument = XmlManager.Instance.Load("vehicles.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("vehicles/vehicle").Count;
				int i = 0;

				foreach (XmlNode objXmlVehicle in objXmlKit.SelectNodes("vehicles/vehicle"))
				{
					await work.StepAsync();
					i++;
					pgbProgress.Value = i;
					await work.CheckpointAsync();

					Gear objDefaultSensor = new Gear(_objCharacter);

					TreeNode objNode = new TreeNode();
					Vehicle objVehicle = new Vehicle(_objCharacter);

					XmlNode objXmlVehicleNode = RequirePacksEntry(objXmlVehicleDocument.SelectSingleNode("/chummer/vehicles/vehicle[name = \"" + objXmlVehicle["name"].InnerText + "\"]"));
					objVehicle.Create(objXmlVehicleNode, objNode, cmsVehicle, cmsVehicleGear, cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponMod, blnCreateChildren);
					if (objVehicle.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					_objCharacter.Vehicles.Add(objVehicle);

					// Grab the default Sensor that comes with the Vehicle.
					foreach (Gear objSensorGear in objVehicle.Gear)
					{
						if (objSensorGear.Category == "Sensors" && objSensorGear.Cost == "0" && objSensorGear.Rating == 0)
						{
							objDefaultSensor = objSensorGear;
							break;
						}
					}

					// Add any Vehicle Mods.
					if (objXmlVehicle["mods"] != null)
					{
						foreach (XmlNode objXmlMod in objXmlVehicle.SelectNodes("mods/mod"))
						{
							TreeNode objModNode = new TreeNode();
							VehicleMod objMod = new VehicleMod(_objCharacter);

							int intRating = 0;
							if (objXmlMod["rating"] != null)
								intRating = Convert.ToInt32(objXmlMod["rating"].InnerText);

							XmlNode objXmlModNode = RequirePacksEntry(objXmlVehicleDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objXmlMod["name"].InnerText + "\"]"));
							objMod.Create(objXmlModNode, objModNode, intRating);
							if (objMod.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
							objVehicle.Mods.Add(objMod);

							objNode.Nodes.Add(objModNode);
							objNode.Expand();
						}
					}

					// Add any Vehicle Gear.
					if (objXmlVehicle["gears"] != null)
					{
						XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");

						foreach (XmlNode objXmlGear in objXmlVehicle.SelectNodes("gears/gear"))
						{
							List<Weapon> objWeapons = new List<Weapon>();
							List<TreeNode> objWeaponNodes = new List<TreeNode>();
							TreeNode objGearNode = new TreeNode();
							Gear objGear = new Gear(_objCharacter);
							int intQty = 1;

							int intRating = 0;
							if (objXmlGear["rating"] != null)
								intRating = Convert.ToInt32(objXmlGear["rating"].InnerText);
							string strForceValue = "";
							if (objXmlGear["name"].Attributes["select"] != null)
								strForceValue = objXmlGear["name"].Attributes["select"].InnerText;
							if (objXmlGear["qty"] != null)
								intQty = Convert.ToInt32(objXmlGear["qty"].InnerText);

							XmlNode objXmlGearNode = RequirePacksEntry(objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlGear["name"].InnerText + "\"]"));
							objGear.Create(objXmlGearNode, _objCharacter, objGearNode, intRating, objWeapons, objWeaponNodes, strForceValue, false, false, false, blnCreateChildren, false);
							if (objGear.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
							objGear.Quantity = intQty;
							objGearNode.Text = objGear.DisplayName;
							objVehicle.Gear.Add(objGear);

							// Look for child components.
							if (objXmlGear["gears"] != null)
							{
								foreach (XmlNode objXmlChild in objXmlGear.SelectNodes("gears/gear"))
								{
									await AddPACKSGear(objXmlGearDocument, objXmlChild, objGearNode, objGear, cmsVehicleGear, blnCreateChildren);
								}
							}

							objGearNode.Expand();
							objGearNode.ContextMenuStrip = cmsVehicleGear;
							objNode.Nodes.Add(objGearNode);
							objNode.Expand();

							// If this is a Sensor, it will replace the Vehicle's base sensor, so remove it.
							if (objGear.Category == "Sensors" && objGear.Cost == "0" && objGear.Rating == 0)
							{
								objVehicle.Gear.Remove(objDefaultSensor);
								foreach (TreeNode objSensorNode in objNode.Nodes)
								{
									if (objSensorNode.Tag.ToString() == objDefaultSensor.InternalId)
									{
										objSensorNode.Remove();
										break;
									}
								}
							}
						}
					}

					// Add any Vehicle Weapons.
					if (objXmlVehicle["weapons"] != null)
					{
						XmlDocument objXmlWeaponDocument = XmlManager.Instance.Load("weapons.xml");

						foreach (XmlNode objXmlWeapon in objXmlVehicle.SelectNodes("weapons/weapon"))
						{
							TreeNode objWeaponNode = new TreeNode();
							Weapon objWeapon = new Weapon(_objCharacter);

							XmlNode objXmlWeaponNode = RequirePacksEntry(objXmlWeaponDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objXmlWeapon["name"].InnerText + "\"]"));
							objWeapon.Create(objXmlWeaponNode, _objCharacter, objWeaponNode, cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponMod, blnCreateChildren);
							if (objWeapon.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
							objWeapon.VehicleMounted = true;

							// Find the first Weapon Mount in the Vehicle.
							foreach (VehicleMod objMod in objVehicle.Mods)
							{
								if (objMod.Name.StartsWith("Weapon Mount"))
								{
									objMod.Weapons.Add(objWeapon);
									foreach (TreeNode objModNode in objNode.Nodes)
									{
										if (objModNode.Tag.ToString() == objMod.InternalId)
										{
											objWeaponNode.ContextMenuStrip = cmsVehicleWeapon;
											objModNode.Nodes.Add(objWeaponNode);
											objModNode.Expand();
											break;
										}
									}
									break;
								}
							}

							// Look for Weapon Accessories.
							if (objXmlWeapon["accessories"] != null)
							{
								foreach (XmlNode objXmlAccessory in objXmlWeapon.SelectNodes("accessories/accessory"))
								{
									XmlNode objXmlAccessoryNode = RequirePacksEntry(objXmlWeaponDocument.SelectSingleNode("/chummer/accessories/accessory[name = \"" + objXmlAccessory["name"].InnerText + "\"]"));
									WeaponAccessory objMod = new WeaponAccessory(_objCharacter);
									TreeNode objModNode = new TreeNode();
									string strMount = "";
									if (objXmlAccessory["mount"] != null)
										strMount = objXmlAccessory["mount"].InnerText;
									objMod.Create(objXmlAccessoryNode, objModNode, strMount);
									if (objMod.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
									objModNode.ContextMenuStrip = cmsWeaponAccessory;
									objMod.Parent = objWeapon;

									objWeapon.WeaponAccessories.Add(objMod);

									objWeaponNode.Nodes.Add(objModNode);
									objWeaponNode.Expand();
								}
							}

							// Look for Weapon Mods.
							if (objXmlWeapon["mods"] != null)
							{
								foreach (XmlNode objXmlMod in objXmlWeapon.SelectNodes("mods/mod"))
								{
									XmlNode objXmlModNode = RequirePacksEntry(objXmlWeaponDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objXmlMod["name"].InnerText + "\"]"));
									WeaponMod objMod = new WeaponMod(_objCharacter);
									TreeNode objModNode = new TreeNode();
									objMod.Create(objXmlModNode, objModNode);
									if (objMod.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
									objModNode.ContextMenuStrip = cmsVehicleWeaponMod;
									objMod.Parent = objWeapon;

									objWeapon.WeaponMods.Add(objMod);

									objWeaponNode.Nodes.Add(objModNode);
									objWeaponNode.Expand();
								}
							}
						}
					}

					objNode.ContextMenuStrip = cmsVehicle;
					treVehicles.Nodes[0].Nodes.Add(objNode);
					treVehicles.Nodes[0].Expand();

					await work.CheckpointAsync();
				}
			}
            await work.CheckpointAsync();
            PopulateGearList();
            UpdateCharacterInfo();
        }

		/// <summary>
		/// Add a piece of Gear that was found in a PACKS Kit.
		/// </summary>
		/// <param name="objXmlGearDocument">XmlDocument that contains the Gear.</param>
		/// <param name="objXmlGear">XmlNode of the Gear to add.</param>
		/// <param name="objParent">TreeNode to attach the created items to.</param>
		/// <param name="objParentObject">Object to associate the newly-created items with.</param>
		/// <param name="cmsContextMenu">ContextMenuStrip to assign to the TreeNodes created.</param>
		/// <param name="blnCreateChildren">Whether or not the default plugins for the Gear should be created.</param>
		private async System.Threading.Tasks.Task AddPACKSGear(XmlDocument objXmlGearDocument, XmlNode objXmlGear, TreeNode objParent, Object objParentObject, ContextMenuStrip cmsContextMenu, bool blnCreateChildren)
		{
            await _packsWork.CheckpointAsync();
			int intRating = 0;
			if (objXmlGear["rating"] != null)
				intRating = Convert.ToInt32(objXmlGear["rating"].InnerText);
			int intQty = 1;
			if (objXmlGear["qty"] != null)
				intQty = Convert.ToInt32(objXmlGear["qty"].InnerText);

			XmlNode objXmlGearNode;
			if (objXmlGear["category"] != null)
				objXmlGearNode = RequirePacksEntry(objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlGear["name"].InnerText + "\" and category = \"" + objXmlGear["category"].InnerText + "\"]"));
			else
				objXmlGearNode = RequirePacksEntry(objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlGear["name"].InnerText + "\"]"));

			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			TreeNode objNode = new TreeNode();
			string strForceValue = "";
			if (objXmlGear["name"].Attributes["select"] != null)
				strForceValue = objXmlGear["name"].Attributes["select"].InnerText;

			Gear objNewGear = new Gear(_objCharacter);
			switch (objXmlGearNode["category"].InnerText)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGearNode, _objCharacter, objNode, intRating, true, blnCreateChildren);
					if (objCommlink.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					objCommlink.Quantity = intQty;
					objNewGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGearNode, _objCharacter, objNode, intRating, true, blnCreateChildren);
					if (objOperatingSystem.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					objOperatingSystem.Quantity = intQty;
					objNewGear = objOperatingSystem;
					break;
				default:
					Gear objGear = new Gear(_objCharacter);
					objGear.Create(objXmlGearNode, _objCharacter, objNode, intRating, objWeapons, objWeaponNodes, strForceValue, false, false, true, blnCreateChildren);
					if (objGear.InternalId == Guid.Empty.ToString()) throw new OperationCanceledException();
					objGear.Quantity = intQty;
					objNode.Text = objGear.DisplayName;
					objNewGear = objGear;
					break;
			}

			if (objParentObject.GetType() == typeof(Character))
				((Character)objParentObject).Gear.Add(objNewGear);
			if (objParentObject.GetType() == typeof(Gear) || objParentObject.GetType() == typeof(Commlink) || objParentObject.GetType() == typeof(OperatingSystem))
			{
				((Gear)objParentObject).Children.Add(objNewGear);
				objNewGear.Parent = (Gear)objParentObject;
			}
			if (objParentObject.GetType() == typeof(Armor))
				((Armor)objParentObject).Gear.Add(objNewGear);
			if (objParentObject.GetType() == typeof(WeaponAccessory))
				((WeaponAccessory)objParentObject).Gear.Add(objNewGear);
			if (objParentObject.GetType() == typeof(Cyberware))
				((Cyberware)objParentObject).Gear.Add(objNewGear);

			// Look for child components.
			if (objXmlGear["gears"] != null)
			{
				foreach (XmlNode objXmlChild in objXmlGear.SelectNodes("gears/gear"))
				{
					await AddPACKSGear(objXmlGearDocument, objXmlChild, objNode, objNewGear, cmsContextMenu, blnCreateChildren);
				}
			}

			objParent.Nodes.Add(objNode);
			objParent.Expand();

			objNode.ContextMenuStrip = cmsContextMenu;
			objNode.Text = objNewGear.DisplayName;

			// Add any Weapons created by the Gear.
			foreach (Weapon objWeapon in objWeapons)
				_objCharacter.Weapons.Add(objWeapon);

			foreach (TreeNode objWeaponNode in objWeaponNodes)
			{
				objWeaponNode.ContextMenuStrip = cmsWeapon;
				treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
				treWeapons.Nodes[0].Expand();
			}

		}
}
