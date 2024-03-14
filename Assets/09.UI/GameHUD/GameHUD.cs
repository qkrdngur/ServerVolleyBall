using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class GameHUD : MonoBehaviour
{
    private UIDocument _uiDocument;

    private Button _startGameBtn;
    private Button _readyGameBtn;

    private List<PlayerUI> _players = new();

    private VisualElement _container;

    private Label _hostScore;
    private Label _clientScore;

    private VisualElement _resultBox;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = _uiDocument.rootVisualElement;
        _container = root.Q<VisualElement>("container");
        _startGameBtn = root.Q<Button>("btn-start");
        _readyGameBtn = root.Q<Button>("btn-ready");

        _hostScore = root.Q<Label>("host-score");
        _clientScore = root.Q<Label>("client-score");

        _resultBox = root.Q<VisualElement>("result-box");
        _resultBox.AddToClassList("off"); //처음시작하면 닫아두고

        root.Query<VisualElement>(className: "player").ToList().ForEach(x =>
        {
            var player = new PlayerUI(x);
            _players.Add(player);
            player.RemovePlayerUI();
        });

        _startGameBtn.RegisterCallback<ClickEvent>(HandleGameStartClick);
        _readyGameBtn.RegisterCallback<ClickEvent>(HandleReadyClick);

        root.Q<Button>("btn-restart").RegisterCallback<ClickEvent>(HandleRestartClick);

        SignalHub.OnScoreChanged += HandleScoreChanged;
        SignalHub.OnEndGame += HandleEndGame;


    }

    private void HandleRestartClick(ClickEvent evt)
    {
        _resultBox.AddToClassList("off"); //결과박스 닫아버리고
        _container.RemoveFromClassList("off"); //레디 창 다시 불러오고
        
    }

    private void HandleEndGame(bool isWin)
    {
        string msg = isWin ? "You Win!" : "You Lose";
        _resultBox.Q<Label>("result-label").text = msg;
        _resultBox.RemoveFromClassList("off");
    }

    private void HandleScoreChanged(int hostScore, int clientScore)
    {
        _hostScore.text = hostScore.ToString();
        _clientScore.text = clientScore.ToString();
    }

    //여기까지 왔으면 게임매니저가 다 완성이 된 상태야. 근데 네트워크 스폰까지 안된.
    private void Start()
    {
        GameManager.Instance.players.OnListChanged += HandlePlayerListChanged;
        GameManager.Instance.GameStateChanged += HandleGameStateChanged;

        //이때 이미 네트워크 리스트에 한명은 들어가 있어. 호스트
        foreach (GameData data in GameManager.Instance.players)
        {
            HandlePlayerListChanged(new NetworkListEvent<GameData>
            {
                Type = NetworkListEvent<GameData>.EventType.Add,
                Value = data
            });
        }
    }

    private void OnDestroy()
    {
        GameManager.Instance.players.OnListChanged -= HandlePlayerListChanged;
        GameManager.Instance.GameStateChanged -= HandleGameStateChanged;
    }

    private bool CheckPlayerExist(ulong clientID)
    {
        return _players.Any(x => x.clientID == clientID);
    }

    private PlayerUI FindEmptyPlayerUI()
    {
        foreach(var playerUI in _players)
        {
            if (playerUI.clientID == 999)
            {
                return playerUI;
            }
        }
        return null;
    }

    private void HandlePlayerListChanged(NetworkListEvent<GameData> evt)
    {
        //Debug.Log($"{evt.Type}, {evt.Value.clientID}");
        switch(evt.Type)
        {
            case NetworkListEvent<GameData>.EventType.Add:
            {
                if(!CheckPlayerExist(evt.Value.clientID))
                {
                    var playerUI = FindEmptyPlayerUI();
                    playerUI.SetGameData(evt.Value);
                    playerUI.SetColor(GameManager.Instance.slimeColors[evt.Value.colorIdx]);
                    playerUI.VisiblePlayerUI();
                }
                break;
            }

             
            case NetworkListEvent<GameData>.EventType.Remove:
            {
                    //알맞은 플레이어 UI를 찾아서 제거해주면돼.
                PlayerUI playerUI = _players.Find(x => x.clientID == evt.Value.clientID);
                playerUI.RemovePlayerUI();
                break;
            }

            case NetworkListEvent<GameData>.EventType.Value:
            {
                PlayerUI playerUI = _players.Find(x => x.clientID == evt.Value.clientID);
                playerUI.SetCheck(evt.Value.ready);
                break;
            }
        }
    }

    private void HandleGameStateChanged(GameState obj)
    {
        if(obj == GameState.Game)
        {
            _container.AddToClassList("off");
            GameManager.Instance.GameReady();
        }
    }

    private void HandleGameStartClick(ClickEvent evt)
    {
        if(GameManager.Instance.myGameRole != GameRole.Host)
        {
            Debug.Log("게임 호스트만 게임 시작이 가능합니다.");
            return;
        }

        //여기서 게임 시작하는 로직 만들면 된다.
        GameManager.Instance.GameStart();
    }

    private void HandleReadyClick(ClickEvent evt)
    {
        GameManager.Instance.GameReady();
    }

}
