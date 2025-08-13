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

public partial class Main : Control
{
	[Export]
	AnimationPlayer nodeBgAnim = null!;
	[Export]
	Button nodeBtnInfo = null!;
	[Export]
	OptionButton nodeComboLanguage = null!;
	[Export]
	Label nodeTextPatcherVersion = null!;
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
	Button nodeBtnBrowse = null!;
	[Export]
	Button nodeBtnPatch = null!;
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
	Window nodeWindowPatch = null!;
	[Export]
	Label nodeWindowPatchContent = null!;


	static readonly string[] chapters = ["1", "2", "3", "4"];
	static string xdelta3 = GetGameDirPath("externals/xdelta3/xdelta3");
	static string _7zip = GetGameDirPath("externals/7zip/7z");
	static readonly Godot.Collections.Dictionary<string,Godot.Collections.Array<string>> available_externals = new()
	{
		{"7z", ["7z", "7zip", "7-zip", "7zr", "7za", "7zz"]},
		{"xdelta", ["xdelta", "xdelta3"]}
	};
	static readonly Godot.Collections.Dictionary<string,string> externals_hash = new()
	{
		{GetGameDirPath("externals/7zip/7z"), "9a556170350dafb60a97348b86a94b087d97fd36007760691576cac0d88b132b"},
		{GetGameDirPath("externals/7zip/7z.exe"), "d2c0045523cf053a6b43f9315e9672fc2535f06aeadd4ffa53c729cd8b2b6dfe"},
		{GetGameDirPath("externals/7zip/7z_mac"), "bd5765978a541323758d82ad1d30df76a2e3c86341f12d6b0524d837411e9b4a"},
		{GetGameDirPath("externals/xdelta3/xdelta3"), "709f63ebb9655dc3b5c84f17e11217494eb34cf00c009a026386e4c8617ea903"},
		{GetGameDirPath("externals/xdelta3/xdelta3.exe"), "6855c01cf4a1662ba421e6f95370cf9afa2b3ab6c148473c63efe60d634dfb9a"},
		{GetGameDirPath("externals/xdelta3/xdelta3_mac"), "714c1680b8fb80052e3851b9007d5d4b9ca0130579b0cdd2fd6135cce041ce6a"}
	};
	static readonly Godot.Collections.Dictionary<string,Godot.Collections.Dictionary<string,string>> default_paths = new()
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
		}
	};
	static string game_path_file = GetGameDirPath("game_path.txt");
	static string patchdir = GetGameDirPath("patch");
	static string patchver = "locNotFound";
	static Godot.Collections.Dictionary patcherreleases = new();
	static Godot.Collections.Dictionary patchreleases = new();
	static readonly string os_name = OS.GetName();
	//static readonly string os_arch = RuntimeInformation.ProcessArchitecture.ToString(); //未来可能会有用
	static readonly string osname = (os_name == "macOS" ? "mac" : "windows");
	static readonly string dataname = (os_name == "macOS" ? "game.ios" : "data.win");
	string[] locales;
	bool inited = false;
	System.IO.FileStream fileStream = null;
	Godot.Collections.Array output = [];
	int patched_count = 0;
	DateTime starttime = DateTime.MinValue;
	public override async void _Ready()
	{
		var window = GetWindow();
		var wid = window.GetWindowId();
		//首次初始化
		if (!inited)
		{
			nodeBgAnim.Play("bg_anim");
			nodeComboLanguage.Disabled = true;

			//修改窗口大小
			var screenId = window.CurrentScreen;
			var screenSize = DisplayServer.ScreenGetUsableRect(screenId);
			var windowDesignSize = new Vector2(640, 480) * 1.5f;

			int windowScale = (int)Mathf.Floor((screenSize.Size.Y-screenSize.Position.Y) / windowDesignSize.Y);
			if (windowScale > 1)
			{
				var windowNewSize = (Vector2I)((windowDesignSize * windowScale).Round());
				DisplayServer.WindowSetSize(windowNewSize, wid);
				// 居中窗口
				window.MoveToCenter();
			}
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
			if (os_name == "macOS")
			{
				foreach (var file in DirAccess.GetFilesAt(GetGameDirPath("../../")))
				{
					if (file.StartsWith("patch_"))
					{
						patchdir = GetGameDirPath(file);
						patchver = System.IO.Path.GetFileNameWithoutExtension(file).Split("_")[^1];
						GD.Print("Found patch file " + patchdir);
						break;
					}
				}
			}
			if (patchdir == GetGameDirPath("patch") || os_name != "macOS")
			{
				foreach (var file in DirAccess.GetFilesAt(GetGameDirPath()))
				{
					if (file.StartsWith("patch_"))
					{
						patchdir = GetGameDirPath(file);
						patchver = System.IO.Path.GetFileNameWithoutExtension(file).Split("_")[^1];
						GD.Print("Found patch file " + patchdir);
						break;
					}
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
						nodeWindowReadme.Title = file;
						nodeWindowReadme.Show();
						readme.Close();
					}
					break;
				}
			}
		}
		switch (TranslationServer.GetLocale())
		{
			default:
				DisplayServer.WindowSetTitle("DELTARUNE Chinese Patcher", wid);
				break;
			case "zh_CN":
				DisplayServer.WindowSetTitle("DELTARUNE 汉化安装器", wid);
				break;
			case "zh_TW":
				DisplayServer.WindowSetTitle("DELTARUNE 漢化安裝器", wid);
				break;
		}
		nodeBtnUpdatePatcher.Visible = false;
		nodeBtnUpdatePatcher.Disabled = false;
		//安装器版本号
		nodeTextPatcherVersion.Text = "v" + ProjectSettings.GetSetting("application/config/version").AsString();
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
			nodeComboLanguage.Set("popup/item_" + Array.IndexOf(locales, current).ToString() + "/text", TranslationServer.GetTranslationObject(current).GetMessage("locLanguageName"));
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
			//寻找游戏路径
			game_path = default_paths["deltarune"][os_name];
			if (DirAccess.DirExistsAbsolute(game_path))
			{
				GD.Print("Found " + game_path);
			}
			else
			{
				game_path = "";
				//Windows读取注册表获取Steam目录
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					string[] paths = [default_paths["deltarune"][os_name], default_paths["libraryfolders"][os_name]];
					var regkey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
					var steampath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86) + "/Steam";
					if (regkey != null)
					{
						steampath = regkey.GetValue("SteamPath").ToString().Replace("\\","/");
						regkey.Close();
					}
					default_paths["deltarune"][os_name] = paths[0].Replace("{STEAMPATH}", steampath);
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
							if (apps.ContainsKey("1671210"))
							{
								game_path = ii["path"].ToString().Replace("\\","/") + "/steamapps/common/DELTARUNE" + (os_name == "macOS" ? "/DELTARUNE.app/Contents/Resources" : "");
								if (DirAccess.DirExistsAbsolute(game_path))
								{
									GD.Print("Found " + game_path);
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
		}
		nodeEditGamePath.Text = game_path;
		//HttpClient
		var httpc = new System.Net.Http.HttpClient();
		httpc.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36");
		nodeBtnInfo.TooltipText = "locInfo";
		nodeTextPatchVersion.Text = TranslationServer.Translate("locLocalVer") + TranslationServer.Translate(patchver) + "\n" + TranslationServer.Translate("locLatestVer") + TranslationServer.Translate("locRequesting");
		//contributors
		var json = new Json();
		try
		{
			json.Parse(await httpc.GetStringAsync("https://api.github.com/repos/gm3dr/DeltaruneChinesePatcher/contributors"));
			var names = "";
			foreach (var contributor in json.Data.AsGodotArray<Godot.Collections.Dictionary<string,string>>())
			{
				names += contributor["login"] + ", ";
			}
			names = names.TrimSuffix(", ");
			if (names != "")
			{
				nodeBtnInfo.TooltipText = TranslationServer.Translate("locInfoContributors").ToString().Replace("{CONTRIBUTORS}",names);
			}
		}
		catch (Exception exc)
		{
			GD.PushError("Exception catched when requesting contributors: " + exc.ToString() + " (" + exc.Message + ")");
		}
		//补丁版本号
		json = new Json();
		try
		{
			if (!inited)
			{
				json.Parse(await httpc.GetStringAsync("https://api.github.com/repos/gm3dr/DeltaruneChinese/releases/latest"));
				patchreleases = json.Data.AsGodotDictionary();
			}
			nodeTextPatchVersion.Text = TranslationServer.Translate("locLocalVer") + TranslationServer.Translate(patchver) + "\n" + TranslationServer.Translate("locLatestVer") + patchreleases["tag_name"].AsString();
			var gamepath = nodeEditGamePath.Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
			if (gamepath != "" && FileAccess.FileExists(gamepath + "/backup/version"))
			{
				var ver = FileAccess.Open(gamepath + "/backup/version", FileAccess.ModeFlags.Read);
				if (ver != null)
				{
					nodeTextPatchVersion.Text += "\n" + TranslationServer.Translate("locInstalledVer") + ver.GetAsText();
					ver.Close();
				}
			}
			if (patchver != patchreleases["tag_name"].AsString())
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
							nodeWindowReadme.Title = "readme.txt";
							nodeWindowReadme.Show();
							readme.Close();
						}
						break;
					}
				}
			}
		}
		catch (HttpRequestException exc)
		{
			GD.PushError("Exception catched when requesting patch latest: " + exc.ToString() + " (" + exc.Message + ")");
			//nodeTextPatchVersion.Text = TranslationServer.Translate("locLocalVer") + TranslationServer.Translate(patchver) + "\n" + TranslationServer.Translate("locLatestVer") + TranslationServer.Translate("locTimeout").ToString().TrimPrefix(" ");
		}
		//安装器更新
		if (!OS.HasFeature("editor"))
		{
			json = new Json();
			try
			{
				if (!inited)
				{
					json.Parse(await httpc.GetStringAsync("https://api.github.com/repos/gm3dr/DeltaruneChinesePatcher/releases/latest"));
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
				GD.PushError("Exception catched when requesting patcher latest: " + exc.ToString() + " (" + exc.Message + ")");
			}
		}
		httpc.Dispose();

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
			GD.Print($"[{Time.GetDatetimeStringFromSystem(false, true)}] Language changed from {TranslationServer.GetLocale()} to {locales[selected]}.");
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
	}
	public void _on_window_close_requested()
	{
		nodeWindowLog.Hide();
	}
	public void _on_patch_pressed()
	{
		nodeBtnPatch.Disabled = true;
		nodeEditGamePath.Editable = false;
		nodeBtnBrowse.Disabled = true;
		var path = nodeEditGamePath.Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
		if (os_name == "macOS")
		{
			string EnsureTrailingSlash(string p) => p.EndsWith("/") ? p : p + "/";

			if (path.Substring(Math.Max(0, path.Length - 10)).Contains("DELTARUNE"))
			{
				path = EnsureTrailingSlash(path) + "DELTARUNE.app/Contents/Resources";
			}
			else if (path.Substring(Math.Max(0, path.Length - 4)).Contains(".app"))
			{
				path = EnsureTrailingSlash(path) + "Contents/Resources";
			}
			else if (path.Substring(Math.Max(0, path.Length - 9)).Contains("/Contents"))
			{
				path = EnsureTrailingSlash(path) + "Resources";
			}
		}
		bool found = FileAccess.FileExists(path + "/"+dataname+".bak") || DirAccess.DirExistsAbsolute(path + "/backup");
		foreach (var chapter in chapters)
		{
			if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak"))
			{
				GD.Print("Found: "+path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak");
				found = true;
				break;
			}
		}
		if (found)
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

	public void _on_rungame_pressed()
	{
		OS.ShellOpen("steam://run/1671210");
	}
	public void _on_popup_close_requested()
	{
		nodeWindowPopup.Hide();
	}
	public void _on_patch_close_requested()
	{
		nodeWindowPatch.Hide();
		nodeBtnPatch.Disabled = false;
		nodeEditGamePath.Editable = true;
		nodeBtnBrowse.Disabled = false;
	}
	public void _on_readme_close_requested()
	{
		nodeWindowReadme.Hide();
	}
	public void _on_update_pressed()
	{
		foreach (var asset in patcherreleases["assets"].AsGodotArray())
		{
			if (asset.AsGodotDictionary()["name"].AsString().ToLower().Contains(os_name.ToLower()))
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
				_on_update_patch_pressed();
				break;
			case 2:
				_on_update_patch_browser_pressed();
				nodeBtnUpdatePatch.Selected = 0;
				break;
		}
	}
	public async void _on_update_patch_pressed()
	{
		nodeBtnUpdatePatch.Disabled = true;
		nodeProgress.Visible = true;
		Godot.Collections.Array output = [];
		//删除旧patch
		foreach (var fff in DirAccess.GetFilesAt(GetGameDirPath()))
		{
			if (fff.StartsWith("patch_"))
			{
				DirAccess.RemoveAbsolute(GetGameDirPath(fff));
				GD.Print("Removed " + GetGameDirPath(fff));
				output.Add("Removed " + GetGameDirPath(fff));
			}
		}
		//下载patch
		var url = "";
		var file = "";
		var size = 0;
		foreach (var asset in patchreleases["assets"].AsGodotArray())
		{
			if (asset.AsGodotDictionary()["name"].AsString().ToLower().Contains(os_name.ToLower()))
			{
				url = /*(TranslationServer.GetLocale() == "zh_CN" ? "https://ghfast.top/" : "") + */asset.AsGodotDictionary()["browser_download_url"].AsString();
				file =  "_downloadingtemp_" + asset.AsGodotDictionary()["name"].AsString();
				size = asset.AsGodotDictionary()["size"].AsInt32();
				nodeProgress.MaxValue = size;
				break;
			}
		}
		if (url != "")
		{
			GD.Print("Downloading " + url + " to " + GetGameDirPath(file));
			output.Add("Downloading " + url + " to " + GetGameDirPath(file));
			try
			{
				using (var httpClient = new System.Net.Http.HttpClient())
				{
					using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
					{
						response.EnsureSuccessStatusCode();
						using var bodyStream = await response.Content.ReadAsStreamAsync();
						fileStream = new System.IO.FileStream(GetGameDirPath(file), System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
						var buffer = new byte[4096];
						double totalRead = 0;
						int bytesRead;
						while ((bytesRead = await bodyStream.ReadAsync(buffer)) > 0)
						{
							await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
							totalRead += bytesRead;
							if (size > 0)
							{
								nodeProgress.Value = totalRead;
								//nodeProgress.TooltipText = $"{Math.Round(totalRead/1024d/1024d, 2)} / {Math.Round(size/1024d/1024d, 2)} MiB";
								var progress = Math.Round(totalRead/1024d/1024d, 2).ToString();
								var sizee = Math.Round(size/1024d/1024d, 2).ToString();
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
								if (OS.IsStdOutVerbose())
								{
									GD.Print($"Downloaded: {totalRead} / {size}");
								}
							}
							if (totalRead >= size)
							{
								break;
							}
						}
					}
				}
			}
			catch (Exception exc)
			{
				nodeBtnUpdatePatch.Text = TranslationServer.Translate("locDownloadFailed") + exc.GetType().ToString();
				GD.PushError("Exception catched when updating patch: " + exc.ToString() + " ("+exc.Message+")");
				return;
			}
			fileStream.Dispose();
			fileStream = null;
			GD.Print($"Download {file} finished.");
			output.Add($"Download {file} finished.");
			DirAccess.RenameAbsolute(GetGameDirPath(file), GetGameDirPath(file.TrimPrefix("_downloadingtemp_")));
			GD.Print($"Renamed {file} to " + file.TrimPrefix("_downloadingtemp_") + ".");
			output.Add($"Renamed {file} to " + file.TrimPrefix("_downloadingtemp_") + ".");
			nodeBtnUpdatePatch.Text = "locWaiting4Restart";
			nodeBtnUpdatePatch.TooltipText = "locPleaseRestart";
		}
	}
	public void _on_update_patch_browser_pressed()
	{
		foreach (var fff in DirAccess.GetFilesAt(GetGameDirPath()))
		{
			if (fff.StartsWith("patch_"))
			{
				DirAccess.RemoveAbsolute(GetGameDirPath(fff));
				GD.Print("Removed " + GetGameDirPath(fff));
			}
		}
		foreach (var asset in patchreleases["assets"].AsGodotArray())
		{
			if (asset.AsGodotDictionary()["name"].AsString().ToLower().Contains(os_name.ToLower()))
			{
				OS.ShellOpen(asset.AsGodotDictionary()["browser_download_url"].AsString());
				break;
			}
		}
	}

	public async void Patch(bool use_backup = true)
	{
		starttime = DateTime.Now;
		patched_count = 0;
		nodeWindowPatch.Hide();
		nodeWindowLogContent.Text = "";
		var path = nodeEditGamePath.Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
		output = ["Patch at " + Time.GetDatetimeStringFromSystem(false, true) + ", " + Time.GetTimeZoneFromSystem()["name"]];
		//chmod加权限
		if (os_name == "macOS" || os_name == "Linux")
		{
			if (xdelta3.Contains("/"))
			{
				OS.Execute("chmod", ["+x", xdelta3]);
				GD.Print($"chmod +x {xdelta3}");
				output.Add($"chmod +x {xdelta3}");
			}
			if (_7zip.Contains("/"))
			{
				OS.Execute("chmod", ["+x", _7zip]);
				GD.Print($"chmod +x {_7zip}");
				output.Add($"chmod +x {_7zip}");
			}
		}
		//外部程序检查
		Godot.Collections.Array externalcheckoutput;
		int external_check_return;
		foreach (var __7z in available_externals["7z"])
		{
			externalcheckoutput = [];
			GD.Print("Checking " + __7z);
			output.Add("Checking " + __7z);
			if (os_name == "Windows")
			{
				external_check_return = OS.Execute("where", [__7z], externalcheckoutput);
			}
			else
			{
				external_check_return = OS.Execute("command", ["-v",__7z], externalcheckoutput);
			}
			GD.Print($"The result of \"{(os_name == "Windows" ? $"where {__7z}" : $"command -v {__7z}")}\": {external_check_return}");
			output.Add($"The result of \"{(os_name == "Windows" ? $"where {__7z}" : $"command -v {__7z}")}\": {external_check_return}");
			GD.Print(externalcheckoutput);
			output.Add(externalcheckoutput);
			if (external_check_return == 0)
			{
				_7zip = __7z;
				GD.Print("Found " + __7z);
				output.Add("Found " + __7z);
				break;
			}
		}
		foreach (var __xdelta in available_externals["xdelta"])
		{
			externalcheckoutput = [];
			GD.Print("Checking " + __xdelta);
			output.Add("Checking " + __xdelta);
			if (os_name == "Windows")
			{
				external_check_return = OS.Execute("where", [__xdelta], externalcheckoutput);
			}
			else
			{
				external_check_return = OS.Execute("command", ["-v",__xdelta], externalcheckoutput);
			}
			GD.Print($"The result of \"{(os_name == "Windows" ? $"where {__xdelta}" : $"command -v {__xdelta}")}\": {external_check_return}");
			output.Add($"The result of \"{(os_name == "Windows" ? $"where {__xdelta}" : $"command -v {__xdelta}")}\": {external_check_return}");
			GD.Print(externalcheckoutput);
			output.Add(externalcheckoutput);
			if (external_check_return == 0)
			{
				xdelta3 = __xdelta;
				GD.Print("Found " + __xdelta);
				output.Add("Found " + __xdelta);
				break;
			}
		}
		//existence check
		foreach (var pathhhhh in externals_hash.Keys)
		{
			if ((pathhhhh.Split("/").Last().Contains("7z") && _7zip == pathhhhh) || (pathhhhh.Split("/").Last().Contains("xdelta3") && xdelta3 == pathhhhh))
			{
				GD.Print($"Checking existence of {pathhhhh}");
				output.Add($"Checking existence of {pathhhhh}");
				if (!FileAccess.FileExists(pathhhhh))
				{
					GD.Print("Unable to find " + pathhhhh);
					output.Add("Unable to find " + pathhhhh);
					PatchResultHandler(false, "locPatchFailedNotExists", (DateTime.Now - starttime).TotalSeconds.ToString(), new Vector2I(640, 360));
					return;
				}
				GD.Print($"Found {pathhhhh}");
				output.Add($"Found {pathhhhh}");
			}
		}
		//hash check
		foreach (var pathhhhh in externals_hash.Keys)
		{
			if (((pathhhhh.Split("/").Last().Contains("7z") && _7zip == pathhhhh) || (pathhhhh.Split("/").Last().Contains("xdelta3") && xdelta3 == pathhhhh)) && FileAccess.FileExists(pathhhhh))
			{
				GD.Print($"Checking hash of {pathhhhh}");
				output.Add($"Checking hash of {pathhhhh}");
				if (FileAccess.GetSha256(pathhhhh) != externals_hash[pathhhhh])
				{
					GD.Print(FileAccess.GetSha256(pathhhhh) + " != " + externals_hash[pathhhhh]);
					output.Add(FileAccess.GetSha256(pathhhhh) + " != " + externals_hash[pathhhhh]);
					PatchResultHandler(false, "locPatchFailedSha256", (DateTime.Now - starttime).TotalSeconds.ToString(), new Vector2I(640, 360));
					return;
				}
				GD.Print("Hash matched: " + externals_hash[pathhhhh]);
				output.Add("Hash matched: " + externals_hash[pathhhhh]);
			}
		}
		GD.Print("Sha256 check all passed.");
		output.Add("Sha256 check all passed.");
		GD.Print("Extracting...");
		output.Add("Extracting...");
		if (use_backup)
		{
			//恢复备份
			if (DirAccess.DirExistsAbsolute(path + "/backup"))
			{
				output += RestoreData(path);
			}
			//兼容v2.1.0以前版本的bak备份
			if (FileAccess.FileExists(path + "/" + dataname + ".bak"))
			{
				DirAccess.RenameAbsolute(path + "/" + dataname + ".bak", path + "/" + dataname);
				GD.Print("Renamed " + path + "/" + dataname + ".bak to " + path + "/" + dataname);
				output.Add("Renamed " + path + "/" + dataname + ".bak to " + path + "/" + dataname);
			}
			foreach (var chapter in chapters)
			{
				if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/" + dataname + ".bak"))
				{
					DirAccess.RenameAbsolute(path + "/chapter" + chapter + "_" + osname + "/" + dataname + ".bak", path + "/chapter" + chapter + "_" + osname + "/" + dataname);
					GD.Print("Renamed " + path + "/chapter" + chapter + "_" + osname + "/" + dataname + ".bak to " + path + "/chapter" + chapter + "_" + osname + "/" + dataname);
					output.Add("Renamed " + path + "/chapter" + chapter + "_" + osname + "/" + dataname + ".bak to " + path + "/chapter" + chapter + "_" + osname + "/" + dataname);
				}
			}
		}
		else
		{
			OS.MoveToTrash(path + "/backup");
			GD.Print("Removed " + path + "/backup");
			output.Add("Removed " + path + "/backup");
		}
		//解压
		string tempPath = "ExtractTemp/";
		string extractArgs = $"x \"{patchdir}\" -o\"" + GetGameDirPath(tempPath) + "\" -aoa -y";
		GD.Print($"{_7zip} {extractArgs}");
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
		GD.Print($"{pname} elapsed {(DateTime.Now - stime7z).TotalSeconds}s");
		output.Add($"{pname} elapsed {(DateTime.Now - stime7z).TotalSeconds}s");
		output += MoveAfterExtracted(GetGameDirPath(tempPath), "", path);
		OS.MoveToTrash(GetGameDirPath(tempPath));
		var ver = FileAccess.Open(path + "/backup/version", FileAccess.ModeFlags.Write);
		if (ver != null)
		{
			ver.StoreString(patchver);
			ver.Close();
		}
		//备份data
		if (FileAccess.FileExists(path + "/main.xdelta") && FileAccess.FileExists(path + "/" + dataname))
		{
			DirAccess.RenameAbsolute(path + "/" + dataname, path + "/backup/" + dataname);
			GD.Print("Renamed " + path + "/" + dataname + " to " + path + "/backup/" + dataname);
			output.Add("Renamed " + path + "/" + dataname + " to " + path + "/backup/" + dataname);
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
				GD.Print("Renamed " + path + "/chapter" + chapter + "_" + osname + "/" + dataname + " to " + path + "/backup/chapter" + chapter + "_" + osname + "/" + dataname);
				output.Add("Renamed " + path + "/chapter" + chapter + "_" + osname + "/" + dataname + " to " + path + "/backup/chapter" + chapter + "_" + osname + "/" + dataname);
			}
		}
		//Patch
		if (FileAccess.FileExists(path + "/main.xdelta"))
		{
			GD.Print("Patching main data");
			output.Add("Patching main data");
			if (FileAccess.FileExists(path + "/" + dataname))
			{
				DirAccess.RemoveAbsolute(path + "/" + dataname);
				GD.Print("Removed " + path + "/" + dataname);
				output.Add("Removed " + path + "/" + dataname);
			}
			GD.Print($"{xdelta3} -f -d -v -s \"{path}/backup/{dataname}\" \"{path}/main.xdelta\" \"{path}/{dataname}\"");
			var xdelta3_process = new Process();
			starti = new ProcessStartInfo();
			starti.FileName = xdelta3;
			starti.Arguments = $"-f -d -v -s \"{path}/backup/{dataname}\" \"{path}/main.xdelta\" \"{path}/{dataname}\"";
			starti.RedirectStandardOutput = true;
			starti.RedirectStandardError = true;
			xdelta3_process.StartInfo = starti;
			xdelta3_process.EnableRaisingEvents = true;
			xdelta3_process.OutputDataReceived += RecivedOutput;
			xdelta3_process.ErrorDataReceived += RecivedOutput;//RecivedError; Xdelta3你神经病吧报错了吗你就返回Error
			xdelta3_process.Exited += Patched;
			xdelta3_process.Start();
			xdelta3_process.BeginOutputReadLine();
			xdelta3_process.BeginErrorReadLine();
			//xdelta3_process.WaitForExit();
		}
		foreach (var chapter in chapters)
		{
			if (FileAccess.FileExists(path + "/chapter" + chapter + ".xdelta"))
			{
				GD.Print("Patching chapter" + chapter + " data");
				output.Add("Patching chapter" + chapter + " data");
				if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/" + dataname))
				{
					DirAccess.RemoveAbsolute(path + "/chapter" + chapter + "_" + osname + "/" + dataname);
					GD.Print("Removed " + path + "/chapter" + chapter + "_" + osname + "/" + dataname);
					output.Add("Removed " + path + "/chapter" + chapter + "_" + osname + "/" + dataname);
				}
				GD.Print($"{xdelta3} -f -d -v -s \"{path}/backup/chapter{chapter}_{osname}/{dataname}\" \"{path}/chapter{chapter}.xdelta\" \"{path}/chapter{chapter}_{osname}/{dataname}\"");
				var xdelta3_process = new Process();
				starti = new ProcessStartInfo();
				starti.FileName = xdelta3;
				starti.Arguments = $"-f -d -v -s \"{path}/backup/chapter{chapter}_{osname}/{dataname}\" \"{path}/chapter{chapter}.xdelta\" \"{path}/chapter{chapter}_{osname}/{dataname}\"";
				starti.RedirectStandardOutput = true;
				starti.RedirectStandardError = true;
				xdelta3_process.StartInfo = starti;
				xdelta3_process.EnableRaisingEvents = true;
				xdelta3_process.OutputDataReceived += RecivedOutput;
				xdelta3_process.ErrorDataReceived += RecivedOutput;//RecivedError; FUCK XDELTA3
				xdelta3_process.Exited += Patched;
				xdelta3_process.Start();
				xdelta3_process.BeginOutputReadLine();
				xdelta3_process.BeginErrorReadLine();
				//xdelta3_process.WaitForExit();
			}
		}
		while (patched_count < chapters.Length + 1 && (DateTime.Now - starttime).TotalSeconds < 30)
		{
			await Task.Delay(100);
		}
		if (patched_count >= chapters.Length + 1)
		{
			CallDeferred("Ending");
			return;
		}
		if ((DateTime.Now - starttime).TotalSeconds >= 30)
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
					OS.Execute("taskkill", ["/f","/im", external + ".exe"]);
				}
				else
				{
					OS.Execute("killall", [external]);
				}
			}
			CallDeferred("PatchResultHandler", false, "locPatchFailedTakingTooLong", "30", new Vector2I(480, 240));
		}
	}
	internal static Godot.Collections.Array MoveAfterExtracted(string dir, string relative_dir, string drsdir)
	{
		Godot.Collections.Array output = [];
		foreach (var di in DirAccess.GetDirectoriesAt(dir))
		{
			output += MoveAfterExtracted(dir + "/" + di, relative_dir + di + "/", drsdir);
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
				GD.Print("Renamed " + drsdir + "/" + relative_dir + file + " to " + drsdir + "/backup/" + relative_dir + file);
				output.Add("Renamed " + drsdir + "/" + relative_dir + file + " to " + drsdir + "/backup/" + relative_dir + file);
			}
			DirAccess.RenameAbsolute(dir + "/" + file, drsdir + "/" + relative_dir + file);
			GD.Print("Renamed " + dir + "/" + file + " to " + drsdir + "/" + relative_dir + file);
			output.Add("Renamed " + dir + "/" + file + " to " + drsdir + "/" + relative_dir + file);
		}
		return output;
	}
	internal static Godot.Collections.Array RestoreData(string path)
	{
		Godot.Collections.Array output = [];
		if (path != "")
		{
			if (FileAccess.FileExists(path + "/backup/version"))
			{
				DirAccess.RemoveAbsolute(path + "/backup/version");
			}
			if (DirAccess.DirExistsAbsolute(path + "/backup"))
			{
				output += RestoreFolder(path + "/backup" , path);
			}
			OS.MoveToTrash(path + "/backup");
			GD.Print("Removed " + path + "/backup");
			output.Add("Removed " + path + "/backup");
		}
		return output;
	}
	internal static Godot.Collections.Array RestoreFolder(string path, string target)
	{
		Godot.Collections.Array output = [];
		foreach (var file in DirAccess.GetFilesAt(path))
		{
			var result = DirAccess.RenameAbsolute(path + "/" + file, target + "/" + file);
			GD.Print("Renamed " + path + "/" + file + " to " + target + "/" + file);
			output.Add("Renamed " + path + "/" + file + " to " + target + "/" + file);
			if (result != Error.Ok)
			{
				GD.PushError("Error " + result.ToString() + " happened when renaming " + path + "/" + file + " to " + target + "/" + file);
				output.Add("Error " + result.ToString() + " happened when renaming " + path + "/" + file + " to " + target + "/" + file);
			}
		}
		foreach (var dir in DirAccess.GetDirectoriesAt(path))
		{
			output += RestoreFolder(path + "/" + dir, target + "/" + dir);
		}
		return output;
	}
	public void _on_unpatch_pressed()
	{
		var path = nodeEditGamePath.Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
		if (!DirAccess.DirExistsAbsolute(path + "/backup"))
		{
			nodeWindowPopupContent.Text = "locNoBakDetected";
			nodeWindowPopup.Size = new Vector2I(360,120);
			nodeWindowPopup.Show();
			return;
		}
		bool found = FileAccess.FileExists(path + "/"+dataname+".bak");
		foreach (var chapter in chapters)
		{
			if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak"))
			{
				GD.Print("Found: "+path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak");
				found = true;
				break;
			}
		}
		if (found)
		{
			nodeWindowPopupContent.Text = "locOldBakDetected";
			nodeWindowPopup.Size = new Vector2I(360,120);
			nodeWindowPopup.Show();
			return;
		}
		RestoreData(path);
		nodeWindowPopupContent.Text = "locUnpatched";
		nodeWindowPopup.Size = new Vector2I(360,120);
		nodeWindowPopup.Show();
	}

	public void _on_edit_game_path_text_changed(string path)
	{
		nodeBtnPatch.Disabled = (path == "" || patchver == "locNotFound");
		nodeBtnUnpatch.Disabled = (path == "");
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
				GD.PushError("Error happened when getting process ID & Name: " + e.ToString() + " (" + e.Message + ")");
			}
		}
		GD.Print(result);
		output.Add(result);
	}
	internal void RecivedError(object process, DataReceivedEventArgs recived)
	{
		var result = recived.Data;
		if (process is Process processs)
		{
			result = $"{processs.Id} ({processs.ProcessName}): {recived.Data}";
		}
		GD.PushError(result);
		output.Add(result);
	}
	internal void Patched(object sender, EventArgs e)
	{
		if (sender is Process process)
		{
			try
			{
				GD.Print($"{process.ProcessName} elapsed {(process.ExitTime - process.StartTime).TotalSeconds}s");
				output.Add($"{process.ProcessName} elapsed {(process.ExitTime - process.StartTime).TotalSeconds}s");
			}
			catch (Exception ee)
			{
				GD.PushError("Error happened when getting process ID & Name: " + ee.ToString() + " (" + ee.Message + ")");
			}
		}
		patched_count += 1;
		GD.Print($"patched_count = {patched_count - 1} + 1 = {patched_count}");
		output.Add($"patched_count = {patched_count - 1} + 1 = {patched_count}");
		// if (patched_count >= chapters.Length + 1)
		// {
		// 	CallDeferred("Ending");
		// }
	}
	internal async void PatchResultHandler(bool success, string information, string usedtime, Vector2I popup_size)
	{
		var path = nodeEditGamePath.Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
		nodeWindowPopupContent.Text = TranslationServer.Translate(information).ToString().Replace("{USEDTIME}",usedtime);
		nodeWindowPopup.Size = popup_size;
		if (success)
		{
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
			//回退安装
			output += RestoreData(path);
		}
		output.Add("Patched at " + Time.GetDatetimeStringFromSystem(false, true) + ", " + Time.GetTimeZoneFromSystem()["name"]);
		var logtext = "";
		foreach (var i in output)
		{
			logtext += i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n") + "\n";
		}
		nodeWindowLogContent.Text = logtext;
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
		nodeBtnPatch.Disabled = false;
		nodeEditGamePath.Editable = true;
		nodeBtnBrowse.Disabled = false;
	}
	internal void Ending()
	{
		var path = nodeEditGamePath.Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
		//cleanup
		foreach (var file in DirAccess.GetFilesAt(path))
		{
			if (file.EndsWith(".xdelta"))
			{
				DirAccess.RemoveAbsolute(path + "/" + file);
				GD.Print("Removed " + path + "/" + file);
				output.Add("Removed " + path + "/" + file);
			}
		}
		var usedtime = DateTime.Now.Subtract(starttime).TotalSeconds.ToString();
		output.Add("Total elapsed " + usedtime + "s");
		//end
		var logtext = "";
		foreach (var i in output)
		{
			logtext += i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n") + "\n";
		}
		if (logtext.Contains("checksum mismatch"))
		{
			PatchResultHandler(false, "locPatchFailedChecksum", usedtime, new Vector2I(640, 480));
		}
		else if (logtext.Contains("cannot find the path specified"))
		{
			PatchResultHandler(false, "locPatchFailedCantFind", usedtime, new Vector2I(640, 360));
		}
		else if (logtext.Replace("\r","").Replace("\n","").Replace(" ","") == "Extracting...")
		{
			PatchResultHandler(false, "locPatchFailedExternals", usedtime, new Vector2I(640, 360));
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
			PatchResultHandler(false, "locPatchFailed", usedtime, new Vector2I(480, 240));
		}
		else
		{
			PatchResultHandler(true, "locPatched", usedtime, new Vector2I(480, 240));
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
	//退出
	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			//保存游戏路径
			var path = nodeEditGamePath.Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
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
					GD.Print("Removed " + GetGameDirPath(file));
				}
			}
		}
	}
}
