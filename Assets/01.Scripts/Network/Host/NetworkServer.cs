using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class NetworkServer : IDisposable
{
    private NetworkManager _networkManager;
    public Action<string, ulong> OnClientJoin; //클라이언트 접속 및 종료시 발행이벤트
    public Action<string, ulong> OnClientLeft;

    private Dictionary<ulong, string> _clientToAuthDictionary = new ();
    private Dictionary<string, UserData> _authIdToUserDataDictionary = new ();

    private NetworkObject _playerPrefab;

    private List<NetworkObject> _playerList = new List<NetworkObject>();

    public NetworkServer(NetworkManager networkManager, NetworkObject playerPrefab)
    {
        _networkManager = networkManager;
        _playerPrefab = playerPrefab;
        _networkManager.ConnectionApprovalCallback += ApprovalCheck;
        //이녀석은 서버 네트워크 매니저만 발행하는 이벤트다.
        _networkManager.OnServerStarted += OnServerReady;
    }

    //승인 체크 과정인데
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest req, 
                                NetworkManager.ConnectionApprovalResponse res)
    {
        string json = Encoding.UTF8.GetString(req.Payload);
        UserData userData = JsonUtility.FromJson<UserData>(json);

        //클라이언트 아이디를 이용해서 authID
        _clientToAuthDictionary[req.ClientNetworkId] = userData.userAuthID;
        _authIdToUserDataDictionary[userData.userAuthID] = userData;

        res.Approved = true;
        res.CreatePlayerObject = false;

        OnClientJoin?.Invoke(userData.userAuthID, req.ClientNetworkId);
    }

    private void OnServerReady()
    {
        _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    //클라이언트가 접속 종료했을 때 해줄 일을 여기다가 쓴다.
    private void OnClientDisconnect(ulong clientID)
    {
        if(_clientToAuthDictionary.TryGetValue(clientID, out var authID))
        {
            _clientToAuthDictionary.Remove(clientID);
            _authIdToUserDataDictionary.Remove(authID);
            OnClientLeft?.Invoke(authID, clientID);
        }
    }

    public UserData GetUserDataByClientID(ulong clientID)
    {
        if(_clientToAuthDictionary.TryGetValue(clientID, out string authID))
        {
            if(_authIdToUserDataDictionary.TryGetValue(authID, out UserData data))
            {
                return data;
            }
        }
        return null;
    }

    public UserData GetUserDataByAuthID(string authID)
    {
        if(_authIdToUserDataDictionary.TryGetValue(authID, out UserData data))
        {
            return data;
        }
        return null;
    }


    public void Dispose()
    {
        if (_networkManager == null) return; //유니티 클라가 꺼진경우

        _networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        _networkManager.OnServerStarted -= OnServerReady;
        _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;

        if (_networkManager.IsListening)
        {
            _networkManager.Shutdown(); //종료
        }
    }


    public void SpawnPlayer(ulong clientID, Vector3 position, ushort colorIdx)
    {
        var player = GameObject.Instantiate(_playerPrefab, position, Quaternion.identity);
        player.SpawnAsPlayerObject(clientID);
        _playerList.Add(player);

        //여기서 이제 컬러값을 받아서 이녀석한테 컬러를 셋팅하라고 명령을 내려야 해.
        PlayerColorizer color = player.GetComponent<PlayerColorizer>();
        color.SetColor(colorIdx);

        PlayerStateController controller = player.GetComponent<PlayerStateController>();
        controller.SetInitStateClientRpc(clientID == NetworkManager.Singleton.LocalClientId);
    }

    public void DestroyAllPlayer()
    {
        foreach(var p in _playerList)
        {
            GameObject.Destroy(p.gameObject);
        }
        _playerList.Clear();
    }
}
