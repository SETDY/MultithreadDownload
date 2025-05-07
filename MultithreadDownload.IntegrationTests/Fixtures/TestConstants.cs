using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.IntegrationTests.Fixtures
{
    /// <summary>
    /// This class contains constants used in the tests.
    /// </summary>
    public static class TestConstants
    {
        public static string SMALL_TESTFILE_PATH { get; private set; } = 
            $"Resources{Path.DirectorySeparatorChar}testfile.txt";

        public static string LARGE_TESTFILE_PATH { get; private set; } = 
            $"Resources{Path.DirectorySeparatorChar}testfile.test";
    }
}
