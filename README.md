# HttpDownloadEngine
一个用于多任务多线程下载http链接文件的库

## 使用教程
首先，请请引用**HttpDownloadEngine**命名空间
```C#
using HttpDownloadEngine;
```
接着，请新建Download实例，两个参数分别为(最大下载任务数,单个任务最大下载线程数)
```C#
Download downlaod = new Download(3,8);
```
然后，添加下载任务
```C#
new Download(3,8).Add(链接,路径);
```
你的文件下载好了！