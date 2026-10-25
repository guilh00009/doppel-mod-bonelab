using System;
using Il2CppSLZ.Marrow;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdEyeBonelabMod.Bots;

public class Navigator
{
	private const float Repath = 0.5f;

	private const float CornerReached = 0.45f;

	private readonly NavMeshPath path = new NavMeshPath();

	private Vector3[] corners = new Vector3[0];

	private int next;

	private float repathAt;

	private Vector3 lastPlace;

	private float lastPlaceAt;

	private float pushedFor;

	private bool gaveUp;

	private Vector3 firstLeg;

	private float wayKeptSince;

	private Vector3 wayGoal;

	private float offMeshFrom = -1f;

	private float offMeshSince = -10f;

	private Vector3 lastDir;

	private Vector3 lastGoal;

	private float lastDirAt;

	private float turnSaidAt;

	private bool wasMesh;

	private int lastNext;

	private int lastCorners;

	private float slideSide;

	private float slideUntil;

	private float sideUntil;

	private Vector3 slideDir;

	public Vector3 Goal { get; private set; }

	public bool HasGoal { get; private set; }

	public bool OnMesh { get; private set; }

	public int StuckCount { get; private set; }

	public int Swerves { get; private set; }

	public int MeshFlips { get; private set; }

	public float DropDepth { get; private set; }

	public void SetGoal(Vector3 goal)
	{
		bool flag = HasGoal && Flat(goal - Goal).magnitude < 3f && corners.Length >= 2;
		Goal = goal;
		HasGoal = true;
		repathAt = (flag ? Mathf.Min(repathAt, Time.time + 0.25f) : 0f);
		if (!flag)
		{
			StuckCount = 0;
			gaveUp = false;
		}
	}

	public void GiveUp()
	{
		gaveUp = true;
		StuckCount = Mathf.Max(StuckCount, 3);
	}

	public void Clear()
	{
		HasGoal = false;
		corners = new Vector3[0];
		StuckCount = 0;
		gaveUp = false;
	}

	public float Remaining(Vector3 feet)
	{
		if (!HasGoal)
		{
			return 0f;
		}
		float num = 0f;
		Vector3 vector = feet;
		for (int i = next; i < corners.Length; i++)
		{
			num += Flat(corners[i] - vector).magnitude;
			vector = corners[i];
		}
		if (next < corners.Length)
		{
			return num;
		}
		return Flat(Goal - feet).magnitude;
	}

	public Vector3 Steer(Vector3 feet, Vector3 chest, Vector3 facing, out float rise, out bool drop)
	{
		rise = 0f;
		drop = false;
		DropDepth = 0f;
		if (!HasGoal)
		{
			return Vector3.zero;
		}
		bool flag = corners.Length < 2 || next >= corners.Length;
		bool flag2 = Flat(Goal - wayGoal).magnitude > Tuning.F("nav.regoal", 1.5f);
		bool flag3 = !flag && OffWay(feet) > Tuning.F("nav.stray", 2f);
		if (flag || flag2 || flag3 || Time.time >= repathAt)
		{
			repathAt = Time.time + Tuning.F("nav.again", 2.5f);
			FindWay(feet, flag || flag3);
			wayGoal = Goal;
		}
		while (next < corners.Length - 1 && (Flat(corners[next] - feet).magnitude < Tuning.F("nav.corner", 0.8f) || (Vector3.Dot(Flat(corners[next] - feet), Flat(corners[next + 1] - corners[next])) < 0f && Flat(corners[next] - feet).magnitude < 1.6f)))
		{
			next++;
		}
		Vector3 vector = ((next < corners.Length) ? corners[next] : Goal);
		float magnitude = Flat(vector - feet).magnitude;
		Vector3 vector2 = Flat((OnMesh ? LookAhead(feet, Tuning.F("nav.ahead", 1.3f)) : vector) - feet);
		if (vector2.sqrMagnitude < 0.0025f)
		{
			vector2 = Flat(vector - feet);
		}
		if (vector2.sqrMagnitude < 0.0025f)
		{
			return Vector3.zero;
		}
		vector2.Normalize();
		rise = ((magnitude < 1.2f) ? (vector.y - feet.y) : 0f);
		Vector3 from = vector2;
		if (!OnMesh)
		{
			vector2 = Feel(feet, chest, vector2, ref rise, out drop);
			if (drop)
			{
				DropDepth = Depth(feet, vector2);
			}
		}
		if (lastDir.sqrMagnitude > 0.01f && Time.time - lastDirAt < 0.3f && !drop)
		{
			float num = Tuning.F("nav.turn", 170f) * Mathf.Max(Time.deltaTime, 0.005f);
			vector2 = Flat(Vector3.RotateTowards(lastDir, vector2, num * ((float)Math.PI / 180f), 0f)).normalized;
		}
		if (lastDir.sqrMagnitude > 0.01f && Time.time - lastDirAt < 0.3f && Vector3.Angle(lastDir, vector2) > 50f && Time.time - turnSaidAt > 0.4f)
		{
			turnSaidAt = Time.time;
			Lab.Event("nav: its way turned " + Vector3.SignedAngle(lastDir, vector2, Vector3.up).ToString("0") + " deg at once: " + (OnMesh ? "on the level's ways" : "by feel") + ((wasMesh != OnMesh) ? (" (was " + (wasMesh ? "on them" : "by feel") + ")") : "") + ", corner " + next + " of " + corners.Length + ((lastNext != next) ? (" (was " + lastNext + ")") : "") + ((lastCorners != corners.Length) ? ", new way" : "") + ((Vector3.Angle(from, vector2) > 10f) ? (", turned " + Vector3.Angle(from, vector2).ToString("0") + " by a wall") : "") + ", " + magnitude.ToString("0.0") + " m to the corner, goal " + Flat(Goal - feet).magnitude.ToString("0") + " m" + ((Flat(Goal - lastGoal).magnitude > 0.5f) ? (", goal moved " + Flat(Goal - lastGoal).magnitude.ToString("0.0") + " m") : ""));
		}
		lastDir = vector2;
		lastDirAt = Time.time;
		wasMesh = OnMesh;
		lastNext = next;
		lastCorners = corners.Length;
		lastGoal = Goal;
		return vector2;
	}

