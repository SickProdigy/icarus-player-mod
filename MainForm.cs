using System;
using System.Collections.Generic;
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
    private readonly Label _playerDataLabel = new();
    private readonly Label _statusLabel = new();
    private readonly BindingList<MetaResourceRow> _resourceRows = new();

    private readonly ComboBox _charactersFilePicker = new();
    private readonly Button _browseCharactersButton = new();
    private readonly Button _reloadCharactersButton = new();
    private readonly Button _saveCharactersButton = new();
    private readonly Button _loadTalentCatalogButton = new();
    private readonly TextBox _talentDataFolderText = new();
    private readonly ComboBox _characterPicker = new();
    private readonly TextBox _talentFilterText = new();
    private readonly DataGridView _talentsGrid = new();
    private readonly DataGridViewTextBoxColumn _talentTreeColumn = new();
    private readonly DataGridViewTextBoxColumn _talentMaxColumn = new();
    private readonly TextBox _talentRowNameText = new();
    private readonly NumericUpDown _talentRankInput = new();
    private readonly Button _applyTalentButton = new();
    private readonly Button _maxTalentButton = new();
    private readonly Button _unlockAllTalentsButton = new();
    private readonly Button _maxAllTalentsButton = new();
    private readonly Label _characterInfoLabel = new();
    private readonly BindingList<TalentRow> _talentRows = new();

    private IcarusProfile? _profile;
    private IcarusCharacters? _characters;
    private IcarusCharacter? _selectedCharacter;
    private TalentCatalog? _talentCatalog;

    public MainForm()
    {
        Text = "Icarus Profile Mod";
        MinimumSize = new Size(960, 620);
        Size = new Size(1080, 720);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        Load += (_, _) =>
        {
            TryLoadDefaultTalentCatalog();
            DiscoverProfiles();
        };
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        Controls.Add(root);

        MenuStrip menuStrip = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0)
        };
        ToolStripMenuItem helpMenu = new("Help");
        ToolStripMenuItem aboutMenuItem = new("About Icarus Profile Mod");
        aboutMenuItem.Click += (_, _) => ShowAboutDialog();
        helpMenu.DropDownItems.Add(aboutMenuItem);
        menuStrip.Items.Add(helpMenu);
        MainMenuStrip = menuStrip;
        root.Controls.Add(menuStrip, 0, 0);

        TableLayoutPanel header = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(2, 0, 2, 8)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.Controls.Add(new Label
        {
            Text = "Icarus Player Data Editor",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 0, 18, 0)
        }, 0, 0);
        _playerDataLabel.Text = "Looking for player data...";
        _playerDataLabel.Dock = DockStyle.Fill;
        _playerDataLabel.AutoEllipsis = true;
        _playerDataLabel.ForeColor = SystemColors.GrayText;
        _playerDataLabel.TextAlign = ContentAlignment.MiddleLeft;
        header.Controls.Add(_playerDataLabel, 1, 0);
        root.Controls.Add(header, 0, 1);

        TabControl tabs = new()
        {
            Dock = DockStyle.Fill
        };
        root.Controls.Add(tabs, 0, 2);

        TabPage resourcesTab = new("Profile Resources");
        resourcesTab.Controls.Add(BuildProfileResourcesTab());
        tabs.TabPages.Add(resourcesTab);

        TabPage talentsTab = new("Character Talents");
        talentsTab.Controls.Add(BuildCharacterTalentsTab());
        tabs.TabPages.Add(talentsTab);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 3);
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
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
        ConfigurePathPicker(_profilePicker);
        _profilePicker.SelectedIndexChanged += (_, _) => LoadSelectedProfile();
        topBar.Controls.Add(_profilePicker, 0, 1);

        ConfigureFileButton(_browseButton, "Browse", Color.FromArgb(232, 248, 236), _profilePicker);
        _browseButton.Click += (_, _) => BrowseForProfile();
        topBar.Controls.Add(_browseButton, 1, 1);

        ConfigureFileButton(_reloadButton, "Reload", Color.FromArgb(255, 246, 204), _profilePicker);
        _reloadButton.Click += (_, _) => LoadSelectedProfile();
        topBar.Controls.Add(_reloadButton, 2, 1);

        ConfigureFileButton(_saveProfileButton, "Save", Color.FromArgb(255, 226, 226), _profilePicker);
        _saveProfileButton.Click += (_, _) => SaveProfile();
        topBar.Controls.Add(_saveProfileButton, 3, 1);

        _resourcesGrid.Dock = DockStyle.Fill;
        _resourcesGrid.AllowUserToAddRows = false;
        _resourcesGrid.AllowUserToDeleteRows = false;
        _resourcesGrid.AutoGenerateColumns = false;
        _resourcesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _resourcesGrid.MultiSelect = false;
        ConfigureGrid(_resourcesGrid);
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
            RowCount = 6,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
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
        fileBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
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
        ConfigurePathPicker(_charactersFilePicker);
        _charactersFilePicker.SelectedIndexChanged += (_, _) => LoadSelectedCharactersFile();
        fileBar.Controls.Add(_charactersFilePicker, 0, 1);

        ConfigureFileButton(_browseCharactersButton, "Browse", Color.FromArgb(232, 248, 236), _charactersFilePicker);
        _browseCharactersButton.Click += (_, _) => BrowseForCharactersFile();
        fileBar.Controls.Add(_browseCharactersButton, 1, 1);

        ConfigureFileButton(_reloadCharactersButton, "Reload", Color.FromArgb(255, 246, 204), _charactersFilePicker);
        _reloadCharactersButton.Click += (_, _) => LoadSelectedCharactersFile();
        fileBar.Controls.Add(_reloadCharactersButton, 2, 1);

        ConfigureFileButton(_saveCharactersButton, "Save", Color.FromArgb(255, 226, 226), _charactersFilePicker);
        _saveCharactersButton.Click += (_, _) => SaveCharactersFile();
        fileBar.Controls.Add(_saveCharactersButton, 3, 1);

        TableLayoutPanel catalogBar = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0, 2, 0, 0)
        };
        catalogBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        catalogBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        catalogBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        catalogBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(catalogBar, 0, 1);

        catalogBar.Controls.Add(new Label
        {
            Text = "Talent Data Folder",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);

        _talentDataFolderText.Dock = DockStyle.Fill;
        _talentDataFolderText.ReadOnly = true;
        catalogBar.Controls.Add(_talentDataFolderText, 0, 1);

        ConfigureFileButton(_loadTalentCatalogButton, "Browse", Color.FromArgb(232, 248, 236), _talentDataFolderText);
        _loadTalentCatalogButton.Click += (_, _) => LoadTalentCatalog();
        catalogBar.Controls.Add(_loadTalentCatalogButton, 1, 1);

        TableLayoutPanel bulkBar = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 6, 0, 0)
        };
        bulkBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bulkBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        bulkBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        root.Controls.Add(bulkBar, 0, 2);

        ConfigureButton(_unlockAllTalentsButton, "Unlock All Talents");
        _unlockAllTalentsButton.Click += (_, _) => UnlockAllTalents();
        bulkBar.Controls.Add(_unlockAllTalentsButton, 1, 0);

        ConfigureButton(_maxAllTalentsButton, "Max Rank All Talents");
        _maxAllTalentsButton.Click += (_, _) => MaxAllTalents();
        bulkBar.Controls.Add(_maxAllTalentsButton, 2, 0);

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
        root.Controls.Add(characterBar, 0, 3);

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
        _talentsGrid.MultiSelect = true;
        ConfigureGrid(_talentsGrid);
        _talentsGrid.DataSource = _talentRows;
        _talentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Talent",
            DataPropertyName = nameof(TalentRow.DisplayName),
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _talentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "RowName",
            DataPropertyName = nameof(TalentRow.RowName),
            ReadOnly = true,
            Width = 220
        });
        _talentTreeColumn.HeaderText = "Tree";
        _talentTreeColumn.DataPropertyName = nameof(TalentRow.TreeName);
        _talentTreeColumn.ReadOnly = true;
        _talentTreeColumn.Width = 180;
        _talentTreeColumn.Visible = false;
        _talentsGrid.Columns.Add(_talentTreeColumn);

        _talentMaxColumn.HeaderText = "Max";
        _talentMaxColumn.DataPropertyName = nameof(TalentRow.MaxRankText);
        _talentMaxColumn.ReadOnly = true;
        _talentMaxColumn.Width = 70;
        _talentMaxColumn.Visible = false;
        _talentsGrid.Columns.Add(_talentMaxColumn);
        _talentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Rank",
            DataPropertyName = nameof(TalentRow.Rank),
            Width = 80
        });
        _talentsGrid.SelectionChanged += (_, _) => FillTalentEditorFromSelection();
        root.Controls.Add(_talentsGrid, 0, 4);

        TableLayoutPanel editor = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(0, 10, 0, 0)
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(editor, 0, 5);

        editor.Controls.Add(new Label { Text = "Selected Talent", Dock = DockStyle.Fill }, 0, 0);
        editor.Controls.Add(new Label { Text = "Rank", Dock = DockStyle.Fill }, 1, 0);

        _talentRowNameText.Dock = DockStyle.Fill;
        _talentRowNameText.ReadOnly = true;
        editor.Controls.Add(_talentRowNameText, 0, 1);

        _talentRankInput.Dock = DockStyle.Fill;
        _talentRankInput.Maximum = 99;
        editor.Controls.Add(_talentRankInput, 1, 1);

        ConfigureButton(_applyTalentButton, "Apply Rank");
        _applyTalentButton.Click += (_, _) => ApplyTalentEdit();
        editor.Controls.Add(_applyTalentButton, 2, 1);

        ConfigureButton(_maxTalentButton, "Max Rank Selected");
        _maxTalentButton.Click += (_, _) => MaxSelectedTalent();
        editor.Controls.Add(_maxTalentButton, 3, 1);

        return root;
    }
    private static void ConfigureButton(Button button, string text)
    {
        ConfigureButton(button, text, SystemColors.Control);
    }

    private static void ConfigureButton(Button button, string text, Color backColor)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(6, 0, 0, 0);
        button.BackColor = backColor;
        button.UseVisualStyleBackColor = backColor == SystemColors.Control;
    }

    private static void ConfigureFileButton(Button button, string text, Color backColor, Control field)
    {
        ConfigureButton(button, text, backColor);
        button.Dock = DockStyle.Top;
        button.Height = field.PreferredSize.Height;
    }

    private static void ConfigurePathPicker(ComboBox picker)
    {
        picker.FormattingEnabled = true;
        picker.Format += (_, args) =>
        {
            if (args.ListItem is not string path)
            {
                return;
            }

            string? playerFolder = Path.GetFileName(Path.GetDirectoryName(path));
            args.Value = string.IsNullOrWhiteSpace(playerFolder)
                ? Path.GetFileName(path)
                : $"{playerFolder}  /  {Path.GetFileName(path)}";
        };
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.RowHeadersVisible = false;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.BackgroundColor = SystemColors.Window;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 251);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        grid.ColumnHeadersHeight = 34;
        grid.RowTemplate.Height = 30;
    }

    private void DiscoverProfiles()
    {
        _profilePicker.Items.Clear();
        _charactersFilePicker.Items.Clear();

        IReadOnlyList<string> profiles = ProfileFinder.FindProfiles();
        IReadOnlyList<string> charactersFiles = ProfileFinder.FindCharactersFiles();

        foreach (string profilePath in profiles)
        {
            _profilePicker.Items.Add(profilePath);
        }

        foreach (string charactersPath in charactersFiles)
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

        string? discoveredFile = profiles.FirstOrDefault() ?? charactersFiles.FirstOrDefault();
        _playerDataLabel.Text = discoveredFile is null
            ? "Player data folder not found — use Browse in either editor"
            : $"Player data: {Path.GetDirectoryName(discoveredFile)}";
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
            UpdatePlayerDataHeader(path);
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
            UpdatePlayerDataHeader(path);
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

            TalentMetadata? metadata = _talentCatalog?.Find(talent.RowName);
            _talentRows.Add(new TalentRow(
                talent.RowName,
                metadata?.DisplayName ?? talent.RowName,
                metadata?.TreeName ?? "",
                metadata?.MaxRank,
                talent.Rank));
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
        List<TalentRow> selectedRows = GetSelectedTalentRows();
        if (selectedRows.Count == 0)
        {
            _talentRowNameText.Text = "";
            _talentRankInput.Maximum = 99;
            return;
        }

        if (selectedRows.Count > 1)
        {
            _talentRowNameText.Text = $"{selectedRows.Count} talents selected";
            _talentRankInput.Maximum = 99;
            return;
        }

        TalentRow row = selectedRows[0];
        _talentRowNameText.Text = row.RowName;
        _talentRankInput.Maximum = 99;
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

        List<TalentRow> selectedRows = GetSelectedTalentRows();
        if (selectedRows.Count == 0)
        {
            SetStatus("Select one or more talents first.");
            return;
        }

        int requestedRank = decimal.ToInt32(_talentRankInput.Value);
        List<string> rowNames = selectedRows.Select(row => row.RowName).ToList();
        foreach (TalentRow row in selectedRows)
        {
            _selectedCharacter.SetTalent(row.RowName, ClampTalentRank(row.RowName, requestedRank));
        }

        RefreshTalentRows();
        SelectTalentRows(rowNames);
        SetStatus($"Applied rank {requestedRank} to {selectedRows.Count:N0} selected talent(s). Save to write Characters.json.");
    }

    private void MaxSelectedTalent()
    {
        if (_selectedCharacter is null)
        {
            SetStatus("Load a character first.");
            return;
        }

        if (_talentCatalog is null)
        {
            SetStatus("Load the talent catalog first to max known talent ranks.");
            return;
        }

        List<TalentRow> selectedRows = GetSelectedTalentRows();
        if (selectedRows.Count == 0)
        {
            SetStatus("Select one or more talents first.");
            return;
        }

        int changed = 0;
        List<string> rowNames = [];
        foreach (TalentRow row in selectedRows)
        {
            TalentMetadata? metadata = _talentCatalog.Find(row.RowName);
            if (metadata is null)
            {
                continue;
            }

            _selectedCharacter.SetTalent(row.RowName, metadata.MaxRank);
            rowNames.Add(row.RowName);
            changed++;
        }

        RefreshTalentRows();
        SelectTalentRows(rowNames);
        SetStatus($"Maxed {changed:N0} selected talent(s). Save to write Characters.json.");
    }

    private void UnlockAllTalents()
    {
        if (_selectedCharacter is null)
        {
            SetStatus("Load a character first.");
            return;
        }

        if (_talentCatalog is null)
        {
            SetStatus("Load the talent catalog first to unlock all talents.");
            return;
        }

        Dictionary<string, int> existingRanks = _selectedCharacter.Talents
            .GroupBy(talent => talent.RowName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Rank, StringComparer.OrdinalIgnoreCase);

        int changed = 0;
        foreach (TalentMetadata metadata in _talentCatalog.CharacterTalents.Where(talent => talent.MaxRank > 0))
        {
            int existingRank = existingRanks.TryGetValue(metadata.RowName, out int rank) ? rank : 0;
            int unlockedRank = Math.Clamp(Math.Max(existingRank, 1), 0, metadata.MaxRank);
            _selectedCharacter.SetTalent(metadata.RowName, unlockedRank);
            changed++;
        }

        RefreshTalentRows();
        SetStatus($"Unlocked {changed:N0} character talent(s) at rank 1 where needed. Blueprints and creature talents were skipped. Save to write Characters.json.");
    }

    private void MaxAllTalents()
    {
        if (_selectedCharacter is null)
        {
            SetStatus("Load a character first.");
            return;
        }

        if (_talentCatalog is null)
        {
            SetStatus("Load the talent catalog first to max all talents.");
            return;
        }

        int changed = 0;
        foreach (TalentMetadata metadata in _talentCatalog.CharacterTalents.Where(talent => talent.MaxRank > 0))
        {
            _selectedCharacter.SetTalent(metadata.RowName, metadata.MaxRank);
            changed++;
        }

        RefreshTalentRows();
        SetStatus($"Maxed {changed:N0} character talent(s). Blueprints and creature talents were skipped. Save to write Characters.json.");
    }

    private int ClampTalentRank(string rowName, int rank)
    {
        TalentMetadata? metadata = _talentCatalog?.Find(rowName);
        if (metadata is null)
        {
            return rank;
        }

        return Math.Clamp(rank, 0, metadata.MaxRank);
    }

    private void LoadTalentCatalog()
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Select the folder that contains D_Talents.json, D_TalentTrees.json, and D_TalentRanks.json",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
            SelectedPath = Directory.Exists(_talentDataFolderText.Text) ? _talentDataFolderText.Text : AppContext.BaseDirectory
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        LoadTalentCatalogFromDirectory(dialog.SelectedPath);
    }
    private void TryLoadDefaultTalentCatalog()
    {
        string? catalogDirectory = TalentCatalog.FindDefaultDirectory();
        if (catalogDirectory is null)
        {
            UpdateTalentCatalogUi();
            return;
        }

        LoadTalentCatalogFromDirectory(catalogDirectory, showErrors: false);
    }

    private void LoadTalentCatalogFromDirectory(string directory, bool showErrors = true)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            SetStatus("Select a folder that contains D_Talents.json, D_TalentTrees.json, and D_TalentRanks.json.");
            return;
        }

        try
        {
            _talentCatalog = TalentCatalog.LoadFromDirectory(directory);
            _talentDataFolderText.Text = directory;
            UpdateTalentCatalogUi();
            RefreshTalentRows();
            SetStatus($"Loaded {_talentCatalog.Count:N0} metadata rows from {directory}. Bulk actions use character talents only.");
        }
        catch (Exception ex)
        {
            _talentCatalog = null;
            _talentDataFolderText.Text = directory;
            UpdateTalentCatalogUi();
            if (showErrors)
            {
                MessageBox.Show(this, ex.Message, "Could not load talent catalog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            SetStatus("Talent catalog load failed.");
        }
    }

    private void UpdateTalentCatalogUi()
    {
        bool loaded = _talentCatalog is not null;
        _loadTalentCatalogButton.Text = "Browse";
        _talentTreeColumn.Visible = loaded;
        _talentMaxColumn.Visible = loaded;
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

    private List<TalentRow> GetSelectedTalentRows()
    {
        List<TalentRow> selectedRows = _talentsGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<TalentRow>()
            .GroupBy(row => row.RowName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (selectedRows.Count == 0 && _talentsGrid.CurrentRow?.DataBoundItem is TalentRow currentRow)
        {
            selectedRows.Add(currentRow);
        }

        return selectedRows;
    }

    private void SelectTalentRows(IEnumerable<string> rowNames)
    {
        HashSet<string> selectedRowNames = new(rowNames, StringComparer.OrdinalIgnoreCase);
        if (selectedRowNames.Count == 0)
        {
            return;
        }

        _talentsGrid.ClearSelection();
        foreach (DataGridViewRow gridRow in _talentsGrid.Rows)
        {
            if (gridRow.DataBoundItem is not TalentRow talentRow || !selectedRowNames.Contains(talentRow.RowName))
            {
                continue;
            }

            gridRow.Selected = true;
            _talentsGrid.CurrentCell ??= gridRow.Cells[0];
        }
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
                    _selectedCharacter.SetTalent(row.RowName, ClampTalentRank(row.RowName, row.Rank));
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

    private void UpdatePlayerDataHeader(string filePath)
    {
        _playerDataLabel.Text = $"Player data: {Path.GetDirectoryName(filePath)}";
    }

    private void ShowAboutDialog()
    {
        using AboutDialog dialog = new();
        dialog.ShowDialog(this);
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
    public TalentRow(string rowName, string displayName, string treeName, int? maxRank, int rank)
    {
        RowName = rowName;
        DisplayName = displayName;
        TreeName = treeName;
        MaxRank = maxRank;
        Rank = rank;
    }

    public string RowName { get; set; }

    public string DisplayName { get; set; }

    public string TreeName { get; set; }

    public int? MaxRank { get; set; }

    public string MaxRankText => MaxRank?.ToString() ?? "";

    public int Rank { get; set; }
}
