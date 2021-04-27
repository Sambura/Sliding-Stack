using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundBlock : MonoBehaviour
{
    public bool isLava = false;
    public RectInt span = new RectInt(0, 0, 1, 1);

    public bool isRamp = false;
    public float startingHeight = 0;
    public float endingHeight = 0;
}
