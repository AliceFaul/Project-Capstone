using Mhieu.AI.BT;
using NUnit.Framework;

namespace Mhieu.AI.BT.Tests
{
    public class BehaviorTreeTests
    {
        private BTContext _ctx;

        [SetUp]
        public void SetUp() => _ctx = new BTContext(null, new Blackboard());

        // Node giả: trả về Result cố định và đếm số lần gọi từng móc vòng đời.
        private sealed class Spy : ActionNode
        {
            public NodeStatus Result;
            public int Enters, Ticks, Exits, Aborts;

            public Spy(NodeStatus result) { Result = result; }

            protected override void OnEnter(BTContext ctx) => Enters++;
            protected override NodeStatus OnTick(BTContext ctx) { Ticks++; return Result; }
            protected override void OnAbort(BTContext ctx) => Aborts++;
            protected override void OnExit(BTContext ctx, NodeStatus status) => Exits++;
        }

        // ---------- Sequence ----------

        [Test]
        public void Sequence_AllSuccess_ReturnsSuccess()
        {
            var seq = new Sequence(new Spy(NodeStatus.Success), new Spy(NodeStatus.Success));
            Assert.AreEqual(NodeStatus.Success, seq.Tick(_ctx));
        }

        [Test]
        public void Sequence_StopsAtFirstFailure()
        {
            var a = new Spy(NodeStatus.Success);
            var b = new Spy(NodeStatus.Failure);
            var c = new Spy(NodeStatus.Success);
            var seq = new Sequence(a, b, c);

            Assert.AreEqual(NodeStatus.Failure, seq.Tick(_ctx));
            Assert.AreEqual(0, c.Ticks);
        }

        [Test]
        public void Sequence_ResumesRunningChild_WithoutReTickingEarlierChildren()
        {
            var a = new Spy(NodeStatus.Success);
            var b = new Spy(NodeStatus.Running);
            var seq = new Sequence(a, b);

            seq.Tick(_ctx);
            seq.Tick(_ctx);
            seq.Tick(_ctx);

            Assert.AreEqual(1, a.Ticks, "Con đã Success không được chạy lại");
            Assert.AreEqual(3, b.Ticks);
            Assert.AreEqual(1, b.Enters, "OnEnter chỉ chạy một lần khi bắt đầu");

            b.Result = NodeStatus.Success;
            Assert.AreEqual(NodeStatus.Success, seq.Tick(_ctx));
            Assert.AreEqual(1, b.Exits);
        }

        [Test]
        public void Sequence_Abort_AbortsRunningChild_AndResetsIndex()
        {
            var a = new Spy(NodeStatus.Success);
            var b = new Spy(NodeStatus.Running);
            var seq = new Sequence(a, b);

            seq.Tick(_ctx);
            seq.Abort(_ctx);

            Assert.AreEqual(1, b.Aborts);
            Assert.IsFalse(b.IsRunning);

            seq.Tick(_ctx);
            Assert.AreEqual(2, a.Ticks, "Sau Abort phải chạy lại từ đầu");
        }

        // ---------- Selector ----------

        [Test]
        public void Selector_ReturnsFirstNonFailure()
        {
            var a = new Spy(NodeStatus.Failure);
            var b = new Spy(NodeStatus.Success);
            var c = new Spy(NodeStatus.Success);
            var sel = new Selector(a, b, c);

            Assert.AreEqual(NodeStatus.Success, sel.Tick(_ctx));
            Assert.AreEqual(0, c.Ticks);
        }

        [Test]
        public void Selector_AllFail_ReturnsFailure()
        {
            var sel = new Selector(new Spy(NodeStatus.Failure), new Spy(NodeStatus.Failure));
            Assert.AreEqual(NodeStatus.Failure, sel.Tick(_ctx));
        }

        [Test]
        public void Selector_HigherPriorityTakesOver_AbortsLowerRunningChild()
        {
            var high = new Spy(NodeStatus.Failure);
            var low = new Spy(NodeStatus.Running);
            var sel = new Selector(high, low);

            Assert.AreEqual(NodeStatus.Running, sel.Tick(_ctx));
            Assert.IsTrue(low.IsRunning);

            high.Result = NodeStatus.Running;
            Assert.AreEqual(NodeStatus.Running, sel.Tick(_ctx));

            Assert.AreEqual(1, low.Aborts);
            Assert.IsFalse(low.IsRunning);
            Assert.IsTrue(high.IsRunning);
        }

