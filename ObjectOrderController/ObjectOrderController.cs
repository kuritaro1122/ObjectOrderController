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
        public virtual T Self => this as T;
        private List<OrderNode> orderNodes = new List<OrderNode>();
        private Action<T> finishCallBack = m => {};
        public int NodeIndex { get; private set; } = 0;
        public int CurrentNodeLoopedCount => GetNode().LoopedCount;
        public bool Running => this.mainCoroutine != null;
        private Coroutine mainCoroutine = null;
        private List<IOrder<T>> extraOrder = new List<IOrder<T>>();
        private const int ErrorIndex = -1;
        private OrderNode GetNode() {
            if (this.orderNodes.Count - 1 >= this.NodeIndex)
                return this.orderNodes[this.NodeIndex];
            else {
                //Debug.LogWarning($"ObjectOrderController/GetNode() is null. nodeSize:{this.orderNodes.Count}, index:{this.NodeIndex}");
                return null;
            }
        }
        /// <summary>
        /// ノードを作成する.
        /// </summary>
        /// <param name="index">このノードのIndex</param>
        /// <param name="loop">何も条件を満たさなかった場合, 再び実行するか</param>
        /// <param name="nodeTransitions">遷移条件と遷移先Index</param>
        /// <returns>self</returns>
        public ObjectOrderController<T> CreateNode(int index, bool loop = false, params (bool, int, Func<T, bool>)[] nodeTransitions) {
            CreateNode(index, nodeTransitions);
            if (loop) GetNode().Add(new NodeTransition(index, condition: t => true), duringExec: false);
            return this;
        }
        /// <summary>
        /// ノードを作成する.
        /// </summary>
        /// <param name="index">このノードのIndex</param>
        /// <param name="nextIndex">デフォルトの遷移先ノードIndex</param>
        /// <param name="nodeTransitions">遷移条件と遷移先Index</param>
        /// <returns></returns>
        public ObjectOrderController<T> CreateNode(int index, int nextIndex, params (bool, int, Func<T, bool>)[] nodeTransitions) {
            CreateNode(index, nodeTransitions);
            GetNode().Add(new NodeTransition(nextIndex, condition: t => true), duringExec: false);
            return this;
        }
        /// <summary>
        /// ノードを作成する.
        /// </summary>
        /// <param name="index">このノードのIndex</param>
        /// <param name="nodeTransitions">遷移条件と遷移先Index</param>
        /// <returns></returns>
        public ObjectOrderController<T> CreateNode(int index, params (bool, int ,Func<T, bool>)[] nodeTransitions) {
            this.NodeIndex = index;
            ResizeNode();
            OrderNode _node = GetNode();
            _node.Clear();
            foreach ((bool _duringExec, int _nextIndex, Func<T, bool> _condition) in nodeTransitions) {
                _node.Add(new NodeTransition(_nextIndex, _condition), _duringExec);
            }
            return this;
        }
        public ObjectOrderController<T> Set(params IOrder<T>[] orders) {
            ResizeNode();
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
        //public T SSet(params IOrder<T>[] orders) {
        //    Set(orders);
        //    return Self;
        //}
        /// <summary>
        /// このノードに遷移した時に実行される処理を追加
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ObjectOrderController<T> AddNodeStartEvent(System.Action<T> action) {
            OrderNode _node = GetNode();
            if (_node == null) {
                Debug.LogError($"ObjectOrderController/node is null. nodeSize:{this.orderNodes.Count} nodeIndex:{this.NodeIndex}", this);
                return this;
            }
            _node.AddNodeStartEvent(action);
            return this;
        }
        /// <summary>
        /// 他のノードに遷移した時に実行される処理を追加
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ObjectOrderController<T> AddNodeFinishEvent(System.Action<T> action) {
            OrderNode _node = GetNode();
            if (_node == null) {
                Debug.LogError($"ObjectOrderController/node is null. nodeSize:{this.orderNodes.Count} nodeIndex:{this.NodeIndex}", this);
                return this;
            }
            _node.AddNodeFinishEvent(action);
            return this;
        }
        public virtual ObjectOrderController<T> StopAll() {
            if (this.mainCoroutine != null) StopCoroutine(this.mainCoroutine);
            this.mainCoroutine = null;
            foreach (var ex in this.extraOrder) {
                if (ex != null) ex.IStopAllCoroutines();
            }
            this.extraOrder.Clear();
            GetNode()?.Finish(Self, this);
            GetNode()?.ResetLoopedCount();
            return this;
        }
        public virtual void Execute(int index = 0, Action<T> finishCallBack = null) {
            StopAll();
            this.NodeIndex = index;
            if (finishCallBack != null) this.finishCallBack = finishCallBack;
            this.mainCoroutine = StartCoroutine(MainCoroutine());
        }

        public void Execute(int index, Action finishCallBack) => Execute(index, m => finishCallBack());
        public void Execute(Action finishCallBack) => Execute(0, m => finishCallBack());
        public void Execute(Action<T> finishCallBack) => Execute(0, finishCallBack);

        /// <summary>
        /// 即席でオーダーを追加する ノードが切り替わったら中断される.
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public Coroutine ExtraOrder(IOrder<T> order) {
            this.extraOrder.Add(order);
            order.Init(this);
            Coroutine coroutine = order.IExecute(this.Self);
            if (coroutine != null) return coroutine;
            else return null;
        }
        /// <summary>
        /// コルーチンを実行する. ノードが切り替わったら中断される.
        /// StartCoroutineの代わりに使用.
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        public Coroutine ExtraOrder(System.Func<IEnumerator<T>> enumerator) {
            return ExtraOrder(new O_Coroutine<T>(enumerator));
        }
        /// <summary>
        /// コルーチンを実行する. ノードが切り替わったら中断される.
        /// StartCoroutineの代わりに使用.
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        public Coroutine ExtraOrder(System.Func<IEnumerator> enumerator) {
            return ExtraOrder(new O_Coroutine<T>(enumerator));
        }

        void ResizeNode() {
            int overflow = Mathf.Max(this.NodeIndex - (this.orderNodes.Count - 1), 0);
            for (int i = 0; i < overflow; i++) this.orderNodes.Add(new OrderNode());
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
                _node.Finish(Self, this);
                int? nextIndex = _node.GetNextIndexWhenFin(Self);
                if (nextIndex == null) {
                    break;
                }
                if (this.NodeIndex != nextIndex) {
                    _node.ResetLoopedCount();
                }
                this.NodeIndex = nextIndex ?? ErrorIndex;
            }
            this.finishCallBack(Self);
            yield break;
        }

        //void Start() {
        //    if (this.orderNodes.Count < 1) {
        //        this.orderNodes.Add(new OrderNode());
        //    } else if (this.orderNodes[0] == null) {
        //        this.orderNodes[0] = new OrderNode();
        //    }
        //}
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
        //[System.Serializable]
        private class OrderNode {
            private List<IOrder<T>> orders = new List<IOrder<T>>();
            private List<NodeTransition> nodeTransitionsWhenFin = new List<NodeTransition>();
            private List<NodeTransition> nodeTransitionsDuringExec = new List<NodeTransition>();
            private Coroutine coroutine = null;
            private event Action<T> OnStartNode = t => { };
            private event Action<T> OnFinishNode = T => { };
            public int LoopedCount { get; private set; } = 0;
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
            public void AddNodeStartEvent(Action<T> onStartNode) {
                this.OnStartNode += onStartNode;
            }
            public void AddNodeFinishEvent(Action<T> onFinishNode) {
                this.OnFinishNode += onFinishNode;
            }
            public void Finish(T self, ObjectOrderController<T> controller) {
                OnFinishNode(self);
                StopAllCoroutines(controller);
            }
            public void ResetLoopedCount() => this.LoopedCount = 0;
            public void Clear() {
                this.orders.Clear();
                this.nodeTransitionsWhenFin.Clear();
                this.nodeTransitionsDuringExec.Clear();
                this.OnStartNode = t => { };
                this.OnFinishNode = t => { };
                this.LoopedCount = 0;
            }
            public Coroutine Execute(T self, ObjectOrderController<T> controller) {
                this.coroutine = controller.StartCoroutine(GetEnumerator(self));
                OnStartNode(self);
                return coroutine;
            }
            private IEnumerator GetEnumerator(T _self) {
                foreach (var o in this.orders) {
                    Coroutine coroutine = o.IExecute(_self);
                    if (coroutine != null) yield return coroutine;
                }
                this.LoopedCount++;
            }
            private void StopAllCoroutines(ObjectOrderController<T> controller) {
                foreach (var o in this.orders) o.IStopAllCoroutines();
                if (this.coroutine != null) {
                    controller.StopCoroutine(this.coroutine);
                    this.coroutine = null;
                }
            }
            /// <summary>
            /// 現在遷移可能なノードを検索する
            /// </summary>
            /// <param name="_self"></param>
            /// <returns></returns>
            public int? GetNextIndexWhenFin(T _self) => OrderNode.GetNextIndex(_self, this.nodeTransitionsWhenFin);
            /// <summary>
            /// 現在遷移可能なノードを検索する（途中遷移）
            /// </summary>
            /// <param name="_self"></param>
            /// <returns></returns>
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
        public ObjectOrderController<T> SetYield(CustomYieldInstruction yieldInstruction) => this.Set(new O_CustomYield<T>(yieldInstruction));
        public ObjectOrderController<T> SetYield(Func<T, YieldInstruction> yieldInstruction) => this.Set(new O_Yield<T>(yieldInstruction(this.Self)));
        public ObjectOrderController<T> SetYield(Func<T, CustomYieldInstruction> yieldInstruction) => this.Set(new O_CustomYield<T>(yieldInstruction(this.Self)));

        public ObjectOrderController<T> SetAction(Action<T> action) => this.Set(new O_Action<T>(action));
        public ObjectOrderController<T> SetAction(Action action) => this.Set(new O_Action<T>(action));

        public ObjectOrderController<T> SetCoroutine(Func<T, IEnumerator> enumerator, bool isNotSync = false) => this.Set(new O_Coroutine<T>(enumerator, isNotSync));
        public ObjectOrderController<T> SetCoroutine(Func<IEnumerator> enumerator, bool isNotSync = false) => this.Set(new O_Coroutine<T>(enumerator, isNotSync));
        #endregion
    }

    public static class ObjectOrderControlComponenter {
        public static T ComponentOrderController<T>(this GameObject self) where T : ObjectOrderController<T> {
            var component = self.gameObject.GetComponent<T>();
            if (component != null) return component;
            else return self.gameObject.AddComponent<T>();
        }
    }
}