#if IL2CPP
using Il2CppGameKit.Utilities;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI;
#elif MONO
using ScheduleOne.PlayerScripts;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI;
#endif
using HarmonyLib;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

[assembly: MelonInfo(typeof(AutoJumpMod.Mod), "AutoJumpMod", "0.2.0", "Robert Rioja")]
[assembly: MelonColor(1, 255, 20, 147)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace AutoJumpMod {
	public partial class Mod : MelonMod {
		private static PlayerMovement playerMovement = null;
		private static bool readyToJump = true;
		private static KeyControl jumpKeyControl = null;

		public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
			if(sceneName != "Main") {
                Melon<Mod>.Logger.Msg("Scene changed to " + sceneName + " - doing nothing");

                return;
			}

            Melon<Mod>.Logger.Msg("Scene changed to main - continuing");

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

#if IL2CPP
				if(!jumpInputAction.name.Contains("Jump", Il2CppSystem.StringComparison.InvariantCultureIgnoreCase)) {
#elif MONO
				if(!jumpInputAction.name.Contains("Jump", StringComparison.InvariantCultureIgnoreCase)) {
#endif
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

#if IL2CPP
				if(jumpInputControl.TryCast<KeyControl>() is null) {
#elif MONO
				if(jumpInputControl as KeyControl is null) {
#endif
					Melon<Mod>.Logger.Msg("Couldn't find jump key control");
					return;
				}

#if IL2CPP
				jumpKeyControl = jumpInputControl.Cast<KeyControl>();
#elif MONO
				jumpKeyControl = jumpInputControl as KeyControl;
#endif
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

#if IL2CPP
			if(jumpInputControl.TryCast<KeyControl>() is null) {
#elif MONO
			if(jumpInputControl as KeyControl is null) {
#endif
				Melon<Mod>.Logger.Msg("Couldn't find jump key control");
				return null;
			}

#if IL2CPP
			return jumpInputControl.Cast<KeyControl>();
#elif MONO
			return jumpInputControl as KeyControl;
#endif
        }

        public override void OnUpdate() {
			if(playerMovement == null) {
				return;
			}

			if(playerMovement.TimeAirborne > 0.01f || playerMovement.TimeGrounded > 0.5f) {
				readyToJump = true;
			}

			if(readyToJump && playerMovement.IsGrounded && playerMovement.CanMove && jumpKeyPressed() && !jumpKeyPressedThisFrame()) {
				readyToJump = false;

				MelonCoroutines.Start(Jump());
			}
		}

		private static bool jumpKeyPressed() {
			return jumpKeyControl?.isPressed ?? false;
		}

		private static bool jumpKeyPressedThisFrame() {
			return jumpKeyControl?.wasPressedThisFrame ?? false;
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
		}
	}
}
