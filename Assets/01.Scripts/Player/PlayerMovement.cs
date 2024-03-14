using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("참조값들")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("셋팅값들")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _jumpPower;

    private bool _jumped = true;
    private Vector2 _playerStartPos;
    private Vector2 _playerSavePos;
    private float _screenWidth;

    private Rect _camRect;
    private Vector2 _spriteSize;

    private float nextXPos = 0;

    private void OnEnable()
    {
        _playerSavePos = transform.position;
        _screenWidth = Screen.width;

        Camera mainCam = Camera.main;

        float orthoSize = mainCam.orthographicSize;
        float ratio = mainCam.aspect;

        float halfWidth = orthoSize * ratio;
        float halfHeight = orthoSize;

        Vector2 topLeft = (Vector2)mainCam.transform.position
                            + new Vector2(-halfWidth, -halfHeight);

        _camRect = new Rect(topLeft.x, topLeft.y, halfWidth * 2, halfHeight * 2);
        _spriteSize = _spriteRenderer.size;

        _inputReader.OnJumpEvent += HandleJump;
    }

    private void OnDisable()
    {
        _inputReader.OnJumpEvent -= HandleJump;
    }


    void Update()
    {
        if (!IsOwner) return;

        _playerStartPos = transform.position;
        PlayerMove();
    }

    private void PlayerMove()
    {
        float xDiff = _inputReader.MovePosition.x;

        xDiff /= (_screenWidth / 10); //0 ~ 1값을 알 수 있다.
        xDiff *= _moveSpeed;
        float halfSprite = _spriteSize.x * 0.5f;

        if (IsServer)
        {
            if (IsOwner)
                nextXPos = Mathf.Clamp(_playerStartPos.x + xDiff, -8, -0.5f);
        }
        else if (IsClient)
        {
            if (IsOwner)
                nextXPos = Mathf.Clamp(_playerStartPos.x + xDiff,0.5f, 8);
        }

        transform.position = new Vector3(nextXPos, transform.position.y);
    }

    private void HandleJump(bool value)
    {
        if (_jumped)
            StartCoroutine(OnJump());
    }

    IEnumerator OnJump()
    {
        float yDiff = .01f;
        yDiff *= _jumpPower;
        float halfSprite = _spriteSize.y * 0.5f;
        float nextYPos = 0;

        _jumped = false;

        //점프시 위로
        while (nextYPos <= .05f)
        {

            nextYPos = Mathf.Clamp(
                transform.position.y + yDiff,
                _camRect.yMin + halfSprite,
                _camRect.yMax - halfSprite);

            transform.position = new Vector3(transform.position.x, nextYPos);

            yield return null;
        }

        yield return new WaitForSeconds(0.07f);

        //아래로
        while (-3 <= transform.position.y)
        {
            nextYPos = Mathf.Clamp(
               transform.position.y - yDiff,
               _camRect.yMin + halfSprite,
               _camRect.yMax - halfSprite);

            transform.position = new Vector3(transform.position.x, nextYPos);

            yield return null;
        }

        _jumped = true;
    }
}
