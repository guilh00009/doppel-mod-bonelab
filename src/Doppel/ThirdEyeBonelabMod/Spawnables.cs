using System;
using BoneLib.Notifications;
using MelonLoader;

namespace ThirdEyeBonelabMod;

public static class Spawnables
{
	private static MelonPreferences_Entry<bool> popupsEntry;

	public static bool Popups
	{
		get
		{
			try
			{
				if (popupsEntry == null)
				{
					MelonPreferences_Category melonPreferences_Category = MelonPreferences.GetCategory("ThirdEyeMods") ?? MelonPreferences.CreateCategory("ThirdEyeMods");
					popupsEntry = melonPreferences_Category.GetEntry<bool>("Popups") ?? melonPreferences_Category.CreateEntry("Popups", default_value: true, "Pop-up messages");
				}
				return popupsEntry.Value;
			}
			catch (Exception)
			{
				return true;
			}
		}
		set
		{
			if (Popups != value && popupsEntry != null)
			{
				popupsEntry.Value = value;
				MelonPreferences.Save();
			}
		}
	}

	public static void Notify(string message)
	{
		MelonLogger.Msg(message);
		if (Popups)
		{
			Notifier.Send(new Notification
			{
				Title = NotificationText.op_Implicit("Doppel"),
				Message = NotificationText.op_Implicit(message),
				Type = (NotificationType)0,
				PopupLength = 1.5f,
				ShowTitleOnPopup = true
			});
		}
	}
}
