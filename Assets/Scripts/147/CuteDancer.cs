using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the 6 dance clips on a humanoid Cute Girl.
/// - Auto-cycles through all dances (loop).
/// - Press Tab (or click the on-screen button) to switch to the next dance.
/// - Press 1..6 to jump to a specific dance.
/// </summary>
public class CuteDancer : MonoBehaviour
{
    [Tooltip("Names of the animation clips (as imported from the FBX).")]
    public string[] danceClips = new string[]
    {
        "Cute_Arms_Hip_Hop_Dance",
        "Cute_Booty_Hip_Hop_Dance",
        "Cute_Dancing_Twerk",
        "Cute_Hip_Hop_Dancing",
        "Cute_Hip_Hop_Dancing_1",
        "Cute_Rumba_Dancing",
    };

    [Tooltip("Auto-cycle through all dances.")]
    public bool autoCycle = true;
    [Tooltip("Seconds to play each dance before switching (0 = play full clip).")]
    public float switchAfter = 0f;
    public KeyCode cycleKey = KeyCode.Tab;
    public bool createUiButton = true;

    Animator animator;
    int index = 0;
    Coroutine cycleRoutine;
    GUIStyle buttonStyle;
    float nextSwitchTime;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("CuteDancer: no Animator found on " + gameObject.name);
            enabled = false;
            return;
        }
        Play(index);
    }

    void Update()
    {
        if (Input.GetKeyDown(cycleKey))
            Cycle();

        if (Input.GetKeyDown(KeyCode.Alpha1)) JumpTo(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) JumpTo(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) JumpTo(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) JumpTo(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) JumpTo(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) JumpTo(5);

        if (autoCycle && switchAfter > 0f && Time.time >= nextSwitchTime)
        {
            Cycle();
        }
    }

    public void Cycle()
    {
        index = (index + 1) % danceClips.Length;
        Play(index);
    }

    public void JumpTo(int i)
    {
        if (i < 0 || i >= danceClips.Length) return;
        index = i;
        Play(index);
    }

    void Play(int i)
    {
        string clip = danceClips[i];
        if (animator.HasState(0, Animator.StringToHash(clip)))
        {
            animator.CrossFadeInFixedTime(clip, 0.15f, 0, 0f);
        }
        else
        {
            animator.Play(clip, 0, 0f);
        }
        nextSwitchTime = Time.time + (switchAfter > 0f ? switchAfter : 9999f);
        Debug.Log($"DANCE_NOW {clip}");
    }

    void OnGUI()
    {
        if (!createUiButton) return;
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 16;
            buttonStyle.padding = new RectOffset(16, 16, 10, 10);
        }
        float y = 10;
        for (int i = 0; i < danceClips.Length; i++)
        {
            string label = (i == index ? "▶ " : "") + ShortName(danceClips[i]);
            if (GUI.Button(new Rect(Screen.width - 190, y, 180, 30), label, buttonStyle))
                JumpTo(i);
            y += 36;
        }
    }

    string ShortName(string full)
    {
        return full.Replace("Cute_", "").Replace("_", " ");
    }
}
