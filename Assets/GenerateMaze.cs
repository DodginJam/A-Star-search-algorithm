using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GenerateMaze : MonoBehaviour
{
    [field: SerializeField] public int Width_X
    { get; private set; }
    [field: SerializeField] public int Height_Y
    { get; private set; }
    [field: SerializeField] public float Spacing
    { get; private set; } = 1.01f;
    public GameObject[,] MazeObjects 
    { get; private set; }
    [field: SerializeField] public GameObject TilePrefab
    { get; private set; }
    [field: SerializeField]public List<GameObject> OpenList 
    { get; private set; } = new List<GameObject>();
    public List<GameObject> ClosedList
    { get; private set; } = new List<GameObject>();

    public GameObject StartingTile
    { get; private set; }
    public int[] StartingIndex 
    { get; private set; }
    public GameObject EndingTile
    { get; private set; }
    public int[] EndingIndex
    { get; private set; }
    public GameObject CurrentTile
    { get; private set; }
    public int[] CurrentIndex
    { get; private set; }

    public Coroutine RunningAStar
    { get; private set; }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (RunningAStar != null)
            {
                StopCoroutine(RunningAStar);
                RunningAStar = null;
            }

            ResetAndRedrawMaze();

            RunningAStar = StartCoroutine(SelectTilesUntilEndFound());
        }

    }

    IEnumerator SelectTilesUntilEndFound()
    {
        while (CurrentTile != EndingTile)
        {
            yield return new WaitForSeconds(0.00001f);

            GameObject[] neighborTiles = GetNeighborTiles(CurrentTile);

            if (neighborTiles.Length == 0)
            {
                CurrentTile.GetComponent<TileInformation>().CurrentState = TileInformation.TileState.TileVisted;
                ClosedList.Add(CurrentTile);
                Debug.Log("No neighbors");

                if (OpenList.Count == 0)
                {
                    Debug.Log("No more tiles to explore, search terminated.");
                    break;
                }

                CalculateOpenListScore();
                continue;
            }

            foreach (GameObject neighbor in neighborTiles)
            {
                if (!OpenList.Contains(neighbor) && !ClosedList.Contains(neighbor))
                {
                    OpenList.Add(neighbor);
                    neighbor.GetComponent<TileInformation>().CurrentState = TileInformation.TileState.TileIsOption;
                }
            }

            CurrentTile.GetComponent<TileInformation>().CurrentState = TileInformation.TileState.TileVisted;
            ClosedList.Add(CurrentTile);

            CalculateOpenListScore();
        }
        Debug.Log("Search ended.");
    }

    void CalculateOpenListScore()
    {
        float[] openListScores = new float[OpenList.Count];
        int[,] openListScoresIndexes = new int[OpenList.Count, 2];

        int listItemCounter = 0;
        foreach (GameObject listItem in OpenList)
        {
            for (int h = 0; h < MazeObjects.GetLength(0); h++)
            {
                for (int w = 0; w < MazeObjects.GetLength(1); w++)
                {
                    if (MazeObjects[h,w] == listItem)
                    {
                        float costToGoal = Mathf.Sqrt(Mathf.Pow(h - EndingIndex[0], 2) + Mathf.Pow(w - EndingIndex[1], 2));
                        float costFromStart = Mathf.Sqrt(Mathf.Pow(h - StartingIndex[0], 2) + Mathf.Pow(w - StartingIndex[1], 2));

                        MazeObjects[h, w].GetComponent<TileInformation>().UpdateText($"{costToGoal + costFromStart}");

                        openListScores[listItemCounter] = costToGoal + costFromStart;

                        openListScoresIndexes[listItemCounter, 0] = h;
                        openListScoresIndexes[listItemCounter, 1] = w;

                        listItemCounter++;
                    }
                }
            }
        }

        int lowestScoreIndex = Array.IndexOf(openListScores, openListScores.Min());

        CurrentTile = OpenList[lowestScoreIndex];
        CurrentIndex = new int[] { openListScoresIndexes[lowestScoreIndex, 0], openListScoresIndexes[lowestScoreIndex, 1] };

        OpenList.Remove(CurrentTile);
    }

    void ResetAndRedrawMaze()
    {
        if (MazeObjects != null)
        {
            foreach (GameObject tile in MazeObjects)
            {
                Destroy(tile);
            }

            MazeObjects = null;

            StartingTile = null;
            CurrentTile = null;
            EndingTile = null;

            StartingIndex = new int[2];
            CurrentIndex = new int[2];
            EndingIndex = new int[2];

            OpenList.Clear();
            ClosedList.Clear();
        }

        MazeObjects = CreateMaze(Width_X, Height_Y);

        ChooseStartingTile();
        ChooseEndTile();
        StartingTile.GetComponent<Renderer>().material.color = Color.blue;
        EndingTile.GetComponent<Renderer>().material.color = Color.cyan;
    }

    public GameObject[,] CreateMaze(int width, int height)
    {
        GameObject[,] newMaze = new GameObject[height, width];

        for (int h = 0; h < height; h++)
        {
            for (int w = 0; w < width; w++)
            {
                newMaze[h, w] = Instantiate(TilePrefab);
                newMaze[h, w].transform.position = new Vector2(h, w) * Spacing;

                int chanceToBlockTile = UnityEngine.Random.Range(0, 10);

                if (chanceToBlockTile < 3)
                {
                    newMaze[h, w].GetComponent<TileInformation>().CurrentState = TileInformation.TileState.TileBlocked;
                }
            }
        }

        return newMaze;
    }

    public void ChooseStartingTile()
    {
        List<GameObject> eligibleTiles = new List<GameObject>();
        List<int[]> eligibleIndexes = new List<int[]>();

        for (int h = 0; h < MazeObjects.GetLength(0); h++)
        {
            for (int w = 0; w < MazeObjects.GetLength(1); w++)
            {
                if (h == 0 || h == MazeObjects.GetLength(0) - 1 || w == 0 || w == MazeObjects.GetLength(1) - 1)
                {
                    eligibleTiles.Add(MazeObjects[h, w]);
                    eligibleIndexes.Add(new int[] {h, w});
                }
            }
        }

        int randomSelection = UnityEngine.Random.Range(0, eligibleTiles.Count);

        StartingTile = eligibleTiles[randomSelection];
        StartingIndex = eligibleIndexes[randomSelection];

        UpdateCurrentTile(StartingTile, StartingIndex);
    }

    public void ChooseEndTile()
    {
        List<GameObject> eligibleTiles = new List<GameObject>();
        List<int[]> eligibleIndexes = new List<int[]>();


        for (int h = 0; h < MazeObjects.GetLength(0); h++)
        {
            for (int w = 0; w < MazeObjects.GetLength(1); w++)
            {
                if (h == 0 || h == MazeObjects.GetLength(0) - 1 || w == 0 || w == MazeObjects.GetLength(1) - 1)
                {
                    if (MazeObjects[h, w] != StartingTile && MazeObjects[h, w].transform.position.x != StartingTile.transform.position.x && MazeObjects[h, w].transform.position.y != StartingTile.transform.position.y)
                    {
                        eligibleTiles.Add(MazeObjects[h, w]);
                        eligibleIndexes.Add(new int[] { h, w });
                    }
                }
            }
        }

        int randomSelection = UnityEngine.Random.Range(0, eligibleTiles.Count);

        EndingTile = eligibleTiles[randomSelection];
        EndingTile.GetComponent<TileInformation>().CurrentState = TileInformation.TileState.TileUnvisted;
        EndingIndex = eligibleIndexes[randomSelection];
    }

    void UpdateCurrentTile(GameObject newTile, int[] newIndex)
    {
        CurrentTile = newTile;
        CurrentIndex = newIndex;
    }

    public GameObject[] GetNeighborTiles(GameObject tile)
    {
        GameObject currentTile;
        List<GameObject> validNeighborsIndex = new List<GameObject>();

        if (tile.TryGetComponent<TileInformation>(out TileInformation tileInfo))
        {
            currentTile = tileInfo.gameObject;
        }
        else
        {
            Debug.LogError("Passing through a gameObject without TileInformation");
            return null;
        }

        int[,] adjacentCoordinates = new int[4, 2];

        int[,] neighborIndexesOffsets = new int[,]
        {
            {0, 1},
            {1, 0},
            {0, -1},
            {-1, 0}
        };

        for (int i = 0; i < adjacentCoordinates.GetLength(0); i++)
        {
            adjacentCoordinates[i, 0] = CurrentIndex[0] + neighborIndexesOffsets[i, 0];
            adjacentCoordinates[i, 1] = CurrentIndex[1] + neighborIndexesOffsets[i, 1];

            // If tile-coord falls out of range of grid, then no tile will exisit and should not be added to valid neighbours index.
            if (adjacentCoordinates[i, 0] >= MazeObjects.GetLength(0) || adjacentCoordinates[i, 0] < 0 || adjacentCoordinates[i, 1] >= MazeObjects.GetLength(1) || adjacentCoordinates[i, 1] < 0)
            {
                continue;
            }

            GameObject currentObject = MazeObjects[adjacentCoordinates[i, 0], adjacentCoordinates[i, 1]];

            if (currentObject.GetComponent<TileInformation>().CurrentState == TileInformation.TileState.TileBlocked || currentObject.GetComponent<TileInformation>().CurrentState == TileInformation.TileState.TileVisted)
            {
                continue;
            }

            validNeighborsIndex.Add(currentObject);
        }

        return validNeighborsIndex.ToArray();    
    }
}
