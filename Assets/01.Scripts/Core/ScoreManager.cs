using Unity.Netcode;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    public NetworkVariable<int> hostScore = new NetworkVariable<int>();
    public NetworkVariable<int> clientScore = new NetworkVariable<int>();

    public Ball egg;

    [SerializeField] private int Hp;
    private int respawnNum = 0;

    private void HandleScoreChanged(int oldScore, int newScore)
    {
        SignalHub.OnScoreChanged(hostScore.Value, clientScore.Value);
    }


    public override void OnNetworkSpawn()
    {
        hostScore.OnValueChanged += HandleScoreChanged;
        clientScore.OnValueChanged += HandleScoreChanged;

        if (!IsServer) return;
        Ball.OnFallInWater += HandleFallInWater;

    }

    
    public override void OnNetworkDespawn()
    {
        hostScore.OnValueChanged -= HandleScoreChanged;
        clientScore.OnValueChanged -= HandleScoreChanged;

        if (!IsServer) return;
        Ball.OnFallInWater -= HandleFallInWater;

    }

    private void HandleFallInWater()
    {
        Vector3 dis = egg.transform.position - Vector3.zero;

        if (dis.x < 0)
        {
            respawnNum = 0;
            clientScore.Value += 1;
        }
        else
        {
            respawnNum = 1;
            hostScore.Value += 1;
        }

        CheckForEndGame();
    }
    
    //게임이 끝났는지를 체크해주는 함수(이건 서버만 실행을 보장)
    private void CheckForEndGame()
    {
        if(hostScore.Value >= Hp)
        {
            GameManager.Instance.SendResultToClient(GameRole.Host);
        }else if(clientScore.Value >= Hp)
        {
            GameManager.Instance.SendResultToClient(GameRole.Client);
        }
        else
        {
            GameManager.Instance.EggManager.ResetEgg(respawnNum);
        }
    }

    private void Start()
    {
        InitializeScore();
    }

    public void InitializeScore()
    {
        hostScore.Value = 0;
        clientScore.Value = 0;
        //나중에 UI갱신까지 해줄꺼다.
    }
}
