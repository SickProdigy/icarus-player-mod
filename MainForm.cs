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
    private const int CreatureGeneticMaxValue = 10;

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

    private static readonly TalentCategory[] RegularTalentCategories =
    [
        new("All Talents", null),
        new("Survival", "Survival"),
        new("Adventure", "Player_Adventure"),
        new("Habitation", "Construction"),
        new("Combat", "Combat")
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
    private readonly TextBox _playerDataPathText = new();
    private readonly Label _statusLabel = new();
    private readonly BindingList<MetaResourceRow> _resourceRows = new();

    private readonly ComboBox _charactersFilePicker = new();
    private readonly Button _browseCharactersButton = new();
    private readonly Button _reloadCharactersButton = new();
    private readonly Button _saveCharactersButton = new();
    private readonly Button _loadTalentCatalogButton = new();
    private readonly TextBox _talentDataFolderText = new();
    private readonly ComboBox _characterPicker = new();
    private readonly ComboBox _talentCategoryPicker = new();
    private readonly ComboBox _talentTreePicker = new();
    private readonly TextBox _talentFilterText = new();
    private readonly DataGridView _talentsGrid = new();
    private readonly DataGridViewTextBoxColumn _talentTreeColumn = new();
    private readonly DataGridViewTextBoxColumn _talentMaxColumn = new();
    private readonly TextBox _talentRowNameText = new();
    private readonly NumericUpDown _talentRankInput = new();
    private readonly Button _resetSelectedTalentButton = new();
    private readonly Button _maxTalentButton = new();
    private readonly Button _maxAllTalentsButton = new();
    private readonly Button _resetAllTalentsButton = new();
    private readonly Label _characterInfoLabel = new();
    private readonly BindingList<TalentRow> _talentRows = new();

    private readonly ComboBox _blueprintCharacterPicker = new();
    private readonly TextBox _blueprintFilterText = new();
    private readonly DataGridView _blueprintsGrid = new();
    private readonly NumericUpDown _blueprintRankInput = new();
    private readonly Button _resetSelectedBlueprintButton = new();
    private readonly Button _maxSelectedBlueprintButton = new();
    private readonly Button _maxAllBlueprintsButton = new();
    private readonly Button _resetAllBlueprintsButton = new();
    private readonly Button _saveBlueprintsButton = new();
    private readonly Label _blueprintInfoLabel = new();
    private readonly BindingList<TalentRow> _blueprintRows = new();

    private readonly ComboBox _mountsFilePicker = new();
    private readonly Button _browseMountsButton = new();
    private readonly Button _reloadMountsButton = new();
    private readonly Button _injectMountButton = new();
    private readonly Button _saveMountsButton = new();
    private readonly ComboBox _mountPicker = new();
    private readonly TextBox _mountNameText = new();
    private readonly TextBox _mountSpeciesText = new();
    private readonly NumericUpDown _mountLevelInput = new();
    private readonly NumericUpDown _mountHealthInput = new();
    private readonly NumericUpDown _mountStaminaInput = new();
    private readonly NumericUpDown _mountFoodInput = new();
    private readonly NumericUpDown _mountWaterInput = new();
    private readonly NumericUpDown _mountOxygenInput = new();
    private readonly NumericUpDown _mountVariationInput = new();
    private readonly NumericUpDown _mountUniqueVariationInput = new();
    private readonly NumericUpDown _mountCosmeticSkinInput = new();
    private readonly NumericUpDown _mountAltCosmeticSkinInput = new();
    private readonly ComboBox _mountBreedColorPicker = new();
    private readonly Button _applyMountDetailsButton = new();
    private readonly Button _maxMountLevelButton = new();
    private readonly TextBox _creatureTalentFilterText = new();
    private readonly DataGridView _creatureTalentsGrid = new();
    private readonly DataGridView _creatureGeneticsGrid = new();
    private readonly TextBox _creatureTalentRowNameText = new();
    private readonly NumericUpDown _creatureTalentRankInput = new();
    private bool _loadingMountSelection;
    private bool _loadingAppearanceVariants;
    private bool _updatingCreatureTalentEditor;
    private readonly Button _maxCreatureTalentButton = new();
    private readonly Button _resetSelectedCreatureTalentButton = new();
    private readonly Button _maxAllCreatureTalentsButton = new();
    private readonly Button _resetCreatureTalentsButton = new();
    private readonly Label _mountInfoLabel = new();
    private readonly BindingList<TalentRow> _creatureTalentRows = new();
    private readonly BindingList<CreatureGeneticRow> _creatureGeneticRows = new();

    private IcarusProfile? _profile;
    private IcarusCharacters? _characters;
    private IcarusCharacter? _selectedCharacter;
    private IcarusMounts? _mounts;
    private IcarusMount? _selectedMount;
    private TalentCatalog? _talentCatalog;
    private bool _showSoloTalents;
    private bool _mountsTabActive;
    private bool _updatingTalentEditor;
    private bool _updatingBlueprintEditor;

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
        _playerDataPathText.Text = "Looking for player data...";
        _playerDataPathText.Dock = DockStyle.Fill;
        _playerDataPathText.ReadOnly = true;
        _playerDataPathText.BorderStyle = BorderStyle.FixedSingle;
        _playerDataPathText.ForeColor = SystemColors.GrayText;
        _playerDataPathText.BackColor = SystemColors.Window;
        header.Controls.Add(_playerDataPathText, 1, 0);
        root.Controls.Add(header, 0, 1);

        TabControl tabs = new()
        {
            Dock = DockStyle.Fill
        };
        root.Controls.Add(tabs, 0, 2);

        TabPage resourcesTab = new("Profile Resources");
        resourcesTab.Controls.Add(BuildProfileResourcesTab());
        tabs.TabPages.Add(resourcesTab);

        Control talentEditor = BuildCharacterTalentsTab();
        TabPage talentsTab = new("Character Talents");
        talentsTab.Controls.Add(talentEditor);
        tabs.TabPages.Add(talentsTab);

        TabPage soloTalentsTab = new("Solo Talents");
        tabs.TabPages.Add(soloTalentsTab);

        TabPage blueprintsTab = new("Blueprints");
        blueprintsTab.Controls.Add(BuildBlueprintsTab());
        tabs.TabPages.Add(blueprintsTab);

        TabPage mountsTab = new("Pet Editor");
        mountsTab.Controls.Add(BuildMountsTab());
        tabs.TabPages.Add(mountsTab);

        tabs.SelectedIndexChanged += (_, _) =>
        {
            _mountsTabActive = tabs.SelectedTab == mountsTab;
            if (_mountsTabActive)
            {
                LoadSelectedMountsFile();
                return;
            }

            if (tabs.SelectedTab != talentsTab && tabs.SelectedTab != soloTalentsTab)
            {
                return;
            }

            _showSoloTalents = tabs.SelectedTab == soloTalentsTab;
            TabPage destination = _showSoloTalents ? soloTalentsTab : talentsTab;
            if (talentEditor.Parent != destination)
            {
                destination.Controls.Add(talentEditor);
                talentEditor.Dock = DockStyle.Fill;
            }
            RefreshTalentNavigation();
        };

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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
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

        TableLayoutPanel characterBar = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 6, 0, 0)
        };
        characterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        characterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        characterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        characterBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        characterBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(characterBar, 0, 2);

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
        _talentsGrid.CellEndEdit += (_, args) => CommitGridRank(_talentsGrid, args.RowIndex);
        TableLayoutPanel navigationBar = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(0, 6, 0, 0)
        };
        navigationBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        navigationBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        navigationBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        navigationBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        navigationBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(navigationBar, 0, 3);

        _talentCategoryPicker.Dock = DockStyle.Fill;
        _talentCategoryPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (TalentCategory category in RegularTalentCategories)
        {
            _talentCategoryPicker.Items.Add(category);
        }
        _talentCategoryPicker.SelectedIndexChanged += (_, _) => RefreshTalentTrees();
        navigationBar.Controls.Add(_talentCategoryPicker, 0, 0);

        _talentTreePicker.Dock = DockStyle.Fill;
        _talentTreePicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _talentTreePicker.SelectedIndexChanged += (_, _) => RefreshTalentRows();
        navigationBar.Controls.Add(_talentTreePicker, 1, 0);

        ConfigureButton(_maxAllTalentsButton, "Max Rank All");
        _maxAllTalentsButton.Click += (_, _) => MaxAllTalents();
        navigationBar.Controls.Add(_maxAllTalentsButton, 2, 0);

        ConfigureButton(_resetAllTalentsButton, "Reset All Ranks");
        _resetAllTalentsButton.Click += (_, _) => ResetAllTalents();
        navigationBar.Controls.Add(_resetAllTalentsButton, 3, 0);

        _talentCategoryPicker.SelectedIndex = 0;

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

        _talentRankInput.ValueChanged += (_, _) => UpdateSelectedTalentRankFromInput();
        ConfigureButton(_resetSelectedTalentButton, "Reset Rank");
        _resetSelectedTalentButton.Click += (_, _) => ResetSelectedTalents();
        editor.Controls.Add(_resetSelectedTalentButton, 2, 1);

        ConfigureButton(_maxTalentButton, "Max Rank Selected");
        _maxTalentButton.Click += (_, _) => MaxSelectedTalent();
        editor.Controls.Add(_maxTalentButton, 3, 1);

        return root;
    }

    private Control BuildBlueprintsTab()
    {
        TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

        TableLayoutPanel header = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(header, 0, 0);
        header.Controls.Add(new Label { Text = "Character", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft }, 0, 0);
        header.Controls.Add(new Label { Text = "Filter Blueprints", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft }, 1, 0);

        _blueprintCharacterPicker.Dock = DockStyle.Fill;
        _blueprintCharacterPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _blueprintCharacterPicker.SelectedIndexChanged += (_, _) => SelectBlueprintCharacter();
        header.Controls.Add(_blueprintCharacterPicker, 0, 1);
        _blueprintFilterText.Dock = DockStyle.Fill;
        _blueprintFilterText.TextChanged += (_, _) => RefreshBlueprintRows();
        header.Controls.Add(_blueprintFilterText, 1, 1);
        ConfigureFileButton(_saveBlueprintsButton, "Save", Color.FromArgb(255, 226, 226), _blueprintFilterText);
        _saveBlueprintsButton.Click += (_, _) => SaveCharactersFile();
        header.Controls.Add(_saveBlueprintsButton, 2, 1);

        TableLayoutPanel actions = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 6, 0, 0) };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        root.Controls.Add(actions, 0, 1);
        ConfigureButton(_maxAllBlueprintsButton, "Max Rank All");
        _maxAllBlueprintsButton.Click += (_, _) => MaxAllBlueprints();
        actions.Controls.Add(_maxAllBlueprintsButton, 1, 0);
        ConfigureButton(_resetAllBlueprintsButton, "Reset All Ranks");
        _resetAllBlueprintsButton.Click += (_, _) => ResetAllBlueprints();
        actions.Controls.Add(_resetAllBlueprintsButton, 2, 0);

        _blueprintsGrid.Dock = DockStyle.Fill;
        _blueprintsGrid.AllowUserToAddRows = false;
        _blueprintsGrid.AllowUserToDeleteRows = false;
        _blueprintsGrid.AutoGenerateColumns = false;
        _blueprintsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _blueprintsGrid.MultiSelect = true;
        ConfigureGrid(_blueprintsGrid);
        _blueprintsGrid.DataSource = _blueprintRows;
        _blueprintsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Blueprint", DataPropertyName = nameof(TalentRow.DisplayName), ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _blueprintsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tier", DataPropertyName = nameof(TalentRow.TreeName), ReadOnly = true, Width = 220 });
        _blueprintsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "RowName", DataPropertyName = nameof(TalentRow.RowName), ReadOnly = true, Width = 240 });
        _blueprintsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Rank", DataPropertyName = nameof(TalentRow.Rank), Width = 80 });
        _blueprintsGrid.SelectionChanged += (_, _) => FillBlueprintEditorFromSelection();
        _blueprintsGrid.CellEndEdit += (_, args) => CommitGridRank(_blueprintsGrid, args.RowIndex);
        root.Controls.Add(_blueprintsGrid, 0, 2);

        TableLayoutPanel editor = new() { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(0, 10, 0, 0) };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        editor.Controls.Add(new Label { Text = "Selected Blueprint", Dock = DockStyle.Fill }, 0, 0);
        editor.Controls.Add(new Label { Text = "Rank", Dock = DockStyle.Fill }, 1, 0);
        _blueprintInfoLabel.Dock = DockStyle.Fill;
        _blueprintInfoLabel.BorderStyle = BorderStyle.Fixed3D;
        _blueprintInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
        editor.Controls.Add(_blueprintInfoLabel, 0, 1);
        _blueprintRankInput.Dock = DockStyle.Fill;
        _blueprintRankInput.Maximum = 99;
        _blueprintRankInput.ValueChanged += (_, _) => UpdateSelectedBlueprintRankFromInput();
        editor.Controls.Add(_blueprintRankInput, 1, 1);
        ConfigureButton(_resetSelectedBlueprintButton, "Reset Rank");
        _resetSelectedBlueprintButton.Click += (_, _) => ResetSelectedBlueprints();
        editor.Controls.Add(_resetSelectedBlueprintButton, 2, 1);
        ConfigureButton(_maxSelectedBlueprintButton, "Max Rank Selected");
        _maxSelectedBlueprintButton.Click += (_, _) => MaxSelectedBlueprints();
        editor.Controls.Add(_maxSelectedBlueprintButton, 3, 1);
        root.Controls.Add(editor, 0, 3);
        return root;
    }


    private Control BuildMountsTab()
    {
        TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        TableLayoutPanel fileBar = new() { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
        fileBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fileBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        fileBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        fileBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        fileBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        fileBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(fileBar, 0, 0);
        fileBar.Controls.Add(new Label { Text = "Mounts.json", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft }, 0, 0);
        _mountsFilePicker.Dock = DockStyle.Fill;
        _mountsFilePicker.DropDownStyle = ComboBoxStyle.DropDownList;
        ConfigurePathPicker(_mountsFilePicker);
        _mountsFilePicker.SelectedIndexChanged += (_, _) => LoadSelectedMountsFile();
        fileBar.Controls.Add(_mountsFilePicker, 0, 1);
        ConfigureFileButton(_browseMountsButton, "Browse", Color.FromArgb(232, 248, 236), _mountsFilePicker);
        _browseMountsButton.Click += (_, _) => BrowseForMountsFile();
        fileBar.Controls.Add(_browseMountsButton, 1, 1);
        ConfigureFileButton(_reloadMountsButton, "Reload", Color.FromArgb(255, 246, 204), _mountsFilePicker);
        _reloadMountsButton.Click += (_, _) => LoadSelectedMountsFile();
        fileBar.Controls.Add(_reloadMountsButton, 2, 1);
        ConfigureFileButton(_saveMountsButton, "Save", Color.FromArgb(255, 226, 226), _mountsFilePicker);
        _saveMountsButton.Click += (_, _) => SaveMountsFile();
        fileBar.Controls.Add(_saveMountsButton, 3, 1);

        TableLayoutPanel selectorBar = new() { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2, Padding = new Padding(0, 2, 0, 8) };
        selectorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        selectorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        selectorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        selectorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        selectorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        selectorBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
        selectorBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        selectorBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.Controls.Add(selectorBar, 0, 1);
        selectorBar.Controls.Add(new Label { Text = "Creature", Dock = DockStyle.Fill }, 0, 0);
        selectorBar.Controls.Add(new Label { Text = "Name", Dock = DockStyle.Fill }, 1, 0);
        selectorBar.Controls.Add(new Label { Text = "Species", Dock = DockStyle.Fill }, 2, 0);
        selectorBar.Controls.Add(new Label { Text = "Level", Dock = DockStyle.Fill }, 3, 0);
        _mountPicker.Dock = DockStyle.Fill;
        _mountPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _mountPicker.SelectedIndexChanged += (_, _) => LoadSelectedMount();
        selectorBar.Controls.Add(_mountPicker, 0, 1);
        _mountNameText.Dock = DockStyle.Fill;
        selectorBar.Controls.Add(_mountNameText, 1, 1);
        _mountSpeciesText.Dock = DockStyle.Fill;
        _mountSpeciesText.ReadOnly = true;
        selectorBar.Controls.Add(_mountSpeciesText, 2, 1);
        _mountLevelInput.Dock = DockStyle.Fill;
        _mountLevelInput.Maximum = 50;
        selectorBar.Controls.Add(_mountLevelInput, 3, 1);
        _mountLevelInput.ValueChanged += (_, _) => UpdateMountLevelFromInput();
        ConfigureButton(_maxMountLevelButton, "Max Level");
        _maxMountLevelButton.Click += (_, _) => MaxMountLevel();
        selectorBar.Controls.Add(_maxMountLevelButton, 4, 1);
        ConfigureButton(_injectMountButton, "Inject Creature");
        _injectMountButton.Click += (_, _) => InjectMount();
        selectorBar.Controls.Add(_injectMountButton, 5, 1);

        TabControl petTabs = new() { Dock = DockStyle.Fill };
        root.Controls.Add(petTabs, 0, 2);

        TabPage statsTab = new("Stats");
        statsTab.Controls.Add(BuildCreatureStatsPanel());
        petTabs.TabPages.Add(statsTab);

        TabPage appearanceTab = new("Appearance");
        appearanceTab.Controls.Add(BuildCreatureAppearancePanel());
        petTabs.TabPages.Add(appearanceTab);

        TabPage talentsTab = new("Talents");
        talentsTab.Controls.Add(BuildCreatureTalentsPanel());
        petTabs.TabPages.Add(talentsTab);

        TabPage geneticsTab = new("Genetics");
        geneticsTab.Controls.Add(BuildCreatureGeneticsPanel());
        petTabs.TabPages.Add(geneticsTab);

        _mountInfoLabel.Dock = DockStyle.Bottom;
        _mountInfoLabel.AutoEllipsis = true;
        root.Controls.Add(_mountInfoLabel, 0, 3);
        return root;
    }

    private Control BuildCreatureStatsPanel()
    {
        TableLayoutPanel stats = new() { Dock = DockStyle.Top, ColumnCount = 5, RowCount = 4, Padding = new Padding(0, 10, 0, 0), AutoSize = true };
        for (int i = 0; i < 5; i++)
        {
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        }
        stats.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        stats.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stats.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        stats.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stats.Controls.Add(new Label { Text = "Health", Dock = DockStyle.Fill }, 0, 0);
        stats.Controls.Add(new Label { Text = "Stamina", Dock = DockStyle.Fill }, 1, 0);
        stats.Controls.Add(new Label { Text = "Food", Dock = DockStyle.Fill }, 2, 0);
        stats.Controls.Add(new Label { Text = "Water", Dock = DockStyle.Fill }, 3, 0);
        stats.Controls.Add(new Label { Text = "Oxygen", Dock = DockStyle.Fill }, 4, 0);
        _mountHealthInput.Dock = DockStyle.Fill;
        _mountHealthInput.Maximum = 1000000;
        _mountHealthInput.ThousandsSeparator = true;
        stats.Controls.Add(_mountHealthInput, 0, 1);
        _mountStaminaInput.Dock = DockStyle.Fill;
        _mountStaminaInput.Maximum = 1000000;
        _mountStaminaInput.ThousandsSeparator = true;
        stats.Controls.Add(_mountStaminaInput, 1, 1);
        ConfigureCreatureNumber(_mountFoodInput);
        stats.Controls.Add(_mountFoodInput, 2, 1);
        ConfigureCreatureNumber(_mountWaterInput);
        stats.Controls.Add(_mountWaterInput, 3, 1);
        ConfigureCreatureNumber(_mountOxygenInput);
        stats.Controls.Add(_mountOxygenInput, 4, 1);
        ConfigureButton(_applyMountDetailsButton, "Apply Details");
        _applyMountDetailsButton.Click += (_, _) => ApplyMountDetails();
        stats.Controls.Add(_applyMountDetailsButton, 3, 3);
        stats.SetColumnSpan(_applyMountDetailsButton, 2);
        return stats;
    }

    private Control BuildCreatureAppearancePanel()
    {
        TableLayoutPanel appearance = new() { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 6, Padding = new Padding(0, 10, 0, 0), AutoSize = true };
        for (int i = 0; i < 4; i++)
        {
            appearance.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }
        appearance.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        appearance.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        appearance.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        appearance.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        appearance.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        appearance.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        appearance.Controls.Add(new Label { Text = "Breed / Color", Dock = DockStyle.Fill }, 0, 0);
        _mountBreedColorPicker.Dock = DockStyle.Fill;
        _mountBreedColorPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _mountBreedColorPicker.SelectedIndexChanged += (_, _) => ApplySelectedAppearanceVariant();
        appearance.Controls.Add(_mountBreedColorPicker, 0, 1);
        appearance.SetColumnSpan(_mountBreedColorPicker, 4);

        appearance.Controls.Add(new Label { Text = "Variation", Dock = DockStyle.Fill }, 0, 2);
        appearance.Controls.Add(new Label { Text = "Unique Variation", Dock = DockStyle.Fill }, 1, 2);
        appearance.Controls.Add(new Label { Text = "Cosmetic Skin", Dock = DockStyle.Fill }, 2, 2);
        appearance.Controls.Add(new Label { Text = "Alt Cosmetic Skin", Dock = DockStyle.Fill }, 3, 2);
        ConfigureCreatureNumber(_mountVariationInput);
        appearance.Controls.Add(_mountVariationInput, 0, 3);
        ConfigureCreatureNumber(_mountUniqueVariationInput);
        appearance.Controls.Add(_mountUniqueVariationInput, 1, 3);
        ConfigureCreatureNumber(_mountCosmeticSkinInput);
        _mountCosmeticSkinInput.Minimum = -1;
        appearance.Controls.Add(_mountCosmeticSkinInput, 2, 3);
        ConfigureCreatureNumber(_mountAltCosmeticSkinInput);
        _mountAltCosmeticSkinInput.Minimum = -1;
        appearance.Controls.Add(_mountAltCosmeticSkinInput, 3, 3);

        Label note = new()
        {
            Text = "Breed/color applies known workshop pet mappings for the same species. Raw variation fields remain visible for unmapped tames.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        appearance.Controls.Add(note, 0, 5);
        appearance.SetColumnSpan(note, 4);
        return appearance;
    }

    private Control BuildCreatureTalentsPanel()
    {
        TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0, 10, 0, 0) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));

        TableLayoutPanel filterBar = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
        filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
        filterBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        filterBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.Controls.Add(filterBar, 0, 0);
        filterBar.Controls.Add(new Label { Text = "Talent Filter", Dock = DockStyle.Fill }, 0, 0);
        _creatureTalentFilterText.Dock = DockStyle.Fill;
        _creatureTalentFilterText.TextChanged += (_, _) => RefreshCreatureTalentRows();
        filterBar.Controls.Add(_creatureTalentFilterText, 0, 1);
        ConfigureButton(_maxAllCreatureTalentsButton, "Max Rank All");
        _maxAllCreatureTalentsButton.Click += (_, _) => MaxAllCreatureTalents();
        filterBar.Controls.Add(_maxAllCreatureTalentsButton, 1, 1);
        ConfigureButton(_resetCreatureTalentsButton, "Reset All Ranks");
        _resetCreatureTalentsButton.Click += (_, _) => ResetCreatureTalents();
        filterBar.Controls.Add(_resetCreatureTalentsButton, 2, 1);

        _creatureTalentsGrid.Dock = DockStyle.Fill;
        _creatureTalentsGrid.AllowUserToAddRows = false;
        _creatureTalentsGrid.AllowUserToDeleteRows = false;
        _creatureTalentsGrid.AutoGenerateColumns = false;
        _creatureTalentsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _creatureTalentsGrid.MultiSelect = true;
        ConfigureGrid(_creatureTalentsGrid);
        _creatureTalentsGrid.DataSource = _creatureTalentRows;
        _creatureTalentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Talent", DataPropertyName = nameof(TalentRow.DisplayName), ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _creatureTalentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "RowName", DataPropertyName = nameof(TalentRow.RowName), ReadOnly = true, Width = 250 });
        _creatureTalentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Max", DataPropertyName = nameof(TalentRow.MaxRankText), ReadOnly = true, Width = 70 });
        _creatureTalentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Rank", DataPropertyName = nameof(TalentRow.Rank), Width = 80 });
        _creatureTalentsGrid.SelectionChanged += (_, _) => FillCreatureTalentEditorFromSelection();
        _creatureTalentsGrid.CellEndEdit += (_, args) => CommitCreatureGridRank(args.RowIndex);
        root.Controls.Add(_creatureTalentsGrid, 0, 1);

        TableLayoutPanel editor = new() { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(0, 10, 0, 0) };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.Controls.Add(editor, 0, 3);
        editor.Controls.Add(new Label { Text = "Selected Talent", Dock = DockStyle.Fill }, 0, 0);
        editor.Controls.Add(new Label { Text = "Rank", Dock = DockStyle.Fill }, 1, 0);
        _creatureTalentRowNameText.Dock = DockStyle.Fill;
        _creatureTalentRowNameText.ReadOnly = true;
        editor.Controls.Add(_creatureTalentRowNameText, 0, 1);
        _creatureTalentRankInput.Dock = DockStyle.Fill;
        _creatureTalentRankInput.Maximum = 99;
        _creatureTalentRankInput.ValueChanged += (_, _) => UpdateSelectedCreatureTalentRankFromInput();
        editor.Controls.Add(_creatureTalentRankInput, 1, 1);
        ConfigureButton(_resetSelectedCreatureTalentButton, "Reset Rank");
        _resetSelectedCreatureTalentButton.Click += (_, _) => ResetSelectedCreatureTalents();
        editor.Controls.Add(_resetSelectedCreatureTalentButton, 2, 1);
        ConfigureButton(_maxCreatureTalentButton, "Max Rank Selected");
        _maxCreatureTalentButton.Click += (_, _) => MaxSelectedCreatureTalent();
        editor.Controls.Add(_maxCreatureTalentButton, 3, 1);
        root.Controls.Add(editor, 0, 2);
        return root;
    }

    private Control BuildCreatureGeneticsPanel()
    {
        TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0, 10, 0, 0) };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        _creatureGeneticsGrid.Dock = DockStyle.Fill;
        _creatureGeneticsGrid.AllowUserToAddRows = false;
        _creatureGeneticsGrid.AllowUserToDeleteRows = false;
        _creatureGeneticsGrid.AutoGenerateColumns = false;
        _creatureGeneticsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _creatureGeneticsGrid.MultiSelect = false;
        ConfigureGrid(_creatureGeneticsGrid);
        _creatureGeneticsGrid.DataSource = _creatureGeneticRows;
        _creatureGeneticsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Genetic", DataPropertyName = nameof(CreatureGeneticRow.Name), ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _creatureGeneticsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", DataPropertyName = nameof(CreatureGeneticRow.Value), Width = 110 });
        _creatureGeneticsGrid.CellEndEdit += (_, args) => CommitCreatureGeneticGridLevel(args.RowIndex);
        root.Controls.Add(_creatureGeneticsGrid, 0, 0);

        Label note = new()
        {
            Text = "Genetics use the saved Value field and are clamped from 0 to 10. Expected lineages are Vitality, Endurance, Muscle, Agility, Toughness, Hardiness, and Utility.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(note, 0, 1);
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

    private static void ConfigureCreatureNumber(NumericUpDown input)
    {
        input.Dock = DockStyle.Fill;
        input.Maximum = 1000000;
        input.ThousandsSeparator = true;
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
        _mountsFilePicker.Items.Clear();

        IReadOnlyList<string> profiles = ProfileFinder.FindProfiles();
        IReadOnlyList<string> charactersFiles = ProfileFinder.FindCharactersFiles();
        IReadOnlyList<string> mountsFiles = ProfileFinder.FindMountsFiles();

        foreach (string profilePath in profiles)
        {
            _profilePicker.Items.Add(profilePath);
        }

        foreach (string charactersPath in charactersFiles)
        {
            _charactersFilePicker.Items.Add(charactersPath);
        }

        foreach (string mountsPath in mountsFiles)
        {
            _mountsFilePicker.Items.Add(mountsPath);
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

        if (_mountsFilePicker.Items.Count > 0)
        {
            _mountsFilePicker.SelectedIndex = 0;
        }

        string? discoveredFile = profiles.FirstOrDefault() ?? charactersFiles.FirstOrDefault() ?? mountsFiles.FirstOrDefault();
        _playerDataPathText.Text = discoveredFile is null
            ? "Player data folder not found - use Browse in either editor"
            : Path.GetDirectoryName(discoveredFile) ?? "";
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


    private void InjectMount()
    {
        if (_mounts is null)
        {
            SetStatus("Load Mounts.json first.");
            return;
        }

        using InjectMountDialog dialog = new(IcarusMounts.SupportedInjectionTypes);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedDefinition is not MountInjectionDefinition definition)
        {
            return;
        }

        try
        {
            IcarusMount injected = _mounts.InjectMount(definition, dialog.MountName, _selectedMount);
            RefreshMountPicker();
            _mountPicker.SelectedItem = injected;
            SetStatus($"Injected {injected.Name} ({injected.MountType}). Save to write Mounts.json.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not inject mount", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Mount injection failed.");
        }
    }
    private void BrowseForMountsFile()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select Icarus Mounts.json",
            Filter = "Icarus mounts (Mounts.json)|Mounts.json|JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        AddComboBoxItem(_mountsFilePicker, dialog.FileName);
        _mountsFilePicker.SelectedItem = dialog.FileName;
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

            string mountsPath = ProfileFinder.GetMountsPathForProfile(path);
            AddComboBoxItem(_mountsFilePicker, mountsPath);
            _mountsFilePicker.SelectedItem = mountsPath;
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


    private void LoadSelectedMountsFile()
    {
        if (_mountsFilePicker.SelectedItem is not string path || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                _mounts = IcarusMounts.Load(path);
                SetStatus($"Loaded {Path.GetFileName(path)}.");
            }
            else
            {
                if (!_mountsTabActive)
                {
                    _mounts = null;
                    RefreshMountPicker();
                    return;
                }

                DialogResult result = MessageBox.Show(this,
                    $"{Path.GetFileName(path)} does not exist for this player data folder. Create a new Mounts.json when you save?",
                    "Create Mounts.json?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    _mounts = null;
                    RefreshMountPicker();
                    SetStatus("Mounts.json was not created. Use Browse to select an existing file.");
                    return;
                }

                _mounts = IcarusMounts.CreateEmpty(path);
                SetStatus("Prepared a new Mounts.json. Inject a mount, then Save to create the file.");
            }
            UpdatePlayerDataHeader(path);
            RefreshMountPicker();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not load mounts", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Mounts load failed.");
        }
    }

    private void RefreshMountPicker()
    {
        _mountPicker.Items.Clear();
        _selectedMount = null;
        _mountNameText.Text = "";
        _mountSpeciesText.Text = "";
        _mountLevelInput.Value = 0;
        _mountHealthInput.Value = 0;
        _mountStaminaInput.Value = 0;
        _mountFoodInput.Value = 0;
        _mountWaterInput.Value = 0;
        _mountOxygenInput.Value = 0;
        _mountVariationInput.Value = 0;
        _mountUniqueVariationInput.Value = 0;
        _mountCosmeticSkinInput.Value = 0;
        _mountAltCosmeticSkinInput.Value = 0;
        _mountBreedColorPicker.Items.Clear();
        _mountBreedColorPicker.Enabled = false;
        _creatureTalentRows.Clear();
        _creatureGeneticRows.Clear();
        _mountInfoLabel.Text = "";

        if (_mounts is null)
        {
            return;
        }

        foreach (IcarusMount mount in _mounts.Mounts)
        {
            _mountPicker.Items.Add(mount);
        }

        if (_mountPicker.Items.Count > 0)
        {
            _mountPicker.SelectedIndex = 0;
        }
        else
        {
            _mountInfoLabel.Text = "No station mounts";
        }
    }

    private void LoadSelectedMount()
    {
        _selectedMount = _mountPicker.SelectedItem as IcarusMount;
        _loadingMountSelection = true;
        try
        {
            if (_selectedMount is not null)
            {
                _mountNameText.Text = _selectedMount.Name;
                _mountSpeciesText.Text = _selectedMount.MountType;
                _mountLevelInput.Maximum = _selectedMount.MaxLevel;
                _mountLevelInput.Value = Math.Clamp(_selectedMount.Level, 0, _selectedMount.MaxLevel);
                _mountHealthInput.Value = Math.Clamp(_selectedMount.CurrentHealth ?? 0, 0, decimal.ToInt32(_mountHealthInput.Maximum));
                _mountStaminaInput.Value = Math.Clamp(_selectedMount.Stamina ?? 0, 0, decimal.ToInt32(_mountStaminaInput.Maximum));
                _mountFoodInput.Value = Math.Clamp(_selectedMount.FoodLevel ?? 0, 0, decimal.ToInt32(_mountFoodInput.Maximum));
                _mountWaterInput.Value = Math.Clamp(_selectedMount.WaterLevel ?? 0, 0, decimal.ToInt32(_mountWaterInput.Maximum));
                _mountOxygenInput.Value = Math.Clamp(_selectedMount.OxygenLevel ?? 0, 0, decimal.ToInt32(_mountOxygenInput.Maximum));
                _mountVariationInput.Value = Math.Clamp(_selectedMount.Variation ?? 0, 0, decimal.ToInt32(_mountVariationInput.Maximum));
                _mountUniqueVariationInput.Value = Math.Clamp(_selectedMount.UniqueVariation ?? 0, 0, decimal.ToInt32(_mountUniqueVariationInput.Maximum));
                _mountCosmeticSkinInput.Value = Math.Clamp(_selectedMount.CosmeticSkinIndex ?? 0, decimal.ToInt32(_mountCosmeticSkinInput.Minimum), decimal.ToInt32(_mountCosmeticSkinInput.Maximum));
                _mountAltCosmeticSkinInput.Value = Math.Clamp(_selectedMount.AlternateCosmeticSkinIndex ?? -1, decimal.ToInt32(_mountAltCosmeticSkinInput.Minimum), decimal.ToInt32(_mountAltCosmeticSkinInput.Maximum));
                RefreshAppearanceVariantPicker();
            }
        }
        finally
        {
            _loadingMountSelection = false;
        }

        RefreshCreatureTalentRows();
        RefreshCreatureGeneticRows();
    }

    private void RefreshAppearanceVariantPicker()
    {
        _loadingAppearanceVariants = true;
        try
        {
            _mountBreedColorPicker.Items.Clear();
            if (_selectedMount is null)
            {
                _mountBreedColorPicker.Enabled = false;
                return;
            }

            IReadOnlyList<CreatureAppearanceVariant> variants = _selectedMount.AppearanceVariants;
            _mountBreedColorPicker.Enabled = variants.Count > 0;
            foreach (CreatureAppearanceVariant variant in variants)
            {
                _mountBreedColorPicker.Items.Add(variant);
            }

            CreatureAppearanceVariant? current = _selectedMount.GetAppearanceVariant();
            if (current is not null)
            {
                for (int i = 0; i < _mountBreedColorPicker.Items.Count; i++)
                {
                    if (_mountBreedColorPicker.Items[i] is CreatureAppearanceVariant item
                        && string.Equals(item.DisplayName, current.DisplayName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(item.AiSetupRowName, current.AiSetupRowName, StringComparison.OrdinalIgnoreCase))
                    {
                        _mountBreedColorPicker.SelectedIndex = i;
                        return;
                    }
                }
            }

            _mountBreedColorPicker.SelectedIndex = -1;
        }
        finally
        {
            _loadingAppearanceVariants = false;
        }
    }

    private void RefreshCharacterPicker()
    {
        _characterPicker.Items.Clear();
        _blueprintCharacterPicker.Items.Clear();
        _selectedCharacter = null;
        _talentRows.Clear();
        _blueprintRows.Clear();

        if (_characters is null)
        {
            _characterInfoLabel.Text = "";
            return;
        }

        foreach (IcarusCharacter character in _characters.Characters)
        {
            _characterPicker.Items.Add(character);
            _blueprintCharacterPicker.Items.Add(character);
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
        SetSelectedCharacter(_characterPicker.SelectedItem as IcarusCharacter);
    }

    private void SelectBlueprintCharacter()
    {
        SetSelectedCharacter(_blueprintCharacterPicker.SelectedItem as IcarusCharacter);
    }

    private void SetSelectedCharacter(IcarusCharacter? character)
    {
        _selectedCharacter = character;
        if (!ReferenceEquals(_characterPicker.SelectedItem, character))
        {
            _characterPicker.SelectedItem = character;
        }
        if (!ReferenceEquals(_blueprintCharacterPicker.SelectedItem, character))
        {
            _blueprintCharacterPicker.SelectedItem = character;
        }
        RefreshTalentRows();
        RefreshBlueprintRows();
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

        Dictionary<string, int> savedRanks = GetSavedTalentRanks();
        string filter = _talentFilterText.Text.Trim();
        if (_talentCatalog is null)
        {
            foreach (TalentEntry talent in _selectedCharacter.Talents.Where(talent =>
                filter.Length == 0 || talent.RowName.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            {
                _talentRows.Add(new TalentRow(talent.RowName, talent.RowName, "", null, talent.Rank));
            }
            _characterInfoLabel.Text = $"XP {_selectedCharacter.Xp:N0} | {_talentRows.Count} saved rows (catalog unavailable)";
            return;
        }

        IEnumerable<TalentMetadata> visibleTalents = GetActiveTalentMetadata();
        foreach (TalentMetadata metadata in visibleTalents)
        {
            if (filter.Length > 0
                && !metadata.RowName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !metadata.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !metadata.TreeName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _talentRows.Add(new TalentRow(
                metadata.RowName,
                metadata.DisplayName,
                metadata.TreeName,
                metadata.MaxRank,
                savedRanks.TryGetValue(metadata.RowName, out int rank) ? rank : 0));
        }

        string scope = _showSoloTalents ? "Solo" : (_talentCategoryPicker.SelectedItem as TalentCategory)?.DisplayName ?? "Talents";
        _characterInfoLabel.Text = $"XP {_selectedCharacter.Xp:N0} | {_talentRows.Count} {scope}";
    }

    private void RefreshTalentNavigation()
    {
        _talentCategoryPicker.Enabled = !_showSoloTalents;
        RefreshTalentTrees();
    }

    private void RefreshTalentTrees()
    {
        _talentTreePicker.Items.Clear();
        _talentTreePicker.Items.Add(new TalentTreeChoice("All Trees", null));

        if (_talentCatalog is not null)
        {
            string? archetype = GetActiveTalentArchetype();
            foreach (TalentTreeChoice tree in _talentCatalog.CharacterTalents
                .Where(talent => archetype is null
                    ? !string.Equals(talent.TreeArchetype, "Solo", StringComparison.OrdinalIgnoreCase)
                    : string.Equals(talent.TreeArchetype, archetype, StringComparison.OrdinalIgnoreCase))
                .GroupBy(talent => talent.TreeRowName, StringComparer.OrdinalIgnoreCase)
                .Select(group => new TalentTreeChoice(group.First().TreeName, group.Key))
                .OrderBy(tree => tree.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                _talentTreePicker.Items.Add(tree);
            }
        }

        _talentTreePicker.SelectedIndex = 0;
    }

    private string? GetActiveTalentArchetype()
    {
        return _showSoloTalents
            ? "Solo"
            : (_talentCategoryPicker.SelectedItem as TalentCategory)?.Archetype;
    }

    private IEnumerable<TalentMetadata> GetActiveTalentMetadata()
    {
        if (_talentCatalog is null)
        {
            return Array.Empty<TalentMetadata>();
        }

        string? archetype = GetActiveTalentArchetype();
        string? treeRowName = (_talentTreePicker.SelectedItem as TalentTreeChoice)?.RowName;
        return _talentCatalog.CharacterTalents.Where(talent =>
            (archetype is null
                ? !string.Equals(talent.TreeArchetype, "Solo", StringComparison.OrdinalIgnoreCase)
                : string.Equals(talent.TreeArchetype, archetype, StringComparison.OrdinalIgnoreCase))
            && (treeRowName is null || string.Equals(talent.TreeRowName, treeRowName, StringComparison.OrdinalIgnoreCase)));
    }

    private Dictionary<string, int> GetSavedTalentRanks()
    {
        return _selectedCharacter?.Talents
            .GroupBy(talent => talent.RowName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Rank, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshBlueprintRows()
    {
        _blueprintRows.Clear();
        if (_selectedCharacter is null || _talentCatalog is null)
        {
            return;
        }

        Dictionary<string, int> savedRanks = GetSavedTalentRanks();
        string filter = _blueprintFilterText.Text.Trim();
        foreach (TalentMetadata metadata in _talentCatalog.Blueprints)
        {
            if (filter.Length > 0
                && !metadata.RowName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !metadata.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !metadata.TreeName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _blueprintRows.Add(new TalentRow(
                metadata.RowName,
                metadata.DisplayName,
                metadata.TreeName,
                metadata.MaxRank,
                savedRanks.TryGetValue(metadata.RowName, out int rank) ? rank : 0));
        }
    }


    private void RefreshCreatureTalentRows()
    {
        _creatureTalentRows.Clear();
        if (_selectedMount is null)
        {
            UpdateMountInfoLabel();
            return;
        }

        if (_talentCatalog is null)
        {
            foreach (TalentEntry talent in _selectedMount.Talents)
            {
                _creatureTalentRows.Add(new TalentRow(talent.RowName, talent.RowName, "", null, talent.Rank));
            }
            UpdateMountInfoLabel();
            return;
        }

        Dictionary<string, int> savedRanks = _selectedMount.Talents
            .GroupBy(talent => talent.RowName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Rank, StringComparer.OrdinalIgnoreCase);
        string filter = _creatureTalentFilterText.Text.Trim();
        string treeRowName = _selectedMount.CreatureTreeRowName;
        foreach (TalentMetadata metadata in _talentCatalog.CreatureTalents.Where(talent =>
            string.Equals(talent.TreeRowName, treeRowName, StringComparison.OrdinalIgnoreCase)))
        {
            if (filter.Length > 0
                && !metadata.RowName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !metadata.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _creatureTalentRows.Add(new TalentRow(
                metadata.RowName,
                metadata.DisplayName,
                metadata.TreeName,
                metadata.MaxRank,
                savedRanks.TryGetValue(metadata.RowName, out int rank) ? rank : 0));
        }

        UpdateMountInfoLabel();
    }

    private void UpdateMountInfoLabel()
    {
        if (_selectedMount is null)
        {
            _mountInfoLabel.Text = "";
            return;
        }

        string treeRowName = _selectedMount.CreatureTreeRowName;
        string appearance = string.IsNullOrWhiteSpace(_selectedMount.AppearanceLabel)
            ? "appearance unmapped"
            : _selectedMount.AppearanceLabel;
        string catalog = _talentCatalog is null
            ? "catalog unavailable"
            : $"{_creatureTalentRows.Count} {treeRowName} talents";
        _mountInfoLabel.Text = $"{_selectedMount.Name} | {_selectedMount.MountType} | {appearance} | {_selectedMount.AiSetupRowName} | XP {_selectedMount.Experience:N0} | HP {_selectedMount.CurrentHealth?.ToString() ?? "?"} | {catalog}";
    }

    private void RefreshCreatureGeneticRows()
    {
        _creatureGeneticRows.Clear();
        if (_selectedMount is null)
        {
            return;
        }

        foreach (CreatureGeneticEntry genetic in _selectedMount.Genetics)
        {
            _creatureGeneticRows.Add(new CreatureGeneticRow(genetic.Name, genetic.Value));
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

    private void FillTalentEditorFromSelection()
    {
        List<TalentRow> selectedRows = GetSelectedTalentRows();
        _updatingTalentEditor = true;
        try
        {
            if (selectedRows.Count == 0)
            {
                _talentRowNameText.Text = "";
                _talentRankInput.Maximum = 99;
                return;
            }

            int maxRank = GetRankInputMaximum(selectedRows);
            ConfigureRankInput(_talentRankInput, maxRank, decimal.ToInt32(_talentRankInput.Value));

            if (selectedRows.Count > 1)
            {
                _talentRowNameText.Text = $"{selectedRows.Count} talents selected";
                return;
            }

            TalentRow row = selectedRows[0];
            _talentRowNameText.Text = row.RowName;
            ConfigureRankInput(_talentRankInput, maxRank, row.Rank);
        }
        finally
        {
            _updatingTalentEditor = false;
        }
    }
    private void FillBlueprintEditorFromSelection()
    {
        List<TalentRow> selectedRows = GetSelectedBlueprintRows();
        _updatingBlueprintEditor = true;
        try
        {
            if (selectedRows.Count == 0)
            {
                _blueprintInfoLabel.Text = "";
                _blueprintRankInput.Maximum = 99;
                return;
            }

            int maxRank = GetRankInputMaximum(selectedRows);
            ConfigureRankInput(_blueprintRankInput, maxRank, decimal.ToInt32(_blueprintRankInput.Value));

            if (selectedRows.Count > 1)
            {
                _blueprintInfoLabel.Text = $"{selectedRows.Count} blueprints selected";
                return;
            }

            TalentRow row = selectedRows[0];
            _blueprintInfoLabel.Text = row.RowName;
            ConfigureRankInput(_blueprintRankInput, maxRank, row.Rank);
        }
        finally
        {
            _updatingBlueprintEditor = false;
        }
    }
    private static void ConfigureRankInput(NumericUpDown input, int maxRank, int value)
    {
        maxRank = Math.Clamp(maxRank, 0, 99);
        if (input.Maximum < maxRank)
        {
            input.Maximum = maxRank;
        }

        input.Value = Math.Clamp(value, 0, maxRank);
        input.Maximum = maxRank;
    }
    private static int GetRankInputMaximum(IEnumerable<TalentRow> rows)
    {
        int? maxRank = rows
            .Where(row => row.MaxRank.HasValue)
            .Select(row => row.MaxRank!.Value)
            .DefaultIfEmpty(99)
            .Min();
        return Math.Clamp(maxRank ?? 99, 0, 99);
    }
    private void CommitGridRank(DataGridView grid, int rowIndex)
    {
        if (_selectedCharacter is null || rowIndex < 0 || rowIndex >= grid.Rows.Count)
        {
            return;
        }

        if (grid.Rows[rowIndex].DataBoundItem is TalentRow row)
        {
            _selectedCharacter.SetTalent(row.RowName, ClampTalentRank(row.RowName, row.Rank));
        }
    }

    private List<TalentRow> GetSelectedBlueprintRows()
    {
        List<TalentRow> rows = _blueprintsGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<TalentRow>()
            .GroupBy(row => row.RowName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (rows.Count == 0 && _blueprintsGrid.CurrentRow?.DataBoundItem is TalentRow current)
        {
            rows.Add(current);
        }
        return rows;
    }

    private void UpdateSelectedBlueprintRankFromInput()
    {
        if (_updatingBlueprintEditor || _selectedCharacter is null)
        {
            return;
        }

        List<TalentRow> rows = GetSelectedBlueprintRows();
        if (rows.Count == 0)
        {
            return;
        }

        int rank = decimal.ToInt32(_blueprintRankInput.Value);
        foreach (TalentRow row in rows)
        {
            row.Rank = ClampTalentRank(row.RowName, rank);
            _selectedCharacter.SetTalent(row.RowName, row.Rank);
        }
        _blueprintsGrid.Refresh();
        SetStatus($"Set rank {rank} on {rows.Count:N0} blueprint(s). Save to write Characters.json.");
    }

    private void ResetSelectedBlueprints()
    {
        if (_selectedCharacter is null)
        {
            SetStatus("Load a character first.");
            return;
        }

        List<TalentRow> rows = GetSelectedBlueprintRows();
        if (rows.Count == 0)
        {
            SetStatus("Select one or more blueprints first.");
            return;
        }

        _updatingBlueprintEditor = true;
        try
        {
            _blueprintRankInput.Value = 0;
        }
        finally
        {
            _updatingBlueprintEditor = false;
        }

        foreach (TalentRow row in rows)
        {
            row.Rank = 0;
            _selectedCharacter.SetTalent(row.RowName, 0);
        }
        _blueprintsGrid.Refresh();
        SetStatus($"Reset {rows.Count:N0} selected blueprint rank(s). Save to write Characters.json.");
    }

    private void MaxSelectedBlueprints()
    {
        if (_selectedCharacter is null || _talentCatalog is null)
        {
            SetStatus("Load a character and talent catalog first.");
            return;
        }

        List<TalentRow> rows = GetSelectedBlueprintRows();
        if (rows.Count == 0)
        {
            SetStatus("Select one or more blueprints first.");
            return;
        }

        int changed = 0;
        foreach (TalentRow row in rows)
        {
            TalentMetadata? metadata = _talentCatalog.Find(row.RowName);
            if (metadata is null)
            {
                continue;
            }

            row.Rank = metadata.MaxRank;
            _selectedCharacter.SetTalent(row.RowName, metadata.MaxRank);
            changed++;
        }
        _blueprintsGrid.Refresh();
        SetStatus($"Maxed {changed:N0} selected blueprint(s). Save to write Characters.json.");
    }
    private void MaxAllBlueprints()
    {
        if (_selectedCharacter is null || _talentCatalog is null)
        {
            SetStatus("Load a character and talent catalog first.");
            return;
        }

        int changed = 0;
        foreach (TalentMetadata metadata in _talentCatalog.Blueprints.Where(blueprint => blueprint.MaxRank > 0))
        {
            _selectedCharacter.SetTalent(metadata.RowName, metadata.MaxRank);
            changed++;
        }
        RefreshBlueprintRows();
        SetStatus($"Maxed {changed:N0} blueprints. Save to write Characters.json.");
    }

    private void ResetAllBlueprints()
    {
        if (_selectedCharacter is null || _talentCatalog is null)
        {
            SetStatus("Load a character and talent catalog first.");
            return;
        }

        int changed = 0;
        foreach (TalentMetadata metadata in _talentCatalog.Blueprints)
        {
            _selectedCharacter.SetTalent(metadata.RowName, 0);
            changed++;
        }
        RefreshBlueprintRows();
        SetStatus($"Reset {changed:N0} blueprint rank(s) to 0. Save to write Characters.json.");
    }


    private void FillCreatureTalentEditorFromSelection()
    {
        List<TalentRow> selectedRows = GetSelectedCreatureTalentRows();
        _updatingCreatureTalentEditor = true;
        try
        {
            if (selectedRows.Count == 0)
            {
                _creatureTalentRowNameText.Text = "";
                _creatureTalentRankInput.Maximum = 99;
                return;
            }

            int maxRank = GetRankInputMaximum(selectedRows);
            ConfigureRankInput(_creatureTalentRankInput, maxRank, decimal.ToInt32(_creatureTalentRankInput.Value));

            if (selectedRows.Count > 1)
            {
                _creatureTalentRowNameText.Text = $"{selectedRows.Count} talents selected";
                return;
            }

            TalentRow row = selectedRows[0];
            _creatureTalentRowNameText.Text = row.RowName;
            ConfigureRankInput(_creatureTalentRankInput, maxRank, row.Rank);
        }
        finally
        {
            _updatingCreatureTalentEditor = false;
        }
    }
    private void CommitCreatureGridRank(int rowIndex)
    {
        if (_selectedMount is null || rowIndex < 0 || rowIndex >= _creatureTalentsGrid.Rows.Count)
        {
            return;
        }

        if (_creatureTalentsGrid.Rows[rowIndex].DataBoundItem is TalentRow row)
        {
            _selectedMount.SetTalent(row.RowName, ClampCreatureTalentRank(row.RowName, row.Rank));
        }
    }

    private void CommitCreatureGeneticGridLevel(int rowIndex)
    {
        if (_selectedMount is null || rowIndex < 0 || rowIndex >= _creatureGeneticsGrid.Rows.Count)
        {
            return;
        }

        if (_creatureGeneticsGrid.Rows[rowIndex].DataBoundItem is CreatureGeneticRow row)
        {
            row.Value = Math.Clamp(row.Value, 0, CreatureGeneticMaxValue);
            _selectedMount.SetGeneticLevel(row.Name, row.Value);
            _creatureGeneticsGrid.Refresh();
            SetStatus($"Set {row.Name} genetic value to {row.Value}. Save to write Mounts.json.");
        }
    }

    private List<TalentRow> GetSelectedCreatureTalentRows()
    {
        List<TalentRow> rows = _creatureTalentsGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<TalentRow>()
            .GroupBy(row => row.RowName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (rows.Count == 0 && _creatureTalentsGrid.CurrentRow?.DataBoundItem is TalentRow current)
        {
            rows.Add(current);
        }
        return rows;
    }

    private void UpdateMountLevelFromInput()
    {
        if (_loadingMountSelection || _selectedMount is null)
        {
            return;
        }

        int level = Math.Clamp(decimal.ToInt32(_mountLevelInput.Value), 0, _selectedMount.MaxLevel);
        _selectedMount.Level = level;
        RefreshCreatureTalentRows();
        SetStatus($"Set {_selectedMount.Name} to level {level}. Save to write Mounts.json.");
    }

    private void ApplySelectedAppearanceVariant()
    {
        if (_loadingMountSelection || _loadingAppearanceVariants || _selectedMount is null)
        {
            return;
        }

        if (_mountBreedColorPicker.SelectedItem is not CreatureAppearanceVariant variant)
        {
            return;
        }

        _selectedMount.SetAppearanceVariant(variant);
        _mountSpeciesText.Text = _selectedMount.MountType;
        _mountVariationInput.Value = Math.Clamp(_selectedMount.Variation ?? 0, 0, decimal.ToInt32(_mountVariationInput.Maximum));
        UpdateMountInfoLabel();
        SetStatus($"Set {_selectedMount.Name} breed/color to {variant.DisplayName}. Save to write Mounts.json.");
    }

    private void ApplyMountDetails()
    {
        if (_selectedMount is null)
        {
            SetStatus("Load a creature first.");
            return;
        }

        _selectedMount.SetName(_mountNameText.Text);
        _selectedMount.SetCurrentHealth(decimal.ToInt32(_mountHealthInput.Value));
        _selectedMount.SetStamina(decimal.ToInt32(_mountStaminaInput.Value));
        _selectedMount.SetFoodLevel(decimal.ToInt32(_mountFoodInput.Value));
        _selectedMount.SetWaterLevel(decimal.ToInt32(_mountWaterInput.Value));
        _selectedMount.SetOxygenLevel(decimal.ToInt32(_mountOxygenInput.Value));
        _selectedMount.SetVariation(decimal.ToInt32(_mountVariationInput.Value));
        _selectedMount.SetUniqueVariation(decimal.ToInt32(_mountUniqueVariationInput.Value));
        _selectedMount.SetCosmeticSkinIndex(decimal.ToInt32(_mountCosmeticSkinInput.Value));
        _selectedMount.SetAlternateCosmeticSkinIndex(decimal.ToInt32(_mountAltCosmeticSkinInput.Value));
        foreach (CreatureGeneticRow row in _creatureGeneticRows)
        {
            _selectedMount.SetGeneticLevel(row.Name, Math.Clamp(row.Value, 0, CreatureGeneticMaxValue));
        }

        int selectedIndex = _mountPicker.SelectedIndex;
        RefreshMountPicker();
        if (selectedIndex >= 0 && selectedIndex < _mountPicker.Items.Count)
        {
            _mountPicker.SelectedIndex = selectedIndex;
        }
        RefreshCreatureTalentRows();
        RefreshCreatureGeneticRows();
        SetStatus($"Updated {_selectedMount?.Name ?? "creature"} details. Save to write Mounts.json.");
    }

    private void MaxMountLevel()
    {
        if (_selectedMount is null)
        {
            SetStatus("Load a creature first.");
            return;
        }

        _mountLevelInput.Value = _selectedMount.MaxLevel;
        UpdateMountLevelFromInput();
    }

    private void UpdateSelectedCreatureTalentRankFromInput()
    {
        if (_updatingCreatureTalentEditor || _selectedMount is null)
        {
            return;
        }

        List<TalentRow> rows = GetSelectedCreatureTalentRows();
        if (rows.Count == 0)
        {
            return;
        }

        int rank = decimal.ToInt32(_creatureTalentRankInput.Value);
        foreach (TalentRow row in rows)
        {
            row.Rank = ClampCreatureTalentRank(row.RowName, rank);
            _selectedMount.SetTalent(row.RowName, row.Rank);
        }
        _creatureTalentsGrid.Refresh();
        SetStatus($"Set rank {rank} on {rows.Count:N0} creature talent(s). Save to write Mounts.json.");
    }
    private void ResetSelectedCreatureTalents()
    {
        if (_selectedMount is null)
        {
            SetStatus("Load a mount first.");
            return;
        }

        List<TalentRow> rows = GetSelectedCreatureTalentRows();
        if (rows.Count == 0)
        {
            SetStatus("Select one or more creature talents first.");
            return;
        }

        _updatingCreatureTalentEditor = true;
        try
        {
            _creatureTalentRankInput.Value = 0;
        }
        finally
        {
            _updatingCreatureTalentEditor = false;
        }

        foreach (TalentRow row in rows)
        {
            row.Rank = 0;
            _selectedMount.SetTalent(row.RowName, 0);
        }
        _creatureTalentsGrid.Refresh();
        SetStatus($"Reset {rows.Count:N0} selected creature talent rank(s). Save to write Mounts.json.");
    }
    private void MaxSelectedCreatureTalent()
    {
        if (_selectedMount is null || _talentCatalog is null)
        {
            SetStatus("Load a mount and talent catalog first.");
            return;
        }

        List<TalentRow> rows = GetSelectedCreatureTalentRows();
        int changed = 0;
        foreach (TalentRow row in rows)
        {
            TalentMetadata? metadata = _talentCatalog.Find(row.RowName);
            if (metadata is null)
            {
                continue;
            }
            _selectedMount.SetTalent(row.RowName, metadata.MaxRank);
            changed++;
        }
        RefreshCreatureTalentRows();
        SetStatus($"Maxed {changed:N0} selected creature talent(s). Save to write Mounts.json.");
    }

    private void MaxAllCreatureTalents()
    {
        if (_selectedMount is null || _talentCatalog is null)
        {
            SetStatus("Load a mount and talent catalog first.");
            return;
        }

        int changed = 0;
        foreach (TalentMetadata metadata in _talentCatalog.CreatureTalents.Where(talent =>
            string.Equals(talent.TreeRowName, _selectedMount.CreatureTreeRowName, StringComparison.OrdinalIgnoreCase)
            && talent.MaxRank > 0))
        {
            _selectedMount.SetTalent(metadata.RowName, metadata.MaxRank);
            changed++;
        }
        RefreshCreatureTalentRows();
        SetStatus($"Maxed {changed:N0} creature talent(s). Save to write Mounts.json.");
    }

    private void ResetCreatureTalents()
    {
        if (_selectedMount is null)
        {
            SetStatus("Load a mount first.");
            return;
        }

        int changed = 0;
        foreach (TalentEntry talent in _selectedMount.Talents.ToList())
        {
            _selectedMount.SetTalent(talent.RowName, 0);
            changed++;
        }
        RefreshCreatureTalentRows();
        SetStatus($"Reset {changed:N0} creature talent(s). Save to write Mounts.json.");
    }

    private int ClampCreatureTalentRank(string rowName, int rank)
    {
        TalentMetadata? metadata = _talentCatalog?.Find(rowName);
        return metadata is null ? Math.Clamp(rank, 0, 99) : Math.Clamp(rank, 0, metadata.MaxRank);
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

    private void UpdateSelectedTalentRankFromInput()
    {
        if (_updatingTalentEditor || _selectedCharacter is null)
        {
            return;
        }

        List<TalentRow> selectedRows = GetSelectedTalentRows();
        if (selectedRows.Count == 0)
        {
            return;
        }

        int requestedRank = decimal.ToInt32(_talentRankInput.Value);
        foreach (TalentRow row in selectedRows)
        {
            row.Rank = ClampTalentRank(row.RowName, requestedRank);
            _selectedCharacter.SetTalent(row.RowName, row.Rank);
        }
        _talentsGrid.Refresh();
        SetStatus($"Set rank {requestedRank} on {selectedRows.Count:N0} selected talent(s). Save to write Characters.json.");
    }

    private void ResetSelectedTalents()
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

        _updatingTalentEditor = true;
        try
        {
            _talentRankInput.Value = 0;
        }
        finally
        {
            _updatingTalentEditor = false;
        }

        foreach (TalentRow row in selectedRows)
        {
            row.Rank = 0;
            _selectedCharacter.SetTalent(row.RowName, 0);
        }
        _talentsGrid.Refresh();
        SetStatus($"Reset {selectedRows.Count:N0} selected talent rank(s). Save to write Characters.json.");
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

    private void ResetAllTalents()
    {
        if (_selectedCharacter is null)
        {
            SetStatus("Load a character first.");
            return;
        }

        if (_talentCatalog is null)
        {
            SetStatus("Load the talent catalog first to reset known talents.");
            return;
        }

        int changed = 0;
        foreach (TalentMetadata metadata in GetActiveTalentMetadata())
        {
            _selectedCharacter.SetTalent(metadata.RowName, 0);
            changed++;
        }

        RefreshTalentRows();
        SetStatus($"Reset {changed:N0} talent rank(s) in the active view to 0. Save to write Characters.json.");
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
        foreach (TalentMetadata metadata in GetActiveTalentMetadata().Where(talent => talent.MaxRank > 0))
        {
            _selectedCharacter.SetTalent(metadata.RowName, metadata.MaxRank);
            changed++;
        }

        RefreshTalentRows();
        SetStatus($"Maxed {changed:N0} talent(s) in the active view. Save to write Characters.json.");
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
            RefreshTalentNavigation();
            RefreshBlueprintRows();
            RefreshCreatureTalentRows();
            SetStatus($"Loaded {_talentCatalog.Count:N0} metadata rows from {directory}.");
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


    private void SaveMountsFile()
    {
        if (_mounts is null)
        {
            SetStatus("Load Mounts.json first.");
            return;
        }

        try
        {
            _creatureTalentsGrid.EndEdit();
            _creatureGeneticsGrid.EndEdit();
            if (_selectedMount is not null)
            {
                _selectedMount.SetName(_mountNameText.Text);
                _selectedMount.SetCurrentHealth(decimal.ToInt32(_mountHealthInput.Value));
                _selectedMount.SetStamina(decimal.ToInt32(_mountStaminaInput.Value));
                _selectedMount.SetFoodLevel(decimal.ToInt32(_mountFoodInput.Value));
                _selectedMount.SetWaterLevel(decimal.ToInt32(_mountWaterInput.Value));
                _selectedMount.SetOxygenLevel(decimal.ToInt32(_mountOxygenInput.Value));
                _selectedMount.SetVariation(decimal.ToInt32(_mountVariationInput.Value));
                _selectedMount.SetUniqueVariation(decimal.ToInt32(_mountUniqueVariationInput.Value));
                _selectedMount.SetCosmeticSkinIndex(decimal.ToInt32(_mountCosmeticSkinInput.Value));
                _selectedMount.SetAlternateCosmeticSkinIndex(decimal.ToInt32(_mountAltCosmeticSkinInput.Value));
                _selectedMount.Level = Math.Clamp(decimal.ToInt32(_mountLevelInput.Value), 0, _selectedMount.MaxLevel);
                foreach (TalentRow row in _creatureTalentRows)
                {
                    _selectedMount.SetTalent(row.RowName, ClampCreatureTalentRank(row.RowName, row.Rank));
                }
                foreach (CreatureGeneticRow row in _creatureGeneticRows)
                {
                    _selectedMount.SetGeneticLevel(row.Name, Math.Clamp(row.Value, 0, CreatureGeneticMaxValue));
                }
            }

            string? backupPath = _mounts.SaveWithBackup();
            SetStatus(backupPath is null
                ? "Created Mounts.json."
                : $"Saved Mounts.json. Backup: {backupPath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save mounts", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Mounts save failed.");
        }
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
            _talentsGrid.EndEdit();
            _blueprintsGrid.EndEdit();
            if (_selectedCharacter is not null)
            {
                foreach (TalentRow row in _talentRows)
                {
                    _selectedCharacter.SetTalent(row.RowName, ClampTalentRank(row.RowName, row.Rank));
                }
                foreach (TalentRow row in _blueprintRows)
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
        _playerDataPathText.Text = Path.GetDirectoryName(filePath) ?? "";
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

internal sealed record TalentCategory(string DisplayName, string? Archetype)
{
    public override string ToString() => DisplayName;
}

internal sealed record TalentTreeChoice(string DisplayName, string? RowName)
{
    public override string ToString() => DisplayName;
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

internal sealed class CreatureGeneticRow
{
    public CreatureGeneticRow(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }

    public int Value { get; set; }
}
