using UnityEngine;
using UnityEngine.InputSystem;

// Talking to NPCs: the nearest NPC in range shows its talk prompt, and E starts the conversation.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private float range = 1.4f;

    private PlayerHealth health;
    private Npc current;
    private bool held;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    private void OnDisable() => SetCurrent(null);

    private void Update()
    {
        // Tracks key state manually instead of wasPressedThisFrame so simulated input is caught too.
        bool down = Keyboard.current != null && Keyboard.current[interactKey].isPressed;
        bool pressed = down && !held;
        held = down;

        bool canTalk = !DialogueBox.IsOpen && (health == null || !health.IsDead);
        SetCurrent(canTalk ? FindNearest() : null);

        if (pressed && current != null && DialogueBox.ClosedFrame != Time.frameCount)
        {
            var npc = current;
            SetCurrent(null);
            npc.Talk(transform);
        }
    }

    private Npc FindNearest()
    {
        Npc nearest = null;
        float best = range * range;
        Vector2 position = transform.position;
        foreach (var npc in Npc.All)
        {
            float distance = ((Vector2)npc.transform.position - position).sqrMagnitude;
            if (distance <= best)
            {
                best = distance;
                nearest = npc;
            }
        }
        return nearest;
    }

    private void SetCurrent(Npc npc)
    {
        if (npc == current) return;
        if (current != null) current.SetPromptVisible(false);
        current = npc;
        if (current != null) current.SetPromptVisible(true);
    }
}
