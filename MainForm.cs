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
    private readonly Button _saveProfileButton = new();
    private readonly DataGridView _resourcesGrid = new();
    private readonly ComboBox _resourcePresetPicker = new();
    private readonly TextBox _resourceNameText = new();
    private readonly NumericUpDown _resourceCountInput = new();
    private readonly Button _applyResourceButton = new();
    private readonly Label _profileInfoLabel = new();
    private readonly Label _statusLabel = new();
    private readonly BindingList<MetaResourceRow> _resourceRows = new();

    private readonly ComboBox _charactersFilePicker = new();
    private readonly Button _browseCharactersButton = new();
    private readonly Button _reloadCharactersButton = new();
    private readonly Button _saveCharactersButton = new();
    private readonly ComboBox _characterPicker = new();
    private readonly TextBox _talentFilterText = new();
    private readonly DataGridView _talentsGrid = new();
    private readonly TextBox _talentRowNameText = new();
    private readonly NumericUpDown _talentRankInput = new();
    private readonly Button _applyTalentButton = new();
    private readonly Label _characterInfoLabel = new();
    private readonly BindingList<TalentRow> _talentRows = new();

    private IcarusProfile? _profile;
    private IcarusCharacters? _characters;
    private IcarusCharacter? _selectedCharacter;

    public MainForm()
    {
        Text = "Icarus Profile Mod";
        MinimumSize = new Size(960, 620);
        Size = new Size(1080, 720);
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
            RowCount = 2,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        Controls.Add(root);

        TabControl tabs = new()
        {
            Dock = DockStyle.Fill
        };
        root.Controls.Add(tabs, 0, 0);

        TabPage resourcesTab = new("Profile Resources");
        resourcesTab.Controls.Add(BuildProfileResourcesTab());
        tabs.TabPages.Add(resourcesTab);

        TabPage talentsTab = new("Character Talents");
        talentsTab.Controls.Add(BuildCharacterTalentsTab());
        tabs.TabPages.Add(talentsTab);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 1);
    }

    private Control BuildProfileResourcesTab()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));

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

        topBar.Controls.Add(new Label
        {
            Text = "Profile.json",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);

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

        ConfigureButton(_saveProfileButton, "Save");
        _saveProfileButton.Click += (_, _) => SaveProfile();
        topBar.Controls.Add(_saveProfileButton, 3, 1);

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

        return root;
    }

    private Control BuildCharacterTalentsTab()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));

        TableLayoutPanel fileBar = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2
        };
        fileBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fileBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        fileBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        fileBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        fileBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        fileBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(fileBar, 0, 0);

        fileBar.Controls.Add(new Label
        {
            Text = "Characters.json",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);

        _charactersFilePicker.Dock = DockStyle.Fill;
        _charactersFilePicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _charactersFilePicker.SelectedIndexChanged += (_, _) => LoadSelectedCharactersFile();
        fileBar.Controls.Add(_charactersFilePicker, 0, 1);

        ConfigureButton(_browseCharactersButton, "Browse");
        _browseCharactersButton.Click += (_, _) => BrowseForCharactersFile();
        fileBar.Controls.Add(_browseCharactersButton, 1, 1);

        ConfigureButton(_reloadCharactersButton, "Reload");
        _reloadCharactersButton.Click += (_, _) => LoadSelectedCharactersFile();
        fileBar.Controls.Add(_reloadCharactersButton, 2, 1);

        ConfigureButton(_saveCharactersButton, "Save");
        _saveCharactersButton.Click += (_, _) => SaveCharactersFile();
        fileBar.Controls.Add(_saveCharactersButton, 3, 1);

        TableLayoutPanel characterBar = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(0, 6, 0, 0)
        };
        characterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        characterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        characterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        characterBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        characterBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(characterBar, 0, 1);

        characterBar.Controls.Add(new Label { Text = "Character", Dock = DockStyle.Fill }, 0, 0);
        characterBar.Controls.Add(new Label { Text = "Filter", Dock = DockStyle.Fill }, 1, 0);

        _characterPicker.Dock = DockStyle.Fill;
        _characterPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _characterPicker.SelectedIndexChanged += (_, _) => LoadSelectedCharacter();
        characterBar.Controls.Add(_characterPicker, 0, 1);

        _talentFilterText.Dock = DockStyle.Fill;
        _talentFilterText.TextChanged += (_, _) => RefreshTalentRows();
        characterBar.Controls.Add(_talentFilterText, 1, 1);

        _characterInfoLabel.Dock = DockStyle.Fill;
        _characterInfoLabel.TextAlign = ContentAlignment.MiddleRight;
        characterBar.Controls.Add(_characterInfoLabel, 2, 1);

        _talentsGrid.Dock = DockStyle.Fill;
        _talentsGrid.AllowUserToAddRows = false;
        _talentsGrid.AllowUserToDeleteRows = false;
        _talentsGrid.AutoGenerateColumns = false;
        _talentsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _talentsGrid.MultiSelect = false;
        _talentsGrid.DataSource = _talentRows;
        _talentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "RowName",
            DataPropertyName = nameof(TalentRow.RowName),
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _talentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Rank",
            DataPropertyName = nameof(TalentRow.Rank),
            Width = 120
        });
        _talentsGrid.SelectionChanged += (_, _) => FillTalentEditorFromSelection();
        root.Controls.Add(_talentsGrid, 0, 2);

        TableLayoutPanel editor = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(0, 10, 0, 0)
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(editor, 0, 3);

        editor.Controls.Add(new Label { Text = "Talent RowName", Dock = DockStyle.Fill }, 0, 0);
        editor.Controls.Add(new Label { Text = "Rank", Dock = DockStyle.Fill }, 1, 0);

        _talentRowNameText.Dock = DockStyle.Fill;
        editor.Controls.Add(_talentRowNameText, 0, 1);

        _talentRankInput.Dock = DockStyle.Fill;
        _talentRankInput.Maximum = 99;
        editor.Controls.Add(_talentRankInput, 1, 1);

        ConfigureButton(_applyTalentButton, "Apply");
        _applyTalentButton.Click += (_, _) => ApplyTalentEdit();
        editor.Controls.Add(_applyTalentButton, 2, 1);

        return root;
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
        _charactersFilePicker.Items.Clear();

        foreach (string profilePath in ProfileFinder.FindProfiles())
        {
            _profilePicker.Items.Add(profilePath);
        }

        foreach (string charactersPath in ProfileFinder.FindCharactersFiles())
        {
            _charactersFilePicker.Items.Add(charactersPath);
        }

        if (_profilePicker.Items.Count > 0)
        {
            _profilePicker.SelectedIndex = 0;
            SetStatus($"Found {_profilePicker.Items.Count} profile file(s).");
        }
        else
        {
            SetStatus("No Profile.json found. Use Browse to select one.");
        }

        if (_charactersFilePicker.Items.Count > 0)
        {
            _charactersFilePicker.SelectedIndex = 0;
        }
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

        AddComboBoxItem(_profilePicker, dialog.FileName);
        _profilePicker.SelectedItem = dialog.FileName;
    }

    private void BrowseForCharactersFile()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select Icarus Characters.json",
            Filter = "Icarus characters (Characters.json)|Characters.json|JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        AddComboBoxItem(_charactersFilePicker, dialog.FileName);
        _charactersFilePicker.SelectedItem = dialog.FileName;
    }

    private static void AddComboBoxItem(ComboBox comboBox, string value)
    {
        if (!comboBox.Items.Cast<string>().Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            comboBox.Items.Add(value);
        }
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

            string charactersPath = ProfileFinder.GetCharactersPathForProfile(path);
            if (File.Exists(charactersPath))
            {
                AddComboBoxItem(_charactersFilePicker, charactersPath);
                _charactersFilePicker.SelectedItem = charactersPath;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not load profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Load failed.");
        }
    }

    private void LoadSelectedCharactersFile()
    {
        if (_charactersFilePicker.SelectedItem is not string path || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            _characters = IcarusCharacters.Load(path);
            RefreshCharacterPicker();
            SetStatus($"Loaded {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not load characters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Characters load failed.");
        }
    }

    private void RefreshCharacterPicker()
    {
        _characterPicker.Items.Clear();
        _selectedCharacter = null;
        _talentRows.Clear();

        if (_characters is null)
        {
            _characterInfoLabel.Text = "";
            return;
        }

        foreach (IcarusCharacter character in _characters.Characters)
        {
            _characterPicker.Items.Add(character);
        }

        if (_characterPicker.Items.Count > 0)
        {
            _characterPicker.SelectedIndex = 0;
            return;
        }

        _characterInfoLabel.Text = "No characters";
    }

    private void LoadSelectedCharacter()
    {
        _selectedCharacter = _characterPicker.SelectedItem as IcarusCharacter;
        RefreshTalentRows();
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

    private void RefreshTalentRows()
    {
        _talentRows.Clear();

        if (_selectedCharacter is null)
        {
            _characterInfoLabel.Text = "";
            return;
        }

        string filter = _talentFilterText.Text.Trim();
        foreach (TalentEntry talent in _selectedCharacter.Talents)
        {
            if (filter.Length > 0 && !talent.RowName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _talentRows.Add(new TalentRow(talent.RowName, talent.Rank));
        }

        _characterInfoLabel.Text = $"XP {_selectedCharacter.Xp:N0} | {_selectedCharacter.Talents.Count} talents";
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

    private void FillTalentEditorFromSelection()
    {
        if (_talentsGrid.CurrentRow?.DataBoundItem is not TalentRow row)
        {
            return;
        }

        _talentRowNameText.Text = row.RowName;
        _talentRankInput.Value = Math.Clamp(row.Rank, 0, 99);
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
        SelectResourceRow(name);
        SetStatus($"Applied {GetFriendlyName(name)} ({name}) = {count:N0}. Save to write Profile.json.");
    }

    private void ApplyTalentEdit()
    {
        if (_selectedCharacter is null)
        {
            SetStatus("Load a character first.");
            return;
        }

        string rowName = _talentRowNameText.Text.Trim();
        if (rowName.Length == 0)
        {
            SetStatus("Talent RowName is required.");
            return;
        }

        int rank = decimal.ToInt32(_talentRankInput.Value);
        _selectedCharacter.SetTalent(rowName, rank);
        RefreshTalentRows();
        SelectTalentRow(rowName);
        SetStatus($"Applied {rowName} rank {rank}. Save to write Characters.json.");
    }

    private void SelectResourceRow(string name)
    {
        MetaResourceRow? row = _resourceRows.FirstOrDefault(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return;
        }

        int index = _resourceRows.IndexOf(row);
        _resourcesGrid.ClearSelection();
        _resourcesGrid.Rows[index].Selected = true;
        _resourcesGrid.CurrentCell = _resourcesGrid.Rows[index].Cells[0];
    }

    private void SelectTalentRow(string rowName)
    {
        TalentRow? row = _talentRows.FirstOrDefault(item =>
            string.Equals(item.RowName, rowName, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return;
        }

        int index = _talentRows.IndexOf(row);
        _talentsGrid.ClearSelection();
        _talentsGrid.Rows[index].Selected = true;
        _talentsGrid.CurrentCell = _talentsGrid.Rows[index].Cells[0];
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
            SetStatus($"Saved Profile.json. Backup: {backupPath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Profile save failed.");
        }
    }

    private void SaveCharactersFile()
    {
        if (_characters is null)
        {
            SetStatus("Load Characters.json first.");
            return;
        }

        try
        {
            if (_selectedCharacter is not null)
            {
                foreach (TalentRow row in _talentRows)
                {
                    _selectedCharacter.SetTalent(row.RowName, row.Rank);
                }
            }

            string backupPath = _characters.SaveWithBackup();
            SetStatus($"Saved Characters.json. Backup: {backupPath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save characters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Characters save failed.");
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

internal sealed class TalentRow
{
    public TalentRow(string rowName, int rank)
    {
        RowName = rowName;
        Rank = rank;
    }

    public string RowName { get; set; }

    public int Rank { get; set; }
}