	private float OffWay(Vector3 feet)
	{
		if (next <= 0 || next >= corners.Length)
		{
			return 0f;
		}
		Vector3 vector = corners[next - 1];
		Vector3 vector2 = corners[next];
		Vector3 rhs = Flat(vector2 - vector);
		float num = ((rhs.sqrMagnitude > 0.0001f) ? Mathf.Clamp01(Vector3.Dot(Flat(feet - vector), rhs) / rhs.sqrMagnitude) : 0f);
		return Flat(feet - (vector + (vector2 - vector) * num)).magnitude;
	}

	private static float Length(Vector3[] way, int from, Vector3 start)
	{
		float num = 0f;
		Vector3 vector = start;
		for (int i = from; i < way.Length; i++)
		{
			num += Flat(way[i] - vector).magnitude;
			vector = way[i];
		}
		return num;
	}

	private void FindWay(Vector3 feet, bool afresh = true)
	{
		bool onMesh = OnMesh;
		OnMesh = false;
		try
		{
			if (NavMesh.SamplePosition(feet, out var hit, 2.2f, -1) && NavMesh.SamplePosition(Goal, out var hit2, 3f, -1) && NavMesh.CalculatePath(hit.position, hit2.position, -1, path) && path.status != NavMeshPathStatus.PathInvalid)
			{
				Vector3[] array = path.corners;
				if (array != null && array.Length >= 2)
				{
					Vector3 to = Flat(array[1] - array[0]);
					Vector3 from = ((onMesh && corners.Length >= 2 && next < corners.Length) ? Flat(corners[next] - feet) : Vector3.zero);
					if (!afresh && from.sqrMagnitude > 0.2f && to.sqrMagnitude > 0.01f && Vector3.Angle(from, to) > Tuning.F("nav.swerve", 40f) && Flat(corners[corners.Length - 1] - Goal).magnitude < Tuning.F("nav.keep.end", 5f) && Length(array, 1, array[0]) > Length(corners, next, feet) * Tuning.F("nav.keep.shorter", 0.75f) - 1f)
					{
						Swerves++;
						OnMesh = true;
						return;
					}
					if (firstLeg.sqrMagnitude < 0.01f || Vector3.Angle(firstLeg, to) > 55f)
					{
						wayKeptSince = Time.time;
					}
					firstLeg = to;
					corners = array;
					next = 1;
					OnMesh = true;
					return;
				}
			}
		}
		catch (Exception)
		{
		}
		if (onMesh)
		{
			MeshFlips++;
			if (corners.Length >= 2 && next < corners.Length && Time.time - offMeshSince > 1.5f)
			{
				if (offMeshFrom < 0f)
				{
					offMeshFrom = Time.time;
				}
				if (Time.time - offMeshFrom < 1.2f)
				{
					OnMesh = true;
					return;
				}
				offMeshSince = Time.time;
			}
		}
		offMeshFrom = -1f;
		firstLeg = Vector3.zero;
		corners = new Vector3[2] { feet, Goal };
		next = 1;
	}

	public Vector3 LookAhead(Vector3 feet, float distance)
	{
		if (!HasGoal)
		{
			return feet;
		}
		Vector3 vector = feet;
		float num = distance;
		for (int i = next; i < corners.Length; i++)
		{
			Vector3 vector2 = Flat(corners[i] - vector);
			float magnitude = vector2.magnitude;
			if (magnitude >= num && magnitude > 0.001f)
			{
				return vector + vector2 / magnitude * num;
			}
			num -= magnitude;
			vector = new Vector3(corners[i].x, vector.y, corners[i].z);
		}
		return Goal;
	}

	private static float Depth(Vector3 feet, Vector3 dir)
	{
		if (Physics.Raycast(feet + dir * 0.9f + Vector3.up * 0.5f, Vector3.down, out var hitInfo, 30f, -1, QueryTriggerInteraction.Ignore))
		{
			return feet.y - hitInfo.point.y;
		}
		return 99f;
	}

