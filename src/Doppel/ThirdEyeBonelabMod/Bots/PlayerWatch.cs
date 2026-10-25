using System;
using BoneLib;
using Il2CppSLZ.Marrow;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class PlayerWatch
{
	private const float BurstGap = 0.16f;

	private static float lastShot = -10f;

	private static float burstStart = -10f;

	private static bool lastAuto;

	private static int burstRounds;

	private static Gun watchedGun;

	private static bool hadMagazine;

	private static float lastAmmoFraction = 1f;

	private static float sampleAt;

	private static float lastNearDistance = -1f;

	private static float chooseAt;

	private static float seenAt = -100f;

	private static float nearestBefore = -1f;

	private static float nearestBeforeAt;

	private static Vector3 placeBefore;

	private static float placeBeforeAt;

	private static float armedSince = -100f;

	private static bool wasArmed;

	private static float closeCallAt = -100f;

	private static readonly float[] seenByPlayer = new float[Knowledge.Features];

	private static Vector3 movedFrom;

	private static float movedFromAt;

	public static float FiredAt { get; private set; } = -10f;


	public static Vector3 ShotAt { get; private set; }

	public static Vector3 AimFrom { get; private set; }

	public static Vector3 AimAlong { get; private set; }

	public static bool Fighting => Time.time - FiredAt < 6f;

	public static int Imitated { get; private set; }

	public static float ShotsSeen { get; private set; }

	public static void OnGunFired(Gun gun)
	{
		if (gun == null || !HeldByPlayer(gun))
		{
			return;
		}
		ShotsSeen += 1f;
		float time = Time.time;
		bool num = gun.fireMode == Gun.FireMode.AUTOMATIC;
		Transform firePointTransform = gun.firePointTransform;
		if (firePointTransform != null)
		{
			AimFrom = firePointTransform.position;
			AimAlong = firePointTransform.forward;
			ShotAt = (Physics.Raycast(firePointTransform.position, firePointTransform.forward, out var hitInfo, 80f, -1, QueryTriggerInteraction.Ignore) ? hitInfo.point : (firePointTransform.position + firePointTransform.forward * 30f));
		}
		float num2 = time - lastShot;
		if (num)
		{
			if (num2 > 0.16f || !lastAuto)
			{
				if (lastAuto && num2 < 3f && burstRounds > 0)
				{
					Knowledge.Style.Add(Knowledge.Style.bursts, lastShot - burstStart + 0.08f);
					Knowledge.Style.Add(Knowledge.Style.burstRounds, burstRounds);
					Knowledge.Style.Add(Knowledge.Style.pauses, num2);
				}
				burstStart = time;
				burstRounds = 0;
			}
			burstRounds++;
		}
		else if (!lastAuto && num2 > 0.05f && num2 < 2.5f)
		{
			Knowledge.Style.Add(Knowledge.Style.gaps, num2);
		}
		lastAuto = num;
		lastShot = time;
		FiredAt = time;
	}

	private static void Choose(Gun gun)
	{
		if (Time.time < chooseAt)
		{
			return;
		}
		_ = Time.time;
		_ = chooseAt;
		chooseAt = Time.time + 0.5f;
		Transform head = Player.Head;
		if (head == null)
		{
			return;
		}
		Vector3 position = head.position;
		bool flag = gun != null;
		if (flag && !wasArmed)
		{
			armedSince = Time.time;
		}
		wasArmed = flag;
		int num = 0;
		int num2 = 0;
		float num3 = 999f;
		float num4 = 999f;
		foreach (Targets.Target item in Targets.Hostiles())
		{
			float magnitude = (item.Point - position).magnitude;
			if (!(magnitude > 45f))
			{
				num2++;
				num4 = Mathf.Min(num4, magnitude);
				if (Targets.InSight(position, item.Point, item.transform, Player.RigManager))
				{
					num++;
					num3 = Mathf.Min(num3, magnitude);
				}
			}
		}
		bool flag2 = num > 0;
		if (flag2)
		{
			seenAt = Time.time;
		}
		if (num4 < 1.6f)
		{
			closeCallAt = Time.time;
		}
		float num5 = ((Time.time - placeBeforeAt < 1.5f) ? (Flat(position - placeBefore).magnitude / Mathf.Max(Time.time - placeBeforeAt, 0.1f)) : 0f);
		float num6 = ((nearestBefore > 0f && flag2 && Time.time - nearestBeforeAt < 1.5f) ? ((num3 - nearestBefore) / Mathf.Max(Time.time - nearestBeforeAt, 0.1f)) : 0f);
		placeBefore = position;
		placeBeforeAt = Time.time;
		nearestBefore = (flag2 ? num3 : (-1f));
		nearestBeforeAt = Time.time;
		seenByPlayer[0] = 1f;
		seenByPlayer[1] = (flag2 ? 1f : 0f);
		seenByPlayer[2] = ((num3 < 5f) ? 1f : 0f);
		seenByPlayer[3] = Mathf.Clamp01((float)num / 4f);
		seenByPlayer[4] = (flag ? 1f : 0f);
		seenByPlayer[5] = ((flag && AmmoFraction(gun) < 0.3f) ? 1f : 0f);
		seenByPlayer[6] = 0f;
		seenByPlayer[7] = ((!flag2 && Time.time - seenAt < 25f) ? 1f : 0f);
		seenByPlayer[8] = ((Time.time - closeCallAt < 5f) ? 1f : 0f);
		seenByPlayer[9] = (Fighting ? 1f : 0f);
		string text = null;
		if (Time.time - GunMoves.PlayerPunchedAt < 1f && !flag)
		{
			text = "Brawl";
		}
		else if (flag2 && flag && Time.time - FiredAt < 1.5f)
		{
			text = "Fight";
		}
		else if (!flag2 && num2 > 0 && Time.time - seenAt < 4f && Time.time - seenAt > 0.4f)
		{
			text = "TakeCover";
		}
		else if (flag2 && num3 < 7f && num6 > 0.6f)
		{
			text = "Retreat";
		}
		else if (flag && Time.time - armedSince < 2f)
		{
			text = "ArmUp";
		}
		else if (!flag2 && Time.time - seenAt < 25f && num5 > 0.8f)
		{
			text = "Investigate";
		}
		else if (num2 == 0 && num5 > 0.8f)
		{
			text = "Explore";
		}
		else if (!flag2 && num5 < 0.3f)
		{
			text = "Idle";
		}
		if (text != null)
		{
			Knowledge.Learn(text, seenByPlayer, Tuning.F("teach.imitate", 0.6f));
			Imitated++;
			if (Imitated % 20 == 0)
			{
				Lab.Event("teach: learned " + Imitated + " of your choices (last: " + text + ")");
			}
		}
	}

	private static void Strafing()
	{
		Transform head = Player.Head;
		if (head == null || Time.time - FiredAt > 2f)
		{
			movedFromAt = 0f;
			return;
		}
		if (movedFromAt <= 0f || Time.time - movedFromAt < 0.5f)
		{
			if (movedFromAt <= 0f)
			{
				movedFrom = head.position;
				movedFromAt = Time.time;
			}
			return;
		}
		Vector3 vector = Flat(head.position - movedFrom) / (Time.time - movedFromAt);
		movedFrom = head.position;
		movedFromAt = Time.time;
		if (!(vector.magnitude < 0.5f))
		{
			Vector3 rhs = ((Flat(AimAlong).sqrMagnitude > 0.01f) ? Flat(AimAlong).normalized : Flat(head.forward).normalized);
			float num = Mathf.Abs(Vector3.Dot(vector.normalized, Vector3.Cross(Vector3.up, rhs)));
			Knowledge.Style.Add(Knowledge.Style.strafe, (num > 0.6f) ? 1f : 0f);
		}
	}

	private static Vector3 Flat(Vector3 v)
	{
		v.y = 0f;
		return v;
	}

	public static void Update()
	{
		if (GunMoves.Teaching)
		{
			Choose(HeldGun());
		}
		Strafing();
		Gun gun = HeldGun();
		if (gun != watchedGun)
		{
			watchedGun = gun;
			hadMagazine = gun != null && SafeHasMagazine(gun);
			lastAmmoFraction = ((gun != null) ? AmmoFraction(gun) : 1f);
		}
		else if (gun != null)
		{
			bool flag = SafeHasMagazine(gun);
			if (hadMagazine && !flag && lastAmmoFraction < 0.95f)
			{
				Knowledge.Style.Add(Knowledge.Style.reloadAt, lastAmmoFraction);
			}
			hadMagazine = flag;
			if (flag)
			{
				lastAmmoFraction = AmmoFraction(gun);
			}
		}
		if (Time.time < sampleAt)
		{
			return;
		}
		sampleAt = Time.time + 0.5f;
		Transform head = Player.Head;
		if (head == null || gun == null || !Fighting)
		{
			lastNearDistance = -1f;
			return;
		}
		float num = float.MaxValue;
		foreach (Targets.Target item in Targets.Hostiles())
		{
			num = Mathf.Min(num, (item.Point - head.position).magnitude);
		}
		if (num > 60f)
		{
			lastNearDistance = -1f;
			return;
		}
		Knowledge.Style.Add(Knowledge.Style.fightDistance, num);
		if (lastNearDistance > 0f && num < 6f)
		{
			float num2 = num - lastNearDistance;
			Knowledge.Style.Add(Knowledge.Style.giveGround, (num2 > 0.1f) ? 1f : 0f);
		}
		lastNearDistance = num;
	}

	private static Gun HeldGun()
	{
		Gun componentInHand = Player.GetComponentInHand<Gun>(Player.RightHand);
		if (!(componentInHand != null))
		{
			return Player.GetComponentInHand<Gun>(Player.LeftHand);
		}
		return componentInHand;
	}

	private static bool HeldByPlayer(Gun gun)
	{
		Grip triggerGrip = gun.triggerGrip;
		if (triggerGrip == null || triggerGrip.attachedHands == null)
		{
			return false;
		}
		Hand[] array = new Hand[2]
		{
			Player.LeftHand,
			Player.RightHand
		};
		foreach (Hand hand in array)
		{
			if (hand != null && triggerGrip.attachedHands.Contains(hand))
			{
				return true;
			}
		}
		return false;
	}

	private static bool SafeHasMagazine(Gun gun)
	{
		try
		{
			return gun.HasMagazine();
		}
		catch (Exception)
		{
			return true;
		}
	}

	private static float AmmoFraction(Gun gun)
	{
		try
		{
			int num = ((gun.defaultMagazine != null) ? Mathf.Max(1, gun.defaultMagazine.rounds) : 15);
			return Mathf.Clamp01((float)gun.AmmoCount() / (float)num);
		}
		catch (Exception)
		{
			return 1f;
		}
	}
}
