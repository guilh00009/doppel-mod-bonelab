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

	private Vector3[] corners = (Vector3[])(object)new Vector3[0];

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
		int num;
		if (HasGoal)
		{
			Vector3 val = Flat(goal - Goal);
			if (((Vector3)(ref val)).magnitude < 3f)
			{
				num = ((corners.Length >= 2) ? 1 : 0);
				goto IL_0039;
			}
		}
		num = 0;
		goto IL_0039;
		IL_0039:
		bool flag = (byte)num != 0;
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
		corners = (Vector3[])(object)new Vector3[0];
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
		Vector3 val = feet;
		Vector3 val2;
		for (int i = next; i < corners.Length; i++)
		{
			float num2 = num;
			val2 = Flat(corners[i] - val);
			num = num2 + ((Vector3)(ref val2)).magnitude;
			val = corners[i];
		}
		if (next < corners.Length)
		{
			return num;
		}
		val2 = Flat(Goal - feet);
		return ((Vector3)(ref val2)).magnitude;
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
		Vector3 val = Flat(Goal - wayGoal);
		bool flag2 = ((Vector3)(ref val)).magnitude > Tuning.F("nav.regoal", 1.5f);
		bool flag3 = !flag && OffWay(feet) > Tuning.F("nav.stray", 2f);
		if (flag || flag2 || flag3 || Time.time >= repathAt)
		{
			repathAt = Time.time + Tuning.F("nav.again", 2.5f);
			FindWay(feet, flag || flag3);
			wayGoal = Goal;
		}
		while (next < corners.Length - 1)
		{
			val = Flat(corners[next] - feet);
			if (!(((Vector3)(ref val)).magnitude < Tuning.F("nav.corner", 0.8f)))
			{
				if (!(Vector3.Dot(Flat(corners[next] - feet), Flat(corners[next + 1] - corners[next])) < 0f))
				{
					break;
				}
				val = Flat(corners[next] - feet);
				if (!(((Vector3)(ref val)).magnitude < 1.6f))
				{
					break;
				}
			}
			next++;
		}
		Vector3 val2 = ((next < corners.Length) ? corners[next] : Goal);
		val = Flat(val2 - feet);
		float magnitude = ((Vector3)(ref val)).magnitude;
		Vector3 val3 = Flat((OnMesh ? LookAhead(feet, Tuning.F("nav.ahead", 1.3f)) : val2) - feet);
		if (((Vector3)(ref val3)).sqrMagnitude < 0.0025f)
		{
			val3 = Flat(val2 - feet);
		}
		if (((Vector3)(ref val3)).sqrMagnitude < 0.0025f)
		{
			return Vector3.zero;
		}
		((Vector3)(ref val3)).Normalize();
		rise = ((magnitude < 1.2f) ? (val2.y - feet.y) : 0f);
		Vector3 val4 = val3;
		if (!OnMesh)
		{
			val3 = Feel(feet, chest, val3, ref rise, out drop);
			if (drop)
			{
				DropDepth = Depth(feet, val3);
			}
		}
		if (((Vector3)(ref lastDir)).sqrMagnitude > 0.01f && Time.time - lastDirAt < 0.3f && !drop)
		{
			float num = Tuning.F("nav.turn", 170f) * Mathf.Max(Time.deltaTime, 0.005f);
			val = Flat(Vector3.RotateTowards(lastDir, val3, num * ((float)Math.PI / 180f), 0f));
			val3 = ((Vector3)(ref val)).normalized;
		}
		if (((Vector3)(ref lastDir)).sqrMagnitude > 0.01f && Time.time - lastDirAt < 0.3f && Vector3.Angle(lastDir, val3) > 50f && Time.time - turnSaidAt > 0.4f)
		{
			turnSaidAt = Time.time;
			string[] obj = new string[18]
			{
				"nav: its way turned ",
				Vector3.SignedAngle(lastDir, val3, Vector3.up).ToString("0"),
				" deg at once: ",
				OnMesh ? "on the level's ways" : "by feel",
				(wasMesh != OnMesh) ? (" (was " + (wasMesh ? "on them" : "by feel") + ")") : "",
				", corner ",
				next.ToString(),
				" of ",
				corners.Length.ToString(),
				(lastNext != next) ? (" (was " + lastNext + ")") : "",
				(lastCorners != corners.Length) ? ", new way" : "",
				(Vector3.Angle(val4, val3) > 10f) ? (", turned " + Vector3.Angle(val4, val3).ToString("0") + " by a wall") : "",
				", ",
				magnitude.ToString("0.0"),
				" m to the corner, goal ",
				null,
				null,
				null
			};
			val = Flat(Goal - feet);
			obj[15] = ((Vector3)(ref val)).magnitude.ToString("0");
			obj[16] = " m";
			val = Flat(Goal - lastGoal);
			object obj2;
			if (!(((Vector3)(ref val)).magnitude > 0.5f))
			{
				obj2 = "";
			}
			else
			{
				val = Flat(Goal - lastGoal);
				obj2 = ", goal moved " + ((Vector3)(ref val)).magnitude.ToString("0.0") + " m";
			}
			obj[17] = (string)obj2;
			Lab.Event(string.Concat(obj));
		}
		lastDir = val3;
		lastDirAt = Time.time;
		wasMesh = OnMesh;
		lastNext = next;
		lastCorners = corners.Length;
		lastGoal = Goal;
		return val3;
	}

	private float OffWay(Vector3 feet)
	{
		if (next <= 0 || next >= corners.Length)
		{
			return 0f;
		}
		Vector3 val = corners[next - 1];
		Vector3 val2 = corners[next];
		Vector3 val3 = Flat(val2 - val);
		float num = ((((Vector3)(ref val3)).sqrMagnitude > 0.0001f) ? Mathf.Clamp01(Vector3.Dot(Flat(feet - val), val3) / ((Vector3)(ref val3)).sqrMagnitude) : 0f);
		Vector3 val4 = Flat(feet - (val + (val2 - val) * num));
		return ((Vector3)(ref val4)).magnitude;
	}

	private static float Length(Vector3[] way, int from, Vector3 start)
	{
		float num = 0f;
		Vector3 val = start;
		for (int i = from; i < way.Length; i++)
		{
			float num2 = num;
			Vector3 val2 = Flat(way[i] - val);
			num = num2 + ((Vector3)(ref val2)).magnitude;
			val = way[i];
		}
		return num;
	}

	private void FindWay(Vector3 feet, bool afresh = true)
	{
		bool onMesh = OnMesh;
		OnMesh = false;
		try
		{
			NavMeshHit val = default(NavMeshHit);
			NavMeshHit val2 = default(NavMeshHit);
			if (NavMesh.SamplePosition(feet, ref val, 2.2f, -1) && NavMesh.SamplePosition(Goal, ref val2, 3f, -1) && NavMesh.CalculatePath(((NavMeshHit)(ref val)).position, ((NavMeshHit)(ref val2)).position, -1, path) && (int)path.status != 2)
			{
				Vector3[] array = path.corners;
				if (array != null && array.Length >= 2)
				{
					Vector3 val3 = Flat(array[1] - array[0]);
					Vector3 val4 = ((onMesh && corners.Length >= 2 && next < corners.Length) ? Flat(corners[next] - feet) : Vector3.zero);
					if (!afresh && ((Vector3)(ref val4)).sqrMagnitude > 0.2f && ((Vector3)(ref val3)).sqrMagnitude > 0.01f && Vector3.Angle(val4, val3) > Tuning.F("nav.swerve", 40f))
					{
						Vector3 val5 = Flat(corners[corners.Length - 1] - Goal);
						if (((Vector3)(ref val5)).magnitude < Tuning.F("nav.keep.end", 5f) && Length(array, 1, array[0]) > Length(corners, next, feet) * Tuning.F("nav.keep.shorter", 0.75f) - 1f)
						{
							Swerves++;
							OnMesh = true;
							return;
						}
					}
					if (((Vector3)(ref firstLeg)).sqrMagnitude < 0.01f || Vector3.Angle(firstLeg, val3) > 55f)
					{
						wayKeptSince = Time.time;
					}
					firstLeg = val3;
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
		corners = (Vector3[])(object)new Vector3[2] { feet, Goal };
		next = 1;
	}

	public Vector3 LookAhead(Vector3 feet, float distance)
	{
		if (!HasGoal)
		{
			return feet;
		}
		Vector3 val = feet;
		float num = distance;
		for (int i = next; i < corners.Length; i++)
		{
			Vector3 val2 = Flat(corners[i] - val);
			float magnitude = ((Vector3)(ref val2)).magnitude;
			if (magnitude >= num && magnitude > 0.001f)
			{
				return val + val2 / magnitude * num;
			}
			num -= magnitude;
			((Vector3)(ref val))._002Ector(corners[i].x, val.y, corners[i].z);
		}
		return Goal;
	}

	private static float Depth(Vector3 feet, Vector3 dir)
	{
		RaycastHit val = default(RaycastHit);
		if (Physics.Raycast(feet + dir * 0.9f + Vector3.up * 0.5f, Vector3.down, ref val, 30f, -1, (QueryTriggerInteraction)1))
		{
			return feet.y - ((RaycastHit)(ref val)).point.y;
		}
		return 99f;
	}

	private Vector3 Feel(Vector3 feet, Vector3 chest, Vector3 dir, ref float rise, out bool drop)
	{
		drop = false;
		RaycastHit hit = default(RaycastHit);
		Vector3 val5;
		if (Physics.SphereCast(chest, 0.18f, dir, ref hit, 0.9f, -1, (QueryTriggerInteraction)1) && !IsBot(hit))
		{
			Vector3 val = Flat(((RaycastHit)(ref hit)).normal);
			if (((Vector3)(ref val)).sqrMagnitude > 0.01f)
			{
				Vector3 val2 = Vector3.Cross(Vector3.up, ((Vector3)(ref val)).normalized);
				float num = ((Time.time < sideUntil && slideSide != 0f) ? slideSide : ((Vector3.Dot(val2, dir) < 0f) ? (-1f) : 1f));
				if (Time.time >= sideUntil || slideSide == 0f)
				{
					if (Mathf.Abs(Vector3.Dot(val2, dir)) < 0.3f)
					{
						RaycastHit val3 = default(RaycastHit);
						bool flag = Physics.SphereCast(chest, 0.18f, val2, ref val3, 3f, -1, (QueryTriggerInteraction)1);
						RaycastHit val4 = default(RaycastHit);
						bool num2 = Physics.SphereCast(chest, 0.18f, -val2, ref val4, 3f, -1, (QueryTriggerInteraction)1);
						float num3 = (flag ? ((RaycastHit)(ref val3)).distance : 3f);
						float num4 = (num2 ? ((RaycastHit)(ref val4)).distance : 3f);
						if (Mathf.Abs(num3 - num4) > 0.3f)
						{
							num = ((num3 > num4) ? 1f : (-1f));
						}
					}
					slideSide = num;
				}
				sideUntil = Time.time + Tuning.F("nav.side.keep", 2.5f);
				val5 = val2 * num + ((Vector3)(ref val)).normalized * 0.4f;
				dir = ((Vector3)(ref val5)).normalized;
				slideDir = dir;
				slideUntil = Time.time + Tuning.F("nav.slide.on", 0.35f);
			}
		}
		else if (Time.time < slideUntil && ((Vector3)(ref slideDir)).sqrMagnitude > 0.01f)
		{
			val5 = slideDir + dir * 0.5f;
			dir = ((Vector3)(ref val5)).normalized;
		}
		RaycastHit val6 = default(RaycastHit);
		if (Physics.Raycast(feet + dir * 0.7f + Vector3.up * 1.4f, Vector3.down, ref val6, 3.4f, -1, (QueryTriggerInteraction)1))
		{
			rise = Mathf.Max(rise, ((RaycastHit)(ref val6)).point.y - feet.y);
		}
		else
		{
			drop = true;
		}
		return dir;
	}

	private static bool IsBot(RaycastHit hit)
	{
		if ((Object)(object)((RaycastHit)(ref hit)).collider != (Object)null)
		{
			return (Object)(object)((Component)((RaycastHit)(ref hit)).collider).GetComponentInParent<RigManager>() != (Object)null;
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
		int num;
		if (pushedFor > 0.9f)
		{
			Vector3 val = Flat(feet - lastPlace);
			num = ((((Vector3)(ref val)).magnitude < 0.2f * push) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		lastPlace = feet;
		lastPlaceAt = Time.time;
		if (num != 0)
		{
			StuckCount++;
			return (byte)num != 0;
		}
		if (!gaveUp)
		{
			StuckCount = 0;
		}
		return (byte)num != 0;
	}

	public static Vector3 Surface(Vector3 place, float level)
	{
		try
		{
			if (Mathf.Abs(place.y - level) < 0.6f)
			{
				return place;
			}
			foreach (RaycastHit item in Physics.RaycastAll(new Vector3(place.x, Mathf.Max(level, place.y) + 2.5f, place.z), Vector3.down, 9f, -1, (QueryTriggerInteraction)1))
			{
				RaycastHit current = item;
				if (!((Object)(object)((RaycastHit)(ref current)).collider == (Object)null) && !((Object)(object)((RaycastHit)(ref current)).collider.attachedRigidbody != (Object)null) && !(((RaycastHit)(ref current)).normal.y < 0.6f) && ((RaycastHit)(ref current)).point.y > place.y + 0.4f && ((RaycastHit)(ref current)).point.y < level + 2.5f)
				{
					place = ((RaycastHit)(ref current)).point;
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
		NavMeshHit val3 = default(NavMeshHit);
		RaycastHit val6 = default(RaycastHit);
		for (int i = 0; i < 20; i++)
		{
			Vector2 insideUnitCircle = Random.insideUnitCircle;
			Vector2 val = ((Vector2)(ref insideUnitCircle)).normalized * Random.Range(near, far);
			Vector3 val2 = around + new Vector3(val.x, 0f, val.y);
			try
			{
				if (NavMesh.SamplePosition(val2, ref val3, 2.5f, -1))
				{
					place = Surface(((NavMeshHit)(ref val3)).position, around.y);
					return true;
				}
			}
			catch (Exception)
			{
			}
			Vector3 val4 = around + Vector3.up * 1.2f;
			Vector3 val5 = val2 + Vector3.up * 1.2f;
			if (!Physics.Linecast(val4, val5, -1, (QueryTriggerInteraction)1) && Physics.Raycast(val5, Vector3.down, ref val6, 3f, -1, (QueryTriggerInteraction)1))
			{
				place = ((RaycastHit)(ref val6)).point;
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