	private Vector3 Feel(Vector3 feet, Vector3 chest, Vector3 dir, ref float rise, out bool drop)
	{
		drop = false;
		if (Physics.SphereCast(chest, 0.18f, dir, out var hitInfo, 0.9f, -1, QueryTriggerInteraction.Ignore) && !IsBot(hitInfo))
		{
			Vector3 vector = Flat(hitInfo.normal);
			if (vector.sqrMagnitude > 0.01f)
			{
				Vector3 vector2 = Vector3.Cross(Vector3.up, vector.normalized);
				float num = ((Time.time < sideUntil && slideSide != 0f) ? slideSide : ((Vector3.Dot(vector2, dir) < 0f) ? (-1f) : 1f));
				if (Time.time >= sideUntil || slideSide == 0f)
				{
					if (Mathf.Abs(Vector3.Dot(vector2, dir)) < 0.3f)
					{
						RaycastHit hitInfo2;
						bool flag = Physics.SphereCast(chest, 0.18f, vector2, out hitInfo2, 3f, -1, QueryTriggerInteraction.Ignore);
						RaycastHit hitInfo3;
						bool num2 = Physics.SphereCast(chest, 0.18f, -vector2, out hitInfo3, 3f, -1, QueryTriggerInteraction.Ignore);
						float num3 = (flag ? hitInfo2.distance : 3f);
						float num4 = (num2 ? hitInfo3.distance : 3f);
						if (Mathf.Abs(num3 - num4) > 0.3f)
						{
							num = ((num3 > num4) ? 1f : (-1f));
						}
					}
					slideSide = num;
				}
				sideUntil = Time.time + Tuning.F("nav.side.keep", 2.5f);
				dir = (vector2 * num + vector.normalized * 0.4f).normalized;
				slideDir = dir;
				slideUntil = Time.time + Tuning.F("nav.slide.on", 0.35f);
			}
		}
		else if (Time.time < slideUntil && slideDir.sqrMagnitude > 0.01f)
		{
			dir = (slideDir + dir * 0.5f).normalized;
		}
		if (Physics.Raycast(feet + dir * 0.7f + Vector3.up * 1.4f, Vector3.down, out var hitInfo4, 3.4f, -1, QueryTriggerInteraction.Ignore))
		{
			rise = Mathf.Max(rise, hitInfo4.point.y - feet.y);
		}
		else
		{
			drop = true;
		}
		return dir;
	}

	private static bool IsBot(RaycastHit hit)
	{
		if (hit.collider != null)
		{
			return hit.collider.GetComponentInParent<RigManager>() != null;
		}
		return false;
	}

	public bool CheckStuck(Vector3 feet, float push, float dt)
	{
		if (push < 0.25f)
		{
			pushedFor = 0f;
			lastPlace = feet;
			lastPlaceAt = Time.time;
			return false;
		}
		pushedFor += dt;
		if (Time.time - lastPlaceAt < 1f)
		{
			return false;
		}
		bool num = pushedFor > 0.9f && Flat(feet - lastPlace).magnitude < 0.2f * push;
		lastPlace = feet;
		lastPlaceAt = Time.time;
		if (num)
		{
			StuckCount++;
			return num;
		}
		if (!gaveUp)
		{
			StuckCount = 0;
		}
		return num;
	}

	public static Vector3 Surface(Vector3 place, float level)
	{
		try
		{
			if (Mathf.Abs(place.y - level) < 0.6f)
			{
				return place;
			}
			foreach (RaycastHit item in Physics.RaycastAll(new Vector3(place.x, Mathf.Max(level, place.y) + 2.5f, place.z), Vector3.down, 9f, -1, QueryTriggerInteraction.Ignore))
			{
				if (!(item.collider == null) && !(item.collider.attachedRigidbody != null) && !(item.normal.y < 0.6f) && item.point.y > place.y + 0.4f && item.point.y < level + 2.5f)
				{
					place = item.point;
				}
			}
		}
		catch (Exception)
		{
		}
		return place;
	}

	public static bool RandomPlace(Vector3 around, float near, float far, out Vector3 place)
	{
		for (int i = 0; i < 20; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(near, far);
			Vector3 vector2 = around + new Vector3(vector.x, 0f, vector.y);
			try
			{
				if (NavMesh.SamplePosition(vector2, out var hit, 2.5f, -1))
				{
					place = Surface(hit.position, around.y);
					return true;
				}
			}
			catch (Exception)
			{
			}
			Vector3 start = around + Vector3.up * 1.2f;
			Vector3 vector3 = vector2 + Vector3.up * 1.2f;
			if (!Physics.Linecast(start, vector3, -1, QueryTriggerInteraction.Ignore) && Physics.Raycast(vector3, Vector3.down, out var hitInfo, 3f, -1, QueryTriggerInteraction.Ignore))
			{
				place = hitInfo.point;
				return true;
			}
		}
		place = around;
		return false;
	}

	private static Vector3 Flat(Vector3 v)
	{
		v.y = 0f;
		return v;
	}
}
