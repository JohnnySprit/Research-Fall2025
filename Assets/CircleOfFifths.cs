using UnityEngine;

public class SimpleCircleOfFifths : MonoBehaviour
{
    public GameObject[] blocks = new GameObject[12]; // C, G, D, A, E, B, F#, C#, G#, D#, A#, F
    public Color activeColor = Color.green;
    public Color idleColor = Color.gray;
    public MIDILoader midiLoader;

    int last = -1;

    void Start() => ClearAll();

    void Update()
    {
        if (midiLoader == null) return;
        string chord = midiLoader.CurrentChordName;
        if (string.IsNullOrEmpty(chord) || chord == "No Chord")
        {
            ClearAll();
            return;
        }

        string root = chord.Split('(')[0].Trim();
        if (root.Length > 1 && (root[1] == '#' || root[1] == 'b')) root = root[..2];
        else if (root.Length > 0) root = root[..1];

        int idx = GetIndex(root);
        if (idx >= 0 && idx != last)
        {
            Light(idx);
            last = idx;
        }
    }

    int GetIndex(string n)
    {
        n = n.ToUpper();
        if (n == "C") return 0;
        if (n == "G") return 1;
        if (n == "D") return 2;
        if (n == "A") return 3;
        if (n == "E") return 4;
        if (n == "B") return 5;
        if (n.StartsWith("F#") || n.StartsWith("GB")) return 6;
        if (n.StartsWith("C#") || n.StartsWith("DB")) return 7;
        if (n.StartsWith("G#") || n.StartsWith("AB")) return 8;
        if (n.StartsWith("D#") || n.StartsWith("EB")) return 9;
        if (n.StartsWith("A#") || n.StartsWith("BB")) return 10;
        if (n == "F") return 11;
        return -1;
    }

    void Light(int i)
    {
        ClearAll();
        if (i < 0 || i >= blocks.Length || blocks[i] == null) return;
        var r = blocks[i].GetComponent<Renderer>();
        if (r != null) r.material.color = activeColor;
    }

    void ClearAll()
    {
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] == null) continue;
            var r = blocks[i].GetComponent<Renderer>();
            if (r != null) r.material.color = idleColor;
        }
    }
}

