using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;
using System.Linq;

using TheoryChord = Melanchall.DryWetMidi.MusicTheory.Chord;

public class MIDILoader : MonoBehaviour
{
    public string midiFilePath = "";
    public GameObject[] noteCubes = new GameObject[12];
    public Color activeColor = Color.green;
    public Color idleColor = Color.gray;
    public float chordStabilityTime = 0.15f;
    public float chordHoldTime = 0.25f;

    private Playback playback;
    private OutputDevice outputDevice;
    private HashSet<int> activeNotes = new HashSet<int>();
    private List<string> currentNotes = new List<string>();
    private string currentChordName = "";
    private string pendingChordName = "";
    private float pendingChordSince;
    private float lastChordTime;
    private bool isPlaying;

    public string CurrentChordName => currentChordName;
    public List<string> CurrentNotes => new List<string>(currentNotes);

    void Start()
    {
        Cleanup();

        foreach (var cube in noteCubes)
        {
            if (cube != null)
                cube.GetComponent<Renderer>().material.color = idleColor;
        }

        string path = System.IO.Path.Combine(Application.dataPath, midiFilePath);

        if (!System.IO.File.Exists(path))
        {
            Debug.LogError("MIDI not found: " + path);
            return;
        }

        var midiFile = MidiFile.Read(path);
        outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
        playback = midiFile.GetPlayback(outputDevice);
        playback.EventPlayed += OnEventPlayed;
        playback.Start();
        isPlaying = true;
        Debug.Log("Playing MIDI...");
    }

    void OnEventPlayed(object sender, MidiEventPlayedEventArgs e)
    {
        if (!isPlaying)
            return;

        lock (activeNotes)
        {
            if (e.Event is NoteOnEvent noteOn && noteOn.Velocity > 0)
                activeNotes.Add(noteOn.NoteNumber);
            else if (e.Event is NoteOffEvent noteOff)
                activeNotes.Remove(noteOff.NoteNumber);
        }
    }

    void Update()
    {
        UpdateCubeColors();
        DetectChord();
    }

    void UpdateCubeColors()
    {
        bool[] isNoteActive = new bool[12];

        lock (activeNotes)
        {
            foreach (int noteNumber in activeNotes)
                isNoteActive[noteNumber % 12] = true;
        }

        for (int i = 0; i < 12 && i < noteCubes.Length; i++)
        {
            if (noteCubes[i] != null)
            {
                Color color = isNoteActive[i] ? activeColor : idleColor;
                noteCubes[i].GetComponent<Renderer>().material.color = color;
            }
        }
    }

    void DetectChord()
    {
        lock (activeNotes)
        {
            if (activeNotes.Count < 3)
            {
                bool holdTimeExpired = Time.time - lastChordTime > chordHoldTime;
                if (currentChordName != "" && holdTimeExpired)
                {
                    currentChordName = "";
                    currentNotes.Clear();
                    pendingChordName = "";
                }
                return;
            }

            try
            {
                var noteNames = activeNotes
                    .Select(n => NoteUtilities.GetNoteName((SevenBitNumber)n))
                    .Distinct()
                    .ToList();

                if (noteNames.Count < 3)
                    return;

                var chordNames = new TheoryChord(noteNames).GetNames();
                var displayNotes = noteNames.Select(n => n.ToString()).ToList();

                string chordName;
                if (chordNames.Any())
                    chordName = chordNames.First() + " (" + string.Join(", ", displayNotes) + ")";
                else
                    chordName = "Unknown (" + string.Join(", ", displayNotes) + ")";

                lastChordTime = Time.time;

                if (chordName != pendingChordName)
                {
                    pendingChordName = chordName;
                    pendingChordSince = Time.time;
                }

                bool isStable = Time.time - pendingChordSince >= chordStabilityTime;
                if (chordName != currentChordName && isStable)
                {
                    currentChordName = chordName;
                    currentNotes = displayNotes;
                    Debug.Log("Chord: " + chordName);
                }
            }
            catch { }
        }
    }

    void OnDisable() => Cleanup();
    void OnDestroy() => Cleanup();
    void OnApplicationQuit() => Cleanup();

    void Cleanup()
    {
        if (playback != null)
        {
            try
            {
                playback.EventPlayed -= OnEventPlayed;
                playback.Stop();
                playback.Dispose();
            }
            catch { }
            playback = null;
        }

        if (outputDevice != null)
        {
            try { outputDevice.Dispose(); }
            catch { }
            outputDevice = null;
        }

        lock (activeNotes)
        {
            activeNotes.Clear();
        }

        isPlaying = false;
        currentChordName = "";
        currentNotes.Clear();
    }
}
