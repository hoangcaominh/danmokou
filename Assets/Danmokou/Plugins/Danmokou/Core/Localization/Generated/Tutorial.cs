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
	public static partial class Tutorial {
		
		public static string mtcirc1(object arg0, object arg1) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"左上に大きな赤い丸と緑色の四角が見えるはず。赤い丸が見えなければ、それとも赤い丸はプレイエリアの真ん中に位置していれば、環境設定メニューの「GRAPHICS」タブで「レンダラー」の設定を「旧型」に変えてください。（",
				"{0}",
				"押すと続く、",
				"{1}",
				"押すとポーズメニューを開く）",
			}, arg0, arg1),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"You should see a large red circle on a green box in the upper left corner. If the red circle is invisible or in the center of the screen, turn the renderer option to LEGACY in the graphics section of the options menu. (",
				"{0}",
				" to continue, ",
				"{1}",
				" to open the pause menu)",
			}, arg0, arg1),
		};
		
		public static LString mtcirc1_ls(object arg0, object arg1) => new LText(Render(null, new[] {
				"You should see a large red circle on a green box in the upper left corner. If the red circle is invisible or in the center of the screen, turn the renderer option to LEGACY in the graphics section of the options menu. (",
				"{0}",
				" to continue, ",
				"{1}",
				" to open the pause menu)",
			}, arg0, arg1),
			(Locales.JP, Render(Locales.JP, new[] {
				"左上に大きな赤い丸と緑色の四角が見えるはず。赤い丸が見えなければ、それとも赤い丸はプレイエリアの真ん中に位置していれば、環境設定メニューの「GRAPHICS」タブで「レンダラー」の設定を「旧型」に変えてください。（",
				"{0}",
				"押すと続く、",
				"{1}",
				"押すとポーズメニューを開く）",
			}, arg0, arg1)))
			{ ID = "mtcirc1" };
		
		public static string mtsafe2(object arg0) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"左側のレーザは「セーフレーザ」です。文字やパターンのあるレーザはプレーヤにダメージを与えません。（",
				"{0}",
				"押すと続く）",
			}, arg0),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"Lasers with letters or patterns are SAFE LASERS. They will not damage you. (",
				"{0}",
				" to continue)",
			}, arg0),
		};
		
		public static LString mtsafe2_ls(object arg0) => new LText(Render(null, new[] {
				"Lasers with letters or patterns are SAFE LASERS. They will not damage you. (",
				"{0}",
				" to continue)",
			}, arg0),
			(Locales.JP, Render(Locales.JP, new[] {
				"左側のレーザは「セーフレーザ」です。文字やパターンのあるレーザはプレーヤにダメージを与えません。（",
				"{0}",
				"押すと続く）",
			}, arg0)))
			{ ID = "mtsafe2" };
		
		public static string welcome1(object arg0) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"チュートリアルへようこそ！白いメッセージを見ると、",
				"{0}",
				"を押せば次のメッセージが現れる。",
			}, arg0),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"Welcome to the tutorial! When you see a message in white, press ",
				"{0}",
				" to continue.",
			}, arg0),
		};
		
		public static LString welcome1_ls(object arg0) => new LText(Render(null, new[] {
				"Welcome to the tutorial! When you see a message in white, press ",
				"{0}",
				" to continue.",
			}, arg0),
			(Locales.JP, Render(Locales.JP, new[] {
				"チュートリアルへようこそ！白いメッセージを見ると、",
				"{0}",
				"を押せば次のメッセージが現れる。",
			}, arg0)))
			{ ID = "welcome1" };
		
		public static string blue2(object arg0) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"青いメッセージを見ると、指図に従ってください。",
				"\n",
				"{0}",
				"を押してポーズメニューを開いてください。",
			}, arg0),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"When you see a message in blue, follow the instructions.",
				"\n",
				"Press ",
				"{0}",
				" to open the pause menu.",
			}, arg0),
		};
		
		public static LString blue2_ls(object arg0) => new LText(Render(null, new[] {
				"When you see a message in blue, follow the instructions.",
				"\n",
				"Press ",
				"{0}",
				" to open the pause menu.",
			}, arg0),
			(Locales.JP, Render(Locales.JP, new[] {
				"青いメッセージを見ると、指図に従ってください。",
				"\n",
				"{0}",
				"を押してポーズメニューを開いてください。",
			}, arg0)))
			{ ID = "blue2" };
		
		public static readonly LString options2_1 = new LText("<--- Open the options menu. The options menu has important settings as well as control flow options.",
			(Locales.JP, "<-------環境設定メニューを開いてください。環境設定メニューには、設定と制御流れの機構があります。"))
			{ ID = "options2_1" };
		
		public static readonly LString graphics3_1 = new LText("Go to the GRAPHICS tab.",
			(Locales.JP, "「GRAPHICS」タブに移動してください。"))
			{ ID = "graphics3_1" };
		
		public static readonly LString shaders4 = new LText("If the game is running slow, you can try lowering visual quality or lowering the resolution.",
			(Locales.JP, "フレームレートが低ければ画質や解像度を下げて見てください。"))
			{ ID = "shaders4" };
		
		public static readonly LString shaders5 = new LText(Render(null, new[] {
				"<------- Visual Quality option",
				"\n",
				"Try changing the visual quality. It takes effect on unpause.",
			}),
			(Locales.JP, Render(Locales.JP, new[] {
				"<-------画質設定",
				"\n",
				"画質を変えてください。ポースを解除するたびに効果が出ます。",
			})))
			{ ID = "shaders5" };
		
		public static readonly LString res6 = new LText(Render(null, new[] {
				"<------- Resolution option",
				"\n",
				"Try changing the resolution. It takes effect immediately.",
			}),
			(Locales.JP, Render(Locales.JP, new[] {
				"<-------解像度設定",
				"\n",
				"解像度を変えてください。即時に効果が出ます。",
			})))
			{ ID = "res6" };
		
		public static readonly LString fullscreen8 = new LText(Render(null, new[] {
				"<------- Fullscreen option",
				"\n",
				"Some computers have trouble playing games in fullscreen. Try turning this off if you have lag.",
			}),
			(Locales.JP, Render(Locales.JP, new[] {
				"<-------フルスクリーン設定",
				"\n",
				"インプットラグとかが発生するとこれを変えて見てください。",
			})))
			{ ID = "fullscreen8" };
		
		public static readonly LString vsync9 = new LText(Render(null, new[] {
				"<------- Vsync option",
				"\n",
				"Vsync will make the game run smoother, but it may cause input lag.",
			}),
			(Locales.JP, Render(Locales.JP, new[] {
				"<-------垂直同期設定",
				"\n",
				"垂直同期はフレームレートを円滑にするが、インプットラグを生じる可能性があります。",
			})))
			{ ID = "vsync9" };
		
		public static readonly LString inputsmooth10 = new LText("If you are sensitive to input lag, turn input smoothing off from the main menu options.",
			(Locales.JP, "インプットラグに敏感のお方は、メインメニューから「インプットスムージング」をOFFにしてください。"))
			{ ID = "inputsmooth10" };
		
		public static string unpause11(object arg0) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"{0}",
				"を押して、または「再開」のボタンを押して、ポーズを解除してください。",
			}, arg0),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"Unpause by pressing ",
				"{0}",
				" or selecting the unpause option.",
			}, arg0),
		};
		
		public static LString unpause11_ls(object arg0) => new LText(Render(null, new[] {
				"Unpause by pressing ",
				"{0}",
				" or selecting the unpause option.",
			}, arg0),
			(Locales.JP, Render(Locales.JP, new[] {
				"{0}",
				"を押して、または「再開」のボタンを押して、ポーズを解除してください。",
			}, arg0)))
			{ ID = "unpause11" };
		
		public static readonly LString redcircle12 = new LText("You should now see a large red circle on a green box in the upper left corner, and two lasers on the right side of the screen.",
			(Locales.JP, "左上に大きな赤い丸と緑色の四角、右側に二つのレーザが見えるはず。"))
			{ ID = "redcircle12" };
		
		public static readonly LString legacy13 = new LText("If you cannot see the red circle, or the red circle appears to be in the center of the screen, turn the renderer option to LEGACY in the graphics menu.",
			(Locales.JP, "赤い丸が見えなければ、それとも赤い丸はプレイエリアの真ん中に位置していれば、環境設定メニューで「レンダラー」の設定を「旧型」に変えてください。"))
			{ ID = "legacy13" };
		
		public static readonly LString safelaser14 = new LText("The lasers on the right are SAFE LASERs. Lasers with letters or patterns do no damage.",
			(Locales.JP, "右側のレーザは「セーフレーザ」です。文字やパターンのあるレーザはプレーヤにダメージを与えません。"))
			{ ID = "safelaser14" };
		
		public static string fire15(object arg0) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"{0}",
				"を押し続けてショットを撃ってください。",
			}, arg0),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"Hold ",
				"{0}",
				" to fire.",
			}, arg0),
		};
		
		public static LString fire15_ls(object arg0) => new LText(Render(null, new[] {
				"Hold ",
				"{0}",
				" to fire.",
			}, arg0),
			(Locales.JP, Render(Locales.JP, new[] {
				"{0}",
				"を押し続けてショットを撃ってください。",
			}, arg0)))
			{ ID = "fire15" };
		
		public static readonly LString move16 = new LText("Use the arrow keys, the left joystick, or the D-Pad to move around.",
			(Locales.JP, "矢印キー・左ジョイスティック・十字キーを押して移動してください。"))
			{ ID = "move16" };
		
		public static string focus17(object arg0) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"{0}",
				"を押し続けて低速モードを発動してください。",
			}, arg0),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"Hold ",
				"{0}",
				" to move slow (focus mode).",
			}, arg0),
		};
		
		public static LString focus17_ls(object arg0) => new LText(Render(null, new[] {
				"Hold ",
				"{0}",
				" to move slow (focus mode).",
			}, arg0),
			(Locales.JP, Render(Locales.JP, new[] {
				"{0}",
				"を押し続けて低速モードを発動してください。",
			}, arg0)))
			{ ID = "focus17" };
		
		public static readonly LString boss18 = new LText("This is a boss enemy. The pink circle around the boss is its healthbar.",
			(Locales.JP, "これはボスです。ボスを回る桃色の線はボスのHPバーです。"))
			{ ID = "boss18" };
		
		public static readonly LString hpbar19 = new LText("The white line at the bottom of the playable area changes into a colored boss healthbar when a boss is active.",
			(Locales.JP, "プレイエリアの下にある白い線は、ボスが現れると色付きのボスHPバーになります。"))
			{ ID = "hpbar19" };
		
		public static readonly LString ns20 = new LText("The boss has started a nonspell card. Try shooting down the boss. You do up to 25% more damage when closer to the boss.",
			(Locales.JP, "ボスが「ノンスペル(NON)」をはじめました。ボスを撃ってください。ボスを近くから撃つとダメージが25%まで上昇します。"))
			{ ID = "ns20" };
		
		public static readonly LString nss21 = new LText("This time, you can see two parts to the healthbar. The bottom half is the current nonspell card. The top half is the following spell card. Try shooting down the boss.",
			(Locales.JP, "今はボスHPバーが二つに分けています。下の半分は現在のノンスペルです。上の半分は次の「スペル(SPELL)」です。ボスを撃ってください。"))
			{ ID = "nss21" };
		
		public static readonly LString spell22 = new LText("The boss has started a spell card, using only the top half of the healthbar. Try shooting down the boss.",
			(Locales.JP, "ボスはスペルに移行しました。ボスを撃ってください。"))
			{ ID = "spell22" };
		
		public static readonly LString survival23 = new LText("The boss has started a survival card. You cannot shoot down the boss. Wait for the timeout to the right of this text to hit zero.",
			(Locales.JP, "ボスは「サバイバル(SURVIVAL)」をはじめました。サバイバルの時はボスを撃てません。このテキストの右にあるタイマーがゼロになるまで死なないように頑張ってください。"))
			{ ID = "survival23" };
		
		public static readonly LString items24 = new LText("<size=2.17>The amount of items dropped by the boss decreases gradually after 50% of the timeout has elapsed. Defeat the boss within the first 50% of the timeout for maximum rewards. (Does not apply to survival cards.)</size>",
			(Locales.JP, "ボスが落とすアイテムの数は、タイマーが50%経たあと徐々に下がります。最大限のアイテムを得るために、ボスをなるべく早く倒してください。＊サバイバルはこの限りでない "))
			{ ID = "items24" };
		
		public static readonly LString bullets25 = new LText("Make sure to dodge the boss' bullets while shooting them down!",
			(Locales.JP, "ボスの弾幕を避けながらボスを撃つのが最善策。"))
			{ ID = "bullets25" };
		
		public static readonly LString shoot26 = new LText("Shoot down the boss, and try not to get hit.",
			(Locales.JP, "弾に当てられずにボスを倒してください。"))
			{ ID = "shoot26" };
		
		public static readonly LString lives27 = new LText(Render(null, new[] {
				"These are your lives. --------->",
				"\n",
				"A red dot is worth 2 lives,",
				"\n",
				"a pink dot is worth 1 life.",
			}),
			(Locales.JP, Render(Locales.JP, new[] {
				"残機ーーーーーーーー＞",
				"\n",
				"赤色のドットは２ライフに等しく、桃色のドットは１ライフに等しい。",
			})))
			{ ID = "lives27" };
		
		public static readonly LString dots28 = new LText("There are 9 dots. Right now, you have 10 lives.",
			(Locales.JP, "今はドットは9つあって、残機は10です。"))
			{ ID = "dots28" };
		
		public static readonly LString dots29 = new LText("Now you have 15 lives.",
			(Locales.JP, "今は残機は15です。"))
			{ ID = "dots29" };
		
		public static readonly LString dots30 = new LText("Now you have 1 life.",
			(Locales.JP, "今は残機は1です。"))
			{ ID = "dots30" };
		
		public static readonly LString nobombs31 = new LText("If you are hit by bullets, you will lose a life.",
			(Locales.JP, "被弾したら残機は1減ります。"))
			{ ID = "nobombs31" };
		
		public static readonly LString pleasedie32 = new LText("Try getting hit by the bullets.",
			(Locales.JP, "被弾してください。"))
			{ ID = "pleasedie32" };
		
		public static readonly LString deathscreen33 = new LText("When you run out of lives, this screen will appear. Depending on the game mode, you may be able to continue. Select the continue option-- there's still more tutorial left!",
			(Locales.JP, "残機が０になるとこのメニューが現れます。ゲームモード次第続けられます。「続ける」ボタンを押してください。チュートリアルはまだ終わってません！"))
			{ ID = "deathscreen33" };
		
		public static readonly LString lifeitems34 = new LText(Render(null, new[] {
				"These are your life items. ----->",
				"\n",
				"Fulfill the requirement to get an extra life.",
			}),
			(Locales.JP, Render(Locales.JP, new[] {
				"ライフアイテムーーー＞",
				"\n",
				"ライフアイテムを集めればエクステンドします。",
			})))
			{ ID = "lifeitems34" };
		
		public static readonly LString lifeitems35 = new LText("Collect life items (red) by running into them. If you go above the point of collection, all items will move to you.",
			(Locales.JP, "ライフアイテム（赤）に直接触れれば取れます。プレイエリアの上部にある自動回収線を超えればすべてのアイテムを回収します。"))
			{ ID = "lifeitems35" };
		
		public static string valueitems36(object arg0) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"得点アイテム（青）のベース点数は",
				"{0}",
				"です。プレイエリアの上の方で取るほどボーナスが出ます。",
			}, arg0),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"Value items (blue) increase your score by ",
				"{0}",
				", with a bonus if you collect them higher on the screen.",
			}, arg0),
		};
		
		public static LString valueitems36_ls(object arg0) => new LText(Render(null, new[] {
				"Value items (blue) increase your score by ",
				"{0}",
				", with a bonus if you collect them higher on the screen.",
			}, arg0),
			(Locales.JP, Render(Locales.JP, new[] {
				"得点アイテム（青）のベース点数は",
				"{0}",
				"です。プレイエリアの上の方で取るほどボーナスが出ます。",
			}, arg0)))
			{ ID = "valueitems36" };
		
		public static readonly LString points37 = new LText("Get 75,000 points by collecting value items.",
			(Locales.JP, "得点アイテムを集めて得点を75,000得てください。"))
			{ ID = "points37" };
		
		public static readonly LString scoremult38 = new LText(Render(null, new[] {
				"The score multiplier is the number below this text.",
				"\n",
				"It multiplies the points gained from value items. Increase it by collecting point++ (green) items.",
			}),
			(Locales.JP, "このテキストの真下にある数字はスコアマルチプライヤーです。得点アイテムの点数はこの数に掛けます。点数増加アイテム（緑）を集めればスコアマルチプライヤーは増加します。"))
			{ ID = "scoremult38" };
		
		public static string faith39(object arg0) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"スコアマルチプライヤーの真下にある白いゲージは信仰メートルです。時間が経て減少します。グレイズすれば・敵を倒せば復します。メートルをすべて失ったらマルチプライヤーは",
				"{0}",
				"減少します。",
			}, arg0),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"The faith meter is the white bar below the multiplier. It will empty over time, but graze and defeating enemies will restore it. When empty, your multiplier will fall by ",
				"{0}",
				".",
			}, arg0),
		};
		
		public static LString faith39_ls(object arg0) => new LText(Render(null, new[] {
				"The faith meter is the white bar below the multiplier. It will empty over time, but graze and defeating enemies will restore it. When empty, your multiplier will fall by ",
				"{0}",
				".",
			}, arg0),
			(Locales.JP, Render(Locales.JP, new[] {
				"スコアマルチプライヤーの真下にある白いゲージは信仰メートルです。時間が経て減少します。グレイズすれば・敵を倒せば復します。メートルをすべて失ったらマルチプライヤーは",
				"{0}",
				"減少します。",
			}, arg0)))
			{ ID = "faith39" };
		
		public static readonly LString faithblue40 = new LText("While the faith meter is blue, it will not decay. Completing stage or boss sections, or collecting items, will add blue to the faith meter.",
			(Locales.JP, "信仰メートルは青い時は減少しません。ステージ部やボススペルカードを倒せば・アイテムを回収すれば信仰メートルは短時間青くなります。"))
			{ ID = "faithblue40" };
		
		public static readonly LString graze41 = new LText("The total graze count also increases the points gained from value items. This bonus is hidden, but does not decay.",
			(Locales.JP, "グレイズの合計数によって得点アイテムの点数にボーナスが出ます。このボーナスは見えないが、減少とかはしません。"))
			{ ID = "graze41" };
		
		public static readonly LString scoremult42 = new LText("Raise the score multiplier to 1.11 by collecting point++ items, then let it decay back to 1.",
			(Locales.JP, "点数増加アイテムを集めてスコアマルチプライヤーを1.11まで増加してください。その後1まで減少させてください。"))
			{ ID = "scoremult42" };
		
		public static readonly LString scoreext43 = new LText("If you get enough score, you will get extra lives. The first score extend is 2,000,000.",
			(Locales.JP, "得点によってエクステンドします。最初のスコアエクステンドは2,000,000点です。"))
			{ ID = "scoreext43" };
		
		public static readonly LString scoreext44 = new LText("Get 2,000,000 points by collecting point++ items and value items.",
			(Locales.JP, "得点アイテムと点数増加アイテムを集めて得点を2,000,000得てください。"))
			{ ID = "scoreext44" };
		
		public static readonly LString ability45 = new LText("There is a yellow bar below the faith meter, which allows you to use a special ability.",
			(Locales.JP, "信仰メートルの真下にある黄色のゲージは特殊能力メートルです。"))
			{ ID = "ability45" };
		
		public static string ability46(object arg0) => Locales.Provider.TextLocale.Value switch {
			Locales.JP => Render(Locales.Provider.TextLocale.Value, new[] {
				"{0}",
				"を押し続けてバレットタイムを発動してください。バレットタイムは時間の経過を50%緩めます。",
			}, arg0),
			_ => Render(Locales.Provider.TextLocale.Value, new[] {
				"Hold ",
				"{0}",
				" to activate bullet time, which slows the game speed to 50%.",
			}, arg0),
		};
		
		public static LString ability46_ls(object arg0) => new LText(Render(null, new[] {
				"Hold ",
				"{0}",
				" to activate bullet time, which slows the game speed to 50%.",
			}, arg0),
			(Locales.JP, Render(Locales.JP, new[] {
				"{0}",
				"を押し続けてバレットタイムを発動してください。バレットタイムは時間の経過を50%緩めます。",
			}, arg0)))
			{ ID = "ability46" };
		
		public static readonly LString ability47 = new LText("While in bullet time, value items and point++ items are worth twice as much, and the player moves comparatively faster. Try collecting some items with or without bullet time.",
			(Locales.JP, "バレットタイムが作動している間は、得点・点数増加アイテムの価値は2倍になって、自機の比較的速度は早くなります。バレットタイムが作動している・していない間にアイテムを集めて見てください。"))
			{ ID = "ability47" };
		
		public static readonly LString meter48 = new LText("You can refill the special meter by collecting yellow gem items.",
			(Locales.JP, "ジェムアイテム（黄）を集めれば特殊能力メートルは復します。"))
			{ ID = "meter48" };
		
		public static readonly LString hitbox49 = new LText("In general, enemy bullets have much smaller hitboxes than their visual size.",
			(Locales.JP, "一通りの弾はスプライトのサイズより当たり判定のサイズが遥かに小さい。"))
			{ ID = "hitbox49" };
		
		public static readonly LString hitbox50 = new LText("The exception is sun bullets. These have very large hitboxes. (similar to Subterranean Animism's sun bullets)",
			(Locales.JP, "例外は太陽弾です。太陽弾の当たり判定は大きいです。（東方地霊殿の核弾に同様）"))
			{ ID = "hitbox50" };
		
		public static readonly LString safelaser51 = new LText("Also, safe lasers do not have hitboxes. Everything above the boss is a normal laser, and everything below is a safe laser.",
			(Locales.JP, "セーフレーザに当たり判定は一切ありません。ボスの上にあるレーザは普通であり、ボスの下にあるレーザはセーフレーザです。"))
			{ ID = "safelaser51" };
		
		public static readonly LString end52 = new LText(Render(null, new[] {
				"That's all! To finish the tutorial, select ",
				"\"",
				"Return to Title",
				"\"",
				" from the pause menu.",
			}),
			(Locales.JP, "チュートリアルおしまい！ポーズメニューの「タイトルに戻る」ボタンを押してください。"))
			{ ID = "end52" };
		
	}
}
}
