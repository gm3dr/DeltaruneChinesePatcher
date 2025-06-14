using Godot;
using System;
using System.Linq;
using System.Net.Http;

public partial class Main : Control
{
	static string[] chapters = ["1", "2", "3", "4"];
	string[] locales;
	bool inited = false;
	static string xdelta3 = GetGameDirPath("externals/xdelta3/xdelta3");
	static string _7zip = GetGameDirPath("externals/7zip/7z");
	static string patchdir = GetGameDirPath("patch");
	static string patchver = "locNotFound";
	static Godot.Collections.Dictionary patcherreleases = new();
	static Godot.Collections.Dictionary patchreleases = new();
	static string osname = (OS.GetName() == "macOS" ? "mac" : "windows");
	static string dataname = (OS.GetName() == "macOS" ? "game.ios" : "data.win");
	System.IO.FileStream fileStream = null;
	public override async void _Ready()
	{
		//首次初始化
		if (!inited)
		{
			GetNode<OptionButton>("OptionButton").Disabled = true;
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
		}
		//安装器版本号
		GetNode<Label>("HBoxContainer/Label").Text = "v" + ProjectSettings.GetSetting("application/config/version").AsString();
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
		//HttpClient
		var httpc = new System.Net.Http.HttpClient();
		httpc.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36");
		//补丁版本号
		GetNode<Label>("CenterContainer/VBoxContainer/Label").Text = TranslationServer.Translate("locLocalVer") + TranslationServer.Translate(patchver) + "\n" + TranslationServer.Translate("locLatestVer") + TranslationServer.Translate("locRequesting");
		var json = new Json();
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
		}
		catch (HttpRequestException exc)
		{
			GD.PushError("Exception catched when requesting patch latest: "+exc.ToString()+" ("+exc.Message+")");
			//GetNode<Label>("CenterContainer/VBoxContainer/Label").Text = TranslationServer.Translate("locLocalVer") + TranslationServer.Translate(patchver) + "\n" + TranslationServer.Translate("locLatestVer") + TranslationServer.Translate("locTimeout").ToString().TrimPrefix(" ");
		}
		//安装器更新
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
			GD.PushError("Exception catched when requesting patcher latest: "+exc.ToString()+" ("+exc.Message+")");
		}

		if (!inited)
		{
			GetNode<OptionButton>("OptionButton").Disabled = false;
			inited = true;
		}
	}

	public override void _Process(double delta)
	{
		GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Patch").Disabled = (GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text == "" || patchver == "locNotFound");
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
		var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text;
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
	public async void _on_update_pressed()
	{
		GetNode<Button>("HBoxContainer/Update").Disabled = true;
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
				GetNode<Button>("HBoxContainer/Update").Text = TranslationServer.Translate("locDownloadFailed") + exc.GetType().ToString();
				GD.PushError("Exception catched when updating patcher: " + exc.ToString() + " ("+exc.Message+")");
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
			GetNode<Button>("HBoxContainer/Update").Text = "locWaiting4Restart";
		}
		if (url != "")
		{
			Godot.Collections.Array output = [];
			GD.Print("Downloading " + url + " to " + GetGameDirPath("UpdateTemp/" + file));
			output.Add("Downloading " + url + " to " + GetGameDirPath("UpdateTemp/" + file));
			var response = await new System.Net.Http.HttpClient().GetAsync(url);
			if (response.IsSuccessStatusCode)
			{
				var data = await response.Content.ReadAsByteArrayAsync();
				System.IO.File.WriteAllBytes(GetGameDirPath("UpdateTemp/" + file), data);
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
				GetNode<Button>("HBoxContainer/Update").Text = "locWaiting4Restart";
			}
			else
			{
				GetNode<Button>("HBoxContainer/Update").Text = TranslationServer.Translate("locDownloadFailed") + response.StatusCode.ToString();
			}
		}
	}
	public void _on_game_updated_pressed()
	{
		var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text;
		if (FileAccess.FileExists(path + "/"+dataname+".bak"))
		{
			DirAccess.RemoveAbsolute(path + "/"+dataname+".bak");
			GD.Print("Removed " + path + "/"+dataname+".bak");
		}
		if (FileAccess.FileExists(path + "/main.xdelta"))
		{
			DirAccess.RemoveAbsolute(path + "/main.xdelta");
			GD.Print("Removed " + path + "/main.xdelta");
		}
		foreach (var chapter in chapters)
		{
			if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak"))
			{
				DirAccess.RemoveAbsolute(path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak");
				GD.Print("Removed " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak");
			}
			if (FileAccess.FileExists(path + "/chapter" + chapter + ".xdelta"))
			{
				DirAccess.RemoveAbsolute(path + "/chapter" + chapter + ".xdelta");
				GD.Print("Removed " + path + "/chapter" + chapter + ".xdelta");
			}
		}
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
								updatepatch.Text = $"{Math.Round(totalRead/1024d/1024d, 2)} / {Math.Round(size/1024d/1024d, 2)} MiB";
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
		var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text;
		Godot.Collections.Array output = [];
		Godot.Collections.Array outputtemp = [];
		GD.Print("Extracting...");
		output.Add("Extracting...");
		OS.Execute(_7zip, ["x", patchdir, "-o" + path, "-aoa", "-y"], outputtemp, true, true);
		GD.Print($"{_7zip} x {patchdir} -o{path} -aoa -y");
		foreach (var i in outputtemp)
		{
			GD.Print(i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n"));
		}
		output += outputtemp;
		if (FileAccess.FileExists(path + "/main.xdelta"))
		{
			GD.Print("Patching main data");
			output.Add("Patching main data");
			if (FileAccess.FileExists(path + "/"+dataname+".bak"))
			{
				DirAccess.RemoveAbsolute(path + "/"+dataname+"");
				GD.Print("Removed " + path + "/"+dataname+"");
				output.Add("Removed " + path + "/"+dataname+"");
			}
			else
			{
				DirAccess.RenameAbsolute(path + "/"+dataname+"", path + "/"+dataname+".bak");
				GD.Print("Renamed " + path + "/"+dataname+" to " + path + "/"+dataname+".bak");
				output.Add("Renamed " + path + "/"+dataname+" to " + path + "/"+dataname+".bak");
			}
			outputtemp = [];
			OS.Execute(xdelta3, ["-f", "-d", "-v", "-s", path + "/"+dataname+".bak", path + "/main.xdelta", path + "/"+dataname+""], outputtemp, true, true);
			GD.Print($"{xdelta3} -f -d -v -s {path}/{dataname}.bak {path}/main.xdelta {path}/{dataname}");
		}
		foreach (var chapter in chapters)
		{
			if (FileAccess.FileExists(path + "/chapter" + chapter + ".xdelta"))
			{
				GD.Print("Patching chapter" + chapter + " data");
				output.Add("Patching chapter" + chapter + " data");
				if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak"))
				{
					DirAccess.RemoveAbsolute(path + "/chapter" + chapter + "_" + osname + "/"+dataname+"");
					GD.Print("Removed " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+"");
					output.Add("Removed " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+"");
				}
				else
				{
					DirAccess.RenameAbsolute(path + "/chapter" + chapter + "_" + osname + "/"+dataname+"", path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak");
					GD.Print("Renamed " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+" to " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak");
					output.Add("Renamed " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+" to " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak");
				}
				OS.Execute(xdelta3, ["-f", "-d", "-v", "-s", path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak", path + "/chapter" + chapter + ".xdelta", path + "/chapter" + chapter + "_" + osname + "/"+dataname+""], outputtemp, true, true);
				GD.Print($"{xdelta3} -f -d -v -s {path}/chapter{chapter}_{osname}/{dataname}.bak {path}/chapter{chapter}.xdelta {path}/chapter{chapter}_{osname}/{dataname}");
			}
		}
		foreach (var i in outputtemp)
		{
			GD.Print(i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n"));
		}
		output += outputtemp;
		foreach (var i in output)
		{
			GetNode<Label>("Log/ScrollContainer/Label").Text += i.AsString().TrimPrefix("\r\n").TrimSuffix("\r\n") + "\n";
		}
		var log = FileAccess.Open(GetGameDirPath("log.txt"), FileAccess.ModeFlags.Write);
		var logtext = GetNode<Label>("Log/ScrollContainer/Label").Text;
		log.StoreString(logtext);
		log.Close();
		GetNode<Window>("Log").Show();
		if (logtext.Contains("checksum mismatch"))
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailedChecksum";
			GetNode<Window>("Popup").Size = new Vector2I(640,360);
			RestoreData();
		}
		else if (logtext.ToLower().Contains("error") || !logtext.Contains("xdelta3: finished"))
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatchFailed";
			GetNode<Window>("Popup").Size = new Vector2I(480,240);
			RestoreData();
		}
		else
		{
			GetNode<Label>("Popup/ScrollContainer/Label").Text = "locPatched";
			GetNode<Window>("Popup").Size = new Vector2I(480,240);
		}
		GetNode<Window>("Popup").Show();
		GetNode<Button>("CenterContainer/VBoxContainer/HBoxContainer2/Patch").Disabled = false;
	}
	internal void RestoreData()
	{
		var path = GetNode<LineEdit>("CenterContainer/VBoxContainer/HBoxContainer/LineEdit").Text;
		foreach (var chapter in chapters)
		{
			if (FileAccess.FileExists(path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak"))
			{
				DirAccess.RenameAbsolute(path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak", path + "/chapter" + chapter + "_" + osname + "/"+dataname+"");
				GD.Print("Renamed " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+".bak to " + path + "/chapter" + chapter + "_" + osname + "/"+dataname+"");
			}
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
