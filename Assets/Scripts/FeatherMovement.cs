using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FeatherMovement : MonoBehaviour
{
    [Header("Feather Position")]
    private Transform feather;

    [Header("Player Position")]
    public GameObject player;
    public float radius;
    public float featherDelay;

    private PlayerMovement playerMovement;

    private int playerPositionIndex = 2;
    private Transform playerPosition;
    private int playerWSIndex = 3;
    private Transform playerWS;

    [Header("Feather Target")]
    private Transform target;
    private BoxCollider2D featherCollider;

    [Header("Feather State")]
    private bool isMoving;

    private void Start()
    {
        feather = GetComponent<Transform>();
        featherCollider = GetComponent<BoxCollider2D>();

        playerMovement = player.GetComponent<PlayerMovement>();

        playerPosition = player.transform.GetChild(playerPositionIndex);
        playerWS = player.transform.GetChild(playerWSIndex);

        target = playerPosition;
        isMoving = false;
    }

    private void Update()
    {
        CheckForTarget();
        WallSlideManager();
        FacingManager();
    }
    //FacingManager
    private void FacingManager()
    {
        if (target == playerPosition)
        {
            feather.rotation = Quaternion.Euler(0f, 90f - (playerMovement.facingDirection * 90f), 0f);
        }
    }
    //Checks for the position of the target and moves if it's too far
    private void CheckForTarget()
    {
        if (featherCollider == Physics2D.OverlapCircle(target.position, radius))
        {
            return;
        }
        else
        {
            if (!isMoving)
            {
                MoveToTarget();
            }
        }
    }
    //Moves to the assigned target
    private void MoveToTarget()
    {
        feather.position = Vector2.Lerp(feather.position, target.position, featherDelay * Time.deltaTime);
    }
    //Receives a target and changes the current target to the new one
    private void ChangeTarget(Transform newTarget)
    {
        target = newTarget;
    }
    //UPGRADE Manages the feather position during a wall slide
    private void WallSlideManager()
    {
        if (playerMovement.isWallSliding)
        {
            StartCoroutine("PlayerStartedWSCoroutine");
        }
    }
    private IEnumerator PlayerStartedWSCoroutine()
    {
        feather.rotation = Quaternion.Euler(0f, 90f + (playerMovement.facingDirection * 90f), 0f);
        ChangeTarget(playerWS);
        while (playerMovement.isWallSliding)
        {
            yield return null;
        }
        StartCoroutine("PlayerEndedWSCoroutine");
        StopCoroutine("PlayerStartedWSCoroutine");
    }
    private IEnumerator PlayerEndedWSCoroutine()
    {
        feather.rotation = Quaternion.Euler(0f, 90f - (playerMovement.facingDirection * 90f), 0f);
        ChangeTarget(playerPosition);
        StopCoroutine("PlayerEndedWSCoroutine");
        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(playerPosition.position, radius);
        Gizmos.DrawWireSphere(playerWS.position, radius);
    }
}
