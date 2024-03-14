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
        _resultBox.AddToClassList("off"); //ó�������ϸ� �ݾƵΰ�

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
        _resultBox.AddToClassList("off"); //����ڽ� �ݾƹ�����
        _container.RemoveFromClassList("off"); //���� â �ٽ� �ҷ�����
        
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

    //������� ������ ���ӸŴ����� �� �ϼ��� �� ���¾�. �ٵ� ��Ʈ��ũ �������� �ȵ�.
    private void Start()
    {
        GameManager.Instance.players.OnListChanged += HandlePlayerListChanged;
        GameManager.Instance.GameStateChanged += HandleGameStateChanged;

        //�̶� �̹� ��Ʈ��ũ ����Ʈ�� �Ѹ��� �� �־�. ȣ��Ʈ
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
                    //�˸��� �÷��̾� UI�� ã�Ƽ� �������ָ��.
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
            Debug.Log("���� ȣ��Ʈ�� ���� ������ �����մϴ�.");
            return;
        }

        //���⼭ ���� �����ϴ� ���� ����� �ȴ�.
        GameManager.Instance.GameStart();
    }

    private void HandleReadyClick(ClickEvent evt)
    {
        GameManager.Instance.GameReady();
    }

}
