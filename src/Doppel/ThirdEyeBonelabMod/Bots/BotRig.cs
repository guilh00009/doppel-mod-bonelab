using System;
using BoneLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.VRMK;
using MelonLoader;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class BotRig
{
	public const string Name = "[RigManager (Bot)]";

	public static bool IsBot(RigManager rig)
	{
		if (rig != null)
		{
			return rig.gameObject.name == "[RigManager (Bot)]";
		}
		return false;
	}

	public static void Create(Vector3 feet, Vector3 forward, Action<RigManager> made)
	{
		MarrowSettings runtimeInstance = MarrowSettings.RuntimeInstance;
		SpawnableCrate spawnableCrate = ((runtimeInstance != null && runtimeInstance.DefaultPlayerRig != null) ? runtimeInstance.DefaultPlayerRig.Crate : null);
		if (spawnableCrate == null)
		{
			Spawnables.Notify("The game's player rig could not be found");
			return;
		}
		spawnableCrate.MainGameObject.LoadAsset((Action<GameObject>)delegate(GameObject asset)
		{
			try
			{
				made(Make(asset, feet, forward));
			}
			catch (Exception ex)
			{
				MelonLogger.Error("The bot's body could not be made: " + ex);
			}
		});
	}

	private static RigManager Make(GameObject asset, Vector3 feet, Vector3 forward)
	{
		RigManager obj = ((asset != null) ? asset.GetComponentInChildren<RigManager>(includeInactive: true) : null);
		if (obj == null)
		{
			throw new InvalidOperationException("the player rig has no RigManager");
		}
		GameObject gameObject = new GameObject("ThirdEyeBotHolder");
		gameObject.SetActive(value: false);
		GameObject gameObject2 = UnityEngine.Object.Instantiate(obj.gameObject, gameObject.transform);
		gameObject2.name = "[RigManager (Bot)]";
		gameObject2.SetActive(value: false);
		gameObject2.transform.position = feet;
		gameObject2.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
		RigManager component = gameObject2.GetComponent<RigManager>();
		Strip(component);
		gameObject2.transform.parent = null;
		UnityEngine.Object.Destroy(gameObject);
		gameObject2.SetActive(value: true);
		Hear(component);
		return component;
	}

	private static void Strip(RigManager rig)
	{
		OpenControllerRig openControllerRig = rig.ControllerRig.TryCast<OpenControllerRig>();
		PhysicsRig physicsRig = rig.physicsRig;
		RemovePlayerTag(physicsRig.marrowEntity);
		foreach (MarrowBody componentsInChild in openControllerRig.GetComponentsInChildren<MarrowBody>(includeInactive: true))
		{
			UnityEngine.Object.DestroyImmediate(componentsInChild);
		}
		foreach (Tracker componentsInChild2 in openControllerRig.GetComponentsInChildren<Tracker>(includeInactive: true))
		{
			UnityEngine.Object.DestroyImmediate(componentsInChild2.gameObject);
		}
		MarrowEntity component = openControllerRig.GetComponent<MarrowEntity>();
		if (component != null)
		{
			UnityEngine.Object.DestroyImmediate(component);
		}
		Player_Health player_Health = ((rig.health != null) ? rig.health.TryCast<Player_Health>() : null);
		if (player_Health != null)
		{
			player_Health.reloadLevelOnDeath = false;
			player_Health.slowMoOnDeath = false;
			player_Health.healthMode = Health.HealthMode.Invincible;
			if (player_Health.Vignetter != null)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(player_Health.Vignetter);
				SkinnedMeshRenderer component2 = gameObject.GetComponent<SkinnedMeshRenderer>();
				if (component2 != null)
				{
					component2.enabled = false;
				}
				gameObject.name = "ThirdEyeBotVignetter";
				gameObject.SetActive(value: false);
				player_Health.Vignetter = gameObject;
			}
		}
		openControllerRig.quickmenuEnabled = false;
		openControllerRig._timeInput = false;
		rig.remapHeptaRig.doubleJump = false;
		Transform headset = openControllerRig.headset;
		Blind(headset);
		openControllerRig.cameras = new Il2CppReferenceArray<Camera>(0L);
		openControllerRig.onLastCameraUpdate = null;
		headset.tag = "Untagged";
		PlayerAvatarArt componentInChildren = rig.GetComponentInChildren<PlayerAvatarArt>(includeInactive: true);
		if (componentInChildren != null)
		{
			UnityEngine.Object.DestroyImmediate(componentInChildren);
		}
		InventoryAmmoReceiver componentInChildren2 = rig.GetComponentInChildren<InventoryAmmoReceiver>(includeInactive: true);
		if (componentInChildren2 != null && componentInChildren2.GetComponent<Collider>() != null)
		{
			componentInChildren2.GetComponent<Collider>().enabled = false;
		}
		Hand[] array = new Hand[2] { physicsRig.leftHand, physicsRig.rightHand };
		foreach (Hand hand in array)
		{
			PhysHand physHand = ((hand != null) ? hand.GetComponent<PhysHand>() : null);
			if (physHand != null && physHand.inventoryPlug != null)
			{
				physHand.inventoryPlug.enabled = false;
				physHand.inventoryPlug.gameObject.SetActive(value: false);
			}
		}
		foreach (InventorySlotReceiver componentsInChild3 in rig.GetComponentsInChildren<InventorySlotReceiver>(includeInactive: true))
		{
			foreach (BoxCollider componentsInChild4 in componentsInChild3.GetComponentsInChildren<BoxCollider>(includeInactive: true))
			{
				if (componentsInChild4.isTrigger)
				{
					componentsInChild4.size *= 0.4f;
				}
			}
		}
		rig.ControllerRig.leftController = Detach(rig.ControllerRig, rig.ControllerRig.leftController, Handedness.LEFT);
		rig.ControllerRig.rightController = Detach(rig.ControllerRig, rig.ControllerRig.rightController, Handedness.RIGHT);
		if (physicsRig.GetComponent<Poolee>() == null)
		{
			physicsRig.gameObject.AddComponent<Poolee>();
		}
	}

	private static BaseController Detach(ControllerRig controls, BaseController original, Handedness side)
	{
		GameObject gameObject = original.gameObject;
		Haptor haptor = original.haptor;
		UIControllerInput component = gameObject.GetComponent<UIControllerInput>();
		if (component != null)
		{
			UnityEngine.Object.DestroyImmediate(component);
		}
		BaseController baseController = gameObject.AddComponent<BaseController>();
		baseController.contRig = controls;
		baseController.handedness = side;
		if (haptor != null)
		{
			haptor.device_Controller = baseController;
			haptor.hapticsAllowed = false;
			haptor.enabled = false;
		}
		return baseController;
	}

	private static void RemovePlayerTag(MarrowEntity entity)
	{
		MarrowSettings runtimeInstance = MarrowSettings.RuntimeInstance;
		if (entity == null || runtimeInstance == null || runtimeInstance.PlayerTag == null || runtimeInstance.PlayerTag.Barcode == null)
		{
			return;
		}
		string iD = runtimeInstance.PlayerTag.Barcode.ID;
		TagList tags = entity.Tags;
		if (tags == null || tags.Tags == null)
		{
			return;
		}
		for (int num = tags.Tags.Count - 1; num >= 0; num--)
		{
			BoneTagReference boneTagReference = tags.Tags[num];
			if (boneTagReference != null && boneTagReference.Barcode != null && boneTagReference.Barcode.ID == iD)
			{
				tags.Tags.RemoveAt(num);
			}
		}
		tags.UpdateCache();
	}

	private static int StripSenses(Transform root)
	{
		int num = 0;
		foreach (Camera componentsInChild in root.GetComponentsInChildren<Camera>(includeInactive: true))
		{
			foreach (Component component in componentsInChild.GetComponents<Component>())
			{
				if (!(component == null) && !(component is Transform) && !(component is Camera))
				{
					UnityEngine.Object.DestroyImmediate(component);
					num++;
				}
			}
			try
			{
				UnityEngine.Object.DestroyImmediate(componentsInChild);
			}
			catch (Exception)
			{
				componentsInChild.enabled = false;
			}
			num++;
		}
		foreach (AudioListener componentsInChild2 in root.GetComponentsInChildren<AudioListener>(includeInactive: true))
		{
			UnityEngine.Object.DestroyImmediate(componentsInChild2);
			num++;
		}
		return num;
	}

	private static void Blind(Transform headset)
	{
		foreach (Camera componentsInChild in headset.GetComponentsInChildren<Camera>(includeInactive: true))
		{
			foreach (Component component in componentsInChild.GetComponents<Component>())
			{
				if (!(component == null) && !(component is Transform) && !(component is Camera))
				{
					UnityEngine.Object.DestroyImmediate(component);
				}
			}
			try
			{
				UnityEngine.Object.DestroyImmediate(componentsInChild);
			}
			catch (Exception)
			{
				componentsInChild.enabled = false;
			}
		}
		foreach (AudioListener componentsInChild2 in headset.GetComponentsInChildren<AudioListener>(includeInactive: true))
		{
			UnityEngine.Object.DestroyImmediate(componentsInChild2);
		}
	}

	private static void Hear(RigManager rig)
	{
		foreach (AudioSource componentsInChild in rig.GetComponentsInChildren<AudioSource>(includeInactive: true))
		{
			if (componentsInChild != null)
			{
				componentsInChild.spatialBlend = 1f;
			}
		}
	}

	public static void HearLate(RigManager rig)
	{
		if (rig != null)
		{
			Hear(rig);
		}
	}

	public static void CopyPlayerAvatar(RigManager rig, Action done)
	{
		RigManager rigManager = Player.RigManager;
		if (!(rig == null) && !(rigManager == null) && rigManager.AvatarCrate != null && !(rigManager.AvatarCrate.Barcode == null))
		{
			Vector3 scale = ((rigManager.avatar != null) ? rigManager.avatar.transform.localScale : Vector3.one);
			UseAvatar(rig, rigManager.AvatarCrate.Barcode.ID, scale, done);
		}
	}

	public static string AvatarOf(RigManager rig)
	{
		try
		{
			return (rig != null && rig._avatarCrate != null && rig._avatarCrate.Barcode != null) ? rig._avatarCrate.Barcode.ID : "";
		}
		catch (Exception)
		{
			return "";
		}
	}

	public static void UseAvatar(RigManager rig, string barcode, Vector3 scale, Action done)
	{
		AvatarCrate crate = null;
		AssetWarehouse instance = AssetWarehouse.Instance;
		if (rig == null || string.IsNullOrEmpty(barcode) || instance == null || !instance.TryGetCrate(new Barcode(barcode), out crate) || crate == null)
		{
			done?.Invoke();
			return;
		}
		crate.MainGameObject.LoadAsset((Action<GameObject>)delegate(GameObject asset)
		{
			try
			{
				if (!(rig == null) && !(asset == null))
				{
					GameObject gameObject = new GameObject("ThirdEyeBotAvatarHolder");
					gameObject.SetActive(value: false);
					GameObject gameObject2 = UnityEngine.Object.Instantiate(asset, gameObject.transform);
					gameObject2.SetActive(value: false);
					gameObject2.name = asset.name;
					int num = StripSenses(gameObject2.transform);
					if (num > 0)
					{
						MelonLogger.Msg("Bot Player: took " + num + " camera/listener parts out of the avatar " + asset.name);
					}
					gameObject2.transform.localScale = scale;
					gameObject2.transform.parent = rig.transform;
					UnityEngine.Object.Destroy(gameObject);
					gameObject2.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
					Il2CppSLZ.VRMK.Avatar componentInParent = gameObject2.GetComponentInParent<Il2CppSLZ.VRMK.Avatar>(includeInactive: true);
					if (componentInParent == null)
					{
						UnityEngine.Object.Destroy(gameObject2);
					}
					else
					{
						HeadsetGuard.SwappedAt = Time.time;
						rig.SwapAvatar(componentInParent);
						rig._avatarCrate = new AvatarCrateReference(crate.Barcode);
						Hear(rig);
						done?.Invoke();
					}
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("The bot could not take the player's avatar: " + ex.Message);
			}
		});
	}
}
