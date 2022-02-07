//----------------------
// <auto-generated>
//     Generated by Bagoum's Localization Utilities CSV Analysis.
//     Github project: https://github.com/Bagoum/localization-utils
// </auto-generated>
//----------------------

using System.Collections.Generic;
using BagoumLib.Culture;
using Danmokou.Core;
using static BagoumLib.Culture.LocalizationRendering;
using static Danmokou.Core.LocalizationRendering;

namespace Danmokou.Core {
public static partial class LocalizedStrings {
	public static partial class Generic {
		
		public static readonly LString generic_off = new LText("Off",
			(Locales.JP, "OFF"))
			{ ID = "generic.generic_off" };
		
		public static readonly LString generic_on = new LText("On",
			(Locales.JP, "ON"))
			{ ID = "generic.generic_on" };
		
		public static readonly LString generic_no = new LText("No",
			(Locales.JP, "いいえ"))
			{ ID = "generic.generic_no" };
		
		public static readonly LString generic_yes = new LText("Yes",
			(Locales.JP, "はい"))
			{ ID = "generic.generic_yes" };
		
		public static readonly LString donotshow = new LText("__do_not_show__")
			{ ID = "generic.donotshow" };
		
		public static readonly LString generic_default = new LText("Default",
			(Locales.JP, "デフォルト"))
			{ ID = "generic.generic_default" };
		
		public static readonly LString generic_deleted = new LText("---Deleted---",
			(Locales.JP, "ーー削除ーー"))
			{ ID = "generic.generic_deleted" };
		
		public static readonly LString generic_na = new LText("-",
			(Locales.JP, "-"))
			{ ID = "generic.generic_na" };
		
		public static string render_hoursminssecs(object arg0, object arg1, object arg2) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"{0}",
				"時",
				"{1}",
				"分",
				"{2}",
				"秒",
			}, arg0, arg1, arg2),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"{0}",
				"h ",
				"{1}",
				"m ",
				"{2}",
				"s",
			}, arg0, arg1, arg2),
		};
		
		public static LString render_hoursminssecs_ls(object arg0, object arg1, object arg2) => new LText(Render(null, new[] {
				"{0}",
				"h ",
				"{1}",
				"m ",
				"{2}",
				"s",
			}, arg0, arg1, arg2),
			(Locales.JP, Render(Locales.JP, new[] {
				"{0}",
				"時",
				"{1}",
				"分",
				"{2}",
				"秒",
			}, arg0, arg1, arg2)))
			{ ID = "generic.render_hoursminssecs" };
		
		public static string render_minssecs(object arg0, object arg1) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"{0}",
				"分",
				"{1}",
				"秒",
			}, arg0, arg1),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"{0}",
				"m ",
				"{1}",
				"s",
			}, arg0, arg1),
		};
		
		public static LString render_minssecs_ls(object arg0, object arg1) => new LText(Render(null, new[] {
				"{0}",
				"m ",
				"{1}",
				"s",
			}, arg0, arg1),
			(Locales.JP, Render(Locales.JP, new[] {
				"{0}",
				"分",
				"{1}",
				"秒",
			}, arg0, arg1)))
			{ ID = "generic.render_minssecs" };
		
		public static readonly LString generic_cancel = new LText("Cancel",
			(Locales.JP, "キャンセル"))
			{ ID = "generic.generic_cancel" };
		
		public static readonly LString generic_back = new LText("Back",
			(Locales.JP, "戻る"))
			{ ID = "generic.generic_back" };
		
		public static readonly LString generic_save = new LText("Save",
			(Locales.JP, "保存"))
			{ ID = "generic.generic_save" };
		
		public static readonly LString generic_delete = new LText("Delete",
			(Locales.JP, "削除"))
			{ ID = "generic.generic_delete" };
		
		public static readonly LString generic_load = new LText("Load",
			(Locales.JP, "ロード"))
			{ ID = "generic.generic_load" };
		
		public static readonly LString generic_overwrite = new LText("Overwrite",
			(Locales.JP, "上書き保存"))
			{ ID = "generic.generic_overwrite" };
		
	}
}
}
