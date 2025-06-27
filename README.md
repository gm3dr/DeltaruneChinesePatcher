<img width="100" height="100" align="left" style="float: left; margin: 0 10px 0 0;" alt="MS-DOS logo" src="patcher_icon.png">

# DELTARUNE 汉化安装器

**下载安装器：[GitHub Release](https://github.com/gm3dr/DeltaruneChinese/releases)**

> [!NOTE]
> 对于 Windows on Arm 用户, 如果遇到无法使用的问题, 请手动构建或手动打补丁！macOS 暂无安装器提供, 请手动打补丁！

## 开发工具

- [Godot Engine 4.1.1 Stable Mono](https://godotengine.org/download/archive/4.1.1-stable/)

- [.NET 8 SDK](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)

- [rcedit](https://github.com/electron/rcedit/releases/latest) *(非必须, 用于 Windows 平台导出时修改 exe 资源)*

- [UPX](https://github.com/upx/upx/releases/latest) *(非必须, 用于压缩可执行文件大小)*

## 路线图
 - [x] \[v1.1.0\] 补丁安装失败错误信息
 - [x] \[v1.1.0\] Linux 平台支持
 - [x] \[v1.1.0\] 安装失败时恢复备份
 - [x] \[v2.0.0\] 设计界面外观
 - [x] \[v2.0.0\] 在线获取并一键下载最新汉化补丁
 - [x] \[v2.0.0\] 检测目录下Readme并弹出窗口
 - [ ] \[v2.1.0\] 调用本地安装的 XDelta3 与 7-Zip
 - [ ] \[v2.1.0\] Linux 与 macOS 自动`chmod +x`外部程序
 - [ ] macOS 平台支持
 - [ ] 删除汉化补丁 (将被覆盖的内容存储到Backup目录用于还原)
 - [ ] 修复安装失败且只含有Extracting... ([#4](https://github.com/gm3dr/DeltaruneChinesePatcher/issues/4))

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