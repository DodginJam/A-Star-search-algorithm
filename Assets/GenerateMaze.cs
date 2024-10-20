using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GenerateMaze : MonoBehaviour
{
    [field: SerializeField] public int Width_X
    { get; private set; } = 15;
    [field: SerializeField] public int Height_Y
    { get; private set; } = 15;
    [field: SerializeField] public float Spacing
    { get; private set; } = 1.1f;
    [field: SerializeField, Range(0, 10)] public int PercentageOfBlockedTiles
    { get; private set; } = 4;
    public GameObject[,] MazeObjects 
    { get; private set; }
    [field: SerializeField] public GameObject TilePrefab
    { get; private set; }
    public List<GameObject> OpenList 
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

    /// <summary>
    /// Loops until the current tile is the same as the ending tile, ending further search and draws the path from start to end.
    /// Will also end if no neighbor tiles are found and the Open List is empty.
    /// Works by getting references to any valid neighbor tiles not currently in the open or closed lists and adding them to Open list.
    /// Then chooses the next tile to select via calculation of each tile in the Open list - calculates distance from start plus distance from end, and chooses lowest cost tile.
    /// That tile is then made the current tile, an and process repeats if the new tile isn't the end tile.
    /// Any tile with no valid neighbors is added to the closed list.
    /// </summary>
    /// <returns></returns>
    IEnumerator SelectTilesUntilEndFound()
    {
        while (CurrentTile != EndingTile)
        {
            yield return new WaitForSeconds(0.2f);

            // Visually update current tile to show as visted.
            CurrentTile.GetComponent<TileInformation>().CurrentState = TileInformation.TileState.TileVisted;

            GameObject[] neighborTiles = GetNeighborTiles(CurrentTile);

            // If there are no valid neighbors, add the current tile to closed list. 
            if (neighborTiles.Length == 0)
            {
                ClosedList.Add(CurrentTile);

                // If no other tiles to explore in open list exist, end the search as no valid path exists.
                if (OpenList.Count == 0)
                {
                    Debug.Log("No more tiles to explore, search terminated.");
                    break;
                }

                // Continue the search of the Open List for a new lowest cost tile, then continue to next loop immediately.
                CalculateOpenListScore();
                continue;
            }

            // Loop over all the valid neighbors and add them to the open list.
            foreach (GameObject neighbor in neighborTiles)
            {
                if (!OpenList.Contains(neighbor) && !ClosedList.Contains(neighbor))
                {
                    OpenList.Add(neighbor);

                    // Important to update the neighbor tiles cost from start for future calculations of choosing lowest cost tile.
                    // Also add reference within tile to the tile that was used to reach it for back-tracking purposes when generating shortest path.
                    neighbor.GetComponent<TileInformation>().TileParent = CurrentTile;
                    neighbor.GetComponent<TileInformation>().CostFromStartCount = CurrentTile.GetComponent<TileInformation>().CostFromStartCount + 1;
                }
            }

            // Current tile needs to be added to closed list before it is reassigned with a new tile - it stops it from being returned the next calculations lowerest cost and neighbors considerations.
            ClosedList.Add(CurrentTile);

            // This will update the Current Tile with the chosen lowest cost tile from the open list.
            CalculateOpenListScore();
        }
        
        if (CurrentTile == EndingTile)
        {
            Debug.Log("Path Complete");
            GeneratePath();
        }
        else
        {
            Debug.Log("No Path Found");
        }
    }

    /// <summary>
    /// Loops through the parent tiles assigned to each tile starting from the ending tile all the way back to the start to visualise path.
    /// </summary>
    void GeneratePath()
    {
        while(CurrentTile != StartingTile)
        {
            if (CurrentTile.GetComponent<TileInformation>().TileParent != null)
            {
                CurrentTile = CurrentTile.GetComponent<TileInformation>().TileParent;
            }
            else
            {
                Debug.LogError("Tile does not have a parent tile.");
            }

            CurrentTile.GetComponent<TileInformation>().CurrentState = TileInformation.TileState.TileOnPath;
        }
    }

    /// <summary>
    /// Loops through all the OpenList tiles, calculates the cost from start plus distance to end and then selects the lowest cost tile to become the current tile.
    /// </summary>
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
                        // Cost to the end is the length from current observed tile to the end in real space.
                        float costToGoal = Mathf.Sqrt(Mathf.Pow(h - EndingIndex[0], 2) + Mathf.Pow(w - EndingIndex[1], 2));
                        // Cost to the end is the amount of tile that have been progressed since the start to the currently observed tile.
                        float costFromStart = MazeObjects[h, w].GetComponent<TileInformation>().CostFromStartCount;

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

        // Important to remove the newly made current tile from the open list so it can not be returned too.
        OpenList.Remove(CurrentTile);
    }

    /// <summary>
    /// Clears any tiles and class variable information to allow a new maze and algorithm to be generated.
    /// </summary>
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

        MazeObjects = CreateMaze(Width_X, Height_Y, PercentageOfBlockedTiles);

        ChooseStartingTile();
        ChooseEndTile();
        StartingTile.GetComponent<Renderer>().material.color = Color.blue;
        EndingTile.GetComponent<Renderer>().material.color = Color.cyan;
    }

    /// <summary>
    /// Creates a maze with a given width and height made up of tiles.
    /// Has a chance to block a generated tile from being visitable, with 0 being 0% and 10 being 100%
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public GameObject[,] CreateMaze(int width, int height, int chanceToBlockTileSpace)
    {
        // Prevents out of range exceptions for chance to block tile calculations.
        if (chanceToBlockTileSpace > 10)
        {
            chanceToBlockTileSpace = 10;
        } 
        else if (chanceToBlockTileSpace < 0)
        {
            chanceToBlockTileSpace = 0;
        }

        GameObject[,] newMaze = new GameObject[height, width];

        for (int h = 0; h < height; h++)
        {
            for (int w = 0; w < width; w++)
            {
                newMaze[h, w] = Instantiate(TilePrefab);
                newMaze[h, w].transform.position = new Vector2(h, w) * Spacing;

                int chanceToBlockTile = UnityEngine.Random.Range(0, 10);

                if (chanceToBlockTile < chanceToBlockTileSpace)
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
