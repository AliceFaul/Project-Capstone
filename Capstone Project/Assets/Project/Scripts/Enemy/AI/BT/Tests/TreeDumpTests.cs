using System.Text;
using NUnit.Framework;

namespace Mhieu.AI.BT.Tests
{
    public class TreeDumpTests
    {
        private BTContext _ctx;

        [SetUp]
        public void SetUp() => _ctx = new BTContext(null, new Blackboard());

        // Node giả trả về kết quả cố định.
        private sealed class StubAction : ActionNode
        {
            public NodeStatus Result;

            public StubAction(NodeStatus result, string name = null)
            {
                Result = result;
                Name = name;
            }

            protected override NodeStatus OnTick(BTContext ctx) => Result;
        }

        private static string RunningPathOf(Node root)
        {
            var sb = new StringBuilder();
            TreeDump.AppendRunningPath(root, sb);
            return sb.ToString();
        }

        // Root(Selector) > [Skip (Failure), Attack(Sequence) > [Check (Success), Act (Running)]]
        private Node BuildSampleTree()
        {
            return new Selector(
                new StubAction(NodeStatus.Failure, "Skip"),
                new Sequence(
                    new StubAction(NodeStatus.Success, "Check"),
                    new StubAction(NodeStatus.Running, "Act")) { Name = "Attack" }) { Name = "Root" };
        }

        [Test]
        public void RunningPath_NothingRunning_ReturnsNone()
        {
            Node root = BuildSampleTree();
            Assert.AreEqual("(none)", RunningPathOf(root));
        }

        [Test]
        public void RunningPath_FollowsRunningChildren()
        {
            Node root = BuildSampleTree();
            root.Tick(_ctx);

            Assert.AreEqual("Root > Attack > Act", RunningPathOf(root));
        }

        [Test]
        public void RunningPath_GoesThroughDecorator()
        {
            var guard = new Guard(new FuncCondition(_ => true), new StubAction(NodeStatus.Running, "Work")) { Name = "Gate" };
            guard.Tick(_ctx);

            Assert.AreEqual("Gate > Work", RunningPathOf(guard));
        }

        [Test]
        public void RunningPath_UnnamedNode_UsesTypeName()
        {
            var node = new StubAction(NodeStatus.Running);
            node.Tick(_ctx);

            Assert.AreEqual("StubAction", RunningPathOf(node));
        }

        [Test]
        public void AppendTree_MarksRunningAndIndentsChildren()
        {
            Node root = BuildSampleTree();
            root.Tick(_ctx);

            var sb = new StringBuilder();
            TreeDump.AppendTree(root, sb);
            string text = sb.ToString();

            StringAssert.Contains("* Root\n", text);
            StringAssert.Contains("  - Skip\n", text);
            StringAssert.Contains("  * Attack\n", text);
            StringAssert.Contains("    - Check\n", text);
            StringAssert.Contains("    * Act\n", text);
        }

        [Test]
        public void AppendTree_WithMarkers_WrapsEachLine()
        {
            Node root = BuildSampleTree();
            root.Tick(_ctx);

            var sb = new StringBuilder();
            TreeDump.AppendTree(root, sb, "<r>", "</r>", "<i>", "</i>");
            string text = sb.ToString();

            StringAssert.Contains("<r>* Root</r>\n", text);
            StringAssert.Contains("  <i>- Skip</i>\n", text);
        }
    }
}
