using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class ClientGameManager
{
    private NetworkManager _networkManager;
    private JoinAllocation _allocation;
    private bool _isLobbyRefresh = false; //이건 나중에 씀.

    public ClientGameManager(NetworkManager networkManager)
    {
        _networkManager = networkManager;
    }


    public void Disconnect()
    {
        //메뉴씬으로 보내기
        if(_networkManager.IsConnectedClient)
        {
            _networkManager.Shutdown(); //강제 종료
        }
    }

    public async Task StartClientAsync(string joinCode, UserData userData)
    {
        try
        {
            _allocation = await Relay.Instance.JoinAllocationAsync(joinCode);
        }catch (Exception e)
        {
            Debug.LogError(e);
            return;
        }

        //트랜스포트를 받아서 
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        //릴레이서 서버데이터를 만들어서 설정해주고
        var relayServerData = new RelayServerData(_allocation, "dtls");
        transport.SetRelayServerData(relayServerData);
        // user데이터를 json으로 만들어서 connectionData에 넣은 후에
        string json = JsonUtility.ToJson(userData);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(json);
        // NetworkManager에 StartClient를 해주면 된다.
        NetworkManager.Singleton.StartClient();
    }
}
