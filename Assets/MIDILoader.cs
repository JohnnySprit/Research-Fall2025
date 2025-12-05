using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;
using System.Linq;

// Aliases to avoid ambiguity
using MidiChord = Melanchall.DryWetMidi.Interaction.Chord;
using TheoryChord = Melanchall.DryWetMidi.MusicTheory.Chord;
using SevenBitNumber = Melanchall.DryWetMidi.Common.SevenBitNumber;

public class MIDILoader : MonoBehaviour
{
    public string midiFilePath = "";
    public GameObject[] noteCubes = new GameObject[12];
    public Color activeColor = Color.green;
    public Color idleColor = Color.gray;

    private Playback playback;
    private MidiFile midiFile;
    private OutputDevice outputDevice;
    private HashSet<int> activeNotes = new HashSet<int>();
    private string currentChordName = "";
    private bool isPlaying = false;

    public float chordStabilityTime = 0.1f;

    private string pendingChordName = "";
    private float pendingChordSince = 0f;

    public string CurrentChordName => currentChordName;


    void Start()
    {
        Cleanup();
        InitializeVisuals();

        string path = System.IO.Path.Combine(Application.dataPath, midiFilePath);

        if (!System.IO.File.Exists(path))
        {
            Debug.LogError($"MIDI file not found at: {path}");
            return;
        }

        midiFile = MidiFile.Read(path);

        // Get output device
        outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
        playback = midiFile.GetPlayback(outputDevice);
        playback.EventPlayed += OnEventPlayed;
        playback.Start();
        isPlaying = true;
        Debug.Log("Playing MIDI...");

    }

    void InitializeVisuals()
    {
        // Sets all of the cubes to idle color (which is gray)
        for (int i = 0; i < noteCubes.Length; i++)
        {
            if (noteCubes[i] != null)
            {
                var renderer = noteCubes[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = idleColor;
                }
            }
        }

    }

    void OnEventPlayed(object sender, MidiEventPlayedEventArgs e)
    {
        if (!isPlaying) return; // Safety check

        // Tracks active notesn
        if (e.Event is NoteOnEvent noteOn && noteOn.Velocity > 0)
        {
            lock (activeNotes)
            {
                activeNotes.Add(noteOn.NoteNumber);
            }
        }
        else if (e.Event is NoteOffEvent noteOff)
        {
            lock (activeNotes)
            {
                activeNotes.Remove(noteOff.NoteNumber);
            }
        }
    }

    void Update()
    {
        // Update visual elements every frame
        UpdateNoteCubes();
        DetectAndDisplayCurrentChord();
    }

    void UpdateNoteCubes()
    {
        bool[] pitchClassActive = new bool[12];

        lock (activeNotes)
        {
            foreach (var note in activeNotes)
            {
                int pitchClass = note % 12;
                pitchClassActive[pitchClass] = true;
            }
        }

        // Update cube colors
        for (int i = 0; i < 12 && i < noteCubes.Length; i++)
        {
            if (noteCubes[i] != null)
            {
                var renderer = noteCubes[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = pitchClassActive[i] ? activeColor : idleColor;
                }
            }
        }
    }

    void DetectAndDisplayCurrentChord()
    {
        lock (activeNotes)
        {
            // Need enough notes to form a chord
            if (activeNotes.Count < 3)
            {
                if (currentChordName != "")
                {
                    currentChordName = "";
                }
                return;
            }

            try
            {
                // Get note names from active MIDI notes
                var noteNames = activeNotes
                    .Select(n => NoteUtilities.GetNoteName((SevenBitNumber)n))
                    .Distinct()
                    .ToList();

                if (noteNames.Count < 2) return;

                // Create a music theory chord
                var theoryChord = new TheoryChord(noteNames);

                // Try to get standard chord names
                var chordNames = theoryChord.GetNames();
                var noteNamesDisplay = noteNames.Select(n => n.ToString()).ToList();

                string chordName;
                if (chordNames.Any())
                {
                    chordName = $"{chordNames.First()} ({string.Join(", ", noteNamesDisplay)})";
                }
                else
                {
                    chordName = $"Unknown ({string.Join(", ", noteNamesDisplay)})";
                }

                // Debounce chord changes to avoid randomly flickering around on weak beats
                if (chordName != pendingChordName)
                {
                    pendingChordName = chordName;
                    pendingChordSince = Time.time;
                }

                if (chordName != currentChordName && (Time.time - pendingChordSince) >= chordStabilityTime)
                {
                    currentChordName = chordName;
                    Debug.Log($"Current Chord: {chordName}");
                }
            }
            catch
            {
            // If chord creation fails, show active notes as fallback
            var noteNames = activeNotes
                .Select(n => NoteUtilities.GetNoteName((SevenBitNumber)n).ToString())
                .ToList();
            string fallback = string.Join(", ", noteNames);
            if (fallback != currentChordName)
            {
                currentChordName = fallback;
            }
            }
        }
    }

    void OnDisable()
    {
        Cleanup();
    }

    void OnDestroy()
    {
        Cleanup();
    }

    void OnApplicationQuit()
    {
        Cleanup();
    }

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
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error disposing playback: {e.Message}");
            }
            playback = null;
        }

        if (outputDevice != null)
        {
            try
            {
                outputDevice.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error disposing output device: {e.Message}");
            }
            outputDevice = null;
        }

        lock (activeNotes)
        {
            activeNotes.Clear();
        }

        isPlaying = false;
        currentChordName = "";
    }
}
