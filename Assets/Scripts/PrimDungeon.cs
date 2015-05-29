using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrimDungeon : MonoBehaviour
{
	// This is just set in the editor to physically create the maze.
	public GameObject cube;
	// Basic maze properties
	public int MazeSizeX = 50;
	public int MazeSizeY = 50;
	public int CellsToAdd = 100;
	public int RoomsToAdd = 5;
	public int RoomMinSize = 2;
	public int RoomMaxSize = 4;
	// Maze generation flags
	public bool PlaceRooms = true;
	public bool SeparateRooms = false;
	public bool PlaceRandomCells = true;
	public bool UncarveMaze = true;
	public bool FixPillars = true;
	public bool MovePillarsToFix = true;
	public bool UncarveAfterPillars = true;

	// Data structures
	bool[,] maze;
	List<Vector2> frontier;

	/*
	 * Add a point to the frontier. If it's out of bounds or already in the maze, ignore.
	 */
	void addFrontier(int x, int y)
	{
		if (x < MazeSizeX && y < MazeSizeY && x >= 0 && y >= 0)
			if (!maze[x, y])
				frontier.Add(new Vector2(x, y));
	}

	/*
	 Mark a cell in the maze as being a path and add adjacent cells to the frontier
	 for future generation.
	*/
	bool mark(int x, int y)
	{
		// Early return if the cell has already been marked.
		if (maze[x, y]) return false;

		// Otherwise, mark it as a path and add all adjacent cells to the frontier.
		maze[x, y] = true;
		addFrontier(x - 1, y);
		addFrontier(x + 1, y);
		addFrontier(x, y - 1);
		addFrontier(x, y + 1);
		return true;
	}

	/*
	 Get a count of all adjacent empty spaces in the maze. Edges of the maze don't count.
	*/
	int neighbours(int x, int y)
	{
		int count = 0;
		if (x > 0)
			if (maze[x - 1, y]) count++;
		if (x < MazeSizeX - 1)
			if (maze[x + 1, y]) count++;
		if (y > 0)
			if (maze[x, y - 1]) count++;
		if (y < MazeSizeY - 1)
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
		for (int i = 0; i < CellsToAdd; i++)
		{
			x = Random.Range(0, MazeSizeX);
			y = Random.Range(0, MazeSizeY);
			for (int j = 0; j < MazeSizeY; j++)
			{
				if (mark(x, y))
					break;
				else
					// Go to the next line.
					x = (x + 1) % MazeSizeX;
			}
		}
	}

	/*
	 * Dispatch to determine how to carve the rooms.
	 */
	void addRooms()
	{
		if (SeparateRooms)
			addRoomsSeparate();
		else
			addRoomsLazy();
	}

	/*
	 * Carve out rooms arbitrarily.
	 */
	void addRoomsLazy()
	{
		int x, y, xdim, ydim;
		for (int i = 0; i < RoomsToAdd; i++)
		{
			xdim = Random.Range(RoomMinSize, RoomMaxSize);
			ydim = Random.Range(RoomMinSize, RoomMaxSize);
			x = Random.Range(0, MazeSizeX - xdim);
			y = Random.Range(0, MazeSizeY - ydim);

			for (int j = x; j < x + xdim; j++)
			{
				for (int k = y; k < y + ydim; k++)
				{
					mark(j, k);
				}
			}
		}
	}

	/*
	 * Carve out rooms so that they don't overlap.
	 */
	void addRoomsSeparate()
	{
		int x, y, xdim, ydim;
		bool overlap;
		bool[,] rooms = new bool[MazeSizeX, MazeSizeY];

		int roomsPlaced = 0, iterations = 0;

		// Keep going until you've placed all the rooms you want to.
		while(roomsPlaced < RoomsToAdd)
		{
			overlap = false;
			// Choose a location and size for the room.
			xdim = Random.Range(RoomMinSize, RoomMaxSize);
			ydim = Random.Range(RoomMinSize, RoomMaxSize);
			x = Random.Range(0, MazeSizeX - xdim);
			y = Random.Range(0, MazeSizeY - ydim);

			// Check if there's a room there already.
			for (int j = x; j < x + xdim; j++)
			{
				for (int k = y; k < y + ydim; k++)
				{
					if (rooms[j,k]) overlap = true;
				}
			}

			// If not, place it.
			if (!overlap)
			{
				for (int j = x; j < x + xdim; j++)
				{
					for (int k = y; k < y + ydim; k++)
					{
						mark(j, k);
						rooms[j,k] = true;
					}
				}
				roomsPlaced++;
			}
			iterations++;

			// If this has been going on for some time and rooms don't seem to be fitting,
			// just find a spot where it'll fit. If there isn't one, stop there, it's good 
			// enough.
			if(iterations > 5 * RoomsToAdd)
			{
				int[] spot = findOpenSpace(rooms, xdim, ydim);
				if (spot != null)
				{
					for (int j = spot[0]; j < spot[0] + xdim; j++)
					{
						for (int k = spot[1]; k < spot[1] + ydim; k++)
						{
							mark(j, k);
							rooms[j, k] = true;
						}
					}
					roomsPlaced++;
				}
				else break;
			}
		}
		Debug.Log(roomsPlaced);
	}

	/*
	 * Determines whether there is space for a rectangle of size x*y in a grid.
	 * This is not a particularly efficient method, but it works and runs quickly enough.
	 * Parameters:
	 *		grid	The array to check
	 *		x, y	The x/y dimensions of the rectangle to fit
	 * Output:
	 *		null if there is no space.
	 *		The coordinates of the open space, otherwise.
	 * Conditions:
	 *		The returned array is always either null or contains two elements.
	 */
	int[] findOpenSpace(bool[,] grid, int x, int y)
	{
		// Early exits if dimensions are nonsense or grid is uninitialized.
		if (x < 0 || y < 0) return null;
		if (grid == null) return null;

		bool space;
		// Just check the whole array.
		for(int i = 0; i < MazeSizeX - x; i++)
		{
			for(int j = 0; j < MazeSizeY - y; j++)
			{
				// Empty spot, see if a room will fit.
				if (!grid[i,j])
				{
					space = true;
					for(int k = i; k < i + x; k++)
					{
						for (int l = j; l < j + y; l++)
						{
							// If one of the spots is already claimed, flag it as such.
							if (grid[k, l]) space = false;
						}
					}
					// If there's space, return the point where that space starts.
					if (space) return  new int[2] { i, j };
				}
			}
		}

		return null;
	}


	/*
	 * Find "pillars" in the map and make them... not pillars.
	 */
	void fixPillars()
	{
		int x;

		for(int i = 0; i < MazeSizeX; i++)
			for(int j = 0; j < MazeSizeY; j++)
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
										if (MovePillarsToFix) maze[i, j] = true;
										maze[i - 1, j] = false;
										x = 5; i = 0; j = 0;
										break;
									}
								x++;
								break;
							case 1:
								if(i+2 < MazeSizeX)
									if(!maze[i+2,j])
									{
										if (MovePillarsToFix) maze[i, j] = true;
										maze[i + 1, j] = false;
										x = 5; i = 0; j = 0;
									}
								x++;
								break;
							case 2:
								if(j-2 > 0)
									if(!maze[i,j-2])
									{
										if (MovePillarsToFix) maze[i, j] = true;
										maze[i, j - 1] = false;
										x = 5; i = 0; j = 0;
										break;
									}
								x++;
								break;
							case 3:
								if(j+2 < MazeSizeY)
									if(!maze[i,j+2])
									{
										if(MovePillarsToFix) maze[i, j] = true;
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
		for(int i = 0; i < MazeSizeX; i++)
		{
			for(int j = 0; j < MazeSizeY; j++)
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
		Terrain.activeTerrain.terrainData.size.Set((MazeSizeX + 1) * cube.transform.localScale.x, 0, (MazeSizeY + 1) * cube.transform.localScale.z);

		maze = new bool[MazeSizeX, MazeSizeY];
		frontier = new List<Vector2>();

		generateMaze();
		if (PlaceRandomCells) addCells();
		if (PlaceRooms) addRooms();
		if (UncarveMaze) uncarve();
		if (FixPillars) fixPillars();
		if (UncarveAfterPillars) uncarve();

		// Create the maze in Unity.
		for (int i = 0; i < MazeSizeX; i++)
		{
			for (int j = 0; j < MazeSizeY; j++)
			{
				if (!maze[i, j]) Instantiate(cube, new Vector3(i * cube.transform.localScale.x, 0.0f, j * cube.transform.localScale.z), Quaternion.identity);
			}
		}

		for (int i = -1; i <= MazeSizeX; i++)
		{
			Instantiate(cube, new Vector3(i * cube.transform.localScale.x, 1.0f, -1 * cube.transform.localScale.z), Quaternion.identity);
			Instantiate(cube, new Vector3(i * cube.transform.localScale.x, 1.0f, MazeSizeY * cube.transform.localScale.z), Quaternion.identity);
		}
		for (int i = 0; i < MazeSizeY; i++)
		{
			Instantiate(cube, new Vector3(-1 * cube.transform.localScale.x, 1.0f, i * cube.transform.localScale.z), Quaternion.identity);
			Instantiate(cube, new Vector3(MazeSizeX * cube.transform.localScale.x, 1.0f, i * cube.transform.localScale.z), Quaternion.identity);
		}
	}
}
