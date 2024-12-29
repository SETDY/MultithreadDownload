using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using MultithreadDownload.Downloads;
using System.Diagnostics;

namespace MultithreadDownload.UnitTest
{
    [TestClass]
    public class MultiDownloadTest
    {
        public void CreateInstance_WithInvalidAmount_Test()
        {
            Assert.ThrowsException<SystemException>(() => new MultiDownload(-1, -2));
        }

        [TestMethod]
        public void Add_SingleFile_WithSpecific_Test()
        {
            MultiDownload multiDownload = new MultiDownload(3,4);
            multiDownload.Add("https://sample-videos.com/video321/mp4/240/big_buck_bunny_240p_30mb.mp4", "G:\\DownloadTest");
            Assert.AreEqual(1, multiDownload.Tasks.Count);
            while (multiDownload.Tasks[0].State == DownloadTaskState.Downloading || multiDownload.Tasks[0].State == DownloadTaskState.Waiting)
            {
                Thread.Sleep(1000);
            }
            Assert.AreEqual(DownloadTaskState.Completed, multiDownload.Tasks[0].State);
            Assert.IsTrue(File.Exists(multiDownload.Tasks[0].Path));
        }

        [TestMethod]
        public void Add_MoreFile_WithAll_Test()
        {
            MultiDownload multiDownload = new MultiDownload(2, 2);
            multiDownload.Add("https://down-tencent.huorong.cn/sysdiag-all-x64-6.0.3.1-2024.11.02.1.exe", "F:\\DownloadTest");
            Thread.Sleep(50);
            multiDownload.Add("https://down-tencent.huorong.cn/sysdiag-all-x64-6.0.3.1-2024.11.02.1.exe", "F:\\DownloadTest");
            while (
                multiDownload.Tasks[0].State != DownloadTaskState.Completed ||
                multiDownload.Tasks[1].State != DownloadTaskState.Completed)
            {
                Thread.Sleep(5000);
                Debug.WriteLine($"Task1:{multiDownload.Tasks[0].State} Task2:{multiDownload.Tasks[1].State}");
            }
            Assert.AreEqual(DownloadTaskState.Completed, multiDownload.Tasks[0].State);
            Assert.AreEqual(DownloadTaskState.Completed, multiDownload.Tasks[1].State);
            Assert.IsTrue(File.Exists(multiDownload.Tasks[0].Path));
            Assert.IsTrue(File.Exists(multiDownload.Tasks[1].Path));
        }

        [TestMethod]
        public void Add_MoreFile_WithWait_Test()
        {
            MultiDownload multiDownload = new MultiDownload(2, 3);
            multiDownload.Add("http://updates-http.cdn-apple.com/2019WinterFCS/fullrestores/041-39257/32129B6C-292C-11E9-9E72-4511412B0A59/iPhone_4.7_12.1.4_16D57_Restore.ipsw", "G:\\DownloadTest\\Safe");
            Thread.Sleep(50);
            multiDownload.Add("https://sample-videos.com/video321/mp4/240/big_buck_bunny_240p_30mb.mp4", "G:\\DownloadTest\\Safe");
            Thread.Sleep(50);
            multiDownload.Add("http://speedtest.zju.edu.cn/1000M", "G:\\DownloadTest\\Safe");
            Assert.AreEqual(3, multiDownload.Tasks.Count);
            while (
                multiDownload.Tasks[0].State != DownloadTaskState.Completed ||
                multiDownload.Tasks[1].State != DownloadTaskState.Completed ||
                multiDownload.Tasks[2].State != DownloadTaskState.Completed)
            {
                Thread.Sleep(5000);
                Debug.WriteLine($"Task1:{multiDownload.Tasks[0].State} Task2:{multiDownload.Tasks[1].State} Task3:{multiDownload.Tasks[2].State}");
            }
            Assert.AreEqual(DownloadTaskState.Completed, multiDownload.Tasks[0].State);
            Assert.AreEqual(DownloadTaskState.Completed, multiDownload.Tasks[1].State);
            Assert.AreEqual(DownloadTaskState.Completed, multiDownload.Tasks[2].State);
            Assert.IsTrue(File.Exists(multiDownload.Tasks[0].Path));
            Assert.IsTrue(File.Exists(multiDownload.Tasks[1].Path));
            Assert.IsTrue(File.Exists(multiDownload.Tasks[2].Path));
        }
    }
}