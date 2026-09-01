using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace IcarusProfileMod;

internal sealed class AboutDialog : Form
{
    private readonly string _website;

    public AboutDialog()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? "Icarus Profile Mod";
        string author = GetMetadata(assembly, "Author")
            ?? assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
            ?? "Unknown";
        string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "Unknown";
        version = version.Split('+')[0];
        _website = GetMetadata(assembly, "Website") ?? "https://sickgaming.net/";

        Text = $"About {product}";
        ClientSize = new Size(430, 250);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(24)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Text = product,
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = $"Author: {author}",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        layout.Controls.Add(new Label
        {
            Text = $"Version: {version}",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 2);
        layout.Controls.Add(new Label
        {
            Text = "License: GNU GPL v3 only",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 3);

        LinkLabel websiteLink = new()
        {
            Text = _website,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            LinkBehavior = LinkBehavior.HoverUnderline
        };
        websiteLink.LinkClicked += (_, _) => OpenWebsite();
        layout.Controls.Add(websiteLink, 0, 4);

        Button closeButton = new()
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Right,
            Width = 90
        };
        layout.Controls.Add(closeButton, 0, 5);
        AcceptButton = closeButton;
        CancelButton = closeButton;
    }

    private static string? GetMetadata(Assembly assembly, string key)
    {
        return assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private void OpenWebsite()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _website,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open website", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
