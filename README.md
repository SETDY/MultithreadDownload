
MultithreadDownload
一个用于多任务多线程下载http链接文件的库

## 使用教程
首先，请请引用**MultithreadDownload.Downloads**命名空间
```C#
using MultithreadDownload.Downloads;
```
接着，请新建Download实例，两个参数分别为(最大下载任务数,单个任务最大下载线程数)
```C#
MultiDownload downlaod = new MultiDownload(3,8);
```
然后，添加下载任务
```C#
new MultiDownload(3,8).Add(链接,路径);
```
你的文件下载好了！
