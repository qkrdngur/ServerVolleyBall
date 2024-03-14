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
    public event Action<GameState> GameStateChanged; //게임의 상태가 변했을 때 발행되는 것.
    private GameState _gameState; //현재 게임 상태

    [SerializeField] private Transform[] _spawnPosition;
    public Color[] slimeColors; //슬라임의 컬러

    public NetworkList<GameData> players;

    public GameRole myGameRole;

    public Transform[] _firecrackerPos;

    private ushort _colorIdx = 0;
    //호스트만 사용하는 변수로 
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

    //이게 스폰보다 먼저 실행될꺼다.
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

            //여기서 본인은 안되게 되어있다. 나중에 처리해줘야 한다.
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
                Debug.LogError($"{data.playerName} 삭제중 오류 발생");
            }
            break;
        }
    }

    public void GameReady()
    {
        //내 클라이언트 아이디로 레디 되었음을 보내면 된다.
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
        if(_readyUserCount >= 1)  //디버그 용도로 1만 되어도 게임 시작하게
        {
            //여기서 플레이어 턴기반으로 돌리는 로직도 함께
            SpawnPlayers();
            StartGameClientRpc();
        }
        else
        {
            Debug.Log("아직 플레이어들이 준비되지 않았습니다.");
        }
    }

    //이건 host만 호출하니까
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
        HostSingleton.Instance.GameManager.NetServer.DestroyAllPlayer(); //모든 플레이어 제거
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
