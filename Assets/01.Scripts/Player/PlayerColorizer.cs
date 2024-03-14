using Unity.Netcode;
using UnityEngine;

public class PlayerColorizer : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public void SetColor(ushort idx)
    {
        SetColorClientRpc(idx);
    }

    [ClientRpc]
    public void SetColorClientRpc(ushort idx)
    {
        //_spriteRenderer.color = GameManager.Instance.slimeColors[idx];
    }
}
