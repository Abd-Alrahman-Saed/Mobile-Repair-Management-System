using System.Windows;

namespace MRMS.UI.Views;

public partial class VoidReasonWindow : Window
{
    public string Reason { get; private set; } = "";

    private VoidReasonWindow(string prompt)
    {
        InitializeComponent();
        PromptText.Text = prompt;
        Loaded += (_, _) => ReasonBox.Focus();
    }

    /// <summary>Shows the dialog modally; returns the trimmed reason, or null if cancelled.</summary>
    public static string? Ask(Window? owner, string prompt = "سبب الإلغاء")
    {
        var dialog = new VoidReasonWindow(prompt) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.Reason : null;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var reason = ReasonBox.Text.Trim();
        if (reason.Length == 0)
        {
            ErrorText.Text = "يرجى كتابة سبب الإلغاء";
            return;
        }
        Reason = reason;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
