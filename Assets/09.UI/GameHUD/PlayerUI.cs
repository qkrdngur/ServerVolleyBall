using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerUI
{
    private VisualElement _sprite;
    private Label _nameLabel;
    private VisualElement _root;

    public ulong clientID = 999;


    public PlayerUI(VisualElement root)
    {
        _root = root;
        _sprite = root.Q<VisualElement>("sprite");
        _nameLabel = root.Q<Label>("name-label");
    }

    public void SetGameData(GameData data)
    {
        clientID = data.clientID;
        _nameLabel.text = data.playerName.Value;
        SetCheck(data.ready); //요거 해줘야 한다.
    }

    public void SetCheck(bool check)
    {
        if(check)
        {
            _root.AddToClassList("ready");
        }
        else
        {
            _root.RemoveFromClassList("ready");
        }
    }

    public void SetColor(Color color)
    {
        _sprite.style.unityBackgroundImageTintColor = color;
    }

    public void RemovePlayerUI()
    {
        clientID = 999;
        _root.style.visibility = Visibility.Hidden;
    }

    public void VisiblePlayerUI()
    {
        _root.style.visibility = Visibility.Visible;
    }
}
