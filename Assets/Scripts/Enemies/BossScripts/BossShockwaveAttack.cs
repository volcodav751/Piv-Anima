using Game.Combat;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossShockwaveAttack : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float interval = 7f;
    [SerializeField, Min(0.1f)] private float warningDuration = 1f;
    [SerializeField, Min(1)] private int damage = 35;
    [SerializeField, Min(0.1f)] private float waveSpeed = 7f;
    [SerializeField, Min(0.1f)] private float maxRadius = 18f;
    [SerializeField, Min(0.01f)] private float waveWidth = 0.2f;
    [Tooltip("Приблизний радіус тіла гравця у світових одиницях.")]
    [SerializeField, Min(0f)] private float playerRadius = 0.3f;
    [SerializeField] private Material waveMaterial;
    [SerializeField] private Color warningColor = new Color(1f, 0.7f, 0.1f, 1f);
    [SerializeField] private Color waveColor = new Color(1f, 0.15f, 0.05f, 1f);

    private enum State { Waiting, Warning, Expanding }
    private State state;
    private float remaining;
    private float radius;
    private float previousGap;
    private bool hitResolved;
    private Vector3 center;
    private Transform target;
    private Health targetHealth;
    private PlayerDash targetDash;
    private LineRenderer ring;
    private Material ownedMaterial;
    private const int Segments = 96;

    public bool IsBusy => state != State.Waiting;

    private void Awake()
    {
        var visual = new GameObject("Boss Shockwave Visual");
        visual.transform.SetParent(transform, false);
        ring = visual.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = Segments;
        ring.sortingLayerName = "Effects";
        ring.sortingOrder = 90;
        if (waveMaterial != null)
            ring.sharedMaterial = waveMaterial;
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                ownedMaterial = new Material(shader);
                ring.sharedMaterial = ownedMaterial;
            }
            else
                Debug.LogWarning("BossShockwaveAttack: assign an unlit Wave Material.", this);
        }
        ring.enabled = false;
    }

    private void OnEnable() => ResetAttack();
    private void OnDisable() => ResetAttack();

    private void OnDestroy()
    {
        if (ownedMaterial != null) Destroy(ownedMaterial);
    }

    public void ResetAttack()
    {
        state = State.Waiting;
        remaining = Mathf.Max(0.1f, interval);
        target = null;
        targetHealth = null;
        targetDash = null;
        if (ring != null) ring.enabled = false;
    }

    // Called only by BossController, keeping all boss attacks coordinated.
    public void Tick(Transform player)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            ResetAttack();
            return;
        }

        if (state == State.Waiting)
        {
            remaining -= Time.deltaTime;
            if (remaining > 0f) return;
            target = player;
            targetHealth = player.GetComponentInParent<Health>();
            targetDash = player.GetComponentInParent<PlayerDash>();
            center = transform.position;
            remaining = Mathf.Max(0.1f, warningDuration);
            state = State.Warning;
            DrawRing(0.3f, warningColor);
            return;
        }

        if (state == State.Warning)
        {
            remaining -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.1f, warningDuration));
            DrawRing(Mathf.Lerp(0.3f, 0.9f, progress), warningColor);
            if (remaining > 0f) return;
            radius = 0f;
            hitResolved = false;
            previousGap = Vector2.Distance(center, target.position);
            state = State.Expanding;
        }

        radius = Mathf.Min(maxRadius, radius + Mathf.Max(0.1f, waveSpeed) * Time.deltaTime);
        DrawRing(radius, waveColor);
        float gap = Vector2.Distance(center, target.position) - radius;
        float tolerance = playerRadius + waveWidth * 0.5f;

        // Swept radial crossing also catches a fast dash or a large frame step.
        bool crossed = Mathf.Min(previousGap, gap) <= tolerance &&
                       Mathf.Max(previousGap, gap) >= -tolerance;
        if (!hitResolved && crossed)
        {
            // Resolve once even when blocked: no delayed hit after the dash ends.
            hitResolved = true;
            if (targetHealth != null && targetHealth.IsAlive &&
                (targetDash == null || !targetDash.IsDashing))
            {
                Vector2 direction = ((Vector2)target.position - (Vector2)center).normalized;
                // Health already respects the player's shield and invulnerability.
                targetHealth.TakeDamage(new DamageInfo(damage, direction, gameObject));
            }
        }
        previousGap = gap;
        if (radius >= maxRadius) ResetAttack();
    }

    private void DrawRing(float size, Color color)
    {
        ring.enabled = true;
        ring.startWidth = ring.endWidth = waveWidth;
        ring.startColor = ring.endColor = color;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * (Mathf.PI * 2f / Segments);
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * size);
        }
    }
}
