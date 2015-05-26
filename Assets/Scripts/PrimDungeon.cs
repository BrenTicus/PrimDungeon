using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrimDungeon : MonoBehaviour
{
	public GameObject cube;
	public int MAZE_SIZE_X = 50;
	public int MAZE_SIZE_Y = 50;
	public int CELLS_TO_ADD = 100;
	public int ROOMS_TO_ADD = 5;
	public int ROOM_MIN_SIZE = 2;
	public int ROOM_MAX_SIZE = 4;
	public bool PLACE_ROOMS = true;
	public bool PLACE_RANDOM_CELLS = true;
	public bool UNCARVE_MAZE = true;
	public bool FIX_PILLARS = true;
	public bool MOVE_PILLARS_TO_FIX = true;
	public bool UNCARVE_AFTER_PILLARS = true;
	bool[,] maze;
	List<Vector2> frontier;

	/*
	 Add a point to the frontier. If it's out of bounds or already in the maze, ignore.
	*/
	void addFrontier(int x, int y)
	{
		if (x < MAZE_SIZE_X && y < MAZE_SIZE_Y && x >= 0 && y >= 0)
			if (!maze[x, y])
				frontier.Add(new Vector2(x, y));
	}

	/*
	 Mark a cell in the maze as being a path and add adjacent cells to the frontier
	 for future generation.
	*/
	int mark(int x, int y)
	{
		// Early return if the cell has already been marked.
		if (maze[x, y]) return -1;

		// Otherwise, mark it as a path and add all adjacent cells to the frontier.
		maze[x, y] = true;
		addFrontier(x - 1, y);
		addFrontier(x + 1, y);
		addFrontier(x, y - 1);
		addFrontier(x, y + 1);
		return 0;
	}

	/*
	 Get a count of all adjacent empty spaces in the maze. Edges of the maze don't count.
	*/
	int neighbours(int x, int y)
	{
		int count = 0;
		if (x > 0)
			if (maze[x - 1, y]) count++;
		if (x < MAZE_SIZE_X - 1)
			if (maze[x + 1, y]) count++;
		if (y > 0)
			if (maze[x, y - 1]) count++;
		if (y < MAZE_SIZE_Y - 1)
			if (maze[x, y + 1]) count++;

		return count;
	}

	/*
	 * Create the maze starting from point (0, 0). 
	 */
	void generateMaze()
	{
		mark(0, 0);

		int i;
		Vector2 spot;
		while (frontier.Count > 0)
		{
			i = Random.Range(0, frontier.Count);
			spot = frontier[i];
			frontier.RemoveAt(i);
			if (neighbours((int)spot.x, (int)spot.y) < 2)
				mark((int)spot.x, (int)spot.y);
		}
	}

	/*
	 * Randomly carve out a bunch of cells.
	 * If a chosen cell is already carved, try others in the row until one can be carved.
	 */
	void addCells()
	{
		int x, y;
		for (int i = 0; i < CELLS_TO_ADD; i++)
		{
			x = Random.Range(0, MAZE_SIZE_X);
			y = Random.Range(0, MAZE_SIZE_Y);
			for (int j = 0; j < MAZE_SIZE_Y; j++)
			{
				if (mark(x, y) == 0)
					break;
				else
					x = (x + 1) % MAZE_SIZE_X;
			}
		}
	}

	/*
	 * Carve out a large room.
	 */
	void addRooms()
	{
		int x, y, xdim, ydim;
		for(int i = 0; i < ROOMS_TO_ADD; i++)
		{
			xdim = Random.Range(ROOM_MIN_SIZE, ROOM_MAX_SIZE);
			ydim = Random.Range(ROOM_MIN_SIZE, ROOM_MAX_SIZE);
			x = Random.Range(0, MAZE_SIZE_X - xdim);
			y = Random.Range(0, MAZE_SIZE_Y - ydim);

			for(int j = x; j < x + xdim; j++)
			{
				for(int k = y; k < y + ydim; k++)
				{
					mark(j, k);
				}
			}
		}
	}

	/*
	 * Find "pillars" in the map and make them... not pillars.
	 */
	void fixPillars()
	{
		int x;

		for(int i = 0; i < MAZE_SIZE_X; i++)
			for(int j = 0; j < MAZE_SIZE_Y; j++)
			{
				// If everything nearby is carved out, it's a pillar.
				if(neighbours(i, j) == 4 && !maze[i,j])
				{
					x = 0;
					// Iterate over the surrounding spaces to see what direction to fix in.
					// Left -> Right -> Down -> Up
					while(x < 4)
					{
						switch(x)
						{
							case 0:
								if(i-2 > 0)
									if(!maze[i-2,j])
									{
										if (MOVE_PILLARS_TO_FIX) maze[i, j] = true;
										maze[i - 1, j] = false;
										x = 5; i = 0; j = 0;
										break;
									}
								x++;
								break;
							case 1:
								if(i+2 < MAZE_SIZE_X)
									if(!maze[i+2,j])
									{
										if (MOVE_PILLARS_TO_FIX) maze[i, j] = true;
										maze[i + 1, j] = false;
										x = 5; i = 0; j = 0;
									}
								x++;
								break;
							case 2:
								if(j-2 > 0)
									if(!maze[i,j-2])
									{
										if (MOVE_PILLARS_TO_FIX) maze[i, j] = true;
										maze[i, j - 1] = false;
										x = 5; i = 0; j = 0;
										break;
									}
								x++;
								break;
							case 3:
								if(j+2 < MAZE_SIZE_Y)
									if(!maze[i,j+2])
									{
										if(MOVE_PILLARS_TO_FIX) maze[i, j] = true;
										maze[i, j + 1] = false;
										x = 5; i = 0; j = 0;
										break;
									}
								x++;
								break;
						}
					}
				}
			}
	}

	/*
	 * Find all dead ends and fill them in.
	 */
	void uncarve()
	{
		for(int i = 0; i < MAZE_SIZE_X; i++)
		{
			for(int j = 0; j < MAZE_SIZE_Y; j++)
			{
				if(maze[i, j] && neighbours(i,j) < 2)
				{
					maze[i, j] = false;
					i = 0; j = 0;
				}
			}
		}
	}

	// Use this for initialization
	void Start()
	{
		Terrain.activeTerrain.transform.position = new Vector3(-1/2f * cube.transform.localScale.x, 0, -1/2f * cube.transform.localScale.z);
		Terrain.activeTerrain.terrainData.size.Set((MAZE_SIZE_X + 1) * cube.transform.localScale.x, 0, (MAZE_SIZE_Y + 1) * cube.transform.localScale.z);

		maze = new bool[MAZE_SIZE_X, MAZE_SIZE_Y];
		frontier = new List<Vector2>();

		generateMaze();
		if (PLACE_RANDOM_CELLS) addCells();
		if (PLACE_ROOMS) addRooms();
		if (UNCARVE_MAZE) uncarve();
		if (FIX_PILLARS) fixPillars();
		if (UNCARVE_AFTER_PILLARS) uncarve();

		// Create the maze in Unity.
		for (int i = 0; i < MAZE_SIZE_X; i++)
		{
			for (int j = 0; j < MAZE_SIZE_Y; j++)
			{
				if (!maze[i, j]) Instantiate(cube, new Vector3(i * cube.transform.localScale.x, 0.0f, j * cube.transform.localScale.z), Quaternion.identity);
			}
		}

		for (int i = -1; i <= MAZE_SIZE_X; i++)
		{
			Instantiate(cube, new Vector3(i * cube.transform.localScale.x, 1.0f, -1 * cube.transform.localScale.z), Quaternion.identity);
			Instantiate(cube, new Vector3(i * cube.transform.localScale.x, 1.0f, MAZE_SIZE_Y * cube.transform.localScale.z), Quaternion.identity);
		}
		for (int i = 0; i < MAZE_SIZE_Y; i++)
		{
			Instantiate(cube, new Vector3(-1 * cube.transform.localScale.x, 1.0f, i * cube.transform.localScale.z), Quaternion.identity);
			Instantiate(cube, new Vector3(MAZE_SIZE_X * cube.transform.localScale.x, 1.0f, i * cube.transform.localScale.z), Quaternion.identity);
		}
	}
}
