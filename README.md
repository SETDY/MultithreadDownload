# 🚀 MultithreadDownload

多线程下载，简单而强大。  
现已支持 HTTP 协议

[![NuGet version](https://img.shields.io/nuget/v/MultithreadDownload.svg)](https://www.nuget.org/packages/MultithreadDownload/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## ✨ 项目亮点

- ⚡ **多线程下载**：自动分段，多线程并发，提高下载速度！
- 🌐 **协议扩展性设计**：只需简单实现接口，即可添加新协议（FTP / BT / ...）
- 🧠 **智能调度**：自动管理下载队列和并发量。
- 🛡️ **可靠安全**：即使在高并发下，也能保持任务安全稳定执行。
- 🔥 **轻量易用**：极简API设计，上手快，适合集成到任何.NET项目。

---

## 📦 快速开始

### 1. 安装 MultithreadDownload

通过 [NuGet](https://www.nuget.org/packages/MultithreadDownload/) 安装：

```bash
dotnet add package MultithreadDownload
```

或者在 Visual Studio 的 NuGet 包管理器中搜索 `MultithreadDownload` 并安装。

---

### 2. 基本用法示例

```csharp
using MultithreadDownload.Core;
using MultithreadDownload.Tasks;
using MultithreadDownload.Protocols;

byte MAX_PARALLEL_THREADS = 8;

// 创建下载服务管理器（使用HTTP协议）
var downloadManager = new MultiDownload(3, DownloadServiceType.Http);

// 获得下载任务的上下文（内容）
var context = await HttpDownloadContext.GetDownloadContext(MAX_PARALLEL_THREADS, "https://example.com/file.zip", "file.zip");

// 把下载任务添加到下载服务管理器
downloadManager.AddTask(context);

// 开启任务分配器 => 自动分配并启动下载任务
downloadManager.StartAllocator();

// 可选：监听任务完成事件
downloadManager.TasksProgressCompleted += (sender, e) =>
{
    Console.WriteLine("Task completed.");
};
```
## ⚙️ 高级用法

- **暂停/恢复任务**
- **取消单个任务或全部任务**
- **扩展新的下载协议**（只需实现 `IDownloadService` 接口！）

👉 查看详细文档：[🔗 Wiki（建设中）](#)

---

## 🛠️ 支持的环境

- .NET 6 / .NET 7 / .NET 8
- Windows / Linux / macOS

---

## 📄 License

MultithreadDownload 遵循 [MIT License](LICENSE)。

> 这意味着你可以自由地使用、修改、分发，甚至在商业项目中使用，只需保留版权声明。

---

## 🙏 致谢

如果你喜欢这个项目，欢迎：
- 🌟 Star 本仓库
- 🐛 提交 issue 或 PR
- 📢 分享给更多开发者！

---

# 🚀 立即体验极速下载！  
🎯 [点此跳转 NuGet 安装](https://www.nuget.org/packages/MultithreadDownload/)
