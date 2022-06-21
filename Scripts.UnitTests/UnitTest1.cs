using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Skyline.DataMiner.Automation;

namespace Scripts.UnitTests
{
    [TestClass]
    public class SignalProblemDetectedTests
    {
        private Script _script;

        [TestInitialize]
        public void Setup()
        {
            _script = new Script();
        }

        [TestMethod]
        public void Test_TriggerMainSite1()
        {
            var mainElementMock = new Mock<ScriptDummy>();
            mainElementMock.SetupGet(s => s.ElementName).Returns("PARIS_OFFICE_SITE1_MAIN");

            var backupElement = new Mock<Element>();
            backupElement.Setup(s => s.GetParameter("LNB1 Supply")).Returns<string>((name) => "1");

            var switchMock = new Mock<Element>();

            var engineMock = new Mock<IEngine>();
            engineMock.Setup(e => e.GetDummy("IRD")).Returns<string>((name) => mainElementMock.Object);
            engineMock.Setup(e => e.FindElement("IRD_EC_SITE1_BACKUP")).Returns<string>((name) => backupElement.Object);
            engineMock.Setup(e => e.FindElement("Site Switch")).Returns<string>((name) => switchMock.Object);

            _script.Run(engineMock.Object);

            switchMock.Verify(s => s.SetParameter("Switch Site 1", 2), Times.Once);
        }
    }
}