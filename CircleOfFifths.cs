using UnityEngine;

public class CircleOfFifths : MonoBehaviour
{
    // Blocks arranged in circle of fifths order: C, G, D, A, E, B, F#, C#, G#, D#, A#, F
    public GameObject[] blocks = new GameObject[12];
    public Color activeColor = Color.green;
    public Color previousColor = new Color(0.3f, 0.6f, 1f);
    public Color idleColor = Color.gray;
    public Color arrowColor = new Color(1f, 0.5f, 0f);
    public float arrowWidth = 0.15f;
    public MIDILoader midiLoader;

    private int currentIndex = -1;
    private int previousIndex = -1;
    private LineRenderer arrowLine;
    private GameObject arrowHead;

    void Start()
    {
        SetAllBlockColors(idleColor);
        CreateArrow();
    }

    void CreateArrow()
    {
        GameObject arrowContainer = new GameObject("Arrow");
        arrowContainer.transform.SetParent(transform);

        // Create line
        arrowLine = arrowContainer.AddComponent<LineRenderer>();
        arrowLine.material = new Material(Shader.Find("Sprites/Default"));
        arrowLine.startColor = arrowColor;
        arrowLine.endColor = arrowColor;
        arrowLine.startWidth = arrowWidth;
        arrowLine.endWidth = arrowWidth * 0.5f;
        arrowLine.positionCount = 2;
        arrowLine.useWorldSpace = true;
        arrowLine.enabled = false;

        // Create arrowhead
        arrowHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrowHead.name = "Head";
        arrowHead.transform.SetParent(arrowContainer.transform);
        arrowHead.transform.localScale = new Vector3(arrowWidth * 2, arrowWidth * 2, arrowWidth * 0.5f);
        arrowHead.GetComponent<Renderer>().material.color = arrowColor;
        Destroy(arrowHead.GetComponent<Collider>());
        arrowHead.SetActive(false);
    }

    void Update()
    {
        if (midiLoader == null)
            return;

        string chordName = midiLoader.CurrentChordName;
        string rootNote = ExtractRootNote(chordName);
        int circleIndex = GetCircleIndex(rootNote);

        if (circleIndex >= 0 && circleIndex != currentIndex)
        {
            previousIndex = currentIndex;
            currentIndex = circleIndex;

            SetAllBlockColors(idleColor);

            if (previousIndex >= 0)
                SetBlockColor(previousIndex, previousColor);

            SetBlockColor(currentIndex, activeColor);
            UpdateArrow();
        }
    }

    string ExtractRootNote(string chordName)
    {
        string beforeParens = chordName.Split('(')[0].Trim();

        if (beforeParens.Length == 0)
            return "";

        // Check if second character is sharp or flat
        if (beforeParens.Length > 1 && (beforeParens[1] == '#' || beforeParens[1] == 'b'))
            return beforeParens.Substring(0, 2).ToUpper();

        return beforeParens.Substring(0, 1).ToUpper();
    }

    int GetCircleIndex(string note)
    {
        switch (note)
        {
            case "C": return 0;
            case "G": return 1;
            case "D": return 2;
            case "A": return 3;
            case "E": return 4;
            case "B": return 5;
            case "F": return 11;
            case "F#": case "GB": return 6;
            case "C#": case "DB": return 7;
            case "G#": case "AB": return 8;
            case "D#": case "EB": return 9;
            case "A#": case "BB": return 10;
            default: return -1;
        }
    }

    void UpdateArrow()
    {
        // Hide arrow if no valid progression
        if (previousIndex < 0 || currentIndex < 0 || previousIndex == currentIndex)
        {
            HideArrow();
            return;
        }

        if (blocks[previousIndex] == null || blocks[currentIndex] == null)
        {
            HideArrow();
            return;
        }

        Vector3 fromPosition = blocks[previousIndex].transform.position;
        Vector3 toPosition = blocks[currentIndex].transform.position;
        Vector3 direction = (toPosition - fromPosition).normalized;

        float blockRadius = 0.5f;
        Vector3 startPosition = fromPosition + direction * blockRadius;
        Vector3 endPosition = toPosition - direction * (blockRadius + arrowWidth * 2);

        arrowLine.SetPosition(0, startPosition);
        arrowLine.SetPosition(1, endPosition);
        arrowLine.enabled = true;

        arrowHead.transform.position = endPosition + direction * arrowWidth;
        arrowHead.transform.rotation = Quaternion.LookRotation(direction);
        arrowHead.SetActive(true);
    }

    void HideArrow()
    {
        arrowLine.enabled = false;
        arrowHead.SetActive(false);
    }

    void SetBlockColor(int index, Color color)
    {
        if (index >= 0 && index < blocks.Length && blocks[index] != null)
            blocks[index].GetComponent<Renderer>().material.color = color;
    }

    void SetAllBlockColors(Color color)
    {
        for (int i = 0; i < blocks.Length; i++)
            SetBlockColor(i, color);
    }
}
