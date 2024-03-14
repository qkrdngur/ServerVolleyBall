using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    [HideInInspector] public NetworkVariable<GameRole> currentTurn = new NetworkVariable<GameRole>();

    private void SwitchTurn()
    {
        if(currentTurn.Value == GameRole.Client)
        {
            currentTurn.Value = GameRole.Host;
        }
        else
            currentTurn.Value = GameRole.Client;

        Debug.Log(currentTurn.Value);
    }

    public void StartGame()
    {
        currentTurn.Value = GameRole.Host;
    }

    public override void OnNetworkSpawn()
    {
        GameManager.Instance.GameStateChanged += HandleGameStateChanged;
        Ball.OnHit += SwitchTurn;
    }

    public override void OnNetworkDespawn()
    {
        GameManager.Instance.GameStateChanged -= HandleGameStateChanged;
        Ball.OnHit -= SwitchTurn;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if(state == GameState.Game)
        {
            StartGame();
        }
    }
}
