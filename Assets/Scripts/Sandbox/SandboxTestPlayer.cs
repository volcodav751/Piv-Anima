using Game.Combat;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Sandbox
{
    /// <summary>
    /// THROWAWAY test player for the sandbox scene only. Not the real player.
    /// WASD — move, hold LMB — hitscan shot toward the mouse.
    /// Works with both the new Input System and the old Input Manager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class SandboxTestPlayer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float shotCooldown = 0.25f;
        [SerializeField] private float range = 15f;
        [Tooltip("What the shot can hit. The player's own layer is excluded automatically.")]
        [SerializeField] private LayerMask shootMask = ~0;

        private Rigidbody2D _rb;
        private Health _health;
        private LineRenderer _line;
        private Vector2 _move;
        private float _nextShot;
        private float _lineHideAt;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _health = GetComponent<Health>();
            shootMask &= ~(1 << gameObject.layer); // don't shoot yourself
            _line = CreateTracer();
        }

        private void OnEnable()
        {
            _health.Changed += OnHealthChanged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Changed -= OnHealthChanged;
            _health.Died -= OnDied;
        }

        private void Update()
        {
            if (_line.enabled && Time.time >= _lineHideAt) _line.enabled = false;

            if (!_health.IsAlive)
            {
                _move = Vector2.zero;
                return;
            }

            _move = ReadMove();
            if (FireHeld() && Time.time >= _nextShot) Shoot();
        }

        private void FixedUpdate()
        {
            _rb.linearVelocity = _move * moveSpeed;
        }

        private void Shoot()
        {
            _nextShot = Time.time + shotCooldown;

            Vector2 origin = _rb.position;
            Vector2 dir = (MouseWorld() - origin).normalized;
            if (dir == Vector2.zero) return;

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, range, shootMask);
            Vector2 end = hit ? hit.point : origin + dir * range;

            if (hit)
            {
                var target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive)
                    target.TakeDamage(new DamageInfo(damage, dir, gameObject));
            }

            _line.SetPosition(0, origin);
            _line.SetPosition(1, end);
            _line.enabled = true;
            _lineHideAt = Time.time + 0.05f;
        }

        private void OnHealthChanged(int current, int max) => Debug.Log($"[TestPlayer] HP {current}/{max}");

        private void OnDied()
        {
            Debug.Log("[TestPlayer] DEAD — enemies should stop chasing and go back to Idle.");
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.color = Color.gray;
        }

        // ───────────── input (new or old system) ─────────────

        private static Vector2 ReadMove()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            var v = new Vector2(
                (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f),
                (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f));
#else
            var v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
            return Vector2.ClampMagnitude(v, 1f);
        }

        private static bool FireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        private static Vector2 MouseWorld()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Vector3 screen = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
            Vector3 screen = Input.mousePosition;
#endif
            screen.z = -cam.transform.position.z;
            return cam.ScreenToWorldPoint(screen);
        }

        private LineRenderer CreateTracer()
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = 0.05f;
            lr.sortingOrder = 10;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            lr.material = new Material(shader);
            lr.startColor = lr.endColor = new Color(0.6f, 1f, 1f);
            lr.enabled = false;
            return lr;
        }
    }
}
