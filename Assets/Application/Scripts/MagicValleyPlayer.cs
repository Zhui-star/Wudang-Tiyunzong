namespace Dreamteck.Forever
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Dreamteck.Splines;
    using HTLibrary.Application;

    public class MagicValleyPlayer : MonoBehaviour
    {
        public float initialSpeed = 5f;
        public float endSpeed = 5f;
        public float acceleration = 0.1f;
        public float initialJumpForce = 10f;
        public float continuousJumpForce = 1f;
        public float gravityForce = 20f;
        public float dieHeight = 5f;
        float speed = 0f;
        public LayerMask groundMask;
        public Transform cameraTransform;
        Rigidbody2D rb;
        bool inAir = true;
        Transform trs;
        Bounds colliderBounds = new Bounds();
        bool dead = false;
        bool ready = false;
        ProjectedPlayer projector;
        Vector3 camOffset = Vector3.zero;

        InputManager inputManager;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            projector = GetComponent<ProjectedPlayer>();
            trs = transform;
            colliderBounds = GetComponent<CapsuleCollider2D>().bounds;
            colliderBounds.center = trs.InverseTransformPoint(colliderBounds.center);
            colliderBounds.size = trs.InverseTransformVector(colliderBounds.size);
            camOffset = cameraTransform.position - trs.position;
            EndScreen.onRestartClicked += OnRestart;

            rb.gravityScale = 0;
        }

        private void Start()
        {
            inputManager = InputManager.Instance;
        }

        private void OnEnable()
        {
            RestartGameEvent.Register(OnRestart);
            ReviveGameEvent.Register(Revive);
            GameManager.Instance.LoadNextEvent += OnRestart;
        }

        private void OnDisable()
        {
            RestartGameEvent.UnRegister(OnRestart);
            ReviveGameEvent.UnRegister(Revive);
            GameManager.Instance.LoadNextEvent -= OnRestart;
        }

        void OnRestart()
        {
            /*  LevelGenerator.instance.Restart();
              ready = false;
              GameManager.Instance._GameState = GameState.Game;*/


            dead = false;
            rb.velocity = Vector2.zero;
            rb.gravityScale = 2.5f;
            trs.rotation = Quaternion.identity;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            GetComponent<CapsuleCollider2D>().enabled = true;
            trs.position = LevelGenerator.instance.segments[0].customEntrance.position + Vector3.up * colliderBounds.size.y;
            rb.AddForce(Vector2.up * initialJumpForce * 0.5f, ForceMode2D.Impulse);
            speed = initialSpeed;
            GameManager.Instance._GameState = GameState.Game;
        }

        private void Update()
        {
            if (!ready)
            {
                if (LevelGenerator.instance.ready)
                {
                    Revive();
                    ready = true;
                }
            }
            if (dead&&GameManager.Instance._GameState!=GameState.Game) return;

            if (!inAir && inputManager.jumpButton.State.CurrentState==ButtonStates.Down)
            {
                rb.AddForce(Vector2.up * initialJumpForce, ForceMode2D.Impulse);
            }
            if (cameraTransform.position.y - transform.position.y >= dieHeight) Die();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (dead) return;
            if(Vector2.Dot(collision.contacts[0].normal, Vector2.left) >= 0.9f)
            {
                if(Physics2D.OverlapArea(trs.position + Vector3.up * colliderBounds.size.y * 0.4f + Vector3.right * colliderBounds.size.x * 0.55f,
                trs.position + Vector3.down * colliderBounds.size.y * 0.4f + Vector3.right * colliderBounds.size.x * 0.55f, groundMask)) Die(); //Die if we hit something in front
            }
        }

        void Revive()
        {
            dead = false;
            rb.gravityScale = 2.5f;
            rb.velocity = Vector2.zero;
            trs.rotation = Quaternion.identity;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            GetComponent<CapsuleCollider2D>().enabled = true;
            trs.position =projector.GetLastSegement() .customEntrance.position + Vector3.up * colliderBounds.size.y;
            rb.AddForce(Vector2.up * initialJumpForce * 0.5f, ForceMode2D.Impulse);
            speed = initialSpeed;
            GameManager.Instance._GameState = GameState.Game;
        }

        void Die()
        {
            if (dead) return;
            dead = true;
            rb.velocity = Vector2.zero;
            rb.AddForce((Vector2.up + Vector2.left * 0.2f) * initialJumpForce * 2f, ForceMode2D.Impulse);
            rb.constraints = RigidbodyConstraints2D.None;
            rb.AddTorque(45f);
            GetComponent<CapsuleCollider2D>().enabled = false;
            GameManager.Instance._GameState = GameState.GameOver;
        }

        private void FixedUpdate()
        {
            if (dead) return;
            speed = Mathf.MoveTowards(speed, endSpeed, Time.deltaTime * acceleration);
            inAir = !Physics2D.OverlapArea(trs.position + Vector3.down * colliderBounds.size.y * 0.5f + Vector3.left * colliderBounds.size.x * 0.5f,
                trs.position + Vector3.down * colliderBounds.size.y * 0.5f + Vector3.right * colliderBounds.size.x * 0.5f, groundMask);

            Vector2 velocity = rb.velocity;
            velocity.x = speed;
            rb.velocity = velocity;
            rb.AddForce(Vector2.down * gravityForce, ForceMode2D.Force);
            if (inAir && velocity.y > 0f)
            {
                if (inputManager.jumpButton.State.CurrentState==ButtonStates.Pressed)
                    rb.AddForce(Vector2.up * continuousJumpForce, ForceMode2D.Force);
            }
        }

        private void LateUpdate()
        {
            cameraTransform.position = projector.result.position + camOffset;
        }
    }
}
