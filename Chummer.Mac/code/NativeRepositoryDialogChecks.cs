#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Chummer;

internal static class NativeRepositoryDialogChecks
{
    internal static async Task RunAsync(frmMain owner, Action<bool, string> check)
    {
        var pending = owner.ConnectRepositoryAsync(false);
        var window = NativeDialogHost.ActiveWindow!;
        try
        {
            await Task.Delay(50);
            var fields = window.GetVisualDescendants().OfType<TextBox>().ToArray();
            var repository = fields.Single(field => AutomationProperties.GetName(field) == "GitHub repository");
            var folder = fields.Single(field => AutomationProperties.GetName(field) == "Character folder");
            var token = fields.Single(field => AutomationProperties.GetName(field) == "GitHub token");
            check(fields.All(field => field.Template != null && field.Bounds.Height >= 24 && field.IsEffectivelyVisible),
                "Native repository setup renders all three input controls with usable bounds");
            check(folder.Text == "characters", "Native repository setup shows its default character folder");
            repository.Focus();
            repository.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = "test/characters" });
            token.Focus();
            token.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = "example-token" });
            check(repository.Text == "test/characters" && token.Text == "example-token" && token.PasswordChar != '\0',
                "Native repository setup accepts repository and masked token input");
        }
        finally
        {
            window.Close();
            await pending;
        }
    }
}
