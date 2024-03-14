using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class Ball : NetworkBehaviour
{
    [SerializeField] private GameObject _firecracker;
    [SerializeField] private float _bounceVelocity;
    [SerializeField] private float _waitingTime = 2f;
    private Rigidbody2D _rigidbody;

    public static Action OnHit;
    public static Action OnFallInWater;

    private const string waterTag = "Water";
    private GameObject spawnedObject;

    private bool _isAlive = true;
    private float _gravityScale = 0;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _gravityScale = _rigidbody.gravityScale;
        _rigidbody.gravityScale = 0;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(WaitAndFall());
    }


    private IEnumerator WaitAndFall()
    {
        yield return new WaitForSeconds(_waitingTime);
        Destroy(spawnedObject);
        _rigidbody.gravityScale = _gravityScale;
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;
        if (!_isAlive) return;


        if (collision.collider.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            if (collision.contacts.Length > 0)
            {
                Vector2 normal = collision.GetContact(0).normal;
                Bounce(normal);
                //OnHit?.Invoke(); // 턴을 넘기기 위해서 이걸 발생시킨다.
            }
        }

    }

    private void Bounce(Vector2 normal)
    {
        _rigidbody.velocity = normal * _bounceVelocity;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (!_isAlive) return;

        if (collision.CompareTag(waterTag))
        {
            SpawnObject();
            _isAlive = false;
            OnFallInWater?.Invoke();
        }
    }

    private void SpawnObject()
    {
        for (int i = 0; i < GameManager.Instance._firecrackerPos.Length; i++)
        {
            spawnedObject = Instantiate(_firecracker, GameManager.Instance._firecrackerPos[i].position, Quaternion.identity);
            spawnedObject.GetComponent<NetworkObject>().Spawn();
            spawnedObject.GetComponent<ParticleSystem>().Play();
        }
    }

    public void ResetToStartPosition(Vector3 eggStartPosition)
    {
        _rigidbody.velocity = Vector2.zero;
        _rigidbody.angularVelocity = 0;
        _rigidbody.gravityScale = 0;
        transform.SetPositionAndRotation(eggStartPosition, Quaternion.identity);
        _isAlive = true;
        StartCoroutine(WaitAndFall());
    }
}
