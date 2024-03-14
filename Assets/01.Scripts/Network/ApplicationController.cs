using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

//이녀석은 호스트 싱글톤과 클라이언트 싱글톤을 관리하면서
//게임이 켜질때 어드레서블로 에셋을 로드하는 역할도 할꺼야. 
public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSignleton _clientPrefab;
    [SerializeField] private HostSingleton _hostPrefab;
    //여기에 로딩할 어드레서블 에셋 리스트 
    [SerializeField] private NetworkObject _playerPrefab;

    public static event Action<string> OnMessageEvent;
    public static ApplicationController Instance;

    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;

        OnMessageEvent?.Invoke("게임 서비스 초기화 진행중...");
        await UnityServices.InitializeAsync();

        OnMessageEvent?.Invoke("네트워크 서비스 인증중...");
        AuthenticationWrapper.OnMessageEvent += HandleAuthMessage;
        var state = await AuthenticationWrapper.DoAuth(3);

        if (state != AuthState.Authenticated) //유니티 인증이 실패
        {
            OnMessageEvent?.Invoke("앱 인증중 오류 발생.. 앱을 다시 시작하세요.");
            return;
        }

        //호스트 크리에이트 해줘야 하고
        HostSingleton host = Instantiate(_hostPrefab, transform);
        host.CreateHost(_playerPrefab);

        //클라이언트 크리에이트 해줘야 한다.
        ClientSignleton client = Instantiate(_clientPrefab, transform);
        client.CreateClient(); //게임매니저를 만들어주는거다.

        //여기서 어드레서블 에셋 로드가 일어나야 한다.


        //메뉴씬으로 넘어간다.
        SceneManager.LoadScene(SceneList.MenuScene);
    }

    private void HandleAuthMessage(string msg)
    {
        OnMessageEvent?.Invoke(msg);
    }

    private void OnDestroy()
    {
        AuthenticationWrapper.OnMessageEvent -= HandleAuthMessage;
    }

    public async Task<bool> StartHostAsync(string username, string lobbyName)
    {
        return await HostSingleton.Instance.GameManager.StartHostAsync(lobbyName, GetUserData(username));
    }

    public async Task StartClientAsync(string username, string joinCode)
    {
        await ClientSignleton.Instance.GameManager.StartClientAsync(joinCode, GetUserData(username));
    }

    private UserData GetUserData(string username)
    {
        return new UserData
        {
            name = username,
            userAuthID = AuthenticationService.Instance.PlayerId
        };
    }


    public async Task<List<Lobby>> GetLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 20;
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0"),
                new QueryFilter(
                    field: QueryFilter.FieldOptions.IsLocked,
                    op: QueryFilter.OpOptions.EQ,
                    value: "0"),
            };

            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);
            return lobbies.Results;
        } catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
            return new List<Lobby>();
        }
    }
}
