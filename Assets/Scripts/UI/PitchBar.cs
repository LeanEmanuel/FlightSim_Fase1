using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PitchBar : MonoBehaviour {
    [SerializeField]
    List<Text> texts;

    Image image;
    List<Transform> transforms;

    void Start() {
        image = GetComponent<Image>();
        transforms = new List<Transform>();

        if (texts == null || texts.Count == 0)
        {
            Debug.LogWarning("PitchBar: No texts assigned in inspector.");
        }

        foreach (var text in texts) {
            transforms.Add(text.GetComponent<Transform>());
        }
    }

    public void SetNumber(int number) {
        foreach (var text in texts) {
            text.text = string.Format("{0}", number);
        }
    }

    public void UpdateRoll(float angle) {
        if (transforms == null || transforms.Count == 0) return;

        foreach (var transform in transforms) {
            transform.localEulerAngles = new Vector3(0, 0, angle);
        }
    }

    public void UpdateColor(Color color) {
        if (image != null)
            image.color = color;

        if (texts != null)
        {
            foreach (var text in texts)
            {
                if (text != null)
                    text.color = color;
            }
        }
    }
}
