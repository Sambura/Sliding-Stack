using System.Collections.Generic;
using UnityEngine;

public class LevelController
{
	public float FinishZ { get; private set; }

	public GameObject LevelInstance
	{
		get { return _levelInstance; }
		set => SetLevelInstance(value);
	}
	private GameObject _levelInstance;
	
	private int levelWidth;
	private int levelLength;
	private float halfLevelWidth;

	private LinkedList<PlayerCube> playerCubes = new LinkedList<PlayerCube>();
	private LinkedList<Money> moneyCollection = new LinkedList<Money>();
	private List<List<Transform>> redCubes;
	private List<List<GroundBlock>> groundMap;

	private List<PlayerCube> collidedPlayerCubes = new List<PlayerCube>();
	private List<Transform> collidedRedCubes = new List<Transform>();
	private List<Money> collidedMoney = new List<Money>();

	private void SetLevelInstance(GameObject level)
	{
		_levelInstance = level;

		CalculateAndInitLevelBounds();
		InitLevelObjects();
	}

	private void CalculateAndInitLevelBounds()
	{
		levelLength = 0;
		levelWidth = 0;
#if DEBUG
		FinishZ = float.PositiveInfinity;
#endif

		Stack<Transform> children = new Stack<Transform>();
		List<GroundBlock> groundBlocks = new List<GroundBlock>();
		children.Push(_levelInstance.transform);

		while (children.Count > 0)
		{
			Transform current = children.Pop();
			foreach (Transform child in current)
			{
				children.Push(child);

				GroundBlock ground = child.GetComponent<GroundBlock>();
				if (ground != null) {
					groundBlocks.Add(ground);
					int zMax = Mathf.RoundToInt(ground.transform.position.z) + ground.span.y + ground.span.height;
					int xMax = Mathf.RoundToInt(ground.transform.position.x) + ground.span.x + ground.span.width - 1;
					levelLength = Mathf.Max(levelLength, zMax);
					levelWidth = Mathf.Max(levelWidth, xMax);
				}

				if (child.CompareTag("Finish")) FinishZ = child.position.z;
			}
		}
#if DEBUG
		if (float.IsPositiveInfinity(FinishZ)) Debug.LogError("Finish marker was not found");
#endif
		levelWidth = 1 + levelWidth * 2;
		halfLevelWidth = levelWidth / 2f;

		groundMap = new List<List<GroundBlock>>(levelWidth);
		for (int i = 0; i < levelWidth; i++)
		{
			groundMap.Add(new List<GroundBlock>(levelLength));
			for (int j = 0; j < levelLength; j++)
				groundMap[i].Add(null);
		}

		foreach (GroundBlock ground in groundBlocks)
		{
			AddGroundBlock(ground);
		}

		redCubes = new List<List<Transform>>(levelLength);
		for (int i = 0; i < levelLength; i++)
		{
			redCubes.Add(new List<Transform>());
		}
	}

	private void InitLevelObjects()
	{
		moneyCollection.Clear();
		playerCubes.Clear();

		Stack<Transform> children = new Stack<Transform>();
		children.Push(_levelInstance.transform);

		while (children.Count > 0)
		{
			Transform current = children.Pop();
			foreach (Transform child in current)
			{
				children.Push(child);

				Money money = child.GetComponent<Money>();
				if (money != null)
				{
					moneyCollection.AddLast(money);
					continue;
				}

				PlayerCube playerCube = child.GetComponent<PlayerCube>();
				if (playerCube != null)
				{
					playerCubes.AddLast(playerCube);
					continue;
				}

				if (child.CompareTag("RedCube")) // Red cubes do not have a script on them
				{
					redCubes[Mathf.RoundToInt(child.position.z)].Add(child);
					continue;
				}
			}
		}
	}

	private void AddGroundBlock(GroundBlock block)
	{
		int x = Mathf.FloorToInt(block.transform.position.x);
		int z = Mathf.FloorToInt(block.transform.position.z);

		int fromX = x + block.span.x + (levelWidth - 1) / 2;
		int toX = fromX + block.span.width;
		int fromZ = z + block.span.y;
		int toZ = fromZ + block.span.height;

		for (int i = fromX; i < toX; i++)
			for (int j = fromZ; j < toZ; j++)
			{
				if (groundMap[i][j] == null || groundMap[i][j].isLava || groundMap[i][j].transform.position.y < block.transform.position.y)
					groundMap[i][j] = block;
			}
	}

