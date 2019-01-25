using UnityEngine;
using UnityEngine.UI;

public class ToggleRenderer : MonoBehaviour
{
    public Toggle ToggleButton = null;

    private void Update()
    {
        GetComponent<Renderer>().enabled = ToggleButton == null || ToggleButton.isOn;
    }
}
