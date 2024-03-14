using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbyPanel
{
    private VisualElement _root;
    private Label _statusLabel;
    public Label StatusLabel => _statusLabel;

    private ScrollView _lobbyScrollView;
    private bool _isLobbyRefresh = false;
    private VisualTreeAsset _lobbyAsset;

    private bool _isJoining = false;
    public event Action<Lobby> JoinLobbyBtnEvent;


    public LobbyPanel(VisualElement root, VisualTreeAsset lobbyAsset)
    {
        _root = root;
        _lobbyAsset = lobbyAsset;   
        _statusLabel = root.Q<Label>("status-label");
        _lobbyScrollView = root.Q<ScrollView>("lobby-scroll");

        root.Q<Button>("btn-refresh").RegisterCallback<ClickEvent>(HandleRefreshBtnClick);
    }


    private void HandleRefreshBtnClick(ClickEvent evt)
    {
        Refresh();
    }

    public async void Refresh()
    {
        if (_isLobbyRefresh) return;

        _isLobbyRefresh = true;
        var list = await ApplicationController.Instance.GetLobbyList();
        _lobbyScrollView.Clear();
        foreach (var lobby in list)
        {
            var lobbyTemplate = _lobbyAsset.Instantiate();
            _lobbyScrollView.Add(lobbyTemplate);

            lobbyTemplate.Q<Label>("lobby-name").text = lobby.Name;

            lobbyTemplate.Q<Button>("btn-join").RegisterCallback<ClickEvent>(evt =>
            {
                JoinLobbyBtnEvent?.Invoke(lobby);
                //JoinToLobby(lobby);
            });
        }
        _isLobbyRefresh = false;
        
    }

    

    public void SetStatusText(string text)
    {
        _statusLabel.text = text;
    }
}
