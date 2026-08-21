using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IcarusProfileMod;

internal sealed class MainForm : Form
{
    private static readonly ResourcePreset[] ResourcePresets =
    [
        new("Credits", "Ren"),
        new("Refund", "Refund Tokens"),
        new("Exotic1", "Exotics"),
        new("Exotic_Red", "Red Exotics"),
        new("Biomass", "Legendary Biomass"),
        new("Licence", "Legendary Licence"),
        new("Exotic_Uranium", "Uranium Rod Currency"),
    ];

    private readonly ComboBox _profilePicker = new();
    private readonly Button _browseButton = new();
    private readonly Button _reloadButton = new();
    private readonly Button _saveButton = new();
    private readonly DataGridView _resourcesGrid = new();
    private readonly ComboBox _resourcePresetPicker = new();
    private readonly TextBox _resourceNameText = new();
    private readonly NumericUpDown _resourceCountInput = new();
    private readonly Button _applyResourceButton = new();
    private readonly Label _profileInfoLabel = new();
    private readonly Label _statusLabel = new();
    private readonly BindingList<MetaResourceRow> _resourceRows = new();

    private IcarusProfile? _profile;

    public MainForm()
    {
        Text = "Icarus Profile Mod";
        MinimumSize = new Size(900, 560);
        Size = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        Load += (_, _) => DiscoverProfiles();
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        Controls.Add(root);

        TableLayoutPanel topBar = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2
        };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        topBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        topBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(topBar, 0, 0);

        Label pathLabel = new()
        {
            Text = "Profile.json",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };
        topBar.Controls.Add(pathLabel, 0, 0);

        _profilePicker.Dock = DockStyle.Fill;
        _profilePicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _profilePicker.SelectedIndexChanged += (_, _) => LoadSelectedProfile();
        topBar.Controls.Add(_profilePicker, 0, 1);

        ConfigureButton(_browseButton, "Browse");
        _browseButton.Click += (_, _) => BrowseForProfile();
        topBar.Controls.Add(_browseButton, 1, 1);

        ConfigureButton(_reloadButton, "Reload");
        _reloadButton.Click += (_, _) => LoadSelectedProfile();
        topBar.Controls.Add(_reloadButton, 2, 1);

        ConfigureButton(_saveButton, "Save");
        _saveButton.Click += (_, _) => SaveProfile();
        topBar.Controls.Add(_saveButton, 3, 1);