        [Test]
        public void Selector_Abort_AbortsRunningChild()
        {
            var low = new Spy(NodeStatus.Running);
            var sel = new Selector(new Spy(NodeStatus.Failure), low);

            sel.Tick(_ctx);
            sel.Abort(_ctx);

            Assert.AreEqual(1, low.Aborts);
        }

        // ---------- Decorators ----------

        [Test]
        public void Guard_ConditionBecomesFalse_AbortsRunningChild()
        {
            bool open = true;
            var child = new Spy(NodeStatus.Running);
            var guard = new Guard(new FuncCondition(_ => open), child);

            Assert.AreEqual(NodeStatus.Running, guard.Tick(_ctx));

            open = false;
            Assert.AreEqual(NodeStatus.Failure, guard.Tick(_ctx));
            Assert.AreEqual(1, child.Aborts);
            Assert.IsFalse(child.IsRunning);
        }

        [Test]
        public void Inverter_FlipsSuccessAndFailure_KeepsRunning()
        {
            var child = new Spy(NodeStatus.Success);
            var inv = new Inverter(child);

            Assert.AreEqual(NodeStatus.Failure, inv.Tick(_ctx));
            child.Result = NodeStatus.Failure;
            Assert.AreEqual(NodeStatus.Success, inv.Tick(_ctx));
            child.Result = NodeStatus.Running;
            Assert.AreEqual(NodeStatus.Running, inv.Tick(_ctx));
        }

        [Test]
        public void Cooldown_BlocksUntilReady()
        {
            var child = new Spy(NodeStatus.Success);
            var cd = new Cooldown(2f, child);

            _ctx.Time = 0f;
            Assert.AreEqual(NodeStatus.Success, cd.Tick(_ctx));

            _ctx.Time = 1f;
            Assert.AreEqual(NodeStatus.Failure, cd.Tick(_ctx));
            Assert.AreEqual(1, child.Ticks);

            _ctx.Time = 2.5f;
            Assert.AreEqual(NodeStatus.Success, cd.Tick(_ctx));
            Assert.AreEqual(2, child.Ticks);
        }

        [Test]
        public void Cooldown_InterruptedChild_StillStartsCooldown()
        {
            var child = new Spy(NodeStatus.Running);
            var cd = new Cooldown(2f, child);

            _ctx.Time = 0f;
            Assert.AreEqual(NodeStatus.Running, cd.Tick(_ctx));
            cd.Abort(_ctx);

            _ctx.Time = 1f;
            Assert.AreEqual(NodeStatus.Failure, cd.Tick(_ctx));
        }

        // ---------- Leaves & tree ----------

        [Test]
        public void WaitNode_RunsUntilDurationPasses()
        {
            var wait = new WaitNode(1f);

            _ctx.Time = 0f;
            Assert.AreEqual(NodeStatus.Running, wait.Tick(_ctx));
            _ctx.Time = 0.5f;
            Assert.AreEqual(NodeStatus.Running, wait.Tick(_ctx));
            _ctx.Time = 1f;
            Assert.AreEqual(NodeStatus.Success, wait.Tick(_ctx));
        }

        [Test]
        public void Abort_OnNodeThatIsNotRunning_IsNoOp()
        {
            var spy = new Spy(NodeStatus.Success);
            spy.Abort(_ctx);
            Assert.AreEqual(0, spy.Aborts);
            Assert.AreEqual(0, spy.Exits);
        }

        [Test]
        public void BehaviorTree_Tick_SetsContextTime_AndAbortReachesRunningLeaf()
        {
            var leaf = new Spy(NodeStatus.Running);
            var tree = new BehaviorTree(new Selector(new Spy(NodeStatus.Failure), leaf), _ctx);

            tree.Tick(3f, 0.1f);
            Assert.AreEqual(3f, _ctx.Time);
            Assert.AreEqual(0.1f, _ctx.DeltaTime);

            tree.Abort();
            Assert.AreEqual(1, leaf.Aborts);
        }
    }
}
