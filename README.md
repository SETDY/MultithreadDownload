# ✨ MultithreadDownload

[![NuGet](https://img.shields.io/nuget/v/MultithreadDownloadLib.svg)](https://www.nuget.org/packages/MultithreadDownloadLib/)	[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)	![.NET Version](https://img.shields.io/badge/.NET-6%2B-blue)	![Build Status](https://github.com/SETDY/MultithreadDownload/actions/workflows/dotnet-test.yml/badge.svg?branch=feature/v3-refactor)

**MultithreadDownload** 是一款基于多线程的高效文件下载库，支持分段并发下载，旨在提供稳定、可扩展的下载解决方案。
 当前版本支持 **HTTP** 协议，未来将扩展至 **FTP**、**BitTorrent** 等多种协议，并计划支持 **断点续传** 等高级功能。

------

## 目录

- [简介](#简介)
- [特点](#特点)
- [安装](#安装)
- [使用示例](#使用示例)
- [功能状态](#功能状态)
- [支持的环境](#支持的环境)
- [贡献](#贡献)
- [许可证](#许可证)
- [依赖声明](#依赖声明)

------

## 简介

MultithreadDownload 通过灵活的线程调度和任务管理，实现了高效且可扩展的文件下载。
 项目采用 MIT 协议开源，已发布至 [NuGet](https://www.nuget.org/packages/MultithreadDownload)，方便集成与使用。

------

## 特点

- **多线程分段下载** 🚀：智能划分文件并发下载，显著提升下载速度。
- **可扩展协议支持** 🔌：提供统一接口，便于开发者自定义其他协议（FTP、BT等）。
- **任务管理与调度** 📦：基于任务队列和信号量机制，高效分配并控制并发任务数量。
- **丰富的事件回调** 🔔：支持进度更新、任务完成等事件通知。
- **规划中功能** 🛠️：
  - 多协议支持（FTP / BitTorrent 等）
  - 断点续传（Resume Download）
  - 更完善的错误处理与恢复机制

------

## 安装

通过 NuGet 快速安装：

```bash
dotnet add package MultithreadDownloadLib
```

或在 Visual Studio 的 NuGet 管理器中搜索 `MultithreadDownloadLib` 进行安装。

------

## 使用示例

以下示例展示了如何使用 HTTP 协议进行多线程文件下载：

```csharp
using MultithreadDownload.Core;
using MultithreadDownload.Tasks;
using MultithreadDownload.Protocols;

byte MAX_PARALLEL_THREADS = 8;
byte MAX_PARALLEL_TASKS = 3;

// 创建下载服务管理器（当前使用 HTTP 协议）
var downloadManager = new MultiDownload(MAX_PARALLEL_TASKS, DownloadServiceType.Http);

// 获取下载任务上下文（包括分段信息等）
var context = await HttpDownloadContext.GetDownloadContext(MAX_PARALLEL_THREADS, 
                                        "D:\\", "https://example.com/file.zip");

// 添加下载任务到管理器
downloadManager.AddTask(context);

// 启动任务分配器，自动管理任务并启动下载
downloadManager.StartAllocator();

// （可选）监听任务完成事件
downloadManager.TasksProgressCompleted += (sender, e) =>
{
    Console.WriteLine("Task completed.");
};

// （可选）监听下载速度变化
downloadManager.GetDownloadTasks()[0].SpeedMonitor.SpeedUpdated += (e) =>
{
    Console.WriteLine($"Current speed: {e}");
};
```

------

## 功能状态

| 功能             | 状态     |
| ---------------- | -------- |
| 多线程 HTTP 下载 | ✅ 已完成 |
| FTP 支持         | 🔧 开发中 |
| BitTorrent 支持  | 🔧 开发中 |
| 断点续传         | 🔧 规划中 |
| 错误重试和处理   | 🔧 规划中 |
| 还有更多...      | ...      |

在提供正确的 HTTP 链接、有效的参数及稳定的网络环境下，**当前版本能够稳定运行，不存在已知运行时异常。**

------

## 支持的环境

- .NET 6 / .NET 7 / .NET 8 / .NET 9
- Windows / Linux / macOS

------

## 贡献

欢迎任何形式的贡献，包括但不限于：

- 提交 Issue 或 Bug 报告 🐛
- 提出功能建议或优化意见 💡
- 提交 Pull Request ✍️

> 如需进行重大修改，请提前通过 Issue 讨论，确保开发方向一致。

------

## 许可证

本项目遵循 MIT License。
 您可以自由地使用、修改、分发本项目代码，仅需保留原作者声明。

## 依赖声明

本项目在测试阶段使用了以下第三方库：

- [**Fluent Assertions**](https://fluentassertions.com/) - 用于增强单元测试的可读性。根据 Xceed Software Inc. 的社区许可协议 (Community License Agreement)，该库仅用于本项目的**非商业性测试用途**，并未进行与其他产品的性能对比测试，也未公开发布任何基准测试结果。

------

MultithreadDownload —— 致力于构建下一代多线程下载解决方案！🚀
 如果你喜欢这个项目，欢迎 ⭐Star 和 🍴Fork！

