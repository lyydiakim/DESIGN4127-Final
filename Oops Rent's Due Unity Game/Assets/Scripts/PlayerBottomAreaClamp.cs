using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps the player inside <see cref="PlayerMovementBoundsDefinition"/> after physics.
/// Station triggers still work because only the final position is clamped.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(100)]
public class PlayerBottomAreaClamp : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Coroutine _co;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (_co == null)
            _co = StartCoroutine(ClampAfterPhysics());
    }

    private void OnDisable()
    {
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }
    }

    private IEnumerator ClampAfterPhysics()
    {
        var wait = new WaitForFixedUpdate();
        while (enabled)
        {
            yield return wait;
            var def = PlayerMovementBoundsDefinition.Instance;
            if (def == null || !def.TryGetBounds(out float minX, out float maxX, out float minY, out float maxY))
                continue;

            Vector2 p = _rb.position;
            Vector2 c = new Vector2(Mathf.Clamp(p.x, minX, maxX), Mathf.Clamp(p.y, minY, maxY));
            if ((c - p).sqrMagnitude < 1e-8f) continue;

            _rb.position = c;
            Vector2 v = _rb.linearVelocity;
            if (Mathf.Abs(p.x - c.x) > 0.0001f) v.x = 0f;
            if (Mathf.Abs(p.y - c.y) > 0.0001f) v.y = 0f;
            _rb.linearVelocity = v;
        }
    }
}
