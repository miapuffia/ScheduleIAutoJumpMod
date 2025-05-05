using HarmonyLib;
using Il2CppGameKit.Utilities;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.ObjectScripts.Soil;
using Il2CppScheduleOne.Packaging;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerTasks;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Property.Utilities.Water;
using Il2CppScheduleOne.StationFramework;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Stations;
using Il2CppSteamworks;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static Il2CppScheduleOne.PlayerScripts.PlayerMovement;

[assembly: MelonInfo(typeof(AutoJumpMod.Mod), "AutoJumpMod", "0.1.0", "Robert Rioja")]
[assembly: MelonColor(1, 255, 20, 147)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace AutoJumpMod {
	public partial class Mod : MelonMod {
		private static PlayerMovement playerMovement = null;
		private static bool readyToJump = true;
		private static KeyControl jumpKeyControl = null;

		public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
			if(sceneName != "Main") {
				return;
			}

			MelonCoroutines.Start(GetPlayerMovementAndJumpControl());
		}

		[HarmonyPatch(typeof(RebindActionUI), "UpdateBindingDisplay")]
		public static class RebindActionUIPatch {
			private static void Postfix(RebindActionUI __instance) {
				InputActionReference jumpInputActionReference = __instance.actionReference;

				if(jumpInputActionReference == null) {
					Melon<Mod>.Logger.Msg("Couldn't find input action reference: " + __instance.name);
					return;
				}

				InputAction jumpInputAction = jumpInputActionReference.action;

				if(jumpInputAction == null) {
					Melon<Mod>.Logger.Msg("Couldn't find input action: " + __instance.name);
					return;
				}

				if(!jumpInputAction.name.Contains("Jump", Il2CppSystem.StringComparison.InvariantCultureIgnoreCase)) {
					return;
				}

				if(jumpInputAction.controls.Count == 0) {
					Melon<Mod>.Logger.Msg("Couldn't find jump input action controls");
					return;
				}

				InputControl jumpInputControl = jumpInputAction.controls[0];

				if(jumpInputControl == null) {
					Melon<Mod>.Logger.Msg("Couldn't find jump input control");
					return;
				}

				if(jumpInputControl.TryCast<KeyControl>() is null) {
					Melon<Mod>.Logger.Msg("Couldn't find jump key control");
					return;
				}

				jumpKeyControl = jumpInputControl.Cast<KeyControl>();
				Melon<Mod>.Logger.Msg("Jump button path changed: " + jumpInputControl.path);
			}
		}

		private static IEnumerator GetPlayerMovementAndJumpControl() {
			while(true) {
				yield return new WaitForSeconds(1f);

				playerMovement = GameObject.FindObjectsOfType<PlayerMovement>().FirstOrDefault();
				jumpKeyControl = GetJumpKeyControl();

				if(playerMovement != null && jumpKeyControl == null) {
					Melon<Mod>.Logger.Msg("Found PlayerMovement, looking for jump control...");
				} else if(playerMovement == null && jumpKeyControl != null) {
					Melon<Mod>.Logger.Msg("Found jump control, looking for PlayerMovement...");
				} else if(playerMovement == null && jumpKeyControl == null) {
					Melon<Mod>.Logger.Msg("Looking for PlayerMovement and jump control...");
				} else {
					Melon<Mod>.Logger.Msg("Found PlayerMovement and jump control");
					yield break;
				}
			}
		}

		private static KeyControl GetJumpKeyControl() {
			PauseMenu pauseMenu = PauseMenu.Instance;

			if(pauseMenu == null) {
				Melon<Mod>.Logger.Msg("Couldn't find pause menu");
				return null;
			}

			Transform jumpControl = pauseMenu.transform.Find("Container/SettingsScreen_Ingame/Content/Controls/Row/Jump");

			if(jumpControl == null) {
				Melon<Mod>.Logger.Msg("Couldn't find jump control");
				return null;
			}

			RebindActionUI jumpRebindAction = jumpControl.GetComponent<RebindActionUI>();

			if(jumpRebindAction == null) {
				Melon<Mod>.Logger.Msg("Couldn't find jump rebind action");
				return null;
			}

			InputActionReference jumpInputActionReference = jumpRebindAction.actionReference;

			if(jumpInputActionReference == null) {
				Melon<Mod>.Logger.Msg("Couldn't find jump input action reference");
				return null;
			}

			InputAction jumpInputAction = jumpInputActionReference.action;

			if(jumpInputAction == null) {
				Melon<Mod>.Logger.Msg("Couldn't find jump input action");
				return null;
			}

			if(jumpInputAction.controls.Count == 0) {
				Melon<Mod>.Logger.Msg("Couldn't find jump input action controls");
				return null;
			}

			InputControl jumpInputControl = jumpInputAction.controls[0];

			if(jumpInputControl == null) {
				Melon<Mod>.Logger.Msg("Couldn't find jump input control");
				return null;
			}

			if(jumpInputControl.TryCast<KeyControl>() is null) {
				Melon<Mod>.Logger.Msg("Couldn't find jump key control");
				return null;
			}

			return jumpInputControl.Cast<KeyControl>();
		}

		public override void OnUpdate() {
			if(playerMovement == null) {
				return;
			}

			if(playerMovement.IsGrounded && (jumpKeyControl?.isPressed ?? false) && !(jumpKeyControl?.wasPressedThisFrame ?? false) && readyToJump) {
				readyToJump = false;

				MelonCoroutines.Start(Jump());
			}
		}

		private static IEnumerator Jump() {
			InputEventPtr eventPtr;

			StateEvent.From(Keyboard.current.device, out eventPtr);
			jumpKeyControl.WriteValueIntoEvent(0f, eventPtr);
			InputSystem.QueueEvent(eventPtr);

			yield return new WaitForSeconds(0.1f);

			StateEvent.From(Keyboard.current.device, out eventPtr);
			jumpKeyControl.WriteValueIntoEvent(1f, eventPtr);
			InputSystem.QueueEvent(eventPtr);

			yield return new WaitForSeconds(0.1f);

			readyToJump = true;
		}
	}
}
