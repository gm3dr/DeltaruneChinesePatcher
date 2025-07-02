# DELTARUNE 汉化安装器
<img src="patcher_icon.png" width="15%">

在线下载并一键安装 [DELTARUNE 汉化补丁](https://github.com/gm3dr/DeltaruneChinese/releases)

## 开源协议
MIT License

## 借物
Godot Engine - Juan Linietsky, Ariel Manzur 与贡献者们<br>
.NET 8 - Microsoft<br>
Visual Studio Code - Microsoft<br>
XDelta 3.1.0 - Joshua MacDonald<br>
7-Zip 24.09 - Igor Pavlov<br>
UPX - Markus & Laszlo & John<br>
DELTARUNE 资源 - Toby Fox<br>
Determination Sans - Toby Fox<br>
SimSunBDF - 北京中易中标电子信息技术有限公司<br>
图标 - WhatDamon

## 开发环境
Godot Engine 4.4.1 Stable Mono<br>
.NET 8 SDK<br>
rcedit（用于修改exe资源）<br>
UPX（用于压缩可执行档案大小）<br>
与导出平台相同的操作系统（由于使用了NativeAOT）

## ~~大饼~~ Roadmap
 - [x] 【v1.1.0】补丁安装失败错误信息
 - [ ] macOS
 - [x] 【v1.1.0】Linux
 - [x] 【v1.1.0】安装失败时恢复备份
 - [x] 【v2.0.0】设计界面外观
 - [x] 【v2.0.0】在线获取并一键下载最新汉化补丁
 - [x] 【v2.0.0】检测目录下Readme并弹出窗口
 - [ ] \[v2.1.0\] 调用本地安装的 XDelta3 与 7-Zip
 - [ ] \[v2.1.0\] Linux 与 macOS 自动`chmod +x`外部程序
 - [ ] \[v2.1.0\]删除汉化补丁<br>（将被覆盖的内容存储到Backup目录用于还原）
 - [ ] 修复安装失败且只含有Extracting...（[#4](https://github.com/gm3dr/DeltaruneChinesePatcher/issues/4)）