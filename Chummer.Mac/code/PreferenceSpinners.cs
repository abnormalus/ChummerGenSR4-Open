#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmOptions
{
    private void InitializePreferenceSpinners()
    {
        if (Avalonia.Application.Current == null)
            return;

        var tips = new Forms.ToolTip();
        Disposed += (_, _) => tips.Dispose();
        foreach (var spinner in Descendants(Controls.Cast<Forms.Control>()).OfType<Forms.NumericUpDown>().ToArray())
        {
            string label = spinner.Parent!.Controls.OfType<Forms.Label>()
                .Where(candidate => Math.Abs(candidate.Top - spinner.Top) <= 7)
                .OrderBy(candidate => Math.Min(Math.Abs(candidate.Right - spinner.Left), Math.Abs(candidate.Left - spinner.Right)))
                .Select(candidate => candidate.Text)
                .FirstOrDefault() ?? spinner.Name;
            NativeCompactSpinner.Attach(this, spinner, tips, label);
        }

        static IEnumerable<Forms.Control> Descendants(IEnumerable<Forms.Control> controls)
        {
            foreach (var control in controls)
            {
                yield return control;
                foreach (var child in Descendants(control.Controls.Cast<Forms.Control>()))
                    yield return child;
            }
        }
    }
}
