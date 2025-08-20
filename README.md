<img width="100" height="100" align="left" style="float: left; margin: 0 10px 0 0;" alt="Logo" src="patcher_icon.png">

# DELTARUNE 汉化安装器

**下载安装器：[GitHub Release](https://github.com/gm3dr/DeltaruneChinesePatcher/releases/latest)**

在线下载并一键安装 [DELTARUNE 汉化补丁](https://github.com/gm3dr/DeltaruneChinese/releases)

> [!NOTE]
> 补丁安装方法见 **[此处](https://github.com/gm3dr/DeltaruneChinese/blob/main/README.md#%E8%A1%A5%E4%B8%81%E5%AE%89%E8%A3%85%E6%96%B9%E6%B3%95)**
> 如果无法正常使用安装器安装补丁<br>
> 可以通过手动方法来进行安装<br>
> 手动安装方式见 **[此处](https://github.com/gm3dr/DeltaruneChinese/blob/main/README.md#%E6%89%8B%E5%8A%A8%E5%AE%89%E8%A3%85)**

## 截图

![Screenshot](./screenshot.png)

## 开发工具

- [Godot Engine](https://godotengine.org) *(修改只需要使用 4.4.1 Stable Mono，文件中的 4.4.2-rc 是用于压缩文件大小的[自定义导出模板](#编辑器与自定义导出模板构建脚本))*
- [.NET 8 SDK](https://dotnet.microsoft.com)
- [Gameloop.Vdf](https://www.nuget.org/packages/Gameloop.Vdf)
- [rcedit](https://github.com/electron/rcedit) *(非必须, 用于 Windows 平台导出时修改 exe 资源)*
- [UPX](https://github.com/upx/upx/releases) *(非必须, 用于压缩可执行文件大小)*

### 编辑器与自定义导出模板构建脚本
`scons` 开头的指令都需要在最后加上[通用参数](#通用参数)的内容

`${{ env.ANGLE_LIB_PATH }}` ANGLE 库的路径，从[这里](https://github.com/godotengine/godot-angle-static/releases)下载（Windows Only）<br>
`${{ env.LOCAL_NUGET_PATH }}` Nuget 包的导出位置<br>
`${{ env.GODOT_EDITOR_PATH }}` Godot 编辑器的位置，不限制是自定义构建或是官方构建，只要函数都相同
#### 通用参数
```
production=yes debug_symbols=no optimize=size module_text_server_adv_enabled=no module_text_server_fb_enabled=yes module_godot_physics_2d_enabled=no module_godot_physics_3d_enabled=no module_jolt_enabled=no disable_physics_2d=yes disable_physics_3d=yes module_basis_universal_enabled=no module_bcdec_enabled=no module_bmp_enabled=no module_camera_enabled=no module_csg_enabled=no module_dds_enabled=no module_enet_enabled=no module_etcpak_enabled=no module_fbx_enabled=no module_gltf_enabled=no module_gridmap_enabled=no module_hdr_enabled=no module_interactive_music_enabled=no module_jsonrpc_enabled=no module_ktx_enabled=no module_mbedtls_enabled=no module_meshoptimizer_enabled=no module_minimp3_enabled=no module_mobile_vr_enabled=no module_msdfgen_enabled=no module_multiplayer_enabled=no module_noise_enabled=no module_navigation_2d_enabled=no module_navigation_3d_enabled=no module_ogg_enabled=no module_openxr_enabled=no module_raycast_enabled=no module_tga_enabled=no module_theora_enabled=no module_tinyexr_enabled=no module_upnp_enabled=no module_vhacd_enabled=no module_vorbis_enabled=no module_webrtc_enabled=no module_websocket_enabled=no module_webxr_enabled=no module_zip_enabled=no
```
#### 构建编辑器
##### Windows（不含编辑器）
```
scons platform=windows arch=x86_64 target=template_release module_mono_enabled=yes d3d12=yes angle_libs=${{ env.ANGLE_LIB_PATH }} disable_3d=yes lto=full module_regex_enabled=no module_svg_enabled=no
```
##### Linux
```
scons platform=linuxbsd arch=x86_64 target=editor module_mono_enabled=yes lto=full module_astcenc_enabled=no
scons platform=linuxbsd arch=x86_64 target=template_release module_mono_enabled=yes lto=full disable_3d=yes module_astcenc_enabled=no module_regex_enabled=no module_svg_enabled=no
```
##### macOS
```
scons platform=macos arch=arm64 target=editor module_mono_enabled=yes module_astcenc_enabled=no
scons platform=macos target=template_release arch=arm64 module_mono_enabled=yes disable_3d=yes module_astcenc_enabled=no module_regex_enabled=no module_svg_enabled=no
scons platform=macos target=template_release arch=x86_64 generate_bundle=yes module_mono_enabled=yes disable_3d=yes module_astcenc_enabled=no module_regex_enabled=no module_svg_enabled=no
```
##### 参数差异原因
Direct3D 12 与 ANGLE 为 Windows 独占所以只有其包含 `d3d12=yes angle_libs=${{ env.ANGLE_LIB_PATH }}`<br>
Windows 包含 ANGLE 库需要 ASTC Encoding 所以不含 `module_astcenc_enabled=no`<br>
编辑器需要 3D 、RegEX 和 SVG 支援所以不含 `disable_3d=yes module_regex_enabled=no module_svg_enabled=no`<br>
macOS 不支援链接时优化所以不含 `lto=full`
#### 生成C#胶水与构建
```
${{ env.GODOT_EDITOR_PATH }} --headless --generate-mono-glue modules/mono/glue
python "./modules/mono/build_scripts/build_assemblies.py" --godot-output-dir=./bin --push-nupkgs-local ${{ env.LOCAL_NUGET_PATH }}
```

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
 - [x] \[v2.1.2\] 默认 DELTARUNE 路径
 - [x] \[v2.1.2\] 从默认 Steam 路径读取 libraryfolders.vdf 来获取 DR 安装路径
 - [x] \[v2.1.2\] Windows 从注册表获取 Steam 路径后读取 libraryfolders.vdf 来获取 DR 安装路径
 - [x] \[v2.1.2\] 安装后显示安装用时
 - [x] \[v2.1.2\] 多线程安装
 - [ ] macOS 平台支持 (#14)

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
