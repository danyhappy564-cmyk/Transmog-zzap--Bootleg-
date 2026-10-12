// Custom Interactions (IcyClawz / IgorEisberg, University of Illinois/NCSA Open Source License)를
// 4.1용으로 다시 쓴 내장판 — 전체 고지는 THIRD-PARTY-NOTICES.md 참고.
//
// 4.0 원본과 다른 점:
//  - Prepatch(Assembly-CSharp 개조) 불필요: 4.1의 DynamicContextInteraction은 이미 public.
//  - 원본 모드가 설치돼 있어도 충돌하지 않도록 Transmog 전용 CustomInteractionImpl 만 건드린다.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace Transmog.Interactions
{
	public interface ICustomInteractionsProvider
	{
		IEnumerable<CustomInteraction> GetCustomInteractions(ItemUiContext context, EItemViewType viewType, Item item);
	}

	public static class CustomInteractionsManager
	{
		internal static readonly LinkedList<ICustomInteractionsProvider> Providers = new LinkedList<ICustomInteractionsProvider>();

		public static void Register(ICustomInteractionsProvider provider)
		{
			if (!Providers.Contains(provider))
				Providers.AddLast(provider);
		}

		public static void EnablePatches()
		{
			new ItemUiContextPatch().Enable();
			new CreateDynamicContextButtonPatch().Enable();
		}
	}

	public class CustomInteraction
	{
		internal readonly CustomInteractionImpl Impl;

		public CustomInteraction(ItemUiContext context)
		{
			Impl = new CustomInteractionImpl(context, UnityEngine.Random.Range(0, int.MaxValue).ToString("x4"));
		}

		public Func<string> Caption { get => Impl.Caption; set => Impl.Caption = value; }
		public Func<Sprite> Icon { get => Impl.IconProvider; set => Impl.IconProvider = value; }
		public Action Action { get => Impl._callback; set => Impl._callback = value; }
		public Func<IEnumerable<CustomInteraction>> SubMenu { get => Impl.SubMenu; set => Impl.SubMenu = value; }
		public Func<bool> Enabled { get => Impl.Enabled; set => Impl.Enabled = value; }
		public Func<string> Error { get => Impl.Error; set => Impl.Error = value; }
	}

	/// <summary>4.1의 DynamicContextInteraction(메뉴에 끼워 넣는 항목)에 캡션·하위 메뉴를 얹은 것.</summary>
	internal sealed class CustomInteractionImpl : DynamicContextInteraction
	{
		internal readonly ItemUiContext Context;

		public Func<string> Caption { get; set; }
		public Func<Sprite> IconProvider { get; set; }
		public Func<IEnumerable<CustomInteraction>> SubMenu { get; set; }
		public Func<bool> Enabled { get; set; }
		public Func<string> Error { get; set; }

		public CustomInteractionImpl(ItemUiContext context, string id) : base(id, id, null)
		{
			Context = context;
		}

		public bool IsInteractive() => Enabled?.Invoke() ?? true;
	}

	/// <summary>하위 메뉴 한 칸 분량의 "빈" 메뉴 — 일반 항목은 전부 비활성, 동적 항목만 담는다.</summary>
	internal sealed class SubMenuInteractions : EmptyContextInteractions
	{
		public override bool HasInteractions => true;
		public override IEnumerable<EItemInfoButton> AllInteractions => Enumerable.Empty<EItemInfoButton>();
		public override bool HasIcons =>
			DynamicInteractions.OfType<CustomInteractionImpl>().Any(i => i.IconProvider?.Invoke() != null);

		public SubMenuInteractions(ItemUiContext context)
		{
			UIContext = context;
		}

		public void Add(CustomInteractionImpl impl) => _dynamicInteractions[impl.Key] = impl;
	}

	/// <summary>아이템 우클릭 메뉴가 만들어질 때 등록된 제공자의 항목을 끼워 넣는다.</summary>
	internal class ItemUiContextPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() =>
			AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.GetItemContextInteractions));

		[PatchPostfix]
		private static void Postfix(ContextInteractions<EItemInfoButton> __result, ItemUiContext __instance, ItemContext itemContext)
		{
			if (__result == null || itemContext?.Item == null)
				return;
			foreach (var provider in CustomInteractionsManager.Providers)
			{
				var interactions = provider.GetCustomInteractions(__instance, itemContext.ViewType, itemContext.Item);
				if (interactions == null)
					continue;
				foreach (var impl in interactions.Select(i => i.Impl))
					__result._dynamicInteractions[impl.Key] = impl;
			}
		}
	}

	/// <summary>메뉴 버튼을 실제로 그릴 때, 우리 항목만 하위 메뉴가 되는 전용 버튼으로 바꿔 그린다.</summary>
	internal class CreateDynamicContextButtonPatch : ModulePatch
	{
		private static readonly FieldInfo SubMenuButtonField =
			AccessTools.Field(typeof(InteractionButtonsContainer), "_subMenuButton");

		protected override MethodBase GetTargetMethod() =>
			AccessTools.Method(typeof(InteractionButtonsContainer), nameof(InteractionButtonsContainer.CreateDynamicContextButton));

		[PatchPrefix]
		private static bool Prefix(InteractionButtonsContainer __instance, DynamicContextInteraction interaction)
		{
			if (interaction is CustomInteractionImpl impl)
			{
				AddCustomButton(__instance, impl);
				return false;
			}
			return true;
		}

		private static void AddCustomButton(InteractionButtonsContainer container, CustomInteractionImpl impl)
		{
			bool isInteractive = impl.IsInteractive();
			// 하위 메뉴 항목은 한 번만 만들어 둔다(Any 와 순회를 따로 돌리면 항목 객체가 두 번 생긴다).
			List<CustomInteraction> subMenu = impl.SubMenu?.Invoke()?.ToList();
			bool hasSubMenu = subMenu != null && subMenu.Count > 0;

			SimpleContextMenuButton button = null;
			button = container.CreateContextButton(
				impl.Key,
				impl.Caption?.Invoke() ?? "",
				container._buttonTemplate,
				container._buttonsContainer,
				impl.IconProvider?.Invoke(),
				() =>
				{
					if (isInteractive)
						impl.Execute();
				},
				() =>
				{
					SubMenuButtonField.SetValue(container, button);
					container.CloseSubMenu();
					if (isInteractive && hasSubMenu)
					{
						var subInteractions = new SubMenuInteractions(impl.Context);
						foreach (var subImpl in subMenu.Select(item => item.Impl))
							subInteractions.Add(subImpl);
						container.SetSubInteractions(subInteractions);
					}
				},
				hasSubMenu,
				!hasSubMenu);
			button.SetButtonInteraction(isInteractive
				? (IResult)SuccessfulResult.New
				: new FailedResult(impl.Error?.Invoke() ?? ""));
			container.BindButton(button);
		}
	}
}
