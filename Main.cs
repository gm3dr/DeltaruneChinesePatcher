using Godot;
using System;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;

public partial class Main : Control
{
	#region Node References
	[Export]
	ColorRect nodeTitleBar = null!;

	[Export]
	AnimationPlayer nodeBgAnim = null!;
	[Export]
	Button nodeBtnInfo = null!;
	[Export]
	OptionButton nodeComboLanguage = null!;
	[Export]
	Button nodeTextPatcherVersion = null!;
	[Export]
	Button nodeBtnUpdatePatcher = null!;

	[Export]
	Label nodeTextPatchVersion = null!;
	[Export]
	Container nodeUpdatePatchRow = null!;
	[Export]
	OptionButton nodeBtnUpdatePatch = null!;
	[Export]
	ProgressBar nodeProgress = null!;
	[Export]
	LineEdit nodeEditGamePath = null!;
	[Export]
	RichTextLabel nodePathValid = null!;
	[Export]
	Button nodeBtnBrowse = null!;
	[Export]
	Button nodeBtnPatch = null!;
	[Export]
	OptionButton nodeBtnRun = null!;
	[Export]
	Button nodeBtnUnpatch = null!;

	[Export]
	FileDialog nodeOpenDialog = null!;
	[Export]
	Window nodeWindowReadme = null!;
	[Export]
	Label nodeWindowReadmeContent = null!;
	[Export]
	Window nodeWindowLog = null!;
	[Export]
	Label nodeWindowLogContent = null!;
	[Export]
	Window nodeWindowPopup = null!;
	[Export]
	Label nodeWindowPopupContent = null!;
	[Export]
	Window nodeWindowPopup1225 = null!;
	[Export]
	Window nodeWindowPatch = null!;
	[Export]
	VBoxContainer nodeWindowPatchVBox = null!;
	[Export]
	Label nodeWindowPatchContent = null!;
	[Export]
	Window nodeWindowTutorial = null!;
	[Export]
	Window nodeWindowAdvanced = null!;
	[Export]
	CenterContainer nodeContainerAdvanced = null!;
	[Export]
	LineEdit nodeOverrideOS = null!;
	[Export]
	SpinBox nodeOverrideScale = null!;
	#endregion

