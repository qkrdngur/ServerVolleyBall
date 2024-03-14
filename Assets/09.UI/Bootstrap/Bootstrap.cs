using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Bootstrap : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Label _descLabel;    

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = _uiDocument.rootVisualElement;
        _descLabel = root.Q<Label>("loading-desc");

        ApplicationController.OnMessageEvent += HandleAppMsg;
    }

    private void OnDisable()
    {
        ApplicationController.OnMessageEvent -= HandleAppMsg;
    }

    private void HandleAppMsg(string msg)
    {
        _descLabel.text = msg;
    }
}
