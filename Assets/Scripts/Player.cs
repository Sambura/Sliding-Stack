using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour
{
    public Transform movingTarget;
    public float cubesPickupAreaSize = 1f;
    public float movementSpeed = 10f;
    public float finishMovementSpeed = 10f;
    public float finishAccelerationDuration = 2f;
    public ParticleSystem accelerationEffect;
    public float controlsSensitivity = 3f;
    public TrailRenderer trailRenderer;
    public Animator animator;
    public float gravityAcceleration = 9.8f;
    public float lavaDrownAcceleration = 0.02f;
    public float lavaFallingSpeed = 0.2f;
    public int playerHeight = 2;
    public float collisionThreshold = 0.05f;
    public float xConstraint = 2.5f;
    public float rampTolerance = 1.1f;
    public GameObject playerCubePrefab;
    public Vector3 trailPostiton;
    public GameObject lavaFallEffect;
    public float effectLifespan = 1;

    public event System.Action<Vector3> MoneyPickedUp; // coordinates where money was located
    public event System.Action Death;
    public event System.Action<int> Completion; // multiplier

    private List<PlayerCube> cubes;
    private LevelController controller;
    private bool isFalling;
    private float targetGround;
    private float lastZ;
    private PlayerCube lowestCube;
    private readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
    private bool isFinished;
    private float _currentMovementSpeed;
    private Tween _finishAcceleration;

	private void Awake()
	{
        _finishAcceleration = DOTween.To(() => { return _currentMovementSpeed; }, x => { _currentMovementSpeed = x; }, finishMovementSpeed, finishAccelerationDuration).SetEase(Ease.InQuad);
	}

	private void Update()
	{
		MoveForward();
        PickupMoney();

        if (isFinished == false && transform.position.z >= controller.FinishZ)
        {
            isFinished = true;
            _finishAcceleration.Restart();
            accelerationEffect.Play();
        }
    }

	private void FixedUpdate()
    {
        // Pickup
        PickupPlayerCubes();
        // Check collisions
        if (CheckRedCubes()) CheckGround();

        lastZ = movingTarget.position.z; // Remember last location
    }

    private void MoveForward()
	{
        Vector3 forwardDelta = Vector3.forward * _currentMovementSpeed * Time.deltaTime;
        movingTarget.Translate(forwardDelta);
    }

    private void MoveSideways(float horizontalDelta)
    {
        float normalizedDelta = (horizontalDelta / Screen.dpi) * controlsSensitivity;

        float newX = movingTarget.position.x + normalizedDelta;
        float absoluteValue = Mathf.Abs(newX);

        if (absoluteValue > xConstraint) // If resulting coordinate is beyond the constraint
            newX = Mathf.Sign(newX) * xConstraint;

        movingTarget.position = new Vector3(newX, movingTarget.position.y, movingTarget.position.z);
    }

    private void PickupPlayerCubes()
	{
        List<PlayerCube> collidedCubes = controller.GetCollidedPlayerCubesAndRemove(
            new Vector2(movingTarget.position.x, movingTarget.position.z),
            cubesPickupAreaSize,
            lowestCube.transform.position.y - 0.5f,
            cubes[cubes.Count - 1].transform.position.y + 0.5f + playerHeight
            );

        foreach (PlayerCube cube in collidedCubes)
            PickupCube(cube);

        if (collidedCubes.Count > 0) animator.SetTrigger("Jump");
    }

    private void PickupMoney()
	{
        List<Money> list = controller.GetCollidedMoneyAndRemove(
            movingTarget.position, 
            1, 
            lowestCube.transform.position.y - 0.5f,
            cubes[cubes.Count - 1].transform.position.y + 0.5f + playerHeight);

        foreach (Money money in list)
		{
            MoneyPickedUp?.Invoke(money.transform.position);
            money.OnPickup();
		}
	}

    private void CheckGround()
	{
        var groundInfo = controller.GetGroundInfo(movingTarget.position, collisionThreshold);
        targetGround = groundInfo.Item1 + 1;
        float coveredDistance = movingTarget.position.z - lastZ;
        float currentRampTolerance = rampTolerance * coveredDistance;
        float groundDelta = targetGround - lowestCube.transform.position.y;

#if UNITY_EDITOR
        if (Mathf.Abs(groundDelta) > float.Epsilon)
		{
           // Debug.Log("Delta: " + groundDelta + "; tolerance: " + currentRampTolerance + "; distance: " + coveredDistance);
		}
#endif

        if (groundDelta > currentRampTolerance) // Delta is large enough to lose cubes
        {
            int cubesLoss = Mathf.FloorToInt(groundDelta);
            groundDelta -= cubesLoss;
            if (groundDelta > currentRampTolerance)
			{
                cubesLoss++;
                groundDelta--;
			}

            if (cubes.Count <= cubesLoss) // If all cubes are lost
            {
                if (movingTarget.position.z >= controller.FinishZ)
                {
                    LevelCompleted();
                    return;
                }
                else
                {
                    GameOver();
                    return;
                }
            }
            for (int i = 0; i < cubesLoss; i++)
                cubes[i].transform.parent = controller.LevelInstance.transform; // Unbinding lost cubes
            cubes.RemoveRange(0, cubesLoss); // Remove cubes form the list
            trailRenderer.transform.Translate(new Vector3(0, cubesLoss), Space.World); // Move trail
            lowestCube = cubes[0];
        }

        if (groundDelta > float.Epsilon) // small positive delta, so we should move player upwards
		{
            MoveHeight(groundDelta);
		} else if (groundDelta < -float.Epsilon) // negative delta, so we should start falling
		{
            StartCoroutine(ComplexFalling(targetGround, gravityAcceleration));
            return;
		} else if (groundInfo.Item2) // No delta, yes lava
		{
            trailRenderer.emitting = false;
            if (cubes.Count == 1)
            {
                GameOver();
                StartCoroutine(LavaDeath(lavaFallingSpeed, lavaDrownAcceleration));
                return;
            }
            StartCoroutine(LavaFalling(lavaFallingSpeed));
            return;
        }

        trailRenderer.emitting = true; // If everything is ok, enable trail
    }

    private bool CheckRedCubes()
	{
        if (isFalling) return false;

        List<Transform> redCubes = controller.GetCollidedRedCubes(movingTarget.position, collisionThreshold);

        if (redCubes.Count == 0) return true;

        bool[] removed = new bool[cubes.Count];
        PlayerCube lastCube = cubes[cubes.Count - 1]; // Last cube (never changes)
        targetGround = 1;
        float phantomFirstCubeY = Mathf.RoundToInt(lastCube.transform.position.y) - cubes.Count + 1;

        foreach (Transform cube in redCubes)
		{
            if (cube.position.y < lowestCube.transform.position.y) continue; // We are not interested in cubes below
            if (cube.position.y > lastCube.transform.position.y + playerHeight) continue; // Above player
            if (cube.position.y >= lastCube.transform.position.y) // In player / last cube (failure)
            {
                GameOver();
                return false;
            }
            targetGround = Mathf.Max(targetGround, cube.position.y + 1);
            removed[Mathf.RoundToInt(cube.position.y - phantomFirstCubeY)] = true;
		}

        float firstCubeY = lowestCube.transform.position.y;
        for (int i = 0; i < cubes.Count; i++)
            if (removed[i] && cubes[i] != null)
            {
                cubes[i].transform.parent = controller.LevelInstance.transform;
                cubes[i] = null;
            }

        lowestCube = cubes.Find(x => x != null);

        trailRenderer.transform.Translate(
            new Vector3(0, 
            cubes.Find(x => x != null).transform.position.y - firstCubeY), 
            Space.World);

        return false;
	}

    private void PickupCube(PlayerCube cube)
    {
        PlayerCube lastCube = cubes[cubes.Count - 1];
        cubes.Add(lastCube); // Place last cube as last
        cubes[cubes.Count - 2] = cube; // Place new cube where last was
        lowestCube = cubes[0];
        cube.transform.position = lastCube.transform.position;
        lastCube.transform.Translate(Vector3.up); // Shift last cube upwards
        cube.transform.parent = movingTarget;

        cube.OnPickup();
    }

    private void MoveHeight(float delta, bool moveTrail = true)
	{
        Vector3 fallVector = new Vector3(0, delta);
        foreach (PlayerCube cube in cubes)
            cube.transform.Translate(fallVector);
        if (moveTrail) trailRenderer.transform.Translate(fallVector, Space.World);
    }

    /// The whole stack of blocks is falling with a constant speed
    private IEnumerator LavaFalling(float speed)
    {
        animator.SetBool("Fall", true);
        isFalling = true;
        GameObject effect = Instantiate(lavaFallEffect, lowestCube.transform.position, Quaternion.identity);
        Destroy(effect, effectLifespan);
        float delta = 1;
        float currentDelta = speed * Time.fixedDeltaTime;
        while (delta > currentDelta)
        {
            delta -= currentDelta;
            MoveHeight(-currentDelta, false);
            yield return waitForFixedUpdate;
            currentDelta = speed * Time.fixedDeltaTime;
        }
        Destroy(cubes[0].gameObject);
        cubes.RemoveAt(0);
        lowestCube = cubes[0];
        for (int i = 0; i < cubes.Count; i++)
        {
            Vector3 currentPosition = cubes[i].transform.position;
            cubes[i].transform.position = new Vector3(currentPosition.x, i + 1, currentPosition.z);
        }
        isFalling = false;
        animator.SetBool("Fall", false);
    }

    /// The whole stack of blocks is falling with a constant speed
    private IEnumerator LavaDeath(float blockSpeed, float playerAcceleration)
    {
        float delta = 1;
        GameObject effect = Instantiate(lavaFallEffect, lowestCube.transform.position, Quaternion.identity);
        Destroy(effect, effectLifespan);
        while (delta > 0)
        {
            float currentDelta = blockSpeed * Time.fixedDeltaTime;
            delta -= currentDelta;
            MoveHeight(-currentDelta, false);
            yield return waitForFixedUpdate;
        }

        delta = 1.25f;
        float speed = 0;
        while (delta > 0)
		{
            speed += Time.fixedDeltaTime * playerAcceleration;
            float currentDelta = Time.fixedDeltaTime * speed;
            delta -= currentDelta;
            MoveHeight(-currentDelta, false);
            yield return waitForFixedUpdate;
        }
    }

    /// The blocks are falling independently with a constant acceleration
    private IEnumerator ComplexFalling(float targetGround, float acceleration)
    {
        isFalling = true;
        animator.SetBool("Fall", true);
        trailRenderer.emitting = false;
        float speed = 0;
        int notLanded = 0;
        cubes.RemoveAll((x) => x == null); 
        while (notLanded < cubes.Count)
        {
            speed += acceleration * Time.fixedDeltaTime;
            for (int i = notLanded; i < cubes.Count; i++)
			{
                if (Mathf.Approximately(cubes[i].transform.position.y, targetGround + i))
				{
                    notLanded = i + 1;
                    continue;
				}
                Vector3 delta = new Vector3(0, 
                    -Mathf.Clamp(speed * Time.fixedDeltaTime, 0, cubes[i].transform.position.y - targetGround - i));
                cubes[i].transform.Translate(delta);
                if (i == 0) trailRenderer.transform.Translate(delta, Space.World);
			}
            yield return waitForFixedUpdate;
        }
        yield return null;
        trailRenderer.emitting = true;
        isFalling = false;
        animator.SetBool("Fall", false);
    }

    private void GameOver()
	{
        InputManager.HorizontalDrag -= MoveSideways;

        animator.SetBool("Dead", true);
        Death?.Invoke();
	}

    private void LevelCompleted()
	{
        InputManager.HorizontalDrag -= MoveSideways;

        this.enabled = false;
        animator.SetBool("Dance", true);
        int multiplier = Mathf.Min(11, Mathf.FloorToInt(movingTarget.position.z - controller.FinishZ) / 5);
        if (multiplier == 11) multiplier = 20;
        if (multiplier == 0) multiplier = 1;
        Completion?.Invoke(multiplier);
        accelerationEffect.Stop();
	}

    public void InitPlayer(LevelController controller, int initialCubeCount)
	{
        this.controller = controller;
        // Reset all
        StopAllCoroutines();
        transform.parent = null;
        Camera.main.transform.position = Vector3.zero;
        movingTarget.transform.position = Vector3.zero;
        lastZ = 0;
        if (cubes != null)
            foreach (PlayerCube cube in cubes)
                if (cube != null) Destroy(cube.gameObject); else Debug.LogError("Null player cube encountered");
        isFalling = false;
        targetGround = 1;
        trailRenderer.transform.localPosition = trailPostiton;
        trailRenderer.Clear();
        isFinished = false;
        _currentMovementSpeed = movementSpeed;
        _finishAcceleration.Pause();
        accelerationEffect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        animator.SetBool("Dead", false);
        animator.SetBool("Fall", false);
        animator.SetBool("Dance", false);
        animator.ResetTrigger("Jump");

        // Generate initial cubes
        cubes = new List<PlayerCube>(initialCubeCount);
        float height = 1;
        for (int i = 0; i < initialCubeCount; i++)
		{
            cubes.Add(Instantiate(
                playerCubePrefab, 
                new Vector3(0, height++), 
                Quaternion.identity
                ).GetComponent<PlayerCube>());
            cubes[0].transform.parent = movingTarget;
		}

        lowestCube = cubes[0];

        transform.position = new Vector3(0, height - 0.5f);
        transform.parent = cubes[cubes.Count - 1].transform;

        InputManager.HorizontalDrag -= MoveSideways; // To be safe (fires on resets during gameplay)
        InputManager.HorizontalDrag += MoveSideways;
	}
}
