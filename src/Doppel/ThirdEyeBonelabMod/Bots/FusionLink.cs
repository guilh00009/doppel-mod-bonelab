using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using LabFusion.Entities;
using LabFusion.Marrow.Extenders;
using LabFusion.Marrow.Messages;
using LabFusion.Marrow.Patching;
using LabFusion.MonoBehaviours;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;
using LabFusion.Scene;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public static class FusionLink
{
	private const long Tag = 5929072799880532017L;

	public static void Start()
	{
		if (ModuleMessageManager.TagToHandlerLookup.TryGetValue(5929072799880532017L, out var value))
		{
			ModuleMessageManager.TagToHandlerLookup.Remove(5929072799880532017L);
			ModuleMessageManager.Handlers.Remove(value);
			ModuleMessageManager.TypeToHandlerLookup.Remove(value.GetType());
		}
		DoppelMessage doppelMessage = new DoppelMessage();
		FieldInfo? field = typeof(ModuleMessageHandler).GetField("_tag", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			throw new Exception("this Fusion keeps its handlers' numbers some other way");
		}
		field.SetValue(doppelMessage, 5929072799880532017L);
		doppelMessage.NetAttributes = Array.Empty<Net.NetAttribute>();
		ModuleMessageManager.Handlers.Add(doppelMessage);
		ModuleMessageManager.TypeToHandlerLookup[typeof(DoppelMessage)] = doppelMessage;
		ModuleMessageManager.TagToHandlerLookup[5929072799880532017L] = doppelMessage;
		NetBots.Online = () => NetworkSceneManager.IsLevelNetworked && PlayerIDManager.HasOtherPlayers;
		NetBots.Send = delegate(byte[] bytes, bool sure)
		{
			MessageRelay.RelayModule<DoppelMessage, DoppelData>(new DoppelData
			{
				bytes = bytes
			}, sure ? CommonMessageRoutes.ReliableToOtherClients : CommonMessageRoutes.UnreliableToOtherClients);
		};
		NetBots.WithUs = delegate(byte who)
		{
			PlayerID playerID = PlayerIDManager.GetPlayerID(who);
			return playerID != null && NetworkSceneManager.InCurrentLevel(playerID);
		};
		NetBots.GunShot = Shot;
		NetBots.Me = () => PlayerIDManager.LocalSmallID;
		NetBots.Others = delegate
		{
			List<RigManager> list = new List<RigManager>();
			foreach (NetworkPlayer player in NetworkPlayer.Players)
			{
				if (player != null && player.NetworkEntity != null && !player.NetworkEntity.IsOwner && player.HasRig)
				{
					RigManager rigManager = ((player.RigRefs != null) ? player.RigRefs.RigManager : null);
					if (rigManager != null)
					{
						list.Add(rigManager);
					}
				}
			}
			return list;
		};
		NetBots.Own = Own;
		NetBots.Hide = Hide;
		NetBots.Shared = delegate(ushort number)
		{
			IMarrowEntityExtender marrowEntityExtender = NetworkEntityManager.IDManager.RegisteredEntities.GetEntity(number)?.GetExtender<IMarrowEntityExtender>();
			return (marrowEntityExtender == null || !(marrowEntityExtender.MarrowEntity != null)) ? null : marrowEntityExtender.MarrowEntity.transform;
		};
	}

	public static void Stop()
	{
		NetBots.Online = () => false;
		NetBots.Send = delegate
		{
		};
		if (ModuleMessageManager.TagToHandlerLookup.TryGetValue(5929072799880532017L, out var value) && value is DoppelMessage)
		{
			ModuleMessageManager.TagToHandlerLookup.Remove(5929072799880532017L);
			ModuleMessageManager.Handlers.Remove(value);
			ModuleMessageManager.TypeToHandlerLookup.Remove(typeof(DoppelMessage));
		}
	}

	private static int Hide(RigManager rig)
	{
		if (rig == null)
		{
			return 0;
		}
		if (rig.GetComponent<AntiHasher>() == null)
		{
			rig.gameObject.AddComponent<AntiHasher>();
		}
		int num = 0;
		foreach (MarrowEntity componentsInChild in rig.GetComponentsInChildren<MarrowEntity>(includeInactive: true))
		{
			if (!(componentsInChild == null))
			{
				MarrowEntityPatches.HashTable.RemoveComponent(componentsInChild);
				if (IMarrowEntityExtender.Cache.TryGet(componentsInChild, out var value) && value != null && value.GetExtender<NetworkPlayer>() == null)
				{
					NetworkEntityManager.IDManager.UnregisterEntity(value);
					num++;
				}
			}
		}
		return num;
	}

	private static ushort Own(MarrowEntity thing)
	{
		NetworkEntity networkEntity = IMarrowEntityExtender.Cache.Get(thing);
		if (networkEntity == null || !networkEntity.IsRegistered)
		{
			return ushort.MaxValue;
		}
		if (!networkEntity.IsOwner)
		{
			NetworkEntityManager.TakeOwnership(networkEntity);
		}
		return networkEntity.ID;
	}

	private static void Shot(Gun gun)
	{
		NetworkEntity networkEntity = GunExtender.Cache.Get(gun);
		if (networkEntity == null || !NetworkInfo.HasServer)
		{
			return;
		}
		if (!networkEntity.IsOwner)
		{
			NetworkEntityManager.TakeOwnership(networkEntity);
		}
		GunExtender extender = networkEntity.GetExtender<GunExtender>();
		if (extender != null)
		{
			ushort? index = extender.GetIndex(gun);
			if (index.HasValue)
			{
				MessageRelay.RelayModule<GunShotMessage, GunShotData>(GunShotData.Create((byte)((gun._magState != null) ? ((uint)Mathf.Clamp(gun._magState.AmmoCount, 0, 255)) : 0u), networkEntity.ID, (byte)index.Value), CommonMessageRoutes.ReliableToOtherClients);
			}
		}
	}
}
