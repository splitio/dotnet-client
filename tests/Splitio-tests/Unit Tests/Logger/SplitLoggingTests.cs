using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Logger;
using System.IO;

namespace Splitio_Tests.Unit_Tests.Logger
{
    [TestClass]
    public class SplitLoggingTests
    {
        #region Trace
        [TestMethod]
        public void TraceExceptionShouldLog()
        {
            // Arrange.
            var path = "trace-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Trace, textWriter);

                // Act.
                logger.Trace("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Trace] test. System.Exception: Exception message."));

            File.Delete(path);
        }

        [TestMethod]
        public void TraceShouldLog()
        {
            // Arrange.
            var path = "trace.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Trace, textWriter);

                // Act.
                logger.Trace("test");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Trace] test."));

            File.Delete(path);
        }

        [TestMethod]
        public void TraceExceptionWithDebugLevelShouldNotLog()
        {
            // Arrange.
            var path = "trace-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Debug, textWriter);

                // Act.
                logger.Trace("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(string.IsNullOrEmpty(content));

            File.Delete(path);
        }

        [TestMethod]
        public void TraceWithDebugLevelShouldNotLog()
        {
            // Arrange.
            var path = "trace.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Debug, textWriter);

                // Act.
                logger.Trace("test");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(string.IsNullOrEmpty(content));

            File.Delete(path);
        }
        #endregion

        #region Debug
        [TestMethod]
        public void DebugExceptionShouldLog()
        {
            // Arrange.
            var path = "debug-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Debug, textWriter);

                // Act.
                logger.Debug("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Debug] test. System.Exception: Exception message."));

            File.Delete(path);
        }

        [TestMethod]
        public void DebugExceptionWithInfoLevelShouldNotLog()
        {
            // Arrange.
            var path = "debug-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Info, textWriter);

                // Act.
                logger.Debug("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(string.IsNullOrEmpty(content));

            File.Delete(path);
        }

        [TestMethod]
        public void DebugShouldLog()
        {
            // Arrange.
            var path = "debug.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Debug, textWriter);

                // Act.
                logger.Debug("test message");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Debug] test message."));

            File.Delete(path);
        }

        [TestMethod]
        public void DebugWithInfoLevelShouldNotLog()
        {
            // Arrange.
            var path = "debug.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Info, textWriter);

                // Act.
                logger.Debug("test message");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(string.IsNullOrEmpty(content));

            File.Delete(path);
        }
        #endregion

        #region Info
        [TestMethod]
        public void InfoExceptionShouldLog()
        {
            // Arrange.
            var path = "info-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Debug, textWriter);

                // Act.
                logger.Info("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Info] test. System.Exception: Exception message."));

            File.Delete(path);
        }

        [TestMethod]
        public void InfoShouldLog()
        {
            // Arrange.
            var path = "info.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Debug, textWriter);

                // Act.
                logger.Info("test");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Info] test."));

            File.Delete(path);
        }

        [TestMethod]
        public void InfoExceptionWithWarnLevelShouldNotLog()
        {
            // Arrange.
            var path = "info-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Warn, textWriter);

                // Act.
                logger.Info("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(string.IsNullOrEmpty(content));

            File.Delete(path);
        }

        [TestMethod]
        public void InfoWithWarnLevelShouldNotLog()
        {
            // Arrange.
            var path = "trace.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Warn, textWriter);

                // Act.
                logger.Info("test");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(string.IsNullOrEmpty(content));

            File.Delete(path);
        }
        #endregion

        #region Warn
        [TestMethod]
        public void WarnExceptionShouldLog()
        {
            // Arrange.
            var path = "Warn-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Debug, textWriter);

                // Act.
                logger.Warn("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Warn] test. System.Exception: Exception message."));

            File.Delete(path);
        }

        [TestMethod]
        public void WarnShouldLog()
        {
            // Arrange.
            var path = "Warn.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Debug, textWriter);

                // Act.
                logger.Warn("test");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Warn] test."));

            File.Delete(path);
        }

        [TestMethod]
        public void WarnExceptionWithErrorLevelShouldNotLog()
        {
            // Arrange.
            var path = "Warn-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Error, textWriter);

                // Act.
                logger.Warn("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(string.IsNullOrEmpty(content));

            File.Delete(path);
        }

        [TestMethod]
        public void WarnWithErrorLevelShouldNotLog()
        {
            // Arrange.
            var path = "Warn.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Error, textWriter);

                // Act.
                logger.Warn("test");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(string.IsNullOrEmpty(content));

            File.Delete(path);
        }
        #endregion

        #region Error
        [TestMethod]
        public void ErrorExceptionShouldLog()
        {
            // Arrange.
            var path = "Error-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Warn, textWriter);

                // Act.
                logger.Error("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Error] test. System.Exception: Exception message."));

            File.Delete(path);
        }

        [TestMethod]
        public void ErrorShouldLog()
        {
            // Arrange.
            var path = "Error.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Warn, textWriter);

                // Act.
                logger.Error("test");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Error] test."));

            File.Delete(path);
        }

        [TestMethod]
        public void ErrorExceptionWithErrorLevelShouldLog()
        {
            // Arrange.
            var path = "Error-e.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Error, textWriter);

                // Act.
                logger.Error("test", new System.Exception("Exception message."));
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Error] test. System.Exception: Exception message."));

            File.Delete(path);
        }

        [TestMethod]
        public void ErrorWithErrorLevelShouldLog()
        {
            // Arrange.
            var path = "Error.txt";

            using (var textWriter = File.CreateText(path))
            {
                var logger = new SplitLogging(Level.Error, textWriter);

                // Act.
                logger.Error("test");
            }

            // Assert.
            var content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("[Error] test."));

            File.Delete(path);
        }
        #endregion
    }
}
