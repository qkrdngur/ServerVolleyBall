using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerStateController : NetworkBehaviour
{
    [Header("참조값")]
    [SerializeField] private Collider2D _collider;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private TextMeshPro text;

    public NetworkVariable<GameRole> myRole;

    public override void OnNetworkSpawn()
    {
        GameManager.Instance.TurnManager.currentTurn.OnValueChanged += HandleTurnChange;

        if (IsServer)
        {
            if (IsOwner)
            {
                text.text = "1P";
                myRole.Value = GameRole.Host;
            }
            else
            {
                text.text = "2P";
                myRole.Value = GameRole.Client;
            }
        }
        else
        {
            if (IsOwner)
                text.text = "2P";
        }
    }


    public override void OnNetworkDespawn()
    {
        GameManager.Instance.TurnManager.currentTurn.OnValueChanged -= HandleTurnChange;
    }

    private void HandleTurnChange(GameRole previousValue, GameRole newValue)
    {
        if(newValue == myRole.Value) //내턴
        {
            EnablePlayer(true, 100);
        }
        else  //니턴
        {
            EnablePlayer(false);
        }
    }

    private async void EnablePlayer(bool value, int waitTime = 0)
    {
        //await Task.Delay( waitTime );
        //_collider.enabled = value;
        //var color = _spriteRenderer.color;
        //color.a = value ? 1 : 0.3f;
        //_spriteRenderer.color = color;
    }
    [ClientRpc]
    public void SetInitStateClientRpc(bool value)
    {
        EnablePlayer(value);
    }
}
