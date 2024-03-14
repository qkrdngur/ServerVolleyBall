using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ClientSignleton : MonoBehaviour
{
    public ClientGameManager GameManager { get; private set; }

    private static ClientSignleton _instance;
    public static ClientSignleton Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<ClientSignleton>();

            if(_instance == null)
            {
                Debug.LogError("No client singleton");
            }

            return _instance;
        }
    }

    public void CreateClient()
    {
        GameManager = new ClientGameManager(NetworkManager.Singleton);
    }

    private void OnDestroy()
    {
        //나중에 여기에 깨끗하게 지우는 처리
    }
}
