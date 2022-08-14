using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ObjectOrderControl {
    /// <summary>
    /// MonoBehaviour型のObjectOrderController
    /// </summary>
    public class ObjectOrderController : ObjectOrderController<MonoBehaviour> {}
    
    public abstract class ObjectOrderController<T> : MonoBehaviour where T : MonoBehaviour {
        protected virtual T Self => this as T;
        private List<OrderNode> orderNodes = new List<OrderNode>();
        private Action finishCallBack = () => {};
        public int NodeIndex { get; private set; } = 0;
        public bool Running => this.mainCoroutine != null;
        private Coroutine mainCoroutine = null;
        private const int ErrorIndex = -1;
        private OrderNode GetNode() {
            if (this.orderNodes.Count - 1 >= this.NodeIndex)
                return this.orderNodes[this.NodeIndex];
            else {
                //Debug.LogWarning($"ObjectOrderController/GetNode() is null. nodeSize:{this.orderNodes.Count}, index:{this.NodeIndex}");
                return null;
            }
        }
        public ObjectOrderController<T> CreateNode(int index, params (bool, int ,Func<T, bool>)[] nodeTransitions) {
            this.NodeIndex = index;
            ResizeNode();
            OrderNode _node = GetNode();
            _node.Clear();
            foreach ((bool _duringExec, int _nextIndex, Func<T, bool> _condition) in nodeTransitions) {
                _node.Add(new NodeTransition(_nextIndex, _condition), _duringExec);
            }
            return this;
            void ResizeNode() {
                int overflow = Mathf.Max(this.NodeIndex - (this.orderNodes.Count - 1), 0);
                for (int i = 0; i < overflow; i++) this.orderNodes.Add(new OrderNode());
            }
        }
        public ObjectOrderController<T> Set(params IOrder<T>[] orders) {
            OrderNode _node = GetNode();
            if (_node == null) {
                Debug.LogError($"ObjectOrderController/node is null. nodeSize:{this.orderNodes.Count} nodeIndex:{this.NodeIndex}", this);
                return this;
            }
            if (orders == null) {
                Debug.Log($"order is null. nodeSize:{this.orderNodes.Count} nodeIndex:{this.NodeIndex}");
                return this;
            }
            foreach (var o in orders) {
                o.Init(this);
                _node.Add(o);
            }
            return this;
        }
        public T SSet(params IOrder<T>[] orders) {
            Set(orders);
            return Self;
        }
        public ObjectOrderController<T> StopAll() {
            if (this.mainCoroutine != null) StopCoroutine(this.mainCoroutine);
            this.mainCoroutine = null;
            GetNode().StopAllCoroutines(this);
            return this;
        }
        public void Execute(int index = 0, Action finishCallBack = null) {
            StopAll();
            this.NodeIndex = index;
            if (finishCallBack != null) this.finishCallBack = finishCallBack;
            this.mainCoroutine = StartCoroutine(MainCoroutine());
            //return this.mainCoroutine;
        }

        private IEnumerator MainCoroutine() {
            while (true) {
                if (this.NodeIndex < 0) {
                    Debug.LogError("ObjectOrderController/nodeIndex < 0.", this);
                    break;
                }
                OrderNode _node = GetNode();
                if (_node == null) {
                    Debug.LogError($"ObjectOrderController/Node is null.\nindex:{NodeIndex}", this);
                    break;
                }
                yield return _node.Execute(Self, this);
                _node.StopAllCoroutines(this);
                int? nextIndex = _node.GetNextIndexWhenFin(Self);
                if (nextIndex == null) {
                    break;
                }
                this.NodeIndex = nextIndex ?? ErrorIndex;
            }
            this.finishCallBack();
            yield break;
        }

        void Update() {
            //Debug.Log($"count:{this.orderNodes.Count}, index:{this.NodeIndex}");
            OrderNode _node = GetNode();
            if (_node != null) {
                int? nextIndex = _node.GetNextIndexDuringExec(Self);
                if (nextIndex != null) {
                    Execute(nextIndex ?? ErrorIndex);
                }
            }
        }
        [System.Serializable]
        private class OrderNode {
            private List<IOrder<T>> orders = new List<IOrder<T>>();
            private List<NodeTransition> nodeTransitionsWhenFin = new List<NodeTransition>();
            private List<NodeTransition> nodeTransitionsDuringExec = new List<NodeTransition>();
            private Coroutine coroutine = null;
            public OrderNode(IOrder<T>[] orders, NodeTransition[] nodeTransitionsWhenFin = null, NodeTransition[] nodeTransitionsDuringExec = null) {
                this.orders.AddRange(orders);
                this.nodeTransitionsWhenFin.AddRange(nodeTransitionsWhenFin);
                this.nodeTransitionsDuringExec.AddRange(nodeTransitionsDuringExec);
            }
            public OrderNode() {
                this.orders.Clear();
                this.nodeTransitionsWhenFin.Clear();
                this.nodeTransitionsDuringExec.Clear();
            }
            public void Add(NodeTransition nodeTransition, bool duringExec) {
                if (duringExec) this.nodeTransitionsDuringExec.Add(nodeTransition);
                else this.nodeTransitionsWhenFin.Add(nodeTransition);
            }
            public void Add(params IOrder<T>[] orders) {
                this.orders.AddRange(orders);
            }
            public void Clear() {
                this.orders.Clear();
                this.nodeTransitionsWhenFin.Clear();
                this.nodeTransitionsDuringExec.Clear();
            }
            public Coroutine Execute(T _self, ObjectOrderController<T> _controller) {
                this.coroutine = _controller.StartCoroutine(GetEnumerator(_self));
                return coroutine;
            }
            private IEnumerator GetEnumerator(T _self) {
                foreach (var o in this.orders) {
                    Coroutine coroutine = o.IExecute(_self);
                    if (coroutine != null) yield return coroutine;
                }
            }
            public void StopAllCoroutines(ObjectOrderController<T> _controller) {
                foreach (var o in this.orders) o.IStopAllCoroutines();
                if (this.coroutine != null) {
                    _controller.StopCoroutine(this.coroutine);
                    this.coroutine = null;
                }
            }
            public int? GetNextIndexWhenFin(T _self) => OrderNode.GetNextIndex(_self, this.nodeTransitionsWhenFin);
            public int? GetNextIndexDuringExec(T _self) => OrderNode.GetNextIndex(_self, this.nodeTransitionsDuringExec);
            private static int? GetNextIndex(T _self, IEnumerable<NodeTransition> _nodeTransitions) {
                foreach (var transitions in _nodeTransitions) {
                    int? nextIndex = transitions.GetTransitionNodeIndex(_self);
                    if (nextIndex != null) return nextIndex;
                }
                return null;
            }
        }
        private struct NodeTransition {
            private Func<T, bool> condition;
            private int nextNodeIndex;
            public NodeTransition(int nextNodeIndex, Func<T, bool> condition) {
                this.condition = condition;
                this.nextNodeIndex = nextNodeIndex;
            }
            /// <summary>
            /// 遷移先のノードを取得
            /// </summary>
            /// <returns>ノードインデックス</returns>
            public int? GetTransitionNodeIndex(T _self) {
                if (condition(_self)) return this.nextNodeIndex;
                else return null;
            }
        }

        #region orders
        public ObjectOrderController<T> SetYield(YieldInstruction yieldInstruction) => this.Set(new O_Yield<T>(yieldInstruction));
        public T SSetYield(YieldInstruction yieldInstruction) => this.SSet(new O_Yield<T>(yieldInstruction));

        public ObjectOrderController<T> SetAction(Action<T> action) => this.Set(new O_Action<T>(action));
        public ObjectOrderController<T> SetAction(Action action) => this.Set(new O_Action<T>(action));
        public T SSetAction(Action<T> action) => this.SSet(new O_Action<T>(action));
        public T SSetAction(Action action) => this.SSet(new O_Action<T>(action));

        public ObjectOrderController<T> SetCoroutine(Func<T, IEnumerator> enumerator, bool isNotSync = false) => this.Set(new O_Coroutine<T>(enumerator, isNotSync));
        public ObjectOrderController<T> SetCoroutine(Func<IEnumerator> enumerator, bool isNotSync = false) => this.Set(new O_Coroutine<T>(enumerator, isNotSync));
        public T SSetCoroutine(Func<T, IEnumerator> enumerator, bool isNotSync = false) => this.SSet(new O_Coroutine<T>(enumerator, isNotSync));
        public T SSetCoroutine(Func<IEnumerator> enumerator, bool isNotSync = false) => this.SSet(new O_Coroutine<T>(enumerator, isNotSync));
        #endregion
    }

    public static class ObjectOrderControlComponenter {
        public static ObjectOrderController<T> ComponentOrderController<T>(this T self) where T : MonoBehaviour {
            var component = self.gameObject.GetComponent<ObjectOrderController<T>>();
            if (component != null) return component;
            else return self.gameObject.AddComponent<ObjectOrderController<T>>();
        }
    }
}