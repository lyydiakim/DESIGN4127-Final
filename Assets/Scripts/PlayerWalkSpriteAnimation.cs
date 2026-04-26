using UnityEngine;

/// <summary>
/// Optional 2-frame walk cycles per character (<see cref="playerController.spriteID"/>).
/// The package <c>playerController</c> only applies a single idle sprite and flips scale for direction;
/// assign two sprites for walking left and two for walking right per character here.
/// </summary>
[DefaultExecutionOrder(50)]
public class PlayerWalkSpriteAnimation : MonoBehaviour
{
    [SerializeField] playerController player;
    [Tooltip("Defaults to player.playerSprite when empty.")]
    [SerializeField] SpriteRenderer spriteRenderer;

    [Header("Walk cycles (index = spriteID / character)")]
    [SerializeField] CharacterWalkFrames[] walkFramesByCharacter;

    [Tooltip("Min horizontal speed (world units/sec) to count as walking.")]
    [SerializeField] float minWalkSpeedX = 0.05f;

    [Tooltip("Seconds per walk frame (two frames per direction).")]
    [SerializeField] float secondsPerFrame = 0.18f;

    Rigidbody2D _rb;
    float _frameTimer;
    int _frameIndex;

    [System.Serializable]
    public class CharacterWalkFrames
    {
        public Sprite walkLeftA;
        public Sprite walkLeftB;
        public Sprite walkRightA;
        public Sprite walkRightB;

        public bool IsComplete =>
            walkLeftA != null && walkLeftB != null && walkRightA != null && walkRightB != null;
    }

    void Awake()
    {
        if (player == null)
            player = GetComponent<playerController>();
        if (spriteRenderer == null && player != null)
            spriteRenderer = player.playerSprite;
        _rb = GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (player == null || spriteRenderer == null || _rb == null)
            return;

        if (walkFramesByCharacter == null || walkFramesByCharacter.Length == 0)
            return;

        int sid = player.spriteID;
        if (sid < 0 || sid >= walkFramesByCharacter.Length)
            return;

        CharacterWalkFrames frames = walkFramesByCharacter[sid];
        if (frames == null || !frames.IsComplete)
            return;

        float vx = _rb.linearVelocity.x;
        bool walking = Mathf.Abs(vx) > minWalkSpeedX;

        if (!walking)
        {
            Sprite idle = ResolveIdleSprite(player);
            if (idle != null)
                spriteRenderer.sprite = idle;
            return;
        }

        bool faceRight = vx > 0f;

        _frameTimer += Time.deltaTime;
        if (_frameTimer >= secondsPerFrame)
        {
            _frameTimer = 0f;
            _frameIndex = 1 - _frameIndex;
        }

        if (faceRight)
            spriteRenderer.sprite = _frameIndex == 0 ? frames.walkRightA : frames.walkRightB;
        else
            spriteRenderer.sprite = _frameIndex == 0 ? frames.walkLeftA : frames.walkLeftB;
    }

    /// <summary>Matches the idle sprite selection used by <see cref="playerController"/> start.</summary>
    static Sprite ResolveIdleSprite(playerController pc)
    {
        if (pc == null)
            return null;

        if (pc.characterSprites != null && pc.characterSprites.Count > 0)
        {
            int id = pc.spriteID;
            if (id >= 0 && id < pc.characterSprites.Count)
                return pc.characterSprites[id];
            if (pc.characterSprites.Count > 0)
                return pc.characterSprites[0];
        }

        Sprite[] fallback = { pc.sprite1, pc.sprite2, pc.sprite3, pc.sprite4 };
        int idx = Mathf.Abs(pc.spriteID % 4);
        for (int i = 0; i < 4; i++)
        {
            int j = (idx + i) % 4;
            if (fallback[j] != null)
                return fallback[j];
        }

        return null;
    }
}
