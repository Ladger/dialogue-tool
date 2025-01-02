using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("For Dialogue Window")]
    [SerializeField] private DSDialoguePlayer player;
    [SerializeField] private int eventID;

    [Header("Movement Properties")]
    [SerializeField] private float playerSpeed;

    

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private bool _hasTalked = false;

    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        Move(moveInput);

        bool isRunning = Mathf.Abs(moveInput) > 0.001f;
        _animator.SetBool("isRunning", isRunning);

        if (Input.GetKeyDown(KeyCode.E))
        {
            player.StartDialogue(_hasTalked, eventID);
            _hasTalked = true;
        }
    }

    private void Move(float dir)
    {
        Vector3 movement = new Vector3(dir, 0f, 0f) * playerSpeed * Time.deltaTime;

        if (dir > 0f)
        {
            _spriteRenderer.flipX = false;
        }
        else if (dir < 0f)
        {
            _spriteRenderer.flipX = true;
        }


        transform.Translate(movement);
    }
}
