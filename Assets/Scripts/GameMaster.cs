using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public InputManager playerInput;
    public Transform playerPosition;

    private void Awake()
    {
        playerInput = new InputManager();
        playerInput.UI.Exit.started += ctx => Application.Quit();
    }
    private void Update()
    {
        if(playerPosition.position.y <= -5f)
        {
            playerPosition.position = new Vector3(0f,0f,0f);
        }
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }
    private void OnDisable()
    {
        playerInput.Disable();
    }
}
