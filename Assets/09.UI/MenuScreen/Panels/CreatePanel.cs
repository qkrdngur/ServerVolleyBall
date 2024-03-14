using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CreatePanel
{
    private VisualElement _root;

    public event Action<string> MakeLobbyBtnEvent;

    private TextField _nameField;
    private Label _statusLabel;

    public Label StatusLabel => _statusLabel;

    public CreatePanel(VisualElement root)
    {
        _root = root;
        _nameField = root.Q<TextField>("text-lobby-name");
        _statusLabel = root.Q<Label>("status-label");

        root.Q<Button>("btn-create").RegisterCallback<ClickEvent>(
            evt => MakeLobbyBtnEvent?.Invoke(_nameField.value));
    }

    public void SetStatusText(string text)
    {
        _statusLabel.text = text;
    }
}
