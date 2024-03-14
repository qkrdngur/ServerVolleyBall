using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum GameState
{
    Ready,
    Game,
    Win,
    Lose
}

public enum GameRole: ushort
{
    Host,
    Client
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public event Action<GameState> GameStateChanged; //������ ���°� ������ �� ����Ǵ� ��.
    private GameState _gameState; //���� ���� ����

    [SerializeField] private Transform[] _spawnPosition;
    public Color[] slimeColors; //�������� �÷�

    public NetworkList<GameData> players;

    public GameRole myGameRole;

    public Transform[] _firecrackerPos;

    private ushort _colorIdx = 0;
    //ȣ��Ʈ�� ����ϴ� ������ 
    private int _readyUserCount = 0;

    public BallManager EggManager { get; private set; }
    public TurnManager TurnManager { get; private set; }
    public ScoreManager ScoreManager { get; private set; }

    private void Awake()
    {
        Instance = this;
        players = new NetworkList<GameData>();

        EggManager = GetComponent<BallManager>();
        TurnManager = GetComponent<TurnManager>();
        ScoreManager = GetComponent<ScoreManager>();
    }

    //�̰� �������� ���� ����ɲ���.
    //https://docs-multiplayer.unity3d.com/netcode/current/basics/networkbehavior/
    private void Start()
    {
        _gameState = GameState.Ready;
    }

    public override void OnNetworkSpawn()
    {
        if(IsHost)
        {
            HostSingleton.Instance.GameManager.OnPlayerConnect += OnPlayerConnectHandle;
            HostSingleton.Instance.GameManager.OnPlayerDisconnect += OnPlayerDisconnectHandle;

            //���⼭ ������ �ȵǰ� �Ǿ��ִ�. ���߿� ó������� �Ѵ�.
            var gameData = HostSingleton.Instance.GameManager.NetServer
                .GetUserDataByClientID(OwnerClientId);
            OnPlayerConnectHandle(gameData.userAuthID, OwnerClientId);
            myGameRole = GameRole.Host;
        }
        else
        {
            myGameRole = GameRole.Client;
        }
    }

    public override void OnNetworkDespawn()
    {
        if(IsHost)
        {
            HostSingleton.Instance.GameManager.OnPlayerConnect -= OnPlayerConnectHandle;
            HostSingleton.Instance.GameManager.OnPlayerDisconnect -= OnPlayerDisconnectHandle;
        }
    }

    private void OnPlayerConnectHandle(string authID, ulong clientID)
    {
        UserData data = HostSingleton.Instance.GameManager.NetServer.GetUserDataByClientID(clientID);
        players.Add(new GameData
        {
            clientID = clientID,
            playerName = data.name,
            ready = false,
            colorIdx = _colorIdx,
        });
        ++_colorIdx;
    }

    private void OnPlayerDisconnectHandle(string authID, ulong clientID)
    {
        foreach(GameData data in players)
        {
            if (data.clientID != clientID) continue;
            try
            {
                players.Remove(data);
                --_colorIdx;
            }catch
            {
                Debug.LogError($"{data.playerName} ������ ���� �߻�");
            }
            break;
        }
    }

    public void GameReady()
    {
        //�� Ŭ���̾�Ʈ ���̵�� ���� �Ǿ����� ������ �ȴ�.
        SendReadyStateServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendReadyStateServerRpc(ulong clientID)
    {
        for(int i = 0; i < players.Count; ++i)
        {
            if (players[i].clientID != clientID) continue;

            var oldData = players[i];
            players[i] = new GameData
            {
                clientID = clientID,
                playerName = oldData.playerName,
                ready = !oldData.ready,
                colorIdx = oldData.colorIdx
            };
            _readyUserCount += players[i].ready ? 1 : -1;
            break;
        }
        
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        players?.Dispose();
    }

    public void GameStart()
    {
        if (!IsHost) return;
        if(_readyUserCount >= 1)  //����� �뵵�� 1�� �Ǿ ���� �����ϰ�
        {
            //���⼭ �÷��̾� �ϱ������ ������ ������ �Բ�
            SpawnPlayers();
            StartGameClientRpc();
        }
        else
        {
            Debug.Log("���� �÷��̾���� �غ���� �ʾҽ��ϴ�.");
        }
    }

    //�̰� host�� ȣ���ϴϱ�
    private void SpawnPlayers()
    {
        int i = 0;
        foreach(var player in players)
        {
            HostSingleton.Instance.GameManager.NetServer.SpawnPlayer(
                player.clientID,
                _spawnPosition[i++].position,
                player.colorIdx);
        }
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        _gameState = GameState.Game;
        GameStateChanged?.Invoke(_gameState);
    }

    public void SendResultToClient(GameRole winner)
    {
        HostSingleton.Instance.GameManager.NetServer.DestroyAllPlayer(); //��� �÷��̾� ����
        ScoreManager.InitializeScore();
        EggManager.DestroyEgg();
        SendResultToClientRpc(winner);
    }

    [ClientRpc]
    public void SendResultToClientRpc(GameRole winner)
    {
        if(winner == myGameRole)
        {
            _gameState = GameState.Win;
            SignalHub.OnEndGame?.Invoke(true);
        }
        else
        {
            _gameState = GameState.Lose;
            SignalHub.OnEndGame?.Invoke(false);
        }

        GameStateChanged?.Invoke(_gameState);
    }

}
