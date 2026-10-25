using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
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
		if ((Object)(object)head == (Object)null)
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
		Vector3 val = Flat(head.forward);
		Vector3 val2 = ((Vector3)(ref val)).normalized;
		if (((Vector3)(ref val2)).sqrMagnitude < 0.01f)
		{
			val2 = Vector3.forward;
		}
		finish = head.position + val2 * Tuning.F("race.far", 80f);
		RaycastHit val3 = default(RaycastHit);
		if (Physics.Raycast(head.position, head.forward, ref val3, Tuning.F("race.most", 300f), -1, (QueryTriggerInteraction)1) && Vector3.Dot(((RaycastHit)(ref val3)).normal, Vector3.up) > 0.6f)
		{
			val = ((RaycastHit)(ref val3)).point - head.position;
			if (((Vector3)(ref val)).magnitude > 15f)
			{
				finish = ((RaycastHit)(ref val3)).point;
				goto IL_0129;
			}
		}
		RaycastHit val4 = default(RaycastHit);
		if (Physics.Raycast(finish + Vector3.up * 20f, Vector3.down, ref val4, 60f, -1, (QueryTriggerInteraction)1))
		{
			finish = ((RaycastHit)(ref val4)).point;
		}
		goto IL_0129;
		IL_0129:
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
		Vector3 val5 = head.position - Vector3.up * 1.5f;
		RaycastHit val6 = default(RaycastHit);
		if (Physics.Raycast(head.position, Vector3.down, ref val6, 3f, -1, (QueryTriggerInteraction)1))
		{
			val5 = ((RaycastHit)(ref val6)).point;
		}
		Vector3 up = Vector3.up;
		val = Flat(finish - val5);
		Vector3 val7 = Vector3.Cross(up, ((Vector3)(ref val)).normalized);
		float num = Tuning.F("race.apart", 3.5f);
		float num2 = 2.5f + num * (float)(racers.Count + 1);
		Vector3 val8 = val5 + Vector3.up * 0.6f;
		RaycastHit val9 = default(RaycastHit);
		float num3 = (Physics.SphereCast(val8, 0.5f, val7, ref val9, num2, -1, (QueryTriggerInteraction)1) ? ((RaycastHit)(ref val9)).distance : num2);
		RaycastHit val10 = default(RaycastHit);
		float num4 = (Physics.SphereCast(val8, 0.5f, -val7, ref val10, num2, -1, (QueryTriggerInteraction)1) ? ((RaycastHit)(ref val10)).distance : num2);
		if (num4 > num3)
		{
			val7 = -val7;
		}
		Lab.Event("race: grid to the " + ((num4 > num3) ? "left" : "right") + " (room " + Mathf.Max(num4, num3).ToString("0.0") + " of " + num2.ToString("0.0") + " m)");
		ClearGrid(val5, 2.5f + num * (float)(racers.Count + 1) + 4f);
		if ((Object)(object)Player.RigManager != (Object)null && (Object)(object)Player.RigManager.activeSeat == (Object)null)
		{
			SpawnKart(val5 + val7 * 2.5f);
		}
		for (int i = 0; i < racers.Count; i++)
		{
			Vector3 val11 = val5 + val7 * (2.5f + num * (float)(i + 1));
			SpawnKart(val11);
			racers[i].RaceLineUp(val11 - val7 * 1.3f);
		}
		stage = Stage.LineUp;
		stageAt = Time.time;
		launched = 0;
		countSaid = -1;
		MakePost();
		Spawnables.Notify("Race! Get in your kart; the bots are going to theirs");
		string[] obj = new string[7]
		{
			"race: to ",
			((Vector3)(ref finish)).ToString("0"),
			", ",
			null,
			null,
			null,
			null
		};
		val = Flat(finish - val5);
		obj[3] = ((Vector3)(ref val)).magnitude.ToString("0");
		obj[4] = " m off, ";
		obj[5] = racers.Count.ToString();
		obj[6] = " bots";
		Lab.Event(string.Concat(obj));
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
		if ((Object)(object)post != (Object)null)
		{
			Object.Destroy((Object)(object)post);
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
		Vector3 val;
		foreach (Bot racer in racers)
		{
			string name = racer.Name;
			if (placed.Exists((string p) => p.StartsWith(name + " ")))
			{
				continue;
			}
			Vector3? bodyAt = racer.BodyAt;
			if (bodyAt.HasValue)
			{
				val = Flat(bodyAt.Value - finish);
				if (((Vector3)(ref val)).magnitude < num2)
				{
					Place(name);
				}
			}
		}
		Transform head = Player.Head;
		if (!youIn && (Object)(object)head != (Object)null)
		{
			val = Flat(head.position - finish);
			if (((Vector3)(ref val)).magnitude < num2)
			{
				youIn = true;
				Place("you");
			}
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
			post = GameObject.CreatePrimitive((PrimitiveType)2);
			Object.Destroy((Object)(object)post.GetComponent<Collider>());
			((Object)post).name = "Race finish";
			post.transform.position = finish + Vector3.up * 6f;
			post.transform.localScale = new Vector3(1.5f, 6f, 1.5f);
			Renderer component = post.GetComponent<Renderer>();
			Shader val = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
			if ((Object)(object)component != (Object)null && (Object)(object)val != (Object)null)
			{
				component.material = new Material(val);
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
			var (val, num2) = toTurn[num];
			if (Time.time - num2 < 0.6f)
			{
				continue;
			}
			try
			{
				Driving.Model model = Driving.Of(Lessons.Drive());
				Seat val2 = null;
				Transform val3 = null;
				foreach (Collider item in Physics.OverlapSphere(val, 2f, -1, (QueryTriggerInteraction)1))
				{
					Poolee val4 = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
					if (!((Object)(object)val4 == (Object)null) && !((Object)(object)val4.SpawnableCrate == (Object)null) && model != null && !(((Scannable)val4.SpawnableCrate).Barcode.ID != model.barcode))
					{
						val2 = ((Component)val4).GetComponentInChildren<Seat>(true);
						val3 = ((Component)val4).transform;
						if ((Object)(object)val2 != (Object)null)
						{
							break;
						}
					}
				}
				if ((Object)(object)val2 == (Object)null || (Object)(object)val3 == (Object)null)
				{
					if (Time.time - num2 > 3f)
					{
						Lab.Event("race: no kart found at its place to turn");
						toTurn.RemoveAt(num);
					}
					continue;
				}
				Vector3 val5 = Flat(((Component)val2).transform.forward);
				Vector3 val6 = ((Vector3)(ref val5)).normalized * (model?.forward ?? 1f);
				val5 = Flat(finish - ((Component)val3).transform.position);
				Vector3 normalized = ((Vector3)(ref val5)).normalized;
				float num3 = Vector3.SignedAngle(val6, normalized, Vector3.up);
				((Component)val3).transform.RotateAround(((Component)val2).transform.position, Vector3.up, num3);
				Lab.Event("race: kart turned " + num3.ToString("0") + " deg to face the finish");
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
			foreach (Seat item in Object.FindObjectsOfType<Seat>())
			{
				Poolee val = (((Object)(object)item != (Object)null) ? ((Component)item).GetComponentInParent<Poolee>() : null);
				if ((Object)(object)val == (Object)null || !hashSet.Add(((Il2CppObjectBase)(object)val).Pointer) || !((Component)val).gameObject.activeInHierarchy || (Object)(object)val.SpawnableCrate == (Object)null || ((Scannable)val.SpawnableCrate).Barcode.ID != model.barcode)
				{
					continue;
				}
				bool flag = false;
				foreach (Seat componentsInChild in ((Component)val).GetComponentsInChildren<Seat>(true))
				{
					flag |= (Object)(object)componentsInChild != (Object)null && (Object)(object)componentsInChild.rigManager != (Object)null;
				}
				if (!flag)
				{
					Vector3 val2 = Flat(((Component)val).transform.position - around);
					if (!(((Vector3)(ref val2)).magnitude > within))
					{
						val.Despawn();
						num++;
					}
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
				Vector3 val = Flat(finish - at);
				Vector3 val2 = ((Vector3)(ref val)).normalized;
				if (((Vector3)(ref val2)).sqrMagnitude < 0.01f)
				{
					val2 = Vector3.forward;
				}
				RaycastHit val3 = default(RaycastHit);
				if (Physics.Raycast(at + Vector3.up * 3f, Vector3.down, ref val3, 8f, -1, (QueryTriggerInteraction)1))
				{
					at = ((RaycastHit)(ref val3)).point;
				}
				toTurn.Add((at, Time.time));
				HelperMethods.SpawnCrate(model.barcode, at + Vector3.up * 0.3f, Quaternion.LookRotation(val2), Vector3.one, false, (Action<GameObject>)null, (Action<GameObject>)null);
			}
		}
		catch (Exception ex)
		{
			Lab.Event("race: no kart: " + ex.Message);
		}
	}
}
