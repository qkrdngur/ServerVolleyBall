using System;
using Unity.Netcode;
using UnityEngine;

public class BallManager : NetworkBehaviour
{
    [Header("참조값들")]
    [SerializeField] private Ball _eggPrefab;

    [Header("셋팅값")]
    [SerializeField] private Transform[] _eggStartPosition;

    private Ball _eggInstance;

    private void Start()
    {
        GameManager.Instance.GameStateChanged += HandleGameStateChanged;   
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GameManager.Instance.GameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Ready:
                break;
            case GameState.Game:
                SpawnEgg();
                break;
            case GameState.Win:
                break;
            case GameState.Lose:
                break;
        }
    }

    private void SpawnEgg()
    {
        if (!IsServer) return;

        _eggInstance = Instantiate(_eggPrefab, _eggStartPosition[0].position, Quaternion.identity);
        _eggInstance.NetworkObject.Spawn();
        
    }

    public void ResetEgg(int idx)
    {
        _eggInstance.ResetToStartPosition(_eggStartPosition[idx].position);
    }

    public void DestroyEgg()
    {
        Destroy(_eggInstance.gameObject);
    }
}
