using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuScreen : MonoBehaviour
{

    [SerializeField] private VisualTreeAsset _createPanelAsset;
    [SerializeField] private VisualTreeAsset _lobbyPanelAsset;
    [SerializeField] private VisualTreeAsset _lobbyTemplateAsset;

    private UIDocument _uiDocument;
    private VisualElement _contentElem;
    public const string nameKey = "userName";

    private bool _isWaiting = false; //로비가 생성중인가?
    private CreatePanel _createPanel;
    private LobbyPanel _lobbyPanel;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = _uiDocument.rootVisualElement;
        
        _contentElem = root.Q<VisualElement>("content");
        root.Q<VisualElement>("tab-container")
            .RegisterCallback<ClickEvent>(TabButtonClickHandle);

        //오프 값을 넣어준다.
        root.Q<VisualElement>("popup-panel").RemoveFromClassList("off");

        var nameText = root.Q<TextField>("name-text");
        nameText.SetValueWithoutNotify(PlayerPrefs.GetString(nameKey, string.Empty));

        root.Q<Button>("btn-ok").RegisterCallback<ClickEvent>(e =>
        {
            string name = nameText.value;
            if (string.IsNullOrEmpty(name))
                return;

            PlayerPrefs.SetString(nameKey, name);
            root.Q<VisualElement>("popup-panel").AddToClassList("off");
        });


        //크리에이트 패널 만들기
        var createPanel = _createPanelAsset.Instantiate();
        createPanel.AddToClassList("panel");
        root.Q<VisualElement>("page-one").Add(createPanel);
        _createPanel = new CreatePanel(createPanel);
        _createPanel.MakeLobbyBtnEvent += HandleCreateLobby;

        //로비패널 만들기
        var lobbyPanel = _lobbyPanelAsset.Instantiate();
        lobbyPanel.AddToClassList("panel");
        root.Q<VisualElement>("page-two").Add(lobbyPanel);
        _lobbyPanel = new LobbyPanel(lobbyPanel, _lobbyTemplateAsset);
        _lobbyPanel.JoinLobbyBtnEvent += HandleJoinToLobby;
    }

    private async void HandleCreateLobby(string lobbyName)
    {
        if (_isWaiting) return; //현재 만들어지는 중이니까 만들지 마세요.

        if(string.IsNullOrEmpty(lobbyName))
        {
            _createPanel.SetStatusText("로비 이름은 공백일 수 없습니다.");
            return;
        }

        _isWaiting = true; 

        string username = PlayerPrefs.GetString(nameKey);
        //여기서는 스테이터스 텍스트에 로딩 텍스트가 나오게 만들어놓고 가면돼
        LoadText(_createPanel.StatusLabel);
        bool result = await ApplicationController.Instance.StartHostAsync(username, lobbyName);
        if(result)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneList.GameScene, LoadSceneMode.Single);
        }
        else
        {
            _createPanel.SetStatusText("로비 생성중 오류 발생!");
        }
        _isWaiting = false;
    }

    private async void HandleJoinToLobby(Lobby lobby)
    {
        if (_isWaiting) return;
        _isWaiting = true;
        LoadText(_lobbyPanel.StatusLabel);
        try
        {
            Lobby joiningLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id);
            string joinCode = joiningLobby.Data["JoinCode"].Value;

            await ApplicationController.Instance.StartClientAsync(
                            PlayerPrefs.GetString(MenuScreen.nameKey), joinCode);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }
        finally
        {
            _isWaiting = false;
        }
    }

    private async void LoadText(Label targetLabel)
    {
        string[] makings = { "Loading", "Loading.", "Loading..", "Loading...", "Loading...." };
        int idx = 0;
        while(_isWaiting)
        {
            targetLabel.text = makings[idx];
            idx = (idx + 1) % makings.Length;
            await Task.Delay(300);
        }
    }

    private void TabButtonClickHandle(ClickEvent evt)
    {
        if(evt.target is DataVisualElement)
        {
            var dve = evt.target as DataVisualElement;
            var percent = dve.panelIndex * 100;

            _contentElem.style.left = new Length(-percent, LengthUnit.Percent);


            //여기는 나중에 통합시킬 예정...근데 귀찮으면 안할 수도 있음.
            if(dve.panelIndex == 1)
            {
                _lobbyPanel.Refresh();
            }
        }
    }
}
