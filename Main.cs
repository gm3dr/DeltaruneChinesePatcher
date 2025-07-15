using Godot;
using System;
using System.Linq;
using System.Net.Http;

public partial class Main : Control
{
	static readonly string[] chapters = ["1", "2", "3", "4"];
	string[] locales;
	bool inited = false;
	static string xdelta3 = GetGameDirPath("externals/xdelta3/xdelta3");
	static string _7zip = GetGameDirPath("externals/7zip/7z");
	static readonly Godot.Collections.Dictionary<string,string> externals_hash = new()
	{
		{GetGameDirPath("externals/7zip/7z"), "9a556170350dafb60a97348b86a94b087d97fd36007760691576cac0d88b132b"},
		{GetGameDirPath("externals/7zip/7z.exe"), "d2c0045523cf053a6b43f9315e9672fc2535f06aeadd4ffa53c729cd8b2b6dfe"},
		{GetGameDirPath("externals/7zip/7z_mac"), "bd5765978a541323758d82ad1d30df76a2e3c86341f12d6b0524d837411e9b4a"},
		{GetGameDirPath("externals/xdelta3/xdelta3"), "709f63ebb9655dc3b5c84f17e11217494eb34cf00c009a026386e4c8617ea903"},
		{GetGameDirPath("externals/xdelta3/xdelta3.exe"), "6855c01cf4a1662ba421e6f95370cf9afa2b3ab6c148473c63efe60d634dfb9a"},
		{GetGameDirPath("externals/xdelta3/xdelta3_mac"), "714c1680b8fb80052e3851b9007d5d4b9ca0130579b0cdd2fd6135cce041ce6a"}
	};
	static string game_path_file = GetGameDirPath("game_path.txt");
	static string patchdir = GetGameDirPath("patch");
	static string patchver = "locNotFound";
	static Godot.Collections.Dictionary patcherreleases = new();
	static Godot.Collections.Dictionary patchreleases = new();
	static readonly string osname = (OS.GetName() == "macOS" ? "mac" : "windows");
	static readonly string dataname = (OS.GetName() == "macOS" ? "game.ios" : "data.win");
	System.IO.FileStream fileStream = null;
	public override async void _Ready()
	{
		//首次初始化
		if (!inited)
		{
			GetNode<OptionButton>("OptionButton").Disabled = true;
			GetNode<AnimationPlayer>("AnimationPlayer").Play("bg_anim");
			//修改窗口大小
			var wid = GetWindow().CurrentScreen;
			var window_multiply = Vector2.One;
			var size = DisplayServer.ScreenGetSize(wid) / (Vector2.One * DisplayServer.ScreenGetScale(wid));
			if (size > new Vector2I(3840,2400))
			{
				window_multiply = Vector2.One * 4;
			}
			else if (size > new Vector2I(2560,1600))
			{
				window_multiply = Vector2.One * 3;
			}
			else if (size > new Vector2I(1920,1200))
			{
				window_multiply = Vector2.One * 2;
			}
			if (window_multiply != Vector2.One)
			{
				DisplayServer.WindowSetSize((Vector2I)(new Vector2I(640, 480) * window_multiply));
			}
			//最大帧率
			Engine.MaxFps = Mathf.RoundToInt(DisplayServer.ScreenGetRefreshRate(GetWindow().CurrentScreen));
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
					patchdir = GetGameDirPath(file);
					patchver = file.Substring(0, file.LastIndexOf(".")).Split("_")[^1];
					GD.Print("Found patch file " + patchdir);
				}
			}
			//自动显示readme
			foreach (var file in DirAccess.GetFilesAt(GetGameDirPath("")))
			{
				if (file.ToLower().Contains("readme"))
				{
					GetNode<Label>("Readme/ScrollContainer/Label").Text = FileAccess.Open(GetGameDirPath(file), FileAccess.ModeFlags.Read).GetAsText();
					GetNode<Window>("Readme").Title = file;
					GetNode<Window>("Readme").Show();
					break;
				}
			}
		}
		//安装器版本号
		GetNode<Label>("HBoxContainer/VBoxContainer/Label").Text = "v" + ProjectSettings.GetSetting("application/config/version").AsString();
		//系统特供目录
		if (OS.GetName() == "Windows")
		{
			xdelta3 = GetGameDirPath("externals/xdelta3/xdelta3.exe");
			_7zip = GetGameDirPath("externals/7zip/7z.exe");
		}
		else if (OS.GetName() == "macOS")
		{
			xdelta3 = GetGameDirPath("externals/xdelta3/xdelta3_mac");
			_7zip = GetGameDirPath("externals/7zip/7z_mac");
		}
		//语言选项
		var node = GetNode<OptionButton>("OptionButton");
		locales = TranslationServer.GetLoadedLocales();
		node.ItemCount = locales.Length;
		foreach (var current in locales)
		{
			node.Set("popup/item_" + Array.IndexOf(locales, current).ToString() + "/text", TranslationServer.GetTranslationObject(current).GetMessage("locLanguageName"));
		}
		node.Selected = Array.IndexOf(locales.ToArray(), locales.Contains(TranslationServer.GetLocale()) ? TranslationServer.GetLocale() : TranslationServer.GetLocale().Left(2));
		//读取之前的游戏路径
		var game_path = FileAccess.Open(game_path_file, FileAccess.ModeFlags.Read);
		if (game_path != null)
		{
			GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text = game_path.GetAsText();
			game_path.Close();
		}
		//HttpClient
		var httpc = new System.Net.Http.HttpClient();
		httpc.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36");
		//contributors
		GetNode<Button>("Info").TooltipText = "locInfo";
		var json = new Json();
		json.Parse(await httpc.GetStringAsync("https://api.github.com/repos/gm3dr/DeltaruneChinesePatcher/contributors"));
		var names = "";
		foreach (var contributor in json.Data.AsGodotArray<Godot.Collections.Dictionary<string,string>>())
		{
			names += contributor["login"] + ", ";
		}
		names = names.TrimSuffix(", ");
		if (names != "")
		{
			GetNode<Button>("Info").TooltipText = TranslationServer.Translate("locInfoContributors").ToString().Replace("{CONTRIBUTORS}",names);
		}
		//补丁版本号
		GetNode<Label>("CenterContainer/VBoxContainer/Label").Text = TranslationServer.Translate("locLocalVer") + TranslationServer.Translate(patchver) + "\n" + TranslationServer.Translate("locLatestVer") + TranslationServer.Translate("locRequesting");
		json = new Json();
		try
		{
			if (!inited)
			{
				json.Parse(await httpc.GetStringAsync("https://api.github.com/repos/gm3dr/DeltaruneChinese/releases/latest"));
				patchreleases = json.Data.AsGodotDictionary();
			}
			GetNode<Label>("CenterContainer/VBoxContainer/Label").Text = TranslationServer.Translate("locLocalVer") + TranslationServer.Translate(patchver) + "\n" + TranslationServer.Translate("locLatestVer") + patchreleases["tag_name"].AsString();
			if (patchver != patchreleases["tag_name"].AsString())
			{
				GetNode<HBoxContainer>("CenterContainer/VBoxContainer/HBoxContainer3").Visible = true;
			}
			if (GetNode<Label>("Readme/ScrollContainer/Label").Text != "")
			{
				foreach (var asset in patchreleases["assets"].AsGodotArray())
				{
					if (asset.AsGodotDictionary()["name"].AsString().ToLower().Contains("readme"))
					{
						var text = await httpc.GetStringAsync(asset.AsGodotDictionary()["browser_download_url"].AsString());
						FileAccess.Open(GetGameDirPath("readme.txt"), FileAccess.ModeFlags.Write).StoreString(text);
						GetNode<Label>("Readme/ScrollContainer/Label").Text = text;
						GetNode<Window>("Readme").Title = "readme.txt";
						GetNode<Window>("Readme").Show();
						break;
					}
				}
			}
		}
		catch (HttpRequestException exc)
		{
			GD.PushError("Exception catched when requesting patch latest: " + exc.ToString() + " (" + exc.Message + ")");
			//GetNode<Label>("CenterContainer/VBoxContainer/Label").Text = TranslationServer.Translate("locLocalVer") + TranslationServer.Translate(patchver) + "\n" + TranslationServer.Translate("locLatestVer") + TranslationServer.Translate("locTimeout").ToString().TrimPrefix(" ");
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
					GetNode<Button>("HBoxContainer/Update").Text = TranslationServer.Translate("locUpdate").ToString().Replace("{VER}", patcherreleases["tag_name"].AsString());
					GetNode<Button>("HBoxContainer/Update").Visible = true;
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
			GetNode<OptionButton>("OptionButton").Disabled = false;
			inited = true;
		}
	}

	public override void _Process(double delta)
	{
		var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
		GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Patch").Disabled = (path == "" || patchver == "locNotFound");
		GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Unpatch").Disabled = (path == "");
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
		GetNode<FileDialog>("FileDialog").Show();
	}
	public void _on_file_dialog_dir_selected(string dir)
	{
		GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text = dir;
	}
	public void _on_window_close_requested()
	{
		GetNode<Window>("Log").Hide();
	}
	public void _on_patch_pressed()
	{
		GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Patch").Disabled = true;
		var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
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
			GetNode<Window>("Patch").Show();
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
	public void _on_popup_close_requested()
	{
		GetNode<Window>("Popup").Hide();
	}
	public void _on_patch_close_requested()
	{
		GetNode<Window>("Patch").Hide();
		GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Patch").Disabled = false;
	}
	public void _on_readme_close_requested()
	{
		GetNode<Window>("Readme").Hide();
	}
	public async void _on_update_pressed()
	{
		var updatebutton = GetNode<Button>("HBoxContainer/Update");
		updatebutton.Disabled = true;
		var url = "";
		var file = "";
		var size = 0;
		foreach (var asset in patcherreleases["assets"].AsGodotArray())
		{
			if (asset.AsGodotDictionary()["name"].AsString().ToLower().Contains(OS.GetName().ToLower()))
			{
				url = asset.AsGodotDictionary()["browser_download_url"].AsString();
				file = asset.AsGodotDictionary()["name"].AsString();
				size = asset.AsGodotDictionary()["size"].AsInt32();
				break;
			}
		}
		if (url != "")
		{
			Godot.Collections.Array output = [];
			GD.Print("Downloading " + url + " to " + GetGameDirPath("UpdateTemp/" + file));
			output.Add("Downloading " + url + " to " + GetGameDirPath("UpdateTemp/" + file));
			try
			{
				using (var httpClient = new System.Net.Http.HttpClient())
				{
					using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
					{
						response.EnsureSuccessStatusCode();
						using var bodyStream = await response.Content.ReadAsStreamAsync();
						fileStream = new System.IO.FileStream(GetGameDirPath("UpdateTemp/" + file), System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
						var buffer = new byte[4096];
						double totalRead = 0;
						int bytesRead;
						while ((bytesRead = await bodyStream.ReadAsync(buffer)) > 0)
						{
							await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
							totalRead += bytesRead;
							if (size > 0)
							{
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
								updatebutton.Text = $"{progress} / {sizee} MiB";
								GD.Print($"Downloaded: {totalRead} / {size}");
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
				updatebutton.Text = TranslationServer.Translate("locDownloadFailed") + exc.GetType().ToString();
				GD.PushError("Exception catched when updating patcher: " + exc.ToString() + " (" + exc.Message + ")");
				return;
			}
			fileStream.Dispose();
			fileStream = null;
			GD.Print("Extracting " + GetGameDirPath("UpdateTemp/" + file));
			output.Add("Extracting " + GetGameDirPath("UpdateTemp/" + file));
			OS.Execute(_7zip, ["x", GetGameDirPath("UpdateTemp/" + file), "-o" + GetGameDirPath(), "-aoa", "-y"], output, true, true);
			GD.Print($"{_7zip} x {GetGameDirPath("UpdateTemp/" + file)} -o{GetGameDirPath()} -aoa -y");
			foreach (var ff in DirAccess.GetFilesAt(GetGameDirPath()))
			{
				if (ff.EndsWith(".pck") && ff != "DELTARUNE Chinese Patcher.pck")
				{
					DirAccess.RemoveAbsolute(GetGameDirPath(ff));
					DirAccess.RenameAbsolute(GetGameDirPath("DELTARUNE Chinese Patcher.pck"), GetGameDirPath(ff));
				}
				if (OS.GetName() == "Windows" && ff.EndsWith(".exe") && ff != "DELTARUNE Chinese Patcher.exe")
				{
					DirAccess.RemoveAbsolute(GetGameDirPath(ff));
					DirAccess.RenameAbsolute(GetGameDirPath("DELTARUNE Chinese Patcher.exe"), GetGameDirPath(ff));
				}
				if (OS.GetName() == "Linux" && ff.EndsWith(".x86_64") && ff != "DELTARUNE Chinese Patcher.x86_64")
				{
					DirAccess.RemoveAbsolute(GetGameDirPath(ff));
					DirAccess.RenameAbsolute(GetGameDirPath("DELTARUNE Chinese Patcher.x86_64"), GetGameDirPath(ff));
				}
			}
			OS.MoveToTrash(GetGameDirPath("UpdateTemp"));
			updatebutton.Text = "locWaiting4Restart";
			updatebutton.TooltipText = "locPleaseRestart";
		}
	}
	public void _on_game_updated_pressed()
	{
		var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
		if (FileAccess.FileExists(path + "/"+dataname+".bak"))
		{
			DirAccess.RemoveAbsolute(path + "/"+dataname+".bak");
			GD.Print("Removed " + path + "/"+dataname+".bak");
		}
		foreach (var chapter in chapters)
		{
			if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak"))
			{
				DirAccess.RemoveAbsolute(path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak");
				GD.Print("Removed " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak");
			}
		}
		OS.MoveToTrash(path + "/backup");
		GetNode<Window>("Patch").Hide();
		GetNode<Label>("Popup/ScrollContainer/Label").Text = "locVerifyIntegrity";
		GetNode<Window>("Popup").Size = new Vector2I(640,360);
		GetNode<Window>("Popup").Show();
		GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Patch").Disabled = false;
	}
	public async void _on_update_patch_pressed()
	{
		var updatepatch = GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer3/UpdatePatch");
		updatepatch.Disabled = true;
		var progressbar = GetNode<ProgressBar>("CenterContainer/VBoxContainer/ProgressBar");
		progressbar.Visible = true;
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
			if (asset.AsGodotDictionary()["name"].AsString().ToLower().Contains(OS.GetName().ToLower()))
			{
				url = /*(TranslationServer.GetLocale() == "zh_CN" ? "https://ghfast.top/" : "") + */asset.AsGodotDictionary()["browser_download_url"].AsString();
				file = "_downloadingtemp_" + asset.AsGodotDictionary()["name"].AsString();
				size = asset.AsGodotDictionary()["size"].AsInt32();
				progressbar.MaxValue = size;
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
								progressbar.Value = totalRead;
								//progressbar.TooltipText = $"{Math.Round(totalRead/1024d/1024d, 2)} / {Math.Round(size/1024d/1024d, 2)} MiB";
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
								updatepatch.Text = $"{progress} / {sizee} MiB";
								GD.Print($"Downloaded: {totalRead} / {size}");
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
				updatepatch.Text = TranslationServer.Translate("locDownloadFailed") + exc.GetType().ToString();
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
			updatepatch.Text = "locWaiting4Restart";
			updatepatch.TooltipText = "locPleaseRestart";
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
			if (asset.AsGodotDictionary()["name"].AsString().ToLower().Contains(OS.GetName().ToLower()))
			{
				OS.ShellOpen(asset.AsGodotDictionary()["browser_download_url"].AsString());
				break;
			}
		}
	}

	public void Patch()
	{
		GetNode<Window>("Patch").Hide();
		GetNode<Label>("Log/ScrollContainer/Label").Text = "";
		var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
		Godot.Collections.Array output = [];
		Godot.Collections.Array outputtemp = [];
		//chmod加权限
		if (OS.GetName() == "macOS" || OS.GetName() == "Linux")
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
		foreach (var __7z in new[]{"7z", "7zip", "7-zip", "7zr", "7za"})
		{
			externalcheckoutput = [];
			OS.Execute(__7z,[], externalcheckoutput);
			GD.Print("Checking " + __7z);
			output.Add("Checking " + __7z);
			GD.Print(externalcheckoutput);
			foreach (var line in externalcheckoutput)
			{
				if (line.AsString().Contains("7-Zip (r) "))
				{
					_7zip = __7z;
					GD.Print("Found " + __7z);
					output.Add("Found " + __7z);
					break;
				}
			}
			if (_7zip == __7z)
			{
				break;
			}
		}
		foreach (var __xdelta in new[]{"xdelta", "xdelta3"})
		{
			externalcheckoutput = [];
			OS.Execute(__xdelta, ["-h"], externalcheckoutput);
			GD.Print("Checking " + __xdelta);
			output.Add("Checking " + __xdelta);
			GD.Print(externalcheckoutput);
			foreach (var line in externalcheckoutput)
			{
				if (line.AsString().Contains("Xdelta version "))
				{
					xdelta3 = __xdelta;
					GD.Print("Found " + __xdelta);
					output.Add("Found " + __xdelta);
					break;
				}
			}
			if (xdelta3 == __xdelta)
			{
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
					GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailedNotExists";
					GetNode<Window>("Popup").Size = new Vector2I(640,360);
					var logtext1 = "";
					foreach (var i in output)
					{
						logtext1 += i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n") + "\n";
					}
					GetNode<Label>("Log/ScrollContainer/Label").Text = logtext1;
					var gdlog1 = FileAccess.Open("user://logs/godot.log", FileAccess.ModeFlags.Read);
					logtext1 = gdlog1.GetAsText();
					gdlog1.Close();
					var log1 = FileAccess.Open(GetGameDirPath("log.txt"), FileAccess.ModeFlags.Write);
					log1.StoreString(logtext1);
					log1.Close();
					GetNode<Window>("Log").Show();
					GetNode<Window>("Popup").Show();
					GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Patch").Disabled = false;
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
					GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailedSha256";
					GetNode<Window>("Popup").Size = new Vector2I(640,360);
					var logtext1 = "";
					foreach (var i in output)
					{
						logtext1 += i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n") + "\n";
					}
					GetNode<Label>("Log/ScrollContainer/Label").Text = logtext1;
					var gdlog1 = FileAccess.Open("user://logs/godot.log", FileAccess.ModeFlags.Read);
					logtext1 = gdlog1.GetAsText();
					gdlog1.Close();
					var log1 = FileAccess.Open(GetGameDirPath("log.txt"), FileAccess.ModeFlags.Write);
					log1.StoreString(logtext1);
					log1.Close();
					GetNode<Window>("Log").Show();
					GetNode<Window>("Popup").Show();
					GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Patch").Disabled = false;
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
		//解压
		OS.Execute(_7zip, ["x", patchdir, "-o" + GetGameDirPath("ExtractTemp/"), "-aoa", "-y"], outputtemp, true, true);
		GD.Print($"{_7zip} x {patchdir} -o{GetGameDirPath("ExtractTemp/")} -aoa -y");
		foreach (var i in outputtemp)
		{
			GD.Print(i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n"));
		}
		output += outputtemp;
		output += MoveAfterExtracted(GetGameDirPath("ExtractTemp"), "", path);
		OS.MoveToTrash(GetGameDirPath("ExtractTemp/"));
		//备份data
		if (FileAccess.FileExists(path + "/" + dataname))
		{
			DirAccess.RenameAbsolute(path + "/" + dataname, path + "/backup/" + dataname);
			GD.Print("Renamed " + path + "/" + dataname + " to " + path + "/backup/" + dataname);
			output.Add("Renamed " + path + "/" + dataname + " to " + path + "/backup/" + dataname);
		}
		foreach (var chapter in chapters)
		{
			if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/" + dataname))
			{
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
			outputtemp = [];
			OS.Execute(xdelta3, ["-f", "-d", "-v", "-s", path + "/backup/" + dataname, path + "/main.xdelta", path + "/" + dataname], outputtemp, true, true);
			GD.Print($"{xdelta3} -f -d -v -s {path}/backup/{dataname} {path}/main.xdelta {path}/{dataname}");
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
				OS.Execute(xdelta3, ["-f", "-d", "-v", "-s", path + "/backup/chapter" + chapter + "_" + osname + "/" + dataname, path + "/chapter" + chapter + ".xdelta", path + "/chapter" + chapter + "_" + osname + "/" + dataname], outputtemp, true, true);
				GD.Print($"{xdelta3} -f -d -v -s {path}/backup/chapter{chapter}_{osname}/{dataname} {path}/chapter{chapter}.xdelta {path}/chapter{chapter}_{osname}/{dataname}");
			}
		}
		foreach (var i in outputtemp)
		{
			GD.Print(i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n"));
		}
		output += outputtemp;
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
		//end
		var logtext = "";
		foreach (var i in output)
		{
			logtext += i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n") + "\n";
		}
		if (logtext.Contains("checksum mismatch"))
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailedChecksum";
			GetNode<Window>("Popup").Size = new Vector2I(640,360);
			output += RestoreData(path);
		}
		else if (logtext.Contains("cannot find the path specified"))
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailedCantFind";
			GetNode<Window>("Popup").Size = new Vector2I(640,360);
			output += RestoreData(path);
		}
		else if (logtext.Replace("\r","").Replace("\n","").Replace(" ","") == "Extracting...")
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailedExternals";
			GetNode<Window>("Popup").Size = new Vector2I(640,360);
			output += RestoreData(path);
		}
		else if ((OS.GetName() == "macOS" || OS.GetName() == "Linux") && logtext.ToLower().Contains("(required by "))
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailedRequired";
			GetNode<Window>("Popup").Size = new Vector2I(640,360);
			output += RestoreData(path);
		}
		else if ((OS.GetName() == "macOS" || OS.GetName() == "Linux") && logtext.ToLower().Contains("permission denied"))
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailedDenied";
			GetNode<Window>("Popup").Size = new Vector2I(640,360);
			output += RestoreData(path);
		}
		else if (logtext.ToLower().Contains("error") || !logtext.Contains("xdelta3: finished") || !logtext.Contains("Everything is Ok"))
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailed";
			GetNode<Window>("Popup").Size = new Vector2I(480,240);
			output += RestoreData(path);
		}
		else
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatched";
			GetNode<Window>("Popup").Size = new Vector2I(480,240);
			//保存游戏路径
			var game_path = FileAccess.Open(game_path_file, FileAccess.ModeFlags.Write);
			game_path.StoreString(path);
			game_path.Close();
		}
		logtext = "";
		foreach (var i in output)
		{
			logtext += i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n") + "\n";
		}
		GetNode<Label>("Log/ScrollContainer/Label").Text = logtext;
		var gdlog = FileAccess.Open("user://logs/godot.log", FileAccess.ModeFlags.Read);
		logtext = gdlog.GetAsText();
		gdlog.Close();
		var log = FileAccess.Open(GetGameDirPath("log.txt"), FileAccess.ModeFlags.Write);
		log.StoreString(logtext);
		log.Close();
		GetNode<Window>("Log").Show();
		GetNode<Window>("Popup").Show();
		GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Patch").Disabled = false;
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
				GD.Print("Renamed" + drsdir + "/" + relative_dir + file + " to " + drsdir + "/backup/" + relative_dir + file);
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
		if (DirAccess.DirExistsAbsolute(path + "/backup"))
		{
			output += RestoreFolder(path + "/backup" , path);
		}
		OS.MoveToTrash(path + "/backup");
		GD.Print("Removed " + path + "/backup");
		output.Add("Removed " + path + "/backup");
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
		var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
		if (!DirAccess.DirExistsAbsolute(path + "/backup"))
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locNoBakDetected";
			GetNode<Window>("Popup").Size = new Vector2I(360,120);
			GetNode<Window>("Popup").Show();
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
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locOldBakDetected";
			GetNode<Window>("Popup").Size = new Vector2I(360,120);
			GetNode<Window>("Popup").Show();
			return;
		}
		RestoreData(path);
		GetNode<Label>("Popup/ScrollContainer/Label").Text = "locUnpatched";
		GetNode<Window>("Popup").Size = new Vector2I(360,120);
		GetNode<Window>("Popup").Show();
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
			var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text.TrimPrefix("\"").TrimSuffix("\"").TrimPrefix("\'").TrimSuffix("\'").TrimSuffix("/").TrimSuffix("\\");
			var game_path = FileAccess.Open(game_path_file, FileAccess.ModeFlags.Write);
			game_path.StoreString(path);
			game_path.Close();
			if (fileStream != null)
			{
				fileStream.Dispose();
				fileStream = null;
			}
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
