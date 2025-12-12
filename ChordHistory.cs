using UnityEngine;
using System.Collections.Generic;
using TMPro;

[ExecuteAlways]
public class ChordHistory : MonoBehaviour
{
    public MIDILoader midiLoader;
    public int maxHistory = 6;
    public float rowSpacing = 1.2f;
    public float blockSize = 0.8f;
    public float blockSpacing = 0.9f;
    public Color activeColor = new Color(0.2f, 0.9f, 0.4f);
    public Color inactiveColor = new Color(0.15f, 0.15f, 0.15f);

    private List<ChordEntry> history = new List<ChordEntry>();
    private List<HistoryRow> rows = new List<HistoryRow>();
    private string lastChordName = "";
    private string lastAddedChord = "";
    private float lastAddedTime;
    private bool gridCreated;

    private const float MIN_INTERVAL = 0.15f;
    private static readonly string[] NOTE_NAMES = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    class ChordEntry
    {
        public string name;
        public HashSet<int> notes = new HashSet<int>();
    }

    class HistoryRow
    {
        public GameObject container;
        public GameObject[] blocks = new GameObject[12];
        public TextMeshPro label;
    }

    void OnEnable()
    {
        lastChordName = "";
        lastAddedChord = "";
        lastAddedTime = -MIN_INTERVAL;
        history.Clear();

        if (!gridCreated)
        {
            CreateGrid();
            gridCreated = true;
        }
    }

    void CreateGrid()
    {
        ClearGrid();
        DestroyAllChildren();
        CreateHeader();
        CreateHistoryRows();
    }

    void DestroyAllChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    void CreateHeader()
    {
        GameObject header = new GameObject("Header");
        header.transform.SetParent(transform);
        header.transform.localPosition = new Vector3(0, rowSpacing, 0);

        for (int i = 0; i < 12; i++)
        {
            GameObject labelObj = new GameObject(NOTE_NAMES[i]);
            labelObj.transform.SetParent(header.transform);
            labelObj.transform.localPosition = new Vector3(i * blockSpacing, 0, 0);

            TextMeshPro text = labelObj.AddComponent<TextMeshPro>();
            text.text = NOTE_NAMES[i];
            text.fontSize = 10;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }
    }

    void CreateHistoryRows()
    {
        for (int rowIndex = 0; rowIndex < maxHistory; rowIndex++)
        {
            HistoryRow row = new HistoryRow();

            row.container = new GameObject("Row_" + rowIndex);
            row.container.transform.SetParent(transform);
            row.container.transform.localPosition = new Vector3(0, -rowIndex * rowSpacing, 0);

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.container.transform);
            labelObj.transform.localPosition = new Vector3(-11f, 0, 0);
            row.label = labelObj.AddComponent<TextMeshPro>();
            row.label.fontSize = 6;
            row.label.alignment = TextAlignmentOptions.Right;

            for (int noteIndex = 0; noteIndex < 12; noteIndex++)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = NOTE_NAMES[noteIndex];
                block.transform.SetParent(row.container.transform);
                block.transform.localPosition = new Vector3(noteIndex * blockSpacing, 0, 0);
                block.transform.localScale = Vector3.one * blockSize;

                if (Application.isPlaying)
                    block.GetComponent<Renderer>().material.color = inactiveColor;

                row.blocks[noteIndex] = block;
            }

            rows.Add(row);
        }
    }

    void Update()
    {
        if (!Application.isPlaying || midiLoader == null)
            return;

        CheckForNewChord();
        UpdateVisuals();
    }

    void CheckForNewChord()
    {
        string chord = midiLoader.CurrentChordName;

        if (string.IsNullOrEmpty(chord))
        {
            lastChordName = "";
            return;
        }

        if (chord == lastChordName)
            return;

        string chordName = chord.Split('(')[0].Trim();
        bool isDifferentChord = chordName != lastAddedChord;
        bool enoughTimePassed = Time.time - lastAddedTime >= MIN_INTERVAL;

        if (isDifferentChord && enoughTimePassed)
        {
            AddChordToHistory(chordName, midiLoader.CurrentNotes);
            lastAddedChord = chordName;
            lastAddedTime = Time.time;
        }

        lastChordName = chord;
    }

    void AddChordToHistory(string chordName, List<string> notes)
    {
        ChordEntry entry = new ChordEntry();
        entry.name = chordName;

        foreach (string note in notes)
        {
            int noteIndex = GetNoteIndex(note);
            if (noteIndex >= 0)
                entry.notes.Add(noteIndex);
        }

        history.Insert(0, entry);

        while (history.Count > maxHistory)
            history.RemoveAt(history.Count - 1);
    }

    int GetNoteIndex(string note)
    {
        note = note.ToUpper().Trim();

        // Handle flat notes
        switch (note)
        {
            case "DB": return 1;
            case "EB": return 3;
            case "GB": return 6;
            case "AB": return 8;
            case "BB": return 10;
            case "E#": return 5;
            case "B#": return 0;
        }

        // Match against standard note names
        for (int i = 0; i < NOTE_NAMES.Length; i++)
        {
            if (note == NOTE_NAMES[i])
                return i;
        }

        return -1;
    }

    void UpdateVisuals()
    {
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            HistoryRow row = rows[rowIndex];

            if (rowIndex < history.Count)
                UpdateRowWithChord(row, history[rowIndex], rowIndex);
            else
                ClearRow(row);
        }
    }

    void UpdateRowWithChord(HistoryRow row, ChordEntry chord, int rowIndex)
    {
        float fade = 1f - rowIndex * 0.12f;

        row.label.text = chord.name;
        row.label.color = new Color(1, 1, 1, fade);

        for (int noteIndex = 0; noteIndex < 12; noteIndex++)
        {
            Color color;
            if (chord.notes.Contains(noteIndex))
            {
                color = activeColor * fade;
                color.a = 1;
            }
            else
            {
                color = inactiveColor;
            }

            row.blocks[noteIndex].GetComponent<Renderer>().material.color = color;
        }
    }

    void ClearRow(HistoryRow row)
    {
        row.label.text = "";

        for (int noteIndex = 0; noteIndex < 12; noteIndex++)
            row.blocks[noteIndex].GetComponent<Renderer>().material.color = inactiveColor;
    }

    void ClearGrid()
    {
        foreach (HistoryRow row in rows)
        {
            if (row.container != null)
            {
                if (Application.isPlaying)
                    Destroy(row.container);
                else
                    DestroyImmediate(row.container);
            }
        }
        rows.Clear();
    }

    void OnDisable()
    {
        ClearGrid();
        history.Clear();
        gridCreated = false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (gridCreated && rows.Count > 0)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    gridCreated = false;
                    CreateGrid();
                    gridCreated = true;
                }
            };
        }
    }
#endif
}
