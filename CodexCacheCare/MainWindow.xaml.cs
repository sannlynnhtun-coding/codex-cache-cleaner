using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;

namespace CodexCacheCare;

public sealed partial class MainWindow : Window
{
    private readonly (CacheKind Kind, string Title, string Description)[] groups =
    [
        (CacheKind.Desktop, "Desktop cache", "Cached web content, scripts and graphics."),
        (CacheKind.Backups, "Previous cache copies", "Timestamped backups of desktop cache folders."),
        (CacheKind.Models, "Model catalog", "Cached model metadata from Codex home.")
    ];

    private readonly Dictionary<CacheKind, CheckBox> choices = [];
    private ScanResult scan = new([], []);
    private bool hasScan;

    public MainWindow()
    {
        InitializeComponent();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.Maximize();
        RenderScan();
    }

    private void LayoutChanged(object sender, SizeChangedEventArgs e)
    {
        bool wide = e.NewSize.Width >= 960;
        SideColumn.Width = new GridLength(wide ? 320 : 0);
        Dashboard.ColumnSpacing = wide ? 24 : 0;
        Dashboard.RowSpacing = wide ? 0 : 16;
        Grid.SetRow(Overview, wide ? 0 : 1);
        Grid.SetColumn(Overview, wide ? 1 : 0);
    }

    private static ScanResult ScanCache()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string? configuredHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        string home = string.IsNullOrWhiteSpace(configuredHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex")
            : Path.GetFullPath(configuredHome);

        return CacheScanner.Scan(home, Path.Combine(roaming, "Codex"),
            Path.Combine(local, "Packages", "OpenAI.Codex_2p2nqsd0c76g0",
                "LocalCache", "Roaming", "Codex"));
    }

    private async void ScanClicked(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        ShowStatus("Scanning cache", "Reading file metadata in known Codex cache locations.");
        try
        {
            scan = await Task.Run(ScanCache);
            hasScan = true;
            ScanButton.Content = "Scan again";
            Total.Text = FormatSize(scan.Files.Sum(file => file.Bytes));
            FileCount.Text = $"{scan.Files.Count:N0} files available for review";
            RenderScan();
            ShowStatus(scan.Skipped.Count == 0 ? "Scan complete" : "Scan completed with skips",
                scan.Skipped.Count == 0 ? "Choose the categories you want to clear."
                    : $"{scan.Skipped.Count:N0} linked or inaccessible paths were skipped.",
                scan.Skipped.Count == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            scan = new([], []);
            hasScan = false;
            Total.Text = "—";
            FileCount.Text = "Scan unavailable";
            RenderScan();
            ShowStatus("Scan failed", ex.Message, InfoBarSeverity.Error);
        }
        finally { SetBusy(false); }
    }

    private void RenderScan()
    {
        Categories.Children.Clear();
        choices.Clear();
        foreach (var group in groups)
        {
            var files = scan.Files.Where(file => file.Kind == group.Kind).ToArray();
            string size = FormatSize(files.Sum(file => file.Bytes));
            var title = new TextBlock { Text = group.Title, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
            var description = new TextBlock { Text = group.Description, TextWrapping = TextWrapping.Wrap };
            var label = new StackPanel { Spacing = 3 };
            label.Children.Add(title);
            label.Children.Add(description);

            var check = new CheckBox { Content = label, IsEnabled = files.Length > 0, Padding = new Thickness(0, 7, 0, 7) };
            AutomationProperties.SetName(check, $"{group.Title}, {files.Length} files, {size}");
            AutomationProperties.SetHelpText(check, group.Description);
            check.Checked += (_, _) => UpdateSelection();
            check.Unchecked += (_, _) => UpdateSelection();
            choices.Add(group.Kind, check);

            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(check);
            var amount = new TextBlock
            {
                Text = hasScan ? size : "—",
                HorizontalAlignment = HorizontalAlignment.Right,
                Style = (Style)Root.Resources["CategorySizeStyle"]
            };
            var count = new TextBlock
            {
                Text = hasScan ? $"{files.Length:N0} {(files.Length == 1 ? "file" : "files")}" : "",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var metrics = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 3 };
            metrics.Children.Add(amount);
            metrics.Children.Add(count);
            Grid.SetColumn(metrics, 1);
            row.Children.Add(metrics);
            Categories.Children.Add(new Border
            {
                Style = (Style)Root.Resources["CategoryRowStyle"],
                Child = row
            });
        }

        UpdateSelection();
    }

    private CacheFile[] SelectedFiles() =>
        scan.Files.Where(file => choices[file.Kind].IsChecked == true).ToArray();

    private void UpdateSelection()
    {
        var selected = SelectedFiles();
        Selection.Text = selected.Length == 0 ? "No files selected"
            : $"{selected.Length:N0} files selected · {FormatSize(selected.Sum(file => file.Bytes))}";
        ReviewButton.IsEnabled = ScanButton.IsEnabled && selected.Length > 0;
    }

    private async void ReviewClicked(object sender, RoutedEventArgs e)
    {
        var files = SelectedFiles();
        if (files.Length == 0)
            return;

        SetBusy(true);
        try
        {
            var summary = new StackPanel { Spacing = 10 };
            foreach (var group in groups)
            {
                var selected = files.Where(file => file.Kind == group.Kind).ToArray();
                if (selected.Length > 0)
                    summary.Children.Add(new TextBlock
                    {
                        Text = $"{group.Title} · {selected.Length:N0} files · {FormatSize(selected.Sum(file => file.Bytes))}",
                        TextWrapping = TextWrapping.Wrap
                    });
            }
            summary.Children.Add(new TextBlock
            {
                Text = "This permanently deletes the selected cache files. Close Codex and any Codex CLI sessions before clearing.",
                TextWrapping = TextWrapping.Wrap
            });

            var dialog = new ContentDialog
            {
                XamlRoot = Root.XamlRoot, Title = "Clear selected cache?", Content = summary,
                PrimaryButtonText = "Clear cache", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Close
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            ShowStatus("Clearing cache", "Checking that Codex is closed and the selected files match the scan.");
            var result = await Task.Run(() =>
            {
                if (CacheScanner.CodexIsRunning())
                    return (Running: true, Removed: 0, Bytes: 0L);

                int removed = 0;
                long bytes = 0;
                foreach (var file in files)
                {
                    try { CacheScanner.Delete(file); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
                    removed++;
                    bytes += file.Bytes;
                }
                return (Running: false, Removed: removed, Bytes: bytes);
            });

            if (result.Running)
            {
                ShowStatus("Codex is still running", "Exit Codex and any Codex CLI sessions, then try again.",
                    InfoBarSeverity.Warning);
                return;
            }

            scan = new([], []);
            hasScan = false;
            Total.Text = "—";
            FileCount.Text = "Scan again to check remaining cache.";
            RenderScan();
            int skipped = files.Length - result.Removed;
            ShowStatus($"{result.Removed:N0} files removed · {FormatSize(result.Bytes)}",
                skipped == 0 ? "Cleanup finished." : $"{skipped:N0} files were skipped because they changed, disappeared, or could not be removed.",
                skipped == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool busy)
    {
        ScanButton.IsEnabled = !busy;
        Categories.IsHitTestVisible = !busy;
        UpdateSelection();
    }

    private void ShowStatus(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        Status.Title = title;
        Status.Message = message;
        Status.Severity = severity;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes:N0} B";
        if (bytes < 1024L * 1024) return $"{bytes / 1024d:0.#} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024):0.#} MB";
        return $"{bytes / (1024d * 1024 * 1024):0.#} GB";
    }
}
