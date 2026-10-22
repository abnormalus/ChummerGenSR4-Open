#nullable enable
using System;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class ContactControl
{
    internal Forms.Form? CreationViewportOwner { get; set; }
    private Forms.IWin32Window ContactDialogOwner => CreationViewportOwner != null ? CreationViewportOwner : this;
    internal Forms.TextBox CreationNameInput => txtContactName;
    internal Forms.NumericUpDown CreationConnectionInput => nudConnection;
    internal Forms.NumericUpDown CreationLoyaltyInput => nudLoyalty;
    internal Forms.Label CreationConnectionLabel => lblConnection;
    internal Forms.Label CreationLoyaltyLabel => lblLoyalty;
    internal Forms.Label CreationGroupLabel => lblGroupLabel;
    internal Forms.Label CreationGroupValue => lblGroup;
    internal Forms.Button CreationGroupButton => cmdGroup;
    internal Forms.Button CreationDeleteButton => cmdDelete;
    internal Forms.PictureBox CreationNotesImage => imgNotes;
    internal Forms.PictureBox CreationLinkImage => imgLink;
    internal Forms.ToolTip CreationToolTips => tipTooltip;
    internal bool HasLinkedCharacter => ContactObject.FileName != "";
    internal void EditCreationNotes() => imgNotes_Click(imgNotes, EventArgs.Empty);
    internal void AttachCreationCharacter() => tsAttachCharacter_Click(tsAttachCharacter, EventArgs.Empty);
    internal void OpenCreationCharacter() => tsContactOpen_Click(tsContactOpen, EventArgs.Empty);
    internal void RemoveCreationCharacter() => tsRemoveCharacter_Click(tsRemoveCharacter, EventArgs.Empty);
}
