using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class Race
{
	private enum Stage
	{
		Off,
		LineUp,
		Count,
		Racing
	}

	private static Stage stage = Stage.Off;

	private static Vector3 finish;

	private static float stageAt;

	private static float goAt;

	private static float endAt;

	private static GameObject post;

	private static readonly List<Bot> racers = new List<Bot>();

	private static readonly List<string> placed = new List<string>();

	private static readonly List<(Vector3 spot, float at)> toTurn = new List<(Vector3, float)>();

	private static bool youIn;

	private static int countSaid;

	private static int launched;

	public static bool On => stage != Stage.Off;

	public static void Start()
	{
		Transform head = Player.Head;
		if (head == null)
		{
			return;
		}
		if (On)
		{
			Stop("a new one");
		}
		if (Lessons.Drive() == null)
		{
			Spawnables.Notify("No race: show a bot how to drive first (drive a kart while it learns)");
			return;
		}
		Vector3 vector = Flat(head.forward).normalized;
		if (vector.sqrMagnitude < 0.01f)
		{
			vector = Vector3.forward;
		}
		finish = head.position + vector * Tuning.F("race.far", 80f);
		RaycastHit hitInfo2;
		if (Physics.Raycast(head.position, head.forward, out var hitInfo, Tuning.F("race.most", 300f), -1, QueryTriggerInteraction.Ignore) && Vector3.Dot(hitInfo.normal, Vector3.up) > 0.6f && (hitInfo.point - head.position).magnitude > 15f)
		{
			finish = hitInfo.point;
		}
		else if (Physics.Raycast(finish + Vector3.up * 20f, Vector3.down, out hitInfo2, 60f, -1, QueryTriggerInteraction.Ignore))
		{
			finish = hitInfo2.point;
		}
		racers.Clear();
		placed.Clear();
		youIn = false;
		foreach (Bot item in BotManager.All)
		{
			if (!item.enemy && !item.dead && !item.Puppet)
			{
				racers.Add(item);
			}
		}
		if (racers.Count == 0)
		{
			Spawnables.Notify("No race: no bots of yours about");
			return;
		}
		Vector3 vector2 = head.position - Vector3.up * 1.5f;
		if (Physics.Raycast(head.position, Vector3.down, out var hitInfo3, 3f, -1, QueryTriggerInteraction.Ignore))
		{
			vector2 = hitInfo3.point;
		}
		Vector3 vector3 = Vector3.Cross(Vector3.up, Flat(finish - vector2).normalized);
		float num = Tuning.F("race.apart", 3.5f);
		float num2 = 2.5f + num * (float)(racers.Count + 1);
		Vector3 origin = vector2 + Vector3.up * 0.6f;
		RaycastHit hitInfo4;
		float num3 = (Physics.SphereCast(origin, 0.5f, vector3, out hitInfo4, num2, -1, QueryTriggerInteraction.Ignore) ? hitInfo4.distance : num2);
		RaycastHit hitInfo5;
		float num4 = (Physics.SphereCast(origin, 0.5f, -vector3, out hitInfo5, num2, -1, QueryTriggerInteraction.Ignore) ? hitInfo5.distance : num2);
		if (num4 > num3)
		{
			vector3 = -vector3;
		}
		Lab.Event("race: grid to the " + ((num4 > num3) ? "left" : "right") + " (room " + Mathf.Max(num4, num3).ToString("0.0") + " of " + num2.ToString("0.0") + " m)");
		ClearGrid(vector2, 2.5f + num * (float)(racers.Count + 1) + 4f);
		if (Player.RigManager != null && Player.RigManager.activeSeat == null)
		{
			SpawnKart(vector2 + vector3 * 2.5f);
		}
		for (int i = 0; i < racers.Count; i++)
		{
			Vector3 vector4 = vector2 + vector3 * (2.5f + num * (float)(i + 1));
			SpawnKart(vector4);
			racers[i].RaceLineUp(vector4 - vector3 * 1.3f);
		}
		stage = Stage.LineUp;
		stageAt = Time.time;
		launched = 0;
		countSaid = -1;
		MakePost();
		Spawnables.Notify("Race! Get in your kart; the bots are going to theirs");
		Lab.Event("race: to " + finish.ToString("0") + ", " + Flat(finish - vector2).magnitude.ToString("0") + " m off, " + racers.Count + " bots");
	}

	public static void Stop(string why)
	{
		if (!On)
		{
			return;
		}
		stage = Stage.Off;
		foreach (Bot racer in racers)
		{
			racer.RaceLineUpOff();
		}
		if (post != null)
		{
			UnityEngine.Object.Destroy(post);
		}
		post = null;
		Lab.Event("race over (" + why + "): " + ((placed.Count > 0) ? string.Join(", ", placed) : "nobody finished"));
	}

	public static void Step()
	{
		if (!On)
		{
			return;
		}
		float time = Time.time;
		racers.RemoveAll((Bot b) => b?.dead ?? true);
		if (racers.Count == 0)
		{
			Stop("no bots left");
			return;
		}
		TurnKarts();
		if (stage == Stage.LineUp)
		{
			if (racers.TrueForAll((Bot b) => b.RaceReady) || time - stageAt > Tuning.F("race.lineup", 20f))
			{
				stage = Stage.Count;
				stageAt = time;
				goAt = time + Tuning.F("race.count", 3f);
				endAt = goAt + Tuning.F("race.longest", 240f);
				Lab.Event("race: lined up (" + racers.FindAll((Bot b) => b.RaceReady).Count + " of " + racers.Count + " by their karts)");
			}
			return;
		}
		if (stage == Stage.Count)
		{
			int num = Mathf.CeilToInt(goAt - time);
			if (num != countSaid && num >= 0)
			{
				countSaid = num;
				Spawnables.Notify((num > 0) ? num.ToString() : "GO! Race to the glowing post");
			}
			if (time < goAt)
			{
				return;
			}
			stage = Stage.Racing;
		}
		while (launched < racers.Count && time >= goAt + (float)launched * Tuning.F("race.gap", 0.4f))
		{
			Bot bot = racers[launched++];
			bot.RaceLineUpOff();
			if (!bot.RaceDrive(finish))
			{
				Lab.Event("race: " + bot.Name + " could not set off");
			}
		}
		float num2 = Tuning.F("race.finish", 8f);
		foreach (Bot racer in racers)
		{
			string name = racer.Name;
			if (!placed.Exists((string p) => p.StartsWith(name + " ")))
			{
				Vector3? bodyAt = racer.BodyAt;
				if (bodyAt.HasValue && Flat(bodyAt.Value - finish).magnitude < num2)
				{
					Place(name);
				}
			}
		}
		Transform head = Player.Head;
		if (!youIn && head != null && Flat(head.position - finish).magnitude < num2)
		{
			youIn = true;
			Place("you");
		}
		if (placed.Count >= racers.Count + 1 || time > endAt)
		{
			Stop((time > endAt) ? "out of time" : "everyone in");
		}
	}

	private static void Place(string who)
	{
		int n = placed.Count + 1;
		string text = who + " " + Nth(n) + " (" + (Time.time - goAt).ToString("0.0") + " s)";
		placed.Add(text);
		Spawnables.Notify(text);
		Lab.Event("race: " + text);
	}

	private static string Nth(int n)
	{
		return n switch
		{
			3 => "3rd", 
			2 => "2nd", 
			1 => "1st", 
			_ => n + "th", 
		};
	}

	private static Vector3 Flat(Vector3 v)
	{
		return new Vector3(v.x, 0f, v.z);
	}

	private static void MakePost()
	{
		try
		{
			post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			UnityEngine.Object.Destroy(post.GetComponent<Collider>());
			post.name = "Race finish";
			post.transform.position = finish + Vector3.up * 6f;
			post.transform.localScale = new Vector3(1.5f, 6f, 1.5f);
			Renderer component = post.GetComponent<Renderer>();
			Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
			if (component != null && shader != null)
			{
				component.material = new Material(shader);
				component.material.color = new Color(1f, 0.85f, 0.1f);
			}
		}
		catch (Exception ex)
		{
			Lab.Event("race: no post: " + ex.Message);
		}
	}

	private static void TurnKarts()
	{
		for (int num = toTurn.Count - 1; num >= 0; num--)
		{
			var (position, num2) = toTurn[num];
			if (Time.time - num2 < 0.6f)
			{
				continue;
			}
			try
			{
				Driving.Model model = Driving.Of(Lessons.Drive());
				Seat seat = null;
				Transform transform = null;
				foreach (Collider item in Physics.OverlapSphere(position, 2f, -1, QueryTriggerInteraction.Ignore))
				{
					Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
					if (!(poolee == null) && !(poolee.SpawnableCrate == null) && model != null && !(poolee.SpawnableCrate.Barcode.ID != model.barcode))
					{
						seat = poolee.GetComponentInChildren<Seat>(includeInactive: true);
						transform = poolee.transform;
						if (seat != null)
						{
							break;
						}
					}
				}
				if (seat == null || transform == null)
				{
					if (Time.time - num2 > 3f)
					{
						Lab.Event("race: no kart found at its place to turn");
						toTurn.RemoveAt(num);
					}
					continue;
				}
				Vector3 from = Flat(seat.transform.forward).normalized * (model?.forward ?? 1f);
				Vector3 normalized = Flat(finish - transform.transform.position).normalized;
				float angle = Vector3.SignedAngle(from, normalized, Vector3.up);
				transform.transform.RotateAround(seat.transform.position, Vector3.up, angle);
				Lab.Event("race: kart turned " + angle.ToString("0") + " deg to face the finish");
			}
			catch (Exception ex)
			{
				Lab.Event("race: kart not turned: " + ex.Message);
			}
			toTurn.RemoveAt(num);
		}
	}

	private static void ClearGrid(Vector3 around, float within)
	{
		try
		{
			Driving.Model model = Driving.Of(Lessons.Drive());
			if (model == null || string.IsNullOrEmpty(model.barcode))
			{
				return;
			}
			int num = 0;
			HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
			foreach (Seat item in UnityEngine.Object.FindObjectsOfType<Seat>())
			{
				Poolee poolee = ((item != null) ? item.GetComponentInParent<Poolee>() : null);
				if (poolee == null || !hashSet.Add(poolee.Pointer) || !poolee.gameObject.activeInHierarchy || poolee.SpawnableCrate == null || poolee.SpawnableCrate.Barcode.ID != model.barcode)
				{
					continue;
				}
				bool flag = false;
				foreach (Seat componentsInChild in poolee.GetComponentsInChildren<Seat>(includeInactive: true))
				{
					flag |= componentsInChild != null && componentsInChild.rigManager != null;
				}
				if (!flag && !(Flat(poolee.transform.position - around).magnitude > within))
				{
					poolee.Despawn();
					num++;
				}
			}
			if (num > 0)
			{
				Lab.Event("race: " + num + " empty karts cleared off the grid");
			}
		}
		catch (Exception ex)
		{
			Lab.Event("race: grid not cleared: " + ex.Message);
		}
	}

	private static void SpawnKart(Vector3 at)
	{
		try
		{
			Driving.Model model = Driving.Of(Lessons.Drive());
			if (model != null && !string.IsNullOrEmpty(model.barcode))
			{
				Vector3 forward = Flat(finish - at).normalized;
				if (forward.sqrMagnitude < 0.01f)
				{
					forward = Vector3.forward;
				}
				if (Physics.Raycast(at + Vector3.up * 3f, Vector3.down, out var hitInfo, 8f, -1, QueryTriggerInteraction.Ignore))
				{
					at = hitInfo.point;
				}
				toTurn.Add((at, Time.time));
				HelperMethods.SpawnCrate(model.barcode, at + Vector3.up * 0.3f, Quaternion.LookRotation(forward), Vector3.one);
			}
		}
		catch (Exception ex)
		{
			Lab.Event("race: no kart: " + ex.Message);
		}
	}
}
