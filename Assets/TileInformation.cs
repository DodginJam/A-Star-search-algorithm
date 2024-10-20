using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TileInformation : MonoBehaviour
{
    public enum TileState
    {
        TileUnvisted,
        TileIsOption,
        TileNotOptions,
        TileVisted,
        TileBlocked,
        TileOnPath
    }
    [SerializeField]
    private TileState currentState;
    public TileState CurrentState
    {
        get { return currentState; }
        set
        {
            currentState = value;

            if (value == TileState.TileUnvisted)
            {
                GetComponent<Renderer>().material.color = Color.white;
            }
            else if (value == TileState.TileIsOption)
            {
                GetComponent<Renderer>().material.color = Color.yellow;
            }
            else if (value == TileState.TileNotOptions)
            {
                GetComponent<Renderer>().material.color = Color.red;
            }
            else if (value == TileState.TileVisted)
            {
                GetComponent<Renderer>().material.color = Color.green;
            }
            else if (value == TileState.TileBlocked)
            {
                GetComponent<Renderer>().material.color = Color.black;
            }
            else if (value == TileState.TileOnPath)
            {
                GetComponent<Renderer>().material.color = Color.magenta;
            }
            else
            {
                Debug.LogError("TileState being set to invalid state");
            }
        }
    }

    [field: SerializeField]
    public TextMeshProUGUI textBox
    { get; set; }

    public GameObject TileParent
    { get; set; } = null;
    public int CostFromStartCount
    { get; set; } = 0;

    // Start is called before the first frame update
    void Start()
    {
        textBox = transform.Find("Canvas/textBox").GetComponentInChildren<TextMeshProUGUI>();

        textBox.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateText(string newText)
    {
        textBox.gameObject.SetActive(true);
        textBox.text = newText.Trim();
    }
}
