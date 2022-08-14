using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ObjectOrderControl {
    public interface IOrder<T> where T : MonoBehaviour {
        public Coroutine IExecute(T _controller);
        public void IStopAllCoroutines();
        public void Init(ObjectOrderController<T> _self);
    }

    public abstract class BaseOrder<T> : IOrder<T> where T : MonoBehaviour {
        private ObjectOrderController<T> controller;
        private readonly List<Coroutine> coroutines = new List<Coroutine>();
        /// <summary>
        /// 内部ではStartCoroutineの代わりにIStartCoroutineを使ってください.
        /// </summary>
        protected abstract IEnumerator IGetOrder(T _controller);
        protected abstract bool IGetIsNotSync();
        /// <summary>
        /// オーダーを実行する. 非同期の時はnull
        /// </summary>
        public Coroutine IExecute(T _self) {
            if (IGetIsNotSync()) {
                IStartCoroutine(IGetOrder(_self));
                return null;
            } else return IStartCoroutine(IGetOrder(_self));
        }
        /// <summary>
        /// コルーチンを実行して, リスト格納する.
        /// </summary>
        protected Coroutine IStartCoroutine(IEnumerator enumerator) {
            if (this.controller == null) {
                Debug.LogError("ObjectOrderController/IStartCoroutine()/controller is null.");
                return null;
            } else if (enumerator == null) return null;
            Coroutine coroutine = this.controller.StartCoroutine(enumerator);
            this.coroutines.Add(coroutine);
            return coroutine;
        }
        public void IStopAllCoroutines() {
            if (this.controller != null) {
                foreach (var c in this.coroutines) {
                    if (c != null) this.controller.StopCoroutine(c);
                }
            } else Debug.LogError("ObjectOrderController/IStopAllCoroutine()/controller is null.");
            this.coroutines.Clear();
        }
        public void Init(ObjectOrderController<T> _controller) {
            this.controller = _controller;
        }
    }
}