using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mhieu.AI.BT
{
    public enum NodeStatus { Success, Failure, Running }

    /// <summary>
    /// Dữ liệu dùng chung cho mọi node trong một cây: Blackboard, thời gian, và các "dịch vụ"
    /// (IMover, AbilityRunner...) do EnemyBrain đăng ký. Node lấy dịch vụ qua Get&lt;T&gt;()
    /// nên không cần closure/delegate và dễ thay bằng bản giả khi test.
    /// </summary>
    public sealed class BTContext
    {
        public GameObject Owner { get; }
        public Blackboard Blackboard { get; }

        /// <summary>Thời gian có scale của lần tick hiện tại (cùng đồng hồ với hit-stop).</summary>
        public float Time;
        public float DeltaTime;

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public BTContext(GameObject owner, Blackboard blackboard)
        {
            Owner = owner;
            Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
        }

        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

        public T Get<T>() where T : class
            => _services.TryGetValue(typeof(T), out var service) ? (T)service : null;
    }

    /// <summary>
    /// Vòng đời một node:
    ///   Tick -> (lần đầu) OnEnter -> OnTick -> nếu != Running thì OnExit.
    ///   Abort -> (chỉ khi đang Running) OnAbort -> OnExit(Failure).
    /// Mỗi instance node chỉ dùng trong MỘT cây (cây dựng riêng cho từng enemy).
    /// </summary>
    public abstract class Node
    {
        public string Name { get; set; }
        public bool IsRunning { get; private set; }
        public NodeStatus LastStatus { get; private set; } = NodeStatus.Failure;

        public NodeStatus Tick(BTContext ctx)
        {
            if (!IsRunning)
            {
                IsRunning = true;
                OnEnter(ctx);
            }

            NodeStatus status = OnTick(ctx);
            LastStatus = status;

            if (status != NodeStatus.Running)
            {
                IsRunning = false;
                OnExit(ctx, status);
            }

            return status;
        }

        /// <summary>Hủy node đang chạy (bị ngắt bởi nhánh ưu tiên cao hơn, Hit, Dead...). No-op nếu không chạy.</summary>
        public void Abort(BTContext ctx)
        {
            if (!IsRunning) return;

            IsRunning = false;
            LastStatus = NodeStatus.Failure;
            OnAbort(ctx);
            OnExit(ctx, NodeStatus.Failure);
        }

        protected virtual void OnEnter(BTContext ctx) { }
        protected abstract NodeStatus OnTick(BTContext ctx);

        /// <summary>Chỉ chạy khi bị hủy giữa chừng. Composite/Decorator hủy con ở đây.</summary>
        protected virtual void OnAbort(BTContext ctx) { }

        /// <summary>Chạy khi node kết thúc (Success/Failure) hoặc bị hủy (status = Failure).</summary>
        protected virtual void OnExit(BTContext ctx, NodeStatus status) { }
    }
}
