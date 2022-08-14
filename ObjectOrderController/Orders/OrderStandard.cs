using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ObjectOrderControl {
    public class O_Yield<T> : BaseOrder<T> where T : MonoBehaviour {
        private readonly YieldInstruction yieldInstruction = null;
        public O_Yield(YieldInstruction yieldInstruction) => this.yieldInstruction = yieldInstruction;
        protected override IEnumerator IGetOrder(T _controller) {
            yield return this.yieldInstruction;
        }
        protected override bool IGetIsNotSync() => false;
    }
    public class O_Action<T> : BaseOrder<T> where T : MonoBehaviour {
        private readonly Action<T> action = null;
        public O_Action(Action<T> action) => this.action = action;
        public O_Action(Action action) => this.action = m => action();
        protected override IEnumerator IGetOrder(T _controller) {
            this.action(_controller);
            return null;
        }
        protected override bool IGetIsNotSync() => false;
    }
    public class O_Coroutine<T> : BaseOrder<T> where T : MonoBehaviour {
        private readonly Func<T, IEnumerator> enumerator = null;
        private readonly bool isNotSync;
        public O_Coroutine(Func<T, IEnumerator> enumerator, bool isNotSync = false) {
            this.enumerator = enumerator;
            this.isNotSync = isNotSync;
        }
        public O_Coroutine(Func<IEnumerator> enumerator, bool isNotSync = false) {
            this.enumerator = m => enumerator();
            this.isNotSync = isNotSync;
        }
        protected override IEnumerator IGetOrder(T _controller) => this.enumerator(_controller);
        protected override bool IGetIsNotSync() => this.isNotSync;
    }
    public class O_OrderChain<T> : BaseOrder<T> where T : MonoBehaviour {
        private readonly List<IOrder<T>> orders = new List<IOrder<T>>();
        private readonly bool isNotSync;
        public O_OrderChain(bool isNotSync, params IOrder<T>[] orders) {
            this.orders.AddRange(orders);
            this.isNotSync = isNotSync;
        }
        protected override IEnumerator IGetOrder(T _controller) {
            foreach (var o in this.orders) yield return o.IExecute(_controller);
        }
        protected override bool IGetIsNotSync() => this.isNotSync;
    }
    public class O_OrderLoop<T> : BaseOrder<T> where T : MonoBehaviour {
        enum LoopType { Condition, Count, Both }
        private readonly LoopType type;
        private readonly Func<bool> condition;
        private readonly int count;
        private readonly bool and;
        private readonly List<IOrder<T>> orders = new List<IOrder<T>>();
        private readonly bool isNotSync;
        public O_OrderLoop(bool isNotSync, Func<bool> condition, params IOrder<T>[] orders) : this(isNotSync, orders) {
            this.type = LoopType.Condition;
            this.condition = condition;
        }
        public O_OrderLoop(bool isNotSync, int count, params IOrder<T>[] orders) : this(isNotSync, orders) {
            this.type = LoopType.Count;
            this.count = count;
        }
        public O_OrderLoop(bool isNotSync, Func<bool> condition, int count, bool and, params IOrder<T>[] orders) : this(isNotSync, orders) {
            this.type = LoopType.Both;
            this.condition = condition;
            this.count = count;
            this.and = and;
        }
        protected O_OrderLoop(bool isNotSync, params IOrder<T>[] orders) {
            this.orders.AddRange(orders);
            this.isNotSync = isNotSync;
        }
        protected override IEnumerator IGetOrder(T _controller) {
            int _count = count;
            while (LoopCondition(_count)) {
                foreach (var o in this.orders) yield return o.IExecute(_controller);
                _count = Mathf.Max(0, _count - 1);
            }
        }
        protected override bool IGetIsNotSync() => this.isNotSync;
        private bool LoopCondition(int count) {
            bool t1 = type == LoopType.Condition && condition();
            bool t2 = type == LoopType.Count && count > 0;
            bool t3 = type == LoopType.Both && (and ? condition() && count > 0 : condition() || count > 0);
            return t1 || t2 || t3;
        }
    }
}