	static readonly System.Net.Http.HttpClient httpc = new();
	static string[] chapters = [];
	static int patch_count_except_chapters = 0; // chapterX.xdelta 以外的 xdelta 数量，目前只有 main.xdelta
	static string xdelta3 = GetGameDirPath("externals/xdelta3/xdelta3");
	static string _7zip = GetGameDirPath("externals/7zip/7z");
	static bool used_fallback = false; // ws3917 - 是否使用了備用安裝補丁
	static bool patch_failed = false; // ws3917 - 补丁安装失败的信号
	static readonly int too_long_time = 45;
	static readonly Godot.Collections.Dictionary<string, Godot.Collections.Array<string>> available_externals = new()
	{
		{"7z", ["7z", "7zip", "7-zip", "7zr", "7za", "7zz"]},
		{"xdelta", ["xdelta", "xdelta3"]}
	};
	static readonly Godot.Collections.Dictionary<string, string> externals_hash = new()
	{
		{GetGameDirPath("externals/7zip/7z"), "20df89e993594c1bb7686f125dabe1acc56c109fb1d9b40435ea5fcbc1ca3453"},
		{GetGameDirPath("externals/7zip/7z.exe"), "56b8cc9f4971cef253644fafe54063ed7fdca551d4dee0f8c6baa81b855acd72"},
		{GetGameDirPath("externals/7zip/7z_mac"), ""}, // 保留用于文件存在检测
		{GetGameDirPath("externals/xdelta3/xdelta3"), "7598709e2a13869d7538602ecc3e0bef931be380680ef521710ff27930182436"},
		{GetGameDirPath("externals/xdelta3/xdelta3.exe"), "8a3f91bdbcc3e8ea3f727937673bf6c46abaa7d0aa4eae475b9733302ebc6674"},
		{GetGameDirPath("externals/xdelta3/xdelta3_mac"), ""} // 保留用于文件存在检测
	};
	static readonly Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>> default_paths = new()
	{
		{"libraryfolders", new()
			{
				{"Windows", "{STEAMPATH}/steamapps/libraryfolders.vdf"},
				{"macOS", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/Library/Application Support/Steam/steamapps/libraryfolders.vdf"},
				{"Linux", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/.local/share/Steam/steamapps/libraryfolders.vdf"}
			}
		},
		{"deltarune", new()
			{
				{"Windows", "{STEAMPATH}/steamapps/common/DELTARUNE"},
				{"macOS", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/Library/Application Support/Steam/steamapps/common/DELTARUNE/DELTARUNE.app/Contents/Resources"},
				{"Linux", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/.local/share/Steam/steamapps/common/DELTARUNE"}
			}
		},
		{"deltarune_demo", new()
			{
				{"Windows", "{STEAMPATH}/steamapps/common/DELTARUNEdemo"},
				{"macOS", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/Library/Application Support/Steam/steamapps/common/DELTARUNEdemo/DELTARUNE.app/Contents/Resources"},
				{"Linux", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/.local/share/Steam/steamapps/common/DELTARUNEdemo"}
			}
		}
	};
	static string game_path_file = GetGameDirPath("game_path.txt");
	static string patchdir = GetGameDirPath("patch");
	static string patchver = "locNotFound";
	static string demopatchver = "";
	static string demopatchdir = GetGameDirPath("patch");
	static Godot.Collections.Dictionary patcherreleases = new();
	static Godot.Collections.Dictionary patchreleases = new();
	static string os_name = OS.GetName();
	static readonly Architecture os_arch = RuntimeInformation.ProcessArchitecture;
	static string osname = (os_name == "macOS" ? "mac" : "windows");
	static string dataname = (os_name == "macOS" ? "game.ios" : "data.win");
	static readonly bool is_outdated_ver = Engine.GetVersionInfo()["major"].AsInt32() <= 4 && Engine.GetVersionInfo()["minor"].AsInt32() <= 4;
	static readonly bool is_self_extract = os_name == "Windows" && GetGameDirPath().Replace("/","\\").Contains(System.IO.Path.GetTempPath().TrimSuffix("\\"));
	static string[] locales;
	static bool inited = false;
	static Vector2 windowDesignSize = new Vector2(640, 480);
	static Vector2 windowDesignSpaceSize = new Vector2(640, 480) * 1.5f;
	static int windowScale = Math.Max(Mathf.FloorToInt((DisplayServer.ScreenGetUsableRect().Size.Y - DisplayServer.ScreenGetUsableRect().Position.Y) / windowDesignSpaceSize.Y), 1);
	static System.IO.FileStream fileStream = null;
	static Godot.Collections.Array output = [];
	static int patched_count = 0;
	static DateTime starttime = DateTime.MinValue;
	static bool patchingdemo = false;
	// advanced options
	static bool bypass_hash = false;
	static bool bypass_too_long = false;
	static bool bypass_same_path = false;
	static bool use_installed_xdelta = false;
	static bool use_installed_7zip = false;
	static bool bypass_restore_when_failed = false;
	static int force_patch = 0; // 0=disabled, 1=full, 2=demo
	static string github_api = "https://api.github.com";
	static string xdelta_override = "";
	static string _7zip_override = "";
	public override async void _Ready()
	{
		var window = GetWindow();
		var wid = window.GetWindowId();
		var datedict = Time.GetDateDictFromSystem();
		//首次初始化
		if (!inited)
		{
			httpc.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36");
			// Outdated notification
			if (is_outdated_ver)
			{
				nodeWindowPopupContent.Text = "locOutdatedVer";
				nodeWindowPopupContent.Set("theme_override_font_sizes/font_size", FontSize(27, windowScale));
				nodeWindowPopup.Size = new Vector2I(TranslationServer.GetLocale().StartsWith("en") ? 960 : 640, 270) * windowScale;
				nodeWindowPopup.Title = "locCaution";
				nodeWindowPopup.Show();
			}

			osname = (os_name == "macOS" ? "mac" : "windows");
			dataname = (os_name == "macOS" ? "game.ios" : "data.win");
			nodeBgAnim.Play("bg_anim");
			nodeComboLanguage.Disabled = true;

			if (os_name == "macOS")
			{
				window.Unresizable = true;
				nodeBtnInfo.Position = new Vector2(nodeBtnInfo.Position.X, 32);
				nodeComboLanguage.Position = new Vector2(nodeComboLanguage.Position.X, 32);
				nodeTitleBar.Visible = true;
				if (OS.IsDebugBuild())
				{
					nodeTitleBar.Color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
				}
				else
				{
					nodeTitleBar.Color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
				}

				// macOS 隔离检测
				var execPath = OS.GetExecutablePath();
				var hasAppTranslocation = Regex.IsMatch(execPath, "^/private/var/folders/(?:[^/]+/)+AppTranslocation/[0-9a-fA-F-]+/");
				PrintLog("macOS AppTranslocation: " + (hasAppTranslocation ? "Detected (" + execPath + ")" : "Not detected"));
				bool hasQuarantine = false;
				string quarantineDetail = "";
				string quarantineApp = "";
				string quarantineDate = "";
				try
				{
					var psi = new ProcessStartInfo("/usr/bin/xattr", "-p com.apple.quarantine \"" + execPath + "\"")
					{
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true
					};
					using var proc = Process.Start(psi);
					if (proc != null)
					{
						var stdout = proc.StandardOutput.ReadToEnd();
						var stderr = proc.StandardError.ReadToEnd();
						proc.WaitForExit();
						hasQuarantine = proc.ExitCode == 0 && !string.IsNullOrEmpty(stdout.Trim());
						if (hasQuarantine)
						{
							quarantineDetail = stdout.Trim();
							// 解析 xattr 格式: <flags_hex>;<timestamp_hex>;<app_name>
							var qParts = quarantineDetail.Split(';');
							if (qParts.Length >= 3 && long.TryParse(qParts[1], System.Globalization.NumberStyles.HexNumber, null, out var ts))
							{
								quarantineApp = qParts[2];
								quarantineDate = DateTimeOffset.FromUnixTimeSeconds(ts).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
								PrintLog("macOS quarantine xattr: Detected (App: " + quarantineApp + ", Date: " + quarantineDate + ")");
							}
							else
							{
								PrintLog("macOS quarantine xattr: Detected (Raw: " + quarantineDetail + ")");
							}
						}
						else
						{
							PrintLog("macOS quarantine xattr: Not detected" + (stderr.Trim() != "" ? " - " + stderr.Trim() : ""));
						}
					}
				}
				catch (Exception exc)
				{
					PrintLog("Exception when checking com.apple.quarantine: " + exc.ToString() + " (" + exc.Message + ")", 2);
				}
				// 弹出检测结果
				if (hasAppTranslocation)
				{
					string quarantineInfo;
					if (hasQuarantine && !string.IsNullOrEmpty(quarantineApp))
						quarantineInfo = TranslationServer.Translate("locMacQuarantineXattrInfo").ToString()
							.Replace("{0}", quarantineApp).Replace("{1}", quarantineDate);
					else
						quarantineInfo = "";
					nodeWindowPopupContent.Text = TranslationServer.Translate("locMacQuarantineFound").ToString()
						.Replace("{0}", quarantineInfo);
					nodeWindowPopupContent.Set("theme_override_font_sizes/font_size", FontSize(27, windowScale));
					nodeWindowPopup.Size = new Vector2I(640, 360) * windowScale;
					nodeWindowPopup.Title = "locMacQuarantineTitle";
					nodeWindowPopup.Show();
				}
			}

			var windowNewSize = (Vector2I)((windowDesignSize * windowScale).Round());
			DisplayServer.WindowSetSize(windowNewSize, wid);
			window.MoveToCenter();
			nodeOverrideScale.Value = windowScale;

			//Tooltip与下拉菜单大小
			/*
			var theme = Theme;
			theme.SetFontSize("font_size", "PopupMenu", FontSize(theme.GetFontSize("font_size", "PopupMenu"), windowScale));
			theme.SetFontSize("font_size", "TooltipLabel", FontSize(theme.GetFontSize("font_size", "TooltipLabel"), windowScale));
			Theme = theme;
			*/
			//最大帧率
			Engine.MaxFps = Mathf.RoundToInt(DisplayServer.ScreenGetRefreshRate(window.CurrentScreen));
			//根据系统语言切换语言
			if (OS.GetLocale() == "zh_TW" || OS.GetLocale() == "zh_HK" || OS.GetLocale() == "zh_MO")
			{
				TranslationServer.SetLocale("zh_TW");
			}
			else if (OS.GetLocaleLanguage() == "zh" || OS.GetLocale() == "zh_CN" || OS.GetLocale() == "zh_SG")
			{
				TranslationServer.SetLocale("zh_CN");
			}
			else
			{
				TranslationServer.SetLocale(OS.GetLocale());
			}
			//寻找patch档案
			foreach (var file in DirAccess.GetFilesAt(GetGameDirPath()))
			{
				if (file.StartsWith("patch_"))
				{
					if (file.Contains("demo"))
					{
						demopatchdir = GetGameDirPath(file);
						demopatchver = System.IO.Path.GetFileNameWithoutExtension(file).Split("_")[^1];
						PrintLog("Found demo patch file " + patchdir);
						continue;
					}
					patchdir = GetGameDirPath(file);
					patchver = System.IO.Path.GetFileNameWithoutExtension(file).Split("_")[^1];
					// 1225 check
					if (patchver == "1225" && datedict["month"].AsString() == "12" && datedict["day"].AsString() == "25")
					{
						patchver = "■■■■";
					}
					PrintLog("Found patch file " + patchdir);
					continue;
				}
			}
			//自动显示readme
			foreach (var file in DirAccess.GetFilesAt(GetGameDirPath("")))
			{
				if (file.ToLower().Contains("readme") && !file.EndsWith(".md"))
				{
					var readme = FileAccess.Open(GetGameDirPath(file), FileAccess.ModeFlags.Read);
					if (readme != null)
					{
						nodeWindowReadmeContent.Text = readme.GetAsText();
						nodeWindowReadmeContent.Set("theme_override_font_sizes/font_size", FontSize(27, windowScale));
						nodeWindowReadme.Title = file;
						nodeWindowReadme.Size = new Vector2I(960, 600) * windowScale;
						nodeWindowReadme.Show();
						readme.Close();
					}
					break;
				}
			}
			nodeBtnPatch.Disabled = (patchver == "locNotFound");
			nodeBtnUpdatePatch.TooltipText = "locUpdatePatchInfo" + (os_name == "macOS" ? "Mac" : "");
			nodeOverrideOS.PlaceholderText = OS.GetName();
			nodeOverrideScale.Value = windowScale;
		}
		DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Normal);
		DisplayServer.WindowSetTaskbarProgressValue(0);
		DisplayServer.WindowSetTitle(TranslationServer.Translate("locTitle"), wid);
		nodeBtnUpdatePatcher.Visible = false;
		nodeBtnUpdatePatcher.Disabled = false;
		//安装器版本号
		nodeTextPatcherVersion.Text = "v" + ProjectSettings.GetSetting("application/config/version").AsString() + (is_self_extract ? "-SelfExtract" : "") + (is_outdated_ver ? "-Outdated" : "");
		// 1225 check
		if (patchver == "1225" || patchver == "■■■■" || (datedict["month"].AsString() == "12" && datedict["day"].AsString() == "25"))
		{
			nodeTextPatcherVersion.TooltipText = nodeTextPatcherVersion.Text + "\n" + TranslationServer.Translate("locAdvancedTooltip");
			nodeTextPatcherVersion.Text = "v1225";
		}
		//系统特供目录
		if (os_name == "Windows")
		{
			xdelta3 = GetGameDirPath("externals/xdelta3/xdelta3.exe");
			_7zip = GetGameDirPath("externals/7zip/7z.exe");
		}
		else if (os_name == "macOS")
		{
			xdelta3 = GetGameDirPath("externals/xdelta3/xdelta3_mac");
			_7zip = GetGameDirPath("externals/7zip/7z_mac");
		}
		//语言选项
		locales = TranslationServer.GetLoadedLocales();
		nodeComboLanguage.ItemCount = locales.Length;
		foreach (var current in locales)
		{
			nodeComboLanguage.Set("popup/item_" + Array.IndexOf(locales, current).ToString() + "/text", TranslationServer.FindTranslations(current, true)[0].GetMessage("locLanguageName"));
		}
		nodeComboLanguage.Selected = Array.IndexOf(locales.ToArray(), locales.Contains(TranslationServer.GetLocale()) ? TranslationServer.GetLocale() : TranslationServer.GetLocale().Left(2));
		//读取之前的游戏路径
		var game_path_f = FileAccess.Open(game_path_file, FileAccess.ModeFlags.Read);
		var game_path = "";
		if (game_path_f != null)
		{
			game_path = game_path_f.GetAsText();
			game_path_f.Close();
		}
		else
		{
			game_path = FindGamePath();
		}
		nodeEditGamePath.Text = game_path;
		_on_edit_game_path_text_changed(game_path);
		//HttpClient
		nodeBtnInfo.TooltipText = "locInfo";
		nodeTextPatchVersion.Text = TranslationServer.Translate("locLocalVer")  + "\n" + TranslationServer.Translate("locFullVersion")  + " [" + TranslationServer.Translate(patchver) + "]" + (!string.IsNullOrEmpty(demopatchver) ? ("  |  " + TranslationServer.Translate("locDemoVersion")  + " [" + demopatchver.ToString() + "]") : "") + (is_self_extract ? "" : "\n" + TranslationServer.Translate("locLatestVer") + TranslationServer.Translate("locRequesting"));
		//contributors
		var json = new Json();
		try
		{
			json.Parse(await httpc.GetStringAsync(github_api + "/repos/gm3dr/DeltaruneChinesePatcher/contributors"));
			var names = "";
			foreach (var contributor in json.Data.AsGodotArray<Godot.Collections.Dictionary<string, string>>())
			{
				names += contributor["login"] + ", ";
			}
			names = names.TrimSuffix(", ");
			if (names != "")
			{
				nodeBtnInfo.TooltipText = TranslationServer.Translate("locInfoContributors").ToString().Replace("{CONTRIBUTORS}", names);
			}
		}
		catch (Exception exc)
		{
			PrintLog("Exception catched when requesting contributors: " + exc.ToString() + " (" + exc.Message + ")", 2);
		}
		//补丁版本号
		json = new Json();
		try
		{
			if (is_self_extract)
			{
				nodeTextPatchVersion.Text = TranslationServer.Translate("locLocalVer")  + "\n" + TranslationServer.Translate("locFullVersion")  + " [" + TranslationServer.Translate(patchver) + "]";
				if (!string.IsNullOrEmpty(demopatchver))
				{
					nodeTextPatchVersion.Text += "  |  " + TranslationServer.Translate("locDemoVersion")  + " [" + demopatchver.ToString() + "]";
				}
				UpdatePathText(nodeEditGamePath.Text, false);
			}
			else
			{
				if (!inited)
				{
					json.Parse(await httpc.GetStringAsync(github_api + "/repos/gm3dr/DeltaruneChinese/releases/latest"));
					patchreleases = json.Data.AsGodotDictionary();
				}
				var latestver = patchreleases["tag_name"].AsString();
				// 1225 check
				if (latestver == "1225" && datedict["month"].AsString() == "12" && datedict["day"].AsString() == "25")
				{
					latestver = "■■■■";
				}
				nodeTextPatchVersion.Text = TranslationServer.Translate("locLocalVer")  + "\n" + TranslationServer.Translate("locFullVersion")  + " [" + TranslationServer.Translate(patchver) + "]";
				if (!string.IsNullOrEmpty(demopatchver))
				{
					nodeTextPatchVersion.Text += "  |  " + TranslationServer.Translate("locDemoVersion")  + " [" + demopatchver.ToString() + "]";
				}
				nodeTextPatchVersion.Text += "\n" + TranslationServer.Translate("locLatestVer") + latestver;
				UpdatePathText(nodeEditGamePath.Text, false);
				if (patchver != patchreleases["tag_name"].AsString() || demopatchver != patchreleases["tag_name"].AsString())
				{
					nodeUpdatePatchRow.Visible = true;
				}
				if (nodeWindowReadmeContent.Text != "")
				{
					foreach (var asset in patchreleases["assets"].AsGodotArray())
					{
						if (asset.AsGodotDictionary()["name"].AsString().ToLower().Contains("readme"))
						{
							var text = await httpc.GetStringAsync(asset.AsGodotDictionary()["browser_download_url"].AsString());
							var readme = FileAccess.Open(GetGameDirPath("readme.txt"), FileAccess.ModeFlags.Write);
							if (readme != null)
							{
								readme.StoreString(text);
								nodeWindowReadmeContent.Text = text;
								nodeWindowReadmeContent.Set("theme_override_font_sizes/font_size", FontSize(27, windowScale));
								nodeWindowReadme.Title = "readme.txt";
								nodeWindowReadme.Size = new Vector2I(960, 600) * windowScale;
								nodeWindowReadme.Show();
								readme.Close();
							}
							break;
						}
					}
				}
			}
		}
		catch (HttpRequestException exc)
		{
			PrintLog("Exception catched when requesting patch latest: " + exc.ToString() + " (" + exc.Message + ")", 2);
			//nodeTextPatchVersion.Text = TranslationServer.Translate("locLocalVer") + TranslationServer.Translate(patchver) + "\n" + TranslationServer.Translate("locLatestVer") + TranslationServer.Translate("locTimeout").ToString().TrimPrefix(" ");
		}
		//安装器更新
		if (!(OS.HasFeature("editor") || ProjectSettings.GetSetting("application/config/version").AsString().Contains("dev") || is_self_extract))
		{
			json = new Json();
			try
			{
				if (!inited)
				{
					json.Parse(await httpc.GetStringAsync(github_api + "/repos/gm3dr/DeltaruneChinesePatcher/releases/latest"));
					patcherreleases = json.Data.AsGodotDictionary();
				}
				if (patcherreleases["tag_name"].AsString() != "v" + ProjectSettings.GetSetting("application/config/version").AsString())
				{
					nodeBtnUpdatePatcher.Text = TranslationServer.Translate("locUpdate").ToString().Replace("{VER}", patcherreleases["tag_name"].AsString());
					nodeBtnUpdatePatcher.Visible = true;
				}
			}
			catch (HttpRequestException exc)
			{
				PrintLog("Exception catched when requesting patcher latest: " + exc.ToString() + " (" + exc.Message + ")", 2);
			}
		}

		if (!inited)
		{
			nodeComboLanguage.Disabled = false;
			inited = true;
		}
	}

	public void _on_language_item_selected(int selected)
	{
		if (OS.IsStdOutVerbose())
		{
			PrintLog($"Language changed from {TranslationServer.GetLocale()} to {locales[selected]}.");
		}
		TranslationServer.SetLocale(locales[selected]);
		_Ready();
		//GetTree().ReloadCurrentScene();
	}
	public void _on_browse_pressed()
	{
		nodeOpenDialog.Show();
	}
	public void _on_file_dialog_dir_selected(string dir)
	{
		nodeEditGamePath.Text = dir;
		_on_edit_game_path_text_changed(dir);
	}
	public void _on_window_close_requested()
	{
		nodeWindowLog.Hide();
	}
	public void _on_patch_pressed()
	{
		DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Indeterminate);
		nodeBtnPatch.Disabled = true;
		nodeBtnUnpatch.Disabled = true;
		nodeEditGamePath.Editable = false;
		nodeBtnBrowse.Disabled = true;

		var path = PathTrim(nodeEditGamePath.Text);

		nodeEditGamePath.Text = path;
		if (DirAccess.DirExistsAbsolute(path + "/backup"))
		{
			if (path != "" && FileAccess.FileExists(path + "/backup/version"))
			{
				var ver = FileAccess.Open(path + "/backup/version", FileAccess.ModeFlags.Read);
				if (ver != null)
				{
					nodeWindowPatchContent.Text = TranslationServer.Translate("locBakVerDetected").ToString().Replace("{VER}", ver.GetAsText());
					ver.Close();
				}
				else
				{
					nodeWindowPatchContent.Text = "locBakDetected";
				}
			}
			else
			{
				nodeWindowPatchContent.Text = "locBakDetected";
			}
			nodeWindowPatchContent.Set("theme_override_font_sizes/font_size", FontSize(27, windowScale));
			nodeWindowPatch.Size = new Vector2I(TranslationServer.GetLocale().StartsWith("en") ? 800 : 640, 320) * windowScale;
			nodeWindowPatchVBox.CustomMinimumSize = new Vector2(640, 0) * windowScale;
			nodeWindowPatch.Show();
		}
		else
		{
			Patch();
		}
	}
	public void _on_info_pressed()
	{
		OS.ShellOpen("https://github.com/gm3dr/DeltaruneChinesePatcher");
	}

	public void _on_rungame_selected(int selected)
	{
		nodeBtnRun.Selected = 0;
		switch (selected)
		{
			case 1:
				OS.ShellOpen("steam://run/1671210");
				break;
			case 2:
				OS.ShellOpen("steam://run/1690940");
				break;
			case 3:
				if (os_name == "Windows")
				{
					var extract_process = new Process();
					var starti = new ProcessStartInfo();
					starti.FileName = PathTrim(nodeEditGamePath.Text) + "/DELTARUNE.exe";
					starti.WorkingDirectory = PathTrim(nodeEditGamePath.Text);
					extract_process.StartInfo = starti;
					extract_process.Start();
				}
				else if (os_name == "macOS")
				{
					var appBundlePath = System.IO.Path.GetFullPath(PathTrim(nodeEditGamePath.Text) + "/../..");
					OS.CreateProcess("/usr/bin/open", ["-a", appBundlePath]);
				}
				break;
		}
	}
	public void _on_popup_close_requested()
	{
		DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Noprogress);
		nodeWindowPopup.Hide();
	}
	public void _on_popup_1225_close_requested()
	{
		nodeWindowPopup1225.Hide();
	}
	public void _on_patch_close_requested()
	{
		DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Noprogress);
		nodeWindowPatch.Hide();
		nodeBtnPatch.Disabled = false;
		nodeBtnUnpatch.Disabled = false;
		nodeEditGamePath.Editable = true;
		nodeBtnBrowse.Disabled = false;
	}
	public void _on_readme_close_requested()
	{
		nodeWindowReadme.Hide();
	}
	public void _on_tutorial_close_requested()
	{
		nodeWindowTutorial.Hide();
	}
	public void _on_advanced_close_requested()
	{
		nodeWindowAdvanced.Hide();
		inited = false;
		_Ready();
	}
	public void _on_tutorial_pressed()
	{
		OS.ShellOpen("https://www.bilibili.com/video/BV1AuZcBJE56");
	}
	public void _on_update_pressed()
	{
		foreach (var asset in patcherreleases["assets"].AsGodotArray())
		{
			var name = asset.AsGodotDictionary()["name"].AsString().ToLower();
			if (name.Contains(os_name.ToLower()) && (((!is_outdated_ver) && (!name.Contains("outdated"))) || (is_outdated_ver && name.Contains("outdated"))))
			{
				OS.ShellOpen(asset.AsGodotDictionary()["browser_download_url"].AsString());
				break;
			}
		}
	}
	public void _on_patch_updated_pressed()
	{
		Patch(true);
	}
	public void _on_game_updated_pressed()
	{
		Patch(false);
	}
	public void _on_option_button_item_selected(int selected)
	{
		switch (selected)
		{
			case 1:
				_on_update_patch_pressed(false);
				break;
			case 2:
				_on_update_patch_browser_pressed(false);
				nodeBtnUpdatePatch.Selected = 0;
				break;
			case 4:
				_on_update_patch_pressed(true);
				break;
			case 5:
				_on_update_patch_browser_pressed(true);
				nodeBtnUpdatePatch.Selected = 0;
				break;
		}
	}
	public async void _on_update_patch_pressed(bool demo)
	{
		nodeBtnUpdatePatch.Disabled = true;
		nodeProgress.Visible = true;
		//下载patch
		var url = "";
		var file = "";
		var size = 0;
		foreach (var asset in patchreleases["assets"].AsGodotArray())
		{
			var filename = asset.AsGodotDictionary()["name"].AsString().ToLower();
			if (filename.Contains(os_name.ToLower()) && ((demo && filename.Contains("demo")) || ((!demo) && !filename.Contains("demo"))))
			{
				url = asset.AsGodotDictionary()["browser_download_url"].AsString();
				file = "_downloadingtemp_" + asset.AsGodotDictionary()["name"].AsString();
				size = asset.AsGodotDictionary()["size"].AsInt32();
				nodeProgress.MaxValue = size;
				break;
			}
		}
		if (url != "")
		{
			PrintLog("Downloading " + url + " to " + GetGameDirPath(file));
			try
			{
				using (var response = await httpc.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
				{
					response.EnsureSuccessStatusCode();
					using var bodyStream = await response.Content.ReadAsStreamAsync();
					fileStream = new System.IO.FileStream(GetGameDirPath(file), System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
					var buffer = new byte[4096];
					double totalRead = 0;
					int bytesRead;
					DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Normal);
					DisplayServer.WindowSetTaskbarProgressValue(0);
					while ((bytesRead = await bodyStream.ReadAsync(buffer)) > 0)
					{
						await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
						totalRead += bytesRead;
						if (size > 0)
						{
							nodeProgress.Value = totalRead;
							//nodeProgress.TooltipText = $"{Math.Round(totalRead/1024d/1024d, 2)} / {Math.Round(size/1024d/1024d, 2)} MiB";
							var progress = Math.Round(totalRead / 1024d / 1024d, 2).ToString();
							var sizee = Math.Round(size / 1024d / 1024d, 2).ToString();
							if (!progress.Contains("."))
							{
								progress += ".00";
							}
							else if (progress.Split(".")[1].Length == 1)
							{
								progress += "0";
							}
							if (!sizee.Contains("."))
							{
								sizee += ".00";
							}
							else if (sizee.Split(".")[1].Length == 1)
							{
								sizee += "0";
							}
							nodeBtnUpdatePatch.Text = $"{progress} / {sizee} MiB";
							DisplayServer.WindowSetTaskbarProgressValue((float)(totalRead / size));
							if (OS.IsStdOutVerbose())
							{
								PrintLog($"Downloaded: {totalRead} / {size}");
							}
						}
						if (totalRead >= size)
						{
							break;
						}
					}
				}
			}
			catch (Exception exc)
			{
				nodeBtnUpdatePatch.Text = TranslationServer.Translate("locDownloadFailed") + exc.GetType().ToString();
				nodeBtnUpdatePatch.TooltipText = exc.Message;
				PrintLog("Exception catched when updating patch: " + exc.ToString() + " (" + exc.Message + ")", 2);
				DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Error);
				return;
			}
			fileStream.Dispose();
			fileStream = null;
			DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Noprogress);
			PrintLog($"Download {file} finished.");
			//删除旧patch
			foreach (var fff in DirAccess.GetFilesAt(GetGameDirPath()))
			{
				if (fff.StartsWith("patch_") && ((demo && fff.Contains("demo")) || ((!demo) && !fff.Contains("demo"))))
				{
					DirAccess.RemoveAbsolute(GetGameDirPath(fff));
					PrintLog("Removed " + GetGameDirPath(fff));
				}
			}
			DirAccess.RenameAbsolute(GetGameDirPath(file), GetGameDirPath(file.TrimPrefix("_downloadingtemp_")));
			PrintLog($"Renamed {file} to " + file.TrimPrefix("_downloadingtemp_") + ".");
			nodeBtnUpdatePatch.Text = "locRestartIn5Sec";
			nodeBtnUpdatePatch.TooltipText = "locRestartIn5Sec";
			await ToSignal(GetTree().CreateTimer(5f), "timeout");
			GetTree().ReloadCurrentScene();
		}
	}
	public void _on_update_patch_browser_pressed(bool demo)
	{
		foreach (var fff in DirAccess.GetFilesAt(GetGameDirPath()))
		{
			if (fff.StartsWith("patch_") && ((demo && fff.Contains("demo")) || ((!demo) && !fff.Contains("demo"))))
			{
				DirAccess.RemoveAbsolute(GetGameDirPath(fff));
				PrintLog("Removed " + GetGameDirPath(fff));
			}
		}
		foreach (var asset in patchreleases["assets"].AsGodotArray())
		{
			var filename = asset.AsGodotDictionary()["name"].AsString().ToLower();
			if (filename.Contains(os_name.ToLower()) && ((demo && filename.Contains("demo")) || ((!demo) && !filename.Contains("demo"))))
			{
				OS.ShellOpen(asset.AsGodotDictionary()["browser_download_url"].AsString());
				break;
			}
		}
	}
	public void _on_text_patcher_version_pressed()
	{
		nodeWindowAdvanced.Size = new Vector2I(720, 860) * windowScale;
		nodeContainerAdvanced.Position = Vector2.Zero;
		nodeContainerAdvanced.Size = nodeWindowAdvanced.Size / windowScale;
		nodeContainerAdvanced.Scale = new(windowScale, windowScale);
		nodeWindowAdvanced.Show();
	}

	public void _on_title_bar_gui_input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				if (mouseButton.Pressed)
				{
					DisplayServer.WindowStartDrag();
				}
			}
		}
	}

	public static string PathTrim(string originalPath)
	{
		var finalPath = originalPath.Replace("\\","/").TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/");

		if (os_name != "Windows" && finalPath.StartsWith("~/"))
		{
			PrintLog("Non-Windows Home Directory Processing");
			string homePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
			finalPath = homePath + finalPath.Substring(1);
		}
		if (os_name == "macOS")
		{
			if (finalPath.EndsWith("/DELTARUNE"))
			{
				finalPath += "/DELTARUNE.app/Contents/Resources";
			}
			else if (finalPath.EndsWith(".app"))
			{
				finalPath += "/Contents/Resources";
			}
			else if (finalPath.EndsWith("/Contents"))
			{
				finalPath += "/Resources";
			}
		}

		PrintLog($"Final game path: {finalPath}");
		return finalPath;
	}
	public async void Patch(bool use_backup = true)
	{
		patch_failed = false;
		starttime = DateTime.Now;
		patched_count = 0;
		nodeWindowPatch.Hide();
		nodeWindowLogContent.Text = "";
		var path = PathTrim(nodeEditGamePath.Text);
		output = ["Patch at " + Time.GetDatetimeStringFromSystem(false, true) + ", " + Time.GetTimeZoneFromSystem()["name"]];
		// demo override
		if (force_patch > 0)
		{
			patchingdemo = (force_patch == 2);
		}
		PrintLog("Patching " + (patchingdemo ? demopatchdir : patchdir) + " on " + path);
		PathCheck(path, true);
		//chmod加权限
		if (os_name == "macOS" || os_name == "Linux")
		{
			if (xdelta3.Contains("/"))
			{
				OS.Execute("chmod", ["+x", xdelta3]);
				PrintLog($"chmod +x {xdelta3}");
			}
			if (_7zip.Contains("/"))
			{
				OS.Execute("chmod", ["+x", _7zip]);
				PrintLog($"chmod +x {_7zip}");
			}
		}
		//外部程序检查
		if (use_installed_7zip)
		{
			Godot.Collections.Array externalcheckoutput;
			int external_check_return;
			foreach (var __7z in available_externals["7z"])
			{
				externalcheckoutput = [];
				PrintLog("Checking " + __7z);
				if (os_name == "Windows")
				{
					external_check_return = OS.Execute("where", [__7z], externalcheckoutput);
				}
				else
				{
					external_check_return = OS.Execute("command", ["-v", __7z], externalcheckoutput);
				}
				PrintLog($"The result of \"{(os_name == "Windows" ? $"where {__7z}" : $"command -v {__7z}")}\": {external_check_return}");
				PrintLog(externalcheckoutput);
				if (external_check_return == 0)
				{
					_7zip = __7z;
					PrintLog("Found " + __7z);
					break;
				}
			}
		}
		if (use_installed_xdelta)
		{
			Godot.Collections.Array externalcheckoutput;
			int external_check_return;
			foreach (var __xdelta in available_externals["xdelta"])
			{
				externalcheckoutput = [];
				PrintLog("Checking " + __xdelta);
				if (os_name == "Windows")
				{
					external_check_return = OS.Execute("where", [__xdelta], externalcheckoutput);
				}
				else
				{
					external_check_return = OS.Execute("command", ["-v", __xdelta], externalcheckoutput);
				}
				PrintLog($"The result of \"{(os_name == "Windows" ? $"where {__xdelta}" : $"command -v {__xdelta}")}\": {external_check_return}");
				PrintLog(externalcheckoutput);
				if (external_check_return == 0)
				{
					xdelta3 = __xdelta;
					PrintLog("Found " + __xdelta);
					break;
				}
			}
		}
		// path override
		if (!string.IsNullOrEmpty(xdelta_override))
		{
			xdelta3 = xdelta_override;
			PrintLog("XDelta3 path was overridden to " + xdelta3);
		}
		if (!string.IsNullOrEmpty(_7zip_override))
		{
			_7zip = _7zip_override;
			PrintLog("7-Zip path was overridden to " + _7zip);
		}
		//existence check
		foreach (var pathhhhh in externals_hash.Keys)
		{
			if ((pathhhhh.Split("/").Last().Contains("7z") && _7zip == pathhhhh) || (pathhhhh.Split("/").Last().Contains("xdelta3") && xdelta3 == pathhhhh))
			{
				PrintLog($"Checking existence of {pathhhhh}");
				if (!FileAccess.FileExists(pathhhhh))
				{
					PrintLog("Unable to find " + pathhhhh);
					PatchResultHandler(false, "locPatchFailedNotExists", (DateTime.Now - starttime).TotalSeconds.ToString(), new Vector2I(640, 360));
					return;
				}
				PrintLog($"Found {pathhhhh}");
			}
		}
		//hash check
		if (bypass_hash || os_name == "macOS")
		{
			PrintLog($"Sha256 check {(os_name == "macOS" ? "disabled on macOS" : "bypassed")}.");
		}
		else
		{
			foreach (var pathhhhh in externals_hash.Keys)
			{
				if (((pathhhhh.Split("/").Last().Contains("7z") && _7zip == pathhhhh) || (pathhhhh.Split("/").Last().Contains("xdelta3") && xdelta3 == pathhhhh)) && FileAccess.FileExists(pathhhhh))
				{
					PrintLog($"Checking hash of {pathhhhh}");
					if (FileAccess.GetSha256(pathhhhh) != externals_hash[pathhhhh])
					{
						PrintLog(FileAccess.GetSha256(pathhhhh) + " != " + externals_hash[pathhhhh]);
						PatchResultHandler(false, "locPatchFailedSha256", (DateTime.Now - starttime).TotalSeconds.ToString(), new Vector2I(640, 360));
						return;
					}
					PrintLog("Hash matched: " + externals_hash[pathhhhh]);
				}
			}
			PrintLog("Sha256 check all passed.");
		}
		PrintLog("Extracting...");
		//解压
		string tempPath = "ExtractTemp";
		string extractArgs = $"x \"{(patchingdemo ? demopatchdir : patchdir)}\" -o\"" + GetGameDirPath(tempPath) + "\" -aoa -y";
		PrintLog($"{_7zip} {extractArgs}");
		var stime7z = DateTime.Now;
		var extract_process = new Process();
		var starti = new ProcessStartInfo();
		starti.FileName = _7zip;
		starti.Arguments = extractArgs;
		starti.RedirectStandardOutput = true;
		starti.RedirectStandardError = true;
		extract_process.StartInfo = starti;
		extract_process.OutputDataReceived += RecivedOutput;
		extract_process.ErrorDataReceived += RecivedError;
		extract_process.Start();
		extract_process.BeginOutputReadLine();
		extract_process.BeginErrorReadLine();
		var pname = extract_process.ProcessName;
		extract_process.WaitForExit();
		PrintLog($"{pname} elapsed {(DateTime.Now - stime7z).TotalSeconds}s");
		// 检查补丁内包含章节
		chapters = [];
		foreach (var file in DirAccess.GetFilesAt(GetGameDirPath(tempPath)))
		{
			if (System.Text.RegularExpressions.Regex.IsMatch(file, @"^chapter\d+\.xdelta$"))
			{
				chapters = chapters.Append(file.TrimSuffix(".xdelta").TrimPrefix("chapter")).ToArray();
			}
		}
		if (use_backup)
		{
			//恢复备份
			if (DirAccess.DirExistsAbsolute(path + "/backup"))
			{
				await RestoreData(path);
			}
		}
		else
		{
			await KillExternals();
			OS.MoveToTrash(path + "/backup");
			while (DirAccess.DirExistsAbsolute(path + "/backup"))
			{
				await Task.Delay(100);
			}
			PrintLog("Removed " + path + "/backup");
		}
		MoveAfterExtracted(GetGameDirPath(tempPath), "", path);
		await Task.Delay(100);
		OS.MoveToTrash(GetGameDirPath(tempPath));
		while (DirAccess.DirExistsAbsolute(GetGameDirPath(tempPath)))
		{
			await Task.Delay(100);
		}
		PrintLog(GetGameDirPath(tempPath));
		var ver = FileAccess.Open(path + "/backup/version", FileAccess.ModeFlags.Write);
		if (ver != null)
		{
			ver.StoreString(patchingdemo ? demopatchver : patchver);
			ver.Close();
		}
		//备份data
		if (FileAccess.FileExists(path + "/main.xdelta") && FileAccess.FileExists(path + "/" + dataname))
		{
			DirAccess.RenameAbsolute(path + "/" + dataname, path + "/backup/" + dataname);
			PrintLog("Renamed " + path + "/" + dataname + " to " + path + "/backup/" + dataname);
		}
		foreach (var chapter in chapters)
		{
			if (FileAccess.FileExists(path + "/chapter" + chapter + ".xdelta") && FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/" + dataname))
			{
				if (!DirAccess.DirExistsAbsolute(path + "/backup/chapter" + chapter + "_" + osname))
				{
					DirAccess.MakeDirAbsolute(path + "/backup/chapter" + chapter + "_" + osname);
				}
				DirAccess.RenameAbsolute(path + "/chapter" + chapter + "_" + osname + "/" + dataname, path + "/backup/chapter" + chapter + "_" + osname + "/" + dataname);
				PrintLog("Renamed " + path + "/chapter" + chapter + "_" + osname + "/" + dataname + " to " + path + "/backup/chapter" + chapter + "_" + osname + "/" + dataname);
			}
		}
		//Patch
		if (patchingdemo)
		{
			patched_count = chapters.Length;
		}
		System.EventHandler CreateFallbackHandler(string backupDataPath, string failedDataPath, string fallbackNamePrefix)
		{
			return (sender, e) =>
			{
				if (patch_failed) return;
				var originalProc = (Process)sender;
				if (originalProc.ExitCode != 0)
				{
					PrintLog($"{fallbackNamePrefix} xdelta3 failed. Attempting fallback...");
					string sha256Full = FileAccess.GetSha256(backupDataPath);
					if (sha256Full == "")
					{
						PrintLog($"No data.win found.");
						patch_failed = true;
						return;
					}
					string shaPrefix = sha256Full.Substring(0, 8);
					string fallbackPatchPath = $"{path}/{fallbackNamePrefix}{shaPrefix}.xdelta";

					if (FileAccess.FileExists(fallbackPatchPath))
					{
						PrintLog($"Fallback patch found: {fallbackPatchPath}");

						//清理
						if (FileAccess.FileExists(failedDataPath))
						{
							DirAccess.RemoveAbsolute(failedDataPath);
						}

						string fallbackArgs = $"-f -d -v -s \"{backupDataPath}\" \"{fallbackPatchPath}\" \"{failedDataPath}\"";
						PrintLog($"{xdelta3} {fallbackArgs}");

						var fallback_process = new Process();
						fallback_process.StartInfo = new ProcessStartInfo
						{
							FileName = xdelta3,
							Arguments = fallbackArgs,
							RedirectStandardOutput = true,
							RedirectStandardError = true
						};
						fallback_process.EnableRaisingEvents = true;
						fallback_process.OutputDataReceived += RecivedOutput;
						fallback_process.ErrorDataReceived += RecivedOutput;

						fallback_process.Exited += (f_sender, f_e) =>
						{
							if (fallback_process.ExitCode == 0)
							{
								// 标一下不是正版游戏，用来结尾提示
								used_fallback = true; 
								Patched(f_sender, f_e);
							}
							else
							{
								PrintLog($"Fallback patch still FAILED for {shaPrefix}.");
								patch_failed = true;
								return;
							}
						};

						fallback_process.Start();
						fallback_process.BeginOutputReadLine();
						fallback_process.BeginErrorReadLine();
					}
					else
					{
						PrintLog($"No fallback patch found for {shaPrefix}.");
						patch_failed = true;
						return;
					}
				}
				else
				{
					// 第一次没报错，直接成功
					Patched(sender, e);
				}
			};
		}
		patch_count_except_chapters = 0;
		if (FileAccess.FileExists(path + "/main.xdelta"))
		{
			patch_count_except_chapters += 1;
			string xdelta3Args = $"-f -d -v -s \"{path}/backup/{dataname}\" \"{path}/main.xdelta\" \"{path}/{dataname}\"";
			PrintLog("Patching main data");
			if (FileAccess.FileExists(path + "/" + dataname))
			{
				DirAccess.RemoveAbsolute(path + "/" + dataname);
				PrintLog("Removed " + path + "/" + dataname);
			}
			PrintLog($"{xdelta3} {xdelta3Args}");
			var xdelta3_process = new Process();
			starti = new ProcessStartInfo();
			starti.FileName = xdelta3;
			starti.Arguments = xdelta3Args;
			starti.RedirectStandardOutput = true;
			starti.RedirectStandardError = true;
			xdelta3_process.StartInfo = starti;
			xdelta3_process.EnableRaisingEvents = true;
			xdelta3_process.OutputDataReceived += RecivedOutput;
			xdelta3_process.ErrorDataReceived += RecivedOutput;//RecivedError; Xdelta3你神经病吧报错了吗你就返回Error
			string backupPath = $"{path}/backup/{dataname}";
			string destPath = $"{path}/{dataname}";
			xdelta3_process.Exited += CreateFallbackHandler(backupPath, destPath, "main_");
			xdelta3_process.Start();
			xdelta3_process.BeginOutputReadLine();
			xdelta3_process.BeginErrorReadLine();
			//xdelta3_process.WaitForExit();
		}
		if (!patchingdemo)
		{
			foreach (var chapter in chapters)
			{
				if (FileAccess.FileExists(path + "/chapter" + chapter + ".xdelta"))
				{
					string xdelta3Args = $"-f -d -v -s \"{path}/backup/chapter{chapter}_{osname}/{dataname}\" \"{path}/chapter{chapter}.xdelta\" \"{path}/chapter{chapter}_{osname}/{dataname}\"";
					PrintLog("Patching chapter" + chapter + " data");
					if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/" + dataname))
					{
						DirAccess.RemoveAbsolute(path + "/chapter" + chapter + "_" + osname + "/" + dataname);
						PrintLog("Removed " + path + "/chapter" + chapter + "_" + osname + "/" + dataname);
					}
					PrintLog($"{xdelta3} {xdelta3Args}");
					var xdelta3_process = new Process();
					starti = new ProcessStartInfo();
					starti.FileName = xdelta3;
					starti.Arguments = xdelta3Args;
					starti.RedirectStandardOutput = true;
					starti.RedirectStandardError = true;
					xdelta3_process.StartInfo = starti;
					xdelta3_process.EnableRaisingEvents = true;
					xdelta3_process.OutputDataReceived += RecivedOutput;
					xdelta3_process.ErrorDataReceived += RecivedOutput;//RecivedError; FUCK XDELTA3
					// ws3917 - 新增xdelta3的fall back，用于旧版游戏安装补丁
					string backupPath = $"{path}/backup/chapter{chapter}_{osname}/{dataname}";
					string destPath = $"{path}/chapter{chapter}_{osname}/{dataname}";
					xdelta3_process.Exited += CreateFallbackHandler(backupPath, destPath, $"chapter{chapter}_");
					xdelta3_process.Start();
					xdelta3_process.BeginOutputReadLine();
					xdelta3_process.BeginErrorReadLine();
					//xdelta3_process.WaitForExit();
				}
			}
		}
		while (patched_count < chapters.Length + patch_count_except_chapters && !patch_failed && (DateTime.Now - starttime).TotalSeconds < too_long_time)
		{
			await Task.Delay(100);
		}
		if (patched_count >= chapters.Length + patch_count_except_chapters)
		{
			CallDeferred("Ending");
			return;
		}
		if (patch_failed || ((!bypass_too_long) && ((DateTime.Now - starttime).TotalSeconds >= too_long_time)))
		{
			await KillExternals();
			if (patch_failed)
			{
				CallDeferred("PatchResultHandler", false, "locPatchFailedInvalidInput", (DateTime.Now - starttime).TotalSeconds, new Vector2I(640, 480));
			}
			else
			{
				CallDeferred("PatchResultHandler", false, "locPatchFailedTakingTooLong", too_long_time.ToString(), new Vector2I(480, 240));
			}
				
		}
	}
	internal static void MoveAfterExtracted(string dir, string relative_dir, string drsdir)
	{
		foreach (var di in DirAccess.GetDirectoriesAt(dir))
		{
			MoveAfterExtracted(dir + "/" + di, relative_dir + di + "/", drsdir);
		}
		foreach (var file in DirAccess.GetFilesAt(dir))
		{
			if (FileAccess.FileExists(drsdir + "/" + relative_dir + file))
			{
				if (!DirAccess.DirExistsAbsolute(drsdir + "/backup/" + relative_dir))
				{
					DirAccess.MakeDirRecursiveAbsolute(drsdir + "/backup/" + relative_dir);
				}
				DirAccess.RenameAbsolute(drsdir + "/" + relative_dir + file, drsdir + "/backup/" + relative_dir + file);
				PrintLog("Renamed " + drsdir + "/" + relative_dir + file + " to " + drsdir + "/backup/" + relative_dir + file);
			}
			DirAccess.RenameAbsolute(dir + "/" + file, drsdir + "/" + relative_dir + file);
			PrintLog("Renamed " + dir + "/" + file + " to " + drsdir + "/" + relative_dir + file);
		}
	}
	internal static async Task RestoreData(string path)
	{
		if (path != "")
		{
			if (FileAccess.FileExists(path + "/backup/version"))
			{
				DirAccess.RemoveAbsolute(path + "/backup/version");
			}
			if (DirAccess.DirExistsAbsolute(path + "/backup"))
			{
				RestoreFolder(path + "/backup", path);
			}
			await KillExternals();
			OS.MoveToTrash(path + "/backup");
			while (DirAccess.DirExistsAbsolute(path + "/backup"))
			{
				await Task.Delay(100);
			}
			PrintLog("Removed " + path + "/backup");
		}
	}
	internal static void RestoreFolder(string path, string target)
	{
		foreach (var file in DirAccess.GetFilesAt(path))
		{
			var result = DirAccess.RenameAbsolute(path + "/" + file, target + "/" + file);
			PrintLog("Renamed " + path + "/" + file + " to " + target + "/" + file);
			if (result != Error.Ok)
			{
				PrintLog("Error " + result.ToString() + " happened when renaming " + path + "/" + file + " to " + target + "/" + file, 2);
			}
		}
		foreach (var dir in DirAccess.GetDirectoriesAt(path))
		{
			RestoreFolder(path + "/" + dir, target + "/" + dir);
		}
	}
	public async void _on_unpatch_pressed()
	{
		nodeBtnPatch.Disabled = true;
		nodeBtnUnpatch.Disabled = true;
		nodeEditGamePath.Editable = false;
		nodeBtnBrowse.Disabled = true;

		var path = PathTrim(nodeEditGamePath.Text);
		DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Indeterminate);
		if (!DirAccess.DirExistsAbsolute(path + "/backup"))
		{
			nodeWindowPopupContent.Text = "locNoBakDetected";
		}
		else
		{
			await RestoreData(path);
			nodeWindowPopupContent.Text = "locUnpatched";
		}
		nodeWindowPopupContent.Set("theme_override_font_sizes/font_size", FontSize(27, windowScale));
		nodeWindowPopup.Size = new Vector2I(360, 120) * windowScale;
		nodeWindowPopup.Title = "locResult";
		nodeWindowPopup.Show();
		
		nodeBtnPatch.Disabled = false;
		nodeBtnUnpatch.Disabled = false;
		nodeEditGamePath.Editable = true;
		nodeBtnBrowse.Disabled = false;
	}

	public void _on_edit_game_path_text_changed(string path)
	{
		var pathvalid = PathCheck(path);
		nodeBtnPatch.Disabled = (!pathvalid) || (patchver == "locNotFound" && !patchingdemo) || (string.IsNullOrEmpty(demopatchver) && patchingdemo);
		nodeBtnUnpatch.Disabled = !pathvalid;
	}

	public void _on_triggerpathcheck_pressed()
	{
		var path = FindGamePath();
		if (path != "")
		{
			nodeEditGamePath.Text = path;
			var game_path = FileAccess.Open(game_path_file, FileAccess.ModeFlags.Write);
			if (game_path != null)
			{
				game_path.StoreString(path);
				game_path.Close();
			}
		}
	}

	public void _on_bypasshash_toggled(bool toggled)
	{
		bypass_hash = toggled;
	}

	public void _on_bypasstoolong_toggled(bool toggled)
	{
		bypass_too_long = toggled;
	}

	public void _on_bypasssamepath_toggled(bool toggled)
	{
		bypass_same_path = toggled;
	}

	public void _on_useinstalled7zip_toggled(bool toggled)
	{
		use_installed_7zip = toggled;
	}

	public void _on_useinstalledxdelta_toggled(bool toggled)
	{
		use_installed_xdelta = toggled;
	}

	public void _on_bypassrestorewhenfailed_toggled(bool toggled)
	{
		bypass_restore_when_failed = toggled;
	}

	public void _on_force_patch_item_selected(int selected)
	{
		force_patch = selected;
	}

	public void _on_overrideos_text_changed(string os)
	{
		if (string.IsNullOrEmpty(os))
		{
			os_name = OS.GetName();
		}
		else
		{
			os_name = os;
		}
	}

	public void _on_overridescale_value_changed(float value)
	{
		windowScale = Mathf.RoundToInt(value);
	}

	public void _on_githubapi_text_changed(string api)
	{
		if (string.IsNullOrEmpty(api))
		{
			api = "https://api.github.com";
		}
		github_api = api;
	}

	public void _on_xdelta_text_changed(string path)
	{
		xdelta_override = path;
	}

	public void _on_7z_text_changed(string path)
	{
		_7zip_override = path;
	}

	public void _on_copy_pressed()
	{
		DisplayServer.ClipboardSet(nodeWindowLogContent.Text);
	}

	internal void RecivedOutput(object process, DataReceivedEventArgs recived)
	{
		var result = recived.Data;
		if (process is Process processs)
		{
			try
			{
				result = $"{processs.Id} ({processs.ProcessName}): {recived.Data}";
			}
			catch (Exception e)
			{
				PrintLog("Exception happened when getting process ID & Name: " + e.ToString() + " (" + e.Message + ")", 2);
			}
		}
		PrintLog(result);
	}
	internal void RecivedError(object process, DataReceivedEventArgs recived)
	{
		var result = recived.Data;
		if (process is Process processs)
		{
			try
			{
				result = $"{processs.Id} ({processs.ProcessName}): {recived.Data}";
			}
			catch (Exception e)
			{
				PrintLog("Exception happened when getting process ID & Name: " + e.ToString() + " (" + e.Message + ")", 2);
			}
		}
		PrintLog(result, 2);
	}
	internal void Patched(object sender, EventArgs e)
	{
		if (sender is Process process)
		{
			try
			{
				PrintLog($"{process.ProcessName} elapsed {(process.ExitTime - process.StartTime).TotalSeconds}s");
			}
			catch (Exception ee)
			{
				PrintLog("Exception happened when getting process ID & Name: " + ee.ToString() + " (" + ee.Message + ")", 2);
			}
		}
		patched_count += 1;
		PrintLog($"patched_count = {patched_count - 1} + 1 = {patched_count}");
		// if (patched_count >= chapters.Length + 1)
		// {
		// 	CallDeferred("Ending");
		// }
	}
	internal async void PatchResultHandler(bool success, string information, string usedtime, Vector2I popup_size)
	{
		var path = PathTrim(nodeEditGamePath.Text);
		//cleanup
		foreach (var file in DirAccess.GetFilesAt(path))
		{
			if (file.EndsWith(".xdelta"))
			{
				DirAccess.RemoveAbsolute(path + "/" + file);
				PrintLog("Removed " + path + "/" + file);
			}
		}
		nodeWindowPopupContent.Text = TranslationServer.Translate(information).ToString().Replace("{USEDTIME}", usedtime);
		nodeWindowPopupContent.Set("theme_override_font_sizes/font_size", FontSize(27, windowScale));
		nodeWindowPopup.Size = popup_size * windowScale;
		nodeWindowPopup.Title = "locResult";
		if (success)
		{
			DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Noprogress);
			var gamepath = nodeEditGamePath.Text;
			//保存游戏路径
			var game_path = FileAccess.Open(game_path_file, FileAccess.ModeFlags.Write);
			if (game_path != null)
			{
				game_path.StoreString(gamepath);
				game_path.Close();
			}
		}
		else
		{
			DisplayServer.WindowSetTaskbarProgressState(DisplayServer.ProgressState.Error);
			//回退安装
			if (bypass_restore_when_failed)
			{
				PrintLog("Restoring backup bypassed.");
			}
			else
			{
				await RestoreData(path);
			}
		}
		output.Add("Patched at " + Time.GetDatetimeStringFromSystem(false, true) + ", " + Time.GetTimeZoneFromSystem()["name"]);
		var logtext = "";
		foreach (var i in output)
		{
			logtext += i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n") + "\n";
		}
		nodeWindowLogContent.Text = logtext;
		nodeWindowLogContent.Set("theme_override_font_sizes/font_size", FontSize(13, windowScale));
		nodeWindowLog.Size = new Vector2I(960, 600) * windowScale;
		var log = FileAccess.Open(GetGameDirPath("log.txt"), FileAccess.ModeFlags.Write);
		if (log != null)
		{
			log.StoreString(logtext);
			log.Close();
		}
		//等待0.1秒给godot.log更新时间
		await Task.Delay(100);
		log = FileAccess.Open("user://logs/godot.log", FileAccess.ModeFlags.Read);
		if (log != null)
		{
			logtext = log.GetAsText();
			log.Close();
			log = FileAccess.Open(GetGameDirPath("godot.log"), FileAccess.ModeFlags.Write);
			if (log != null)
			{
				log.StoreString(logtext);
				log.Close();
			}
		}
		nodeWindowLog.Show();
		nodeWindowPopup.Show();
		// 1225 check
		var datedict = Time.GetDateDictFromSystem();
		if (datedict["month"].AsString() == "12" && datedict["day"].AsString() == "25")
		{
			nodeWindowPopup1225.Show();
		}
		nodeBtnPatch.Disabled = false;
		nodeBtnUnpatch.Disabled = false;
		nodeEditGamePath.Editable = true;
		nodeBtnBrowse.Disabled = false;
	}
	internal void Ending()
	{
		var usedtime = DateTime.Now.Subtract(starttime).TotalSeconds.ToString();
		PrintLog("Total elapsed " + usedtime + "s");
		//end
		var logtext = "";
		foreach (var i in output)
		{
			logtext += i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n") + "\n";
		}
		if (logtext.Contains("XD3_INVALID_INPUT") && !used_fallback)
		{
			PatchResultHandler(false, "locPatchFailedInvalidInput", usedtime, new Vector2I(640, 480));
		}
		else if (logtext.Contains("cannot find the path specified") || logtext.Contains("找不到指定的路径") || logtext.Contains("找不到指定的路徑") || logtext.Contains("No such file or directory"))
		{
			PatchResultHandler(false, "locPatchFailedCantFind", usedtime, new Vector2I(640, 360));
		}
		else if (logtext.Contains("insufficient disk space") || logtext.Contains("磁盘空间不足") || logtext.Contains("磁碟空間不足"))
		{
			PatchResultHandler(false, "locPatchFailedDiskSpace", usedtime, new Vector2I(640, 180));
		}
		else if (logtext.Replace("\r", "").Replace("\n", "").Replace(" ", "") == "Extracting...")
		{
			PatchResultHandler(false, "locPatchFailedExternals" + (os_name == "macOS" ? "Mac" : ""), usedtime, new Vector2I(640, 360));
		}
		else if ((os_name == "macOS" || os_name == "Linux") && logtext.ToLower().Contains("(required by "))
		{
			PatchResultHandler(false, "locPatchFailedRequired", usedtime, new Vector2I(640, 360));
		}
		else if ((os_name == "macOS" || os_name == "Linux") && logtext.ToLower().Contains("permission denied"))
		{
			PatchResultHandler(false, "locPatchFailedDenied", usedtime, new Vector2I(640, 360));
		}
		else if (!logtext.Contains("xdelta3: finished") || !logtext.Contains("Everything is Ok") || (logtext.ToLower().Contains("error") && !logtext.Contains("wrong ELF class: ELFCLASS")))
		{
			PatchResultHandler(false, "locPatchFailed" + (os_name == "macOS" ? "Mac" : ""), usedtime, new Vector2I(480, 240));
		}
		else
		{
			string resultKey = used_fallback ? "locPatchedFallback" : "locPatched";
			PatchResultHandler(true, resultKey, usedtime, new Vector2I(480, 240));
		}
	}

	internal static string GetGameDirPath(string str = "")
	{
		if (OS.HasFeature("editor"))
		{
			return ProjectSettings.GlobalizePath("res://" + str);
		}
		else
		{
			return OS.GetExecutablePath().GetBaseDir() + "/" + str;
		}
	}

	//寻找游戏路径
	internal static string FindGamePath(string ver = "deltarune")
	{
		var is_demo = (ver == "deltarune_demo");
		var steampath = ""; // Variable only for windows
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			steampath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86) + "/Steam";
			default_paths[ver][os_name] = default_paths[ver][os_name].Replace("{STEAMPATH}", steampath);
		}
		var game_path = default_paths[ver][os_name];
		if (DirAccess.DirExistsAbsolute(game_path))
		{
			PrintLog("Found " + game_path);
		}
		else
		{
			game_path = "";
			//Windows读取注册表获取Steam目录
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				string[] paths = [default_paths[ver][os_name], default_paths["libraryfolders"][os_name]];
				var regkey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
				if (regkey != null)
				{
					steampath = regkey.GetValue("SteamPath").ToString().Replace("\\", "/");
					regkey.Close();
				}
				default_paths[ver][os_name] = paths[0].Replace("{STEAMPATH}", steampath);
				default_paths["libraryfolders"][os_name] = paths[1].Replace("{STEAMPATH}", steampath);
			}
			if (FileAccess.FileExists(default_paths["libraryfolders"][os_name]))
			{
				var lff = FileAccess.Open(default_paths["libraryfolders"][os_name], FileAccess.ModeFlags.Read);
				if (lff != null)
				{
					VObject vdfc = (VObject)VdfConvert.Deserialize(lff.GetAsText()).Value;
					lff.Close();
					foreach (VProperty i in vdfc.Properties())
					{
						VObject ii = (VObject)i.Value;
						VObject apps = (VObject)ii["apps"];
						if (apps.ContainsKey(is_demo ? "1690940" : "1671210"))
						{
							game_path = ii["path"].ToString().Replace("\\", "/") + "/steamapps/common/DELTARUNE" + (is_demo ? "demo" : "") + (os_name == "macOS" ? "/DELTARUNE.app/Contents/Resources" : "");
							if (DirAccess.DirExistsAbsolute(game_path))
							{
								PrintLog("Found " + game_path);
							}
							else
							{
								game_path = "";
							}
						}
					}
				}
			}
		}
		if ((!is_demo) && string.IsNullOrEmpty(game_path))
		{
			game_path = FindGamePath("deltarune_demo");
		}
		return game_path;
	}

	// 检查DR路径
	internal bool PathCheck(string path, bool patching = false)
	{
		//file check
		if (FileAccess.FileExists(path))
		{
			nodePathValid.Text = "locPatchInvalidFile";
			if (patching)
			{
				PatchResultHandler(false, "locPatchFailedPath", (DateTime.Now - starttime).TotalSeconds.ToString(), new Vector2I(640, 180));
			}
			return false;
		}
		//path check
		if (!DirAccess.DirExistsAbsolute(path))
		{
			nodePathValid.Text = "locPatchInvalidExists";
			if (patching)
			{
				PatchResultHandler(false, "locPatchFailedPath", (DateTime.Now - starttime).TotalSeconds.ToString(), new Vector2I(640, 180));
			}
			return false;
		}
		//Same path check
		if (!bypass_same_path)
		{
			var patcherpath = GetGameDirPath().Replace("\\","/").TrimSuffix("/");
			PrintLog($"Target Path: {path}\nPatcher Path: {patcherpath}");
			if (path == patcherpath)
			{
				nodePathValid.Text = "locPatchInvalidSame";
				if (patching)
				{
					PatchResultHandler(false, "locPatchFailedSamePath", (DateTime.Now - starttime).TotalSeconds.ToString(), new Vector2I(640, 180));
				}
				return false;
			}
		}
		//UT path check
		if (path.ToLower().TrimSuffix("/").TrimSuffix(".app").EndsWith("undertale") || FileAccess.FileExists(path + "/UNDERTALE.exe"))
		{
			nodePathValid.Text = "locPatchInvalidUT";
			if (patching)
			{
				PatchResultHandler(false, "locPatchFailedUT", (DateTime.Now - starttime).TotalSeconds.ToString(), new Vector2I(640, 270));
			}
			return false;
		}
		// path check
		var path_exists = true;
		if (FileAccess.FileExists(path + "/" + dataname))
		{
			PrintLog("Found " + path + "/" + dataname);
		}
		else
		{
			PrintLog("Unable to find " + path + "/" + dataname);
			path_exists = false;
		}
		if (path_exists)
		{
			patchingdemo = true;
			foreach (var folder in DirAccess.GetDirectoriesAt(path))
			{
				if (folder.StartsWith("chapter") && FileAccess.FileExists(path + "/" + folder + "/" + dataname))
				{
					PrintLog("Found " + path + "/" + folder);
					patchingdemo = false;
					break;
				}
			}
		}
		if (path_exists)
		{
			nodePathValid.Text = patchingdemo ? "locPatchValidDemo" : "locPatchValidFull";
			UpdatePathText(path);
			//保存游戏路径
			var game_path = FileAccess.Open(game_path_file, FileAccess.ModeFlags.Write);
			if (game_path != null)
			{
				game_path.StoreString(path);
				game_path.Close();
			}
		}
		else
		{
			nodePathValid.Text = "locPatchInvalid";
			if (patching)
			{
				PatchResultHandler(false, "locPatchFailedPath", (DateTime.Now - starttime).TotalSeconds.ToString(), new Vector2I(640, 180));
			}
		}
		nodeBtnRun.SetItemDisabled(3, (os_name != "Windows" && os_name != "macOS") || !path_exists);
		return path_exists;
	}
	// 更新路径文本
	internal void UpdatePathText(string gamepath, bool trimming = true)
	{
		if (gamepath != "" && FileAccess.FileExists(gamepath + "/backup/version"))
		{
			var ver = FileAccess.Open(gamepath + "/backup/version", FileAccess.ModeFlags.Read);
			if (ver != null)
			{
				var vertxt = ver.GetAsText();
				// 1225 check
				var datedict = Time.GetDateDictFromSystem();
				if (vertxt == "1225" && datedict["month"].AsString() == "12" && datedict["day"].AsString() == "25")
				{
					vertxt = "■■■■";
				}
				var versiontxt = nodeTextPatchVersion.Text;
				versiontxt = (trimming ? versiontxt.Substring(0, versiontxt.LastIndexOf("\n")) : versiontxt) + "\n" + TranslationServer.Translate("locInstalledVer") + vertxt;
				nodeTextPatchVersion.Text = versiontxt;
				ver.Close();
			}
		}
	}
	// 杀死外部程序
	internal static async Task KillExternals()
	{
		Godot.Collections.Array<string> externals = [];
		foreach (var programs in available_externals.Values)
		{
			externals += programs;
		}
		foreach (var external in externals)
		{
			if (os_name == "Windows")
			{
				OS.Execute("taskkill", ["/f", "/im", external + ".exe"]);
			}
			else
			{
				OS.Execute("killall", [external]);
			}
			while (Process.GetProcessesByName(external + ((os_name == "Windows") ? ".exe" : "")).Length > 0)
			{
				await Task.Delay(100); 
			}
		}
	}
	// 输出日志
	internal static void PrintLog(string text, int type = 0)
	{
		var prefix = $"[{Time.GetDatetimeStringFromSystem(false, true)}";
		switch (type)
		{
			default:
				prefix += "/Info] ";
				GD.Print(prefix + text);
				break;
			case 1:
				prefix += "/Warning] ";
				GD.PushWarning(prefix + text);
				break;
			case 2:
				prefix += "/Exception] ";
				GD.PushError(prefix + text);
				break;
		}
		output.Add(prefix + text);
	}
	public static void PrintLog(params object[] what)
	{
		StringBuilder stringBuilder = new();
		for (int i = 0; i < what.Length; i++)
		{
			stringBuilder.Append(what[i]?.ToString() ?? "");
		}

		PrintLog(stringBuilder.ToString());
	}

	//这个奇怪的DTM字体 最小是13 然后是13+14=27
	//从27开始公差却是13 14显示会出问题
	internal static int FontSize(int size, int multiply)
	{
		if (multiply == 1)
		{
			return size;
		}
		if (size % 13 == 0)
		{
			return size * multiply + 1;
		}
		if ((size - 1) % 13 == 0)
		{
			return (size - 1) * multiply;
		}
		return size * multiply;
	}
	//退出
	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			//保存游戏路径
			var path = nodeEditGamePath.Text;
			if (path != "")
			{
				var game_path = FileAccess.Open(game_path_file, FileAccess.ModeFlags.Write);
				if (game_path != null)
				{
					game_path.StoreString(path);
					game_path.Close();
				}
			}
			//Dispose掉文件流
			if (fileStream != null)
			{
				fileStream.Dispose();
				fileStream = null;
			}
			//删除未清理的下载缓存
			foreach (var file in DirAccess.GetFilesAt(GetGameDirPath()))
			{
				if (file.StartsWith("_downloadingtemp_"))
				{
					DirAccess.RemoveAbsolute(GetGameDirPath(file));
					PrintLog("Removed " + GetGameDirPath(file));
				}
			}
		}
	}
}