        _resourcesGrid.Dock = DockStyle.Fill;
        _resourcesGrid.AllowUserToAddRows = false;
        _resourcesGrid.AllowUserToDeleteRows = false;
        _resourcesGrid.AutoGenerateColumns = false;
        _resourcesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _resourcesGrid.MultiSelect = false;
        _resourcesGrid.DataSource = _resourceRows;
        _resourcesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Resource",
            DataPropertyName = nameof(MetaResourceRow.DisplayName),
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _resourcesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "MetaRow",
            DataPropertyName = nameof(MetaResourceRow.Name),
            ReadOnly = true,
            Width = 180
        });
        _resourcesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Count",
            DataPropertyName = nameof(MetaResourceRow.Count),
            Width = 150
        });
        _resourcesGrid.SelectionChanged += (_, _) => FillEditorFromSelection();
        root.Controls.Add(_resourcesGrid, 0, 1);

        TableLayoutPanel editor = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 2,
            Padding = new Padding(0, 10, 0, 0)
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(editor, 0, 2);

        editor.Controls.Add(new Label { Text = "Preset", Dock = DockStyle.Fill }, 0, 0);
        editor.Controls.Add(new Label { Text = "MetaRow", Dock = DockStyle.Fill }, 1, 0);
        editor.Controls.Add(new Label { Text = "Count", Dock = DockStyle.Fill }, 2, 0);

        _resourcePresetPicker.Dock = DockStyle.Fill;
        _resourcePresetPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _resourcePresetPicker.Items.Add("Custom");
        foreach (ResourcePreset preset in ResourcePresets)
        {
            _resourcePresetPicker.Items.Add(preset);
        }
        _resourcePresetPicker.SelectedIndex = 0;
        _resourcePresetPicker.SelectedIndexChanged += (_, _) => ApplySelectedPreset();
        editor.Controls.Add(_resourcePresetPicker, 0, 1);

        _resourceNameText.Dock = DockStyle.Fill;
        editor.Controls.Add(_resourceNameText, 1, 1);

        _resourceCountInput.Dock = DockStyle.Fill;
        _resourceCountInput.Maximum = int.MaxValue;
        _resourceCountInput.ThousandsSeparator = true;
        editor.Controls.Add(_resourceCountInput, 2, 1);

        ConfigureButton(_applyResourceButton, "Apply");
        _applyResourceButton.Click += (_, _) => ApplyResourceEdit();
        editor.Controls.Add(_applyResourceButton, 3, 1);

        _profileInfoLabel.Dock = DockStyle.Fill;
        _profileInfoLabel.TextAlign = ContentAlignment.MiddleRight;
        editor.Controls.Add(_profileInfoLabel, 4, 1);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 3);
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(6, 0, 0, 0);
    }

    private void DiscoverProfiles()
    {
        _profilePicker.Items.Clear();

        foreach (string profilePath in ProfileFinder.FindProfiles())
        {
            _profilePicker.Items.Add(profilePath);
        }

        if (_profilePicker.Items.Count > 0)
        {
            _profilePicker.SelectedIndex = 0;
            SetStatus($"Found {_profilePicker.Items.Count} profile file(s).");
            return;
        }

        SetStatus("No Profile.json found. Use Browse to select one.");
    }

    private void BrowseForProfile()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select Icarus Profile.json",
            Filter = "Icarus profile (Profile.json)|Profile.json|JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!_profilePicker.Items.Cast<string>().Contains(dialog.FileName, StringComparer.OrdinalIgnoreCase))
        {
            _profilePicker.Items.Add(dialog.FileName);
        }

        _profilePicker.SelectedItem = dialog.FileName;
    }

    private void LoadSelectedProfile()
    {
        if (_profilePicker.SelectedItem is not string path || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            _profile = IcarusProfile.Load(path);
            RefreshResourceRows();
            _profileInfoLabel.Text = $"User {_profile.UserId}";
            SetStatus($"Loaded {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not load profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Load failed.");
        }
    }

    private void RefreshResourceRows()
    {
        _resourceRows.Clear();

        if (_profile is null)
        {
            return;
        }

        foreach (MetaResource resource in _profile.MetaResources)
        {
            _resourceRows.Add(new MetaResourceRow(resource.Name, GetFriendlyName(resource.Name), resource.Count));
        }
    }

    private void FillEditorFromSelection()
    {
        if (_resourcesGrid.CurrentRow?.DataBoundItem is not MetaResourceRow row)
        {
            return;
        }

        SelectPreset(row.Name);
        _resourceNameText.Text = row.Name;
        _resourceCountInput.Value = Math.Clamp(row.Count, 0, int.MaxValue);
    }

    private void ApplySelectedPreset()
    {
        if (_resourcePresetPicker.SelectedItem is ResourcePreset preset)
        {
            _resourceNameText.Text = preset.MetaRow;
        }
    }

    private void SelectPreset(string metaRow)
    {
        ResourcePreset? preset = ResourcePresets.FirstOrDefault(item =>
            string.Equals(item.MetaRow, metaRow, StringComparison.OrdinalIgnoreCase));

        _resourcePresetPicker.SelectedItem = preset is null ? "Custom" : preset;
    }

    private void ApplyResourceEdit()
    {
        if (_profile is null)
        {
            SetStatus("Load a profile first.");
            return;
        }

        string name = _resourceNameText.Text.Trim();
        if (name.Length == 0)
        {
            SetStatus("MetaRow is required.");
            return;
        }

        int count = decimal.ToInt32(_resourceCountInput.Value);
        _profile.SetMetaResource(name, count);
        RefreshResourceRows();

        MetaResourceRow? row = _resourceRows.FirstOrDefault(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (row is not null)
        {
            int index = _resourceRows.IndexOf(row);
            _resourcesGrid.ClearSelection();
            _resourcesGrid.Rows[index].Selected = true;
            _resourcesGrid.CurrentCell = _resourcesGrid.Rows[index].Cells[0];
        }

        SetStatus($"Applied {GetFriendlyName(name)} ({name}) = {count:N0}. Save to write Profile.json.");
    }

    private void SaveProfile()
    {
        if (_profile is null)
        {
            SetStatus("Load a profile first.");
            return;
        }

        try
        {
            foreach (MetaResourceRow row in _resourceRows)
            {
                _profile.SetMetaResource(row.Name, row.Count);
            }

            string backupPath = _profile.SaveWithBackup();
            SetStatus($"Saved. Backup: {backupPath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Save failed.");
        }
    }

    private static string GetFriendlyName(string metaRow)
    {
        return ResourcePresets.FirstOrDefault(item =>
            string.Equals(item.MetaRow, metaRow, StringComparison.OrdinalIgnoreCase))?.FriendlyName ?? metaRow;
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }
}

internal sealed record ResourcePreset(string MetaRow, string FriendlyName)
{
    public override string ToString()
    {
        return $"{FriendlyName} ({MetaRow})";
    }
}

internal sealed class MetaResourceRow
{
    public MetaResourceRow(string name, string displayName, int count)
    {
        Name = name;
        DisplayName = displayName;
        Count = count;
    }

    public string Name { get; set; }

    public string DisplayName { get; set; }

    public int Count { get; set; }
}
