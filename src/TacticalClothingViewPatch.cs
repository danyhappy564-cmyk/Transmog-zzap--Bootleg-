using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SPT.Reflection.Patching;
using EFT;
using EFT.UI;
using HarmonyLib;

namespace Transmog
{
	/// <summary>
	/// This patch is unimportant. It's used to make sure transmog won't wrongly show in clothing shopping view,
	/// where player's equipments should be hidden.
	/// 4.1: 4.0의 profile_1 은 필드 선언 순서상 `_emptyProfile`(장비를 비운 미리보기용 복제본). `_profile`(실제 프로필)이 아니다 —
	/// 실제 프로필에 표식을 달면 플레이어 닉네임이 바뀐다.
	/// </summary>
	public class TacticalClothingViewPatch : ModulePatch
	{
		public const string MAGIC_ID_SKIP_TRANSMOG = "___SKIP_TF_TRANSMOG___";

		protected override MethodBase GetTargetMethod()
		{
			var method = AccessTools.Method(typeof(TacticalClothingView), "Show");
			if (method != null)
				Plugin.LogInfo("Found TacticalClothingView Show method.");
			else
				Plugin.LogError("Unable to find TacticalClothingView Show method.");
			return method;
		}

		[PatchTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var codeInstruction in instructions)
			{
				if (codeInstruction.opcode == OpCodes.Stfld &&
				    codeInstruction.operand is FieldInfo fieldInfo &&
				    fieldInfo.Name == "_emptyProfile")
				{
					yield return new CodeInstruction(OpCodes.Dup);
					yield return new CodeInstruction(OpCodes.Call,
						AccessTools.Method(typeof(TacticalClothingViewPatch), nameof(MarkProfile)));
				}
				yield return codeInstruction;
			}
		}

		public static void MarkProfile(Profile profile)
		{
			profile.Info.Nickname = MAGIC_ID_SKIP_TRANSMOG;
		}
	}
}