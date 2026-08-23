using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IcarusProfileMod;

internal sealed class InjectMountDialog : Form
{
    private readonly ListBox _mountTypeList = new();
    private readonly TextBox _nameText = new();
    private readonly Label _detailsLabel = new();
    private readonly IReadOnlyList<MountInjectionDefinition> _definitions;
    private string _lastSuggestedName = "";
    private bool _nameEdited;
    private bool _updatingName;

    public InjectMountDialog(IReadOnlyList<MountInjectionDefinition> definitions)
    {
        _definitions = definitions;
        Text = "Inject Creature";
        MinimumSize = new Size(640, 420);
        Size = new Size(700, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(12) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        Controls.Add(root);

        root.Controls.Add(new Label { Text = "Creature Type", Dock = DockStyle.Fill }, 0, 0);
        root.Controls.Add(new Label { Text = "Details", Dock = DockStyle.Fill }, 1, 0);

        _mountTypeList.Dock = DockStyle.Fill;
        _mountTypeList.DisplayMember = nameof(MountInjectionDefinition.DisplayText);
        _mountTypeList.SelectedIndexChanged += (_, _) => UpdateSelectionDetails();
        foreach (MountInjectionDefinition definition in definitions.OrderBy(definition => definition.DisplayName))
        {
            _mountTypeList.Items.Add(definition);
        }
        root.Controls.Add(_mountTypeList, 0, 1);

        _detailsLabel.Dock = DockStyle.Fill;
        _detailsLabel.BorderStyle = BorderStyle.FixedSingle;
        _detailsLabel.Padding = new Padding(8);
        root.Controls.Add(_detailsLabel, 1, 1);

        TableLayoutPanel nameBar = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        nameBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        nameBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        nameBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        nameBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.Controls.Add(nameBar, 0, 2);
        root.SetColumnSpan(nameBar, 2);
        nameBar.Controls.Add(new Label { Text = "Name", Dock = DockStyle.Fill }, 0, 1);
        _nameText.Dock = DockStyle.Fill;
        _nameText.TextChanged += (_, _) =>
        {
            if (!_updatingName)
            {
                _nameEdited = true;
            }
        };
        nameBar.Controls.Add(_nameText, 1, 1);

        FlowLayoutPanel buttons = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        root.Controls.Add(buttons, 0, 3);
        root.SetColumnSpan(buttons, 2);

        Button cancelButton = new() { Text = "Cancel", Width = 92, Height = 30, DialogResult = DialogResult.Cancel };
        Button injectButton = new() { Text = "Inject", Width = 92, Height = 30, DialogResult = DialogResult.OK };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(injectButton);
        AcceptButton = injectButton;
        CancelButton = cancelButton;

        if (_mountTypeList.Items.Count > 0)
        {
            _mountTypeList.SelectedIndex = 0;
        }
    }

    public MountInjectionDefinition? SelectedDefinition => _mountTypeList.SelectedItem as MountInjectionDefinition;

    public string MountName => string.IsNullOrWhiteSpace(_nameText.Text)
        ? SelectedDefinition?.DefaultName ?? "Injected Mount"
        : _nameText.Text.Trim();

    private void UpdateSelectionDetails()
    {
        if (SelectedDefinition is not MountInjectionDefinition definition)
        {
            _detailsLabel.Text = "";
            return;
        }

        if (!_nameEdited || string.IsNullOrWhiteSpace(_nameText.Text) || string.Equals(_nameText.Text, _lastSuggestedName, StringComparison.Ordinal))
        {
            _updatingName = true;
            _lastSuggestedName = definition.DefaultName;
            _nameText.Text = _lastSuggestedName;
            _nameEdited = false;
            _updatingName = false;
        }

        _detailsLabel.Text =
            $"{definition.DisplayName}\r\n\r\n" +
            $"Type: {definition.TypeKey}\r\n" +
            $"AI setup: {definition.AiSetupRowName}\r\n" +
            $"Blueprint: {definition.BlueprintClassName}\r\n" +
            $"Max level: {definition.MaxLevel}\r\n\r\n" +
            definition.Description;
    }
}
