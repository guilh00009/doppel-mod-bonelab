using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public struct Frame
{
	public float time;

	public float yaw;

	public Vector3 headWorld;

	public float height;

	public Quaternion head;

	public Vector3 leftPos;

	public Vector3 rightPos;

	public Quaternion leftRot;

	public Quaternion rightRot;

	public Vector2 stick;

	public Vector2 turnStick;

	public float leftGrip;

	public float rightGrip;

	public float leftTrigger;

	public float rightTrigger;

	public bool leftA;

	public bool leftB;

	public bool rightA;

	public bool rightB;

	public bool jump;

	public bool otherA;

	public float crouch;

	public float feet;

	public bool jumped;

	public bool primaryStick;

	public bool secondaryStick;

	public int enemies;

	public float enemyDistance;

	public bool armed;

	public string leftHeld;

	public string rightHeld;
}
