<img width="100" height="100" align="left" style="float: left; margin: 0 10px 0 0;" alt="Logo" src="patcher_icon.png">

# DELTARUNE 汉化安装器

**下载安装器：[GitHub Release](https://github.com/gm3dr/DeltaruneChinesePatcher/releases/latest)**

在线下载并一键安装 [DELTARUNE 汉化补丁](https://github.com/gm3dr/DeltaruneChinese/releases)

> [!NOTE]
> 如果无法正常使用安装器安装补丁<br>
> 可以通过手动方法来进行安装<br>
> 安装方式见 **[此处](https://github.com/gm3dr/DeltaruneChinese/blob/main/README.md)**

## 截图

![Screenshot](./screenshot.png)

## 开发工具

- [Godot Engine](https://godotengine.org) *(修改只需要使用 4.4.1 Stable Mono，文件中的 4.4.2-rc 是用于压缩文件大小的自定义导出模板)*

- [.NET 8 SDK](https://dotnet.microsoft.com)

- [rcedit](https://github.com/electron/rcedit) *(非必须, 用于 Windows 平台导出时修改 exe 资源)*

- [UPX](https://github.com/upx/upx/releases) *(非必须, 用于压缩可执行文件大小)*

## 路线图

 - [x] \[v1.1.0\] 补丁安装失败错误信息
 - [x] \[v1.1.0\] Linux 平台支持
 - [x] \[v1.1.0\] 安装失败时恢复备份
 - [x] \[v2.0.0\] 设计界面外观
 - [x] \[v2.0.0\] 在线获取并一键下载最新汉化补丁
 - [x] \[v2.0.0\] 检测目录下Readme并弹出窗口
 - [x] \[v2.1.0\] 调用本地安装的 XDelta3 与 7-Zip
 - [x] \[v2.1.0\] Linux 与 macOS 自动`chmod +x`外部程序
 - [x] \[v2.1.0\] 删除汉化补丁 (将被覆盖的内容存储到Backup目录用于还原)
 - [ ] macOS 平台支持

## 借物


<table>
	<tr>
		<th>资源</th>
		<th>提供者</th>
	</tr>
	<tr>
		<td>DELTARUNE 资源</td>
		<td rowspan="2">Toby Fox</td>
	</tr>
	<tr>
		<td>Determination Sans</td>
	</tr>
    <tr>
        <td>Godot Engine</td>
        <td>Juan Linietsky, Ariel Manzur 与贡献者们</td>
    </tr>
    <tr>
        <td>.NET 8</td>
        <td rowspan="2">Microsoft 与贡献者们</td>
    </tr>
    <tr>
        <td>Visual Studio Code</td>
    </tr>
    </tr>
        <td>7-Zip</td>
        <td>Igor Pavlov</td>
    </tr>
    </tr>
        <td>XDelta</td>
        <td>Joshua MacDonald</td>
    </tr>
    </tr>
        <td>UPX</td>
        <td>Markus & Laszlo & John</td>
    </tr>
    </tr>
        <td>rcedit</td>
        <td>electron 开源贡献者</td>
    </tr>
    </tr>
        <td>Gameloop.Vdf</td>
        <td>Shravan Rajinikanth</td>
    </tr>
    </tr>
        <td>SimSunBDF</td>
        <td>北京中易中标电子信息技术有限公司</td>
    </tr>
    </tr>
        <td>应用图标</td>
        <td>WhatDamon</td>
    </tr>
</table>

 ## 开源协议

MIT License