using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Bottom-of-screen dialogue window: speaker portrait, name and typewritten lines.
// E, Space, Enter or a click first finishes the line being typed, then moves to the next one.
// While it's open the player's movement, attacks and spells are paused.
public class DialogueBox : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject portraitFrame;
    [SerializeField] private Image portrait;
    [SerializeField] private Text nameLabel;
    [SerializeField] private Text body;
    [SerializeField] private RectTransform continueArrow;
    [SerializeField] private float charactersPerSecond = 45f;

    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("Player components switched off while a conversation is open.")]
    [SerializeField] private Behaviour[] pausedWhileOpen;

    public static DialogueBox Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;
    // Frame the last conversation closed on, so the key that closed it doesn't reopen it.
    public static int ClosedFrame { get; private set; } = -1;

    private string[] lines;
    private int lineIndex;
    private float shownCharacters;
    private Action onClosed;
    private bool open;
    private bool held;
    private bool[] wasEnabled;
    private Vector2 arrowRest;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        if (continueArrow != null) arrowRest = continueArrow.anchoredPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string speaker, Sprite face, string[] conversation, Action closed = null)
    {
        if (conversation == null || conversation.Length == 0)
        {
            closed?.Invoke();
            return;
        }

        lines = conversation;
        onClosed = closed;
        nameLabel.text = speaker;
        portrait.sprite = face;
        portraitFrame.SetActive(face != null);
        open = true;
        // The key that opened the conversation is still down; wait for it to be released.
        held = true;

        wasEnabled = new bool[pausedWhileOpen.Length];
        for (int i = 0; i < pausedWhileOpen.Length; i++)
        {
            wasEnabled[i] = pausedWhileOpen[i].enabled;
            pausedWhileOpen[i].enabled = false;
        }

        panel.SetActive(true);
        StartLine(0);
    }

    private void Update()
    {
        if (!open) return;

        var line = lines[lineIndex];
        if (shownCharacters < line.Length)
        {
            shownCharacters += charactersPerSecond * Time.deltaTime;
            body.text = line.Substring(0, Mathf.Min(line.Length, Mathf.FloorToInt(shownCharacters)));
        }

        bool lineDone = shownCharacters >= line.Length;
        continueArrow.gameObject.SetActive(lineDone);
        if (lineDone) continueArrow.anchoredPosition = arrowRest + Vector2.up * (Mathf.Sin(Time.time * 6f) * 4f);

        if (!AdvancePressed()) return;
        if (!lineDone)
        {
            shownCharacters = line.Length;
            body.text = line;
        }
        else if (lineIndex + 1 < lines.Length)
        {
            StartLine(lineIndex + 1);
        }
        else
        {
            Close();
        }
    }

    private void StartLine(int index)
    {
        lineIndex = index;
        shownCharacters = 0f;
        body.text = "";
    }

    private void Close()
    {
        open = false;
        ClosedFrame = Time.frameCount;
        panel.SetActive(false);

        bool dead = playerHealth != null && playerHealth.IsDead;
        for (int i = 0; i < pausedWhileOpen.Length; i++)
            if (wasEnabled[i] && !dead) pausedWhileOpen[i].enabled = true;

        var closed = onClosed;
        onClosed = null;
        closed?.Invoke();
    }

    // Tracks button state manually instead of wasPressedThisFrame so simulated input is caught too.
    private bool AdvancePressed()
    {
        var keyboard = Keyboard.current;
        bool down = (keyboard != null && (keyboard.eKey.isPressed || keyboard.spaceKey.isPressed || keyboard.enterKey.isPressed))
            || (Mouse.current != null && Mouse.current.leftButton.isPressed);
        bool pressed = down && !held;
        held = down;
        return pressed;
    }
}