	// Ground level, isLava
	public (float, bool) GetGroundInfo(Vector3 location, float collisionThreshold)
	{
		float mappedX = location.x + halfLevelWidth; // [0..levelWidth)
		int xLeft = Mathf.FloorToInt(mappedX - 0.5f + collisionThreshold); // location.x - 0.5f + 2.5f
		int xRight = Mathf.FloorToInt(mappedX + 0.5f - collisionThreshold); // location.x + 0.5f + 2.5f
		int zBottom = Mathf.FloorToInt(location.z);
		int zTop = Mathf.FloorToInt(location.z) + 1;

		float groundLevel = float.NegativeInfinity;
		bool isInLava = true;

		for (int x = xLeft; x <= xRight; x++)
			for (int z = zBottom; z <= zTop; z++)
			{
				GroundBlock block = groundMap[x][z];
				if (!block.isRamp) 
					groundLevel = Mathf.Max(groundLevel, block.transform.position.y); 
				else
				{
					float zLocation = location.z;
					if (z == zBottom) zLocation -= 1f;
					float progress = zLocation - Mathf.Floor(zLocation);
					float height = block.transform.position.y + 
						block.startingHeight * (1 - progress) + block.endingHeight * progress;
					groundLevel = Mathf.Max(groundLevel, height);
				}
				isInLava &= groundMap[x][z].isLava;
			}

		return (groundLevel, isInLava);
	}

	public List<PlayerCube> GetCollidedPlayerCubesAndRemove(Vector2 position, float size, float lowest, float highest)
	{
		collidedPlayerCubes.Clear();

		Rect playerRect = new Rect(position - new Vector2(size / 2, size / 2), new Vector2(size, size));
		for (var iter = playerCubes.First; iter != null;) // Evil foreach with element deletion
		{
			var current = iter;
			iter = iter.Next;

			PlayerCube cube = current.Value;
			if (cube.transform.position.y >= lowest &&
				cube.transform.position.y <= highest &&
				playerRect.Overlaps(cube.rectangle)) {
				collidedPlayerCubes.Add(cube);
				playerCubes.Remove(current);
			}
		}

		return collidedPlayerCubes;
	}

	public List<Money> GetCollidedMoneyAndRemove(Vector3 position, float size, float lowest, float highest)
	{
		collidedMoney.Clear();

		Rect playerRect = new Rect(new Vector2(position.x - size / 2, position.z - size / 2), new Vector2(size, size));
		for (var iter = moneyCollection.First; iter != null;) // Evil foreach with element deletion
		{
			var current = iter;
			iter = iter.Next;

			Money money = current.Value;
			if (highest >= money.height && lowest <= money.height && playerRect.Overlaps(money.rectangle))
			{
				collidedMoney.Add(money);
				moneyCollection.Remove(current);
			}
		}

		return collidedMoney;
	}

	public List<Transform> GetCollidedRedCubes(Vector3 position, float collisionThreshold)
	{
		collidedRedCubes.Clear();

		List<Transform> first = redCubes[Mathf.FloorToInt(position.z)];
		List<Transform> second = redCubes[Mathf.CeilToInt(position.z)];

		float one = Mathf.FloorToInt(position.x + collisionThreshold);
		float two = Mathf.CeilToInt(position.x - collisionThreshold);

		foreach (Transform cube in first)
		{
			if (Mathf.Approximately(one, cube.transform.position.x) ||
				Mathf.Approximately(two, cube.transform.position.x))
				collidedRedCubes.Add(cube);
		}
		if (first != second)
		{
			foreach (Transform cube in second)
			{
				if (Mathf.Approximately(one, cube.transform.position.x) ||
					Mathf.Approximately(two, cube.transform.position.x))
					collidedRedCubes.Add(cube);
			}
		}

		return collidedRedCubes;
	}
}
